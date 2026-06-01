import { createSignal, createEffect, Show, Switch, Match } from 'solid-js';
import type { BookEntry } from '../types/BookData';
import type { Lang } from '../types/ui';
import T from '../i18n/translations.json';
import WRITING_PROMPTS from '../i18n/writing_prompts.json';
import './SubmissionPanel.css';

type Step = 'form' | 'confirm' | 'thanks';
type CoverState = 'empty' | 'loading' | 'loaded';

interface SubmissionPanelProps {
  open: boolean;
  lang: () => Lang;
  setLang: (l: Lang) => void;
  onClose: () => void;
  onSubmitted: (entry: BookEntry, coverUrl: string) => void;
  fetchCover: (title: string, author: string) => Promise<string | null>;
  submitBook:  (entry: BookEntry, coverUrl: string) => Promise<boolean>;
}

export function SubmissionPanel(props: SubmissionPanelProps) {
  const lang = props.lang;
  const setLang = props.setLang;
  const [step, setStep] = createSignal<Step>('form');
  const [title, setTitle] = createSignal('');
  const [author, setAuthor] = createSignal('');
  const [response, setResponse] = createSignal('');
  const [contributor, setContributor] = createSignal('');

  const [coverUrl, setCoverUrl] = createSignal('');
  const [coverState, setCoverState] = createSignal<CoverState>('empty');
  const [errorTitle, setErrorTitle] = createSignal('');
  const [errorAuthor, setErrorAuthor] = createSignal('');
  const [errorSubmit, setErrorSubmit] = createSignal('');
  const [submitting, setSubmitting] = createSignal(false);
  const [promptIndex, setPromptIndex] = createSignal(-1);

  const placeholderText = () => {
    const idx = promptIndex();
    if (idx >= 0) {
      return WRITING_PROMPTS[lang()][idx];
    }
    return t('phResponse');
  };

  let textareaRef!: HTMLTextAreaElement;

  createEffect(() => {
    const idx = promptIndex();
    if (textareaRef && idx >= 0) {
      textareaRef.style.animation = 'none';
      void textareaRef.offsetHeight;
      textareaRef.style.animation = 'promptFadeIn 0.35s ease both';
    }
  });

  function handleRandomPrompt() {
    const prompts = WRITING_PROMPTS[lang()];
    let newIdx;
    do {
      newIdx = Math.floor(Math.random() * prompts.length);
    } while (newIdx === promptIndex() && prompts.length > 1);
    setPromptIndex(newIdx);
  }

  type TranslationKey = keyof typeof T.en;
  const t = (k: TranslationKey): string => T[lang()][k];
  const dateStr = () => new Date().toLocaleDateString(T[lang()].dateLocale, { year: 'numeric', month: 'long', day: 'numeric' });

  function generateFallbackCover(title: string): string {
    let hash = 0;
    for (let i = 0; i < title.length; i++) {
      hash = title.charCodeAt(i) + ((hash << 5) - hash);
    }
    const hue = Math.abs(hash) % 360;
    return `data:image/svg+xml,${encodeURIComponent(
      `<svg xmlns="http://www.w3.org/2000/svg" width="200" height="300">
        <rect width="200" height="300" fill="hsl(${hue}, 25%, 35%)"/>
        <text x="100" y="150" fill="rgba(255,255,255,0.85)" font-family="Georgia,serif" font-size="15" text-anchor="middle" dominant-baseline="middle">${title.replace(/"/g, '&quot;')}</text>
       </svg>`
    )}`;
  }

  async function findCover() {
    if (!title().trim() || !author().trim()) {
      if (!title().trim()) setErrorTitle(t('errTitle'));
      if (!author().trim()) setErrorAuthor(t('errAuthor'));
      return;
    }
    setCoverState('loading');
    const url = await props.fetchCover(title(), author());
    setCoverUrl(url || generateFallbackCover(title()));
    setCoverState('loaded');
  }

  function validate(): boolean {
    let ok = true;
    if (!title().trim()) { setErrorTitle(t('errTitle')); ok = false; }
    if (!author().trim()) { setErrorAuthor(t('errAuthor')); ok = false; }
    return ok;
  }

  function handleSubmit() {
    if (!validate()) return;
    if (!response().trim()) {
      setStep('confirm');
      return;
    }
    doSubmit();
  }

  async function doSubmit() {
    setSubmitting(true);
    const finalCover = coverUrl();
    const entry: BookEntry = {
      title: title(), author: author(), submittedAt: new Date().toISOString(),
      coverImagePath: '', coverImageUrl: finalCover,
      responseText: response(), audioPath: '', audioUrl: '',
      isHandwritten: false, wantsNarrated: false,
      contributorName: contributor()
    };
    const ok = await props.submitBook(entry, finalCover);
    setSubmitting(false);
    if (!ok) { setErrorSubmit(t('errorSubmit')); setStep('form'); return; }
    setStep('thanks');
    props.onSubmitted(entry, finalCover);
  }

  function reset() {
    setTitle(''); setAuthor(''); setResponse(''); setCoverUrl('');
    setCoverState('empty');
    setErrorTitle(''); setErrorAuthor(''); setErrorSubmit('');
    setStep('form'); setContributor(''); setPromptIndex(-1);
  }

  function close() {
    reset();
    props.onClose();
  }

  return (
    <div class="panel-overlay" classList={{ visible: props.open }}>
      <div class="panel" classList={{ 'is-ko': lang() === 'ko' }}>

        <Switch>

          {/* ── Step 1: Form ── */}
          <Match when={step() === 'form'}>
            <div class="form-step">
              <div class="form-header">
                <div class="form-meta-row">
                  <span class="form-date">{dateStr()}</span>
                  <button class="lang-pill" onClick={() => {
                    const order: Lang[] = ['en', 'ko', 'es'];
                    const next = order[(order.indexOf(lang()) + 1) % order.length];
                    setLang(next);
                  }}>{t('pill')}</button>
                </div>
                <h2 class="form-heading">{t('heading')}</h2>
              </div>

              <div class="book-row">
                <div class="cover-col">
                  <div class="cover-frame">
                    <Show when={coverState() === 'empty'}>
                      <div class="cover-placeholder">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1"
                             stroke-linecap="round" stroke-linejoin="round">
                          <rect x="4" y="2" width="16" height="20" rx="1"/>
                          <line x1="4" y1="6" x2="20" y2="6"/>
                          <line x1="8" y1="2" x2="8" y2="22"/>
                        </svg>
                        <span class="cover-placeholder-label">{t('coverLabel')}</span>
                      </div>
                    </Show>
                    <Show when={coverState() === 'loading'}>
                      <div class="cover-loader">
                        <div class="cover-dot" />
                        <div class="cover-dot" />
                        <div class="cover-dot" />
                      </div>
                    </Show>
                    <Show when={coverState() === 'loaded'}>
                      <img src={coverUrl()} alt="book cover" />
                    </Show>
                  </div>
                  <button class="find-cover-btn" onClick={findCover} disabled={coverState() === 'loading'}>
                    {t('findCover')}
                  </button>
                </div>

                <div class="details-col">
                  <div class="field-group">
                    <label class="field-label" for="sp-title">{t('labelTitle')}</label>
                    <input
                      class="field-input" classList={{ 'has-error': !!errorTitle() }}
                      id="sp-title" type="text" autocomplete="off"
                      value={title()}
                      onInput={(e) => { setTitle(e.currentTarget.value); setErrorTitle(''); setCoverUrl(''); setCoverState('empty'); }}
                      placeholder={t('phTitle')}
                    />
                    <Show when={errorTitle()}>
                      <p class="field-err">{errorTitle()}</p>
                    </Show>
                  </div>
                  <div class="field-group">
                    <label class="field-label" for="sp-author">{t('labelAuthor')}</label>
                    <input
                      class="field-input" classList={{ 'has-error': !!errorAuthor() }}
                      id="sp-author" type="text" autocomplete="off"
                      value={author()}
                      onInput={(e) => { setAuthor(e.currentTarget.value); setErrorAuthor(''); setCoverUrl(''); setCoverState('empty'); }}
                      placeholder={t('phAuthor')}
                    />
                    <Show when={errorAuthor()}>
                      <p class="field-err">{errorAuthor()}</p>
                    </Show>
                  </div>
                </div>
              </div>

              {/* Contributor moved above response — "who I am" grouped with "what book" */}
              <div class="contributor-section">
                <label class="field-label" for="contributor-name">{t('labelContributor')}</label>
                <input
                  class="field-input"
                  id="contributor-name" type="text" autocomplete="off"
                  value={contributor()}
                  onInput={(e) => setContributor(e.currentTarget.value)}
                  placeholder={t('phContributor')}
                />
              </div>

              <div class="response-section">
                <div class="response-header">
                  <span class="response-label">{t('labelResponse')}</span>
                  <button
                    class="prompt-pill"
                    classList={{ 'is-active': promptIndex() >= 0 }}
                    onClick={handleRandomPrompt}
                  >
                    ⟳ {t('promptBtn')}
                  </button>
                </div>
                <textarea
                  ref={textareaRef!}
                  class="response-textarea"
                  value={response()}
                  onInput={(e) => setResponse(e.currentTarget.value)}
                  placeholder={placeholderText()}
                  rows={8}
                />
              </div>

              <Show when={errorSubmit()}>
                <p class="submit-err">{errorSubmit()}</p>
              </Show>

              <div class="form-actions">
                <button class="cancel-btn" onClick={close}>{t('cancel')}</button>
                <button class="submit-btn" onClick={handleSubmit} disabled={submitting()}>
                  {t('submit')}
                </button>
              </div>
            </div>
          </Match>

          {/* ── Step 2: Empty-response confirm ── */}
          <Match when={step() === 'confirm'}>
            <div class="confirm-step">
              <span class="confirm-ornament">—</span>
              <p class="confirm-heading">{t('confirmHead')}</p>
              <p class="confirm-body">{t('confirmBody')}</p>
              <div class="confirm-actions">
                <button class="confirm-yes" onClick={doSubmit}>{t('confirmYes')}</button>
                <button class="confirm-no" onClick={() => setStep('form')}>{t('confirmNo')}</button>
              </div>
            </div>
          </Match>

          {/* ── Step 3: Thanks ── */}
          <Match when={step() === 'thanks'}>
            <div class="thanks-step">
              <span class="thanks-ornament">✦</span>
              <h2 class="thanks-heading">{t('thanksHead')}</h2>
              <p class="thanks-body">{t('thanksBody')}</p>
              <button class="thanks-close" onClick={close}>{t('thanksClose')}</button>
            </div>
          </Match>

        </Switch>
      </div>
    </div>
  );
}
