import { createSignal, Show, Switch, Match } from 'solid-js';
import type { BookEntry } from '../types/BookData';
import type { Lang } from '../types/ui';
import T from '../i18n/translations.json';
import { updateContributor } from '../supabase/submitBook';
import { PromptSidebar } from './PromptSidebar';
import './SubmissionPanel.css';

type Step = 'form' | 'confirm' | 'thanks';
type CoverState = 'empty' | 'loading' | 'loaded';

interface SubmissionPanelProps {
  open: boolean;
  lang: () => Lang;
  setLang: (l: Lang) => void;
  onClose: () => void;
  onSubmitted: (entry: BookEntry, coverUrl: string) => void;
  fetchCover: (title: string, author: string) => Promise<string[]>;
  submitBook:  (entry: BookEntry, coverUrl: string) => Promise<boolean>;
  onContributor: (title: string, author: string, name: string) => void;
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
  const [errorSubmit, setErrorSubmit] = createSignal('');
  const [submitting, setSubmitting] = createSignal(false);
  const [coverUrls, setCoverUrls] = createSignal<string[]>([]);
  const [coverIndex, setCoverIndex] = createSignal(0);
  const [lastSubmitted, setLastSubmitted] = createSignal<{title: string; author: string; submittedAt: string} | null>(null);

  let textareaRef!: HTMLTextAreaElement;

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
    if (!title().trim()) {
      setErrorTitle(t('errTitle'));
      return;
    }
    setCoverState('loading');
    const urls = await props.fetchCover(title(), author());
    setCoverUrls(urls);
    setCoverIndex(0);
    setCoverUrl(urls.length > 0 ? urls[0] : generateFallbackCover(title()));
    setCoverState('loaded');
  }

  function validate(): boolean {
    let ok = true;
    if (!title().trim()) { setErrorTitle(t('errTitle')); ok = false; }
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
    const submittedAt = new Date().toISOString();
    const entry: BookEntry = {
      title: title(), author: author(), submittedAt,
      coverImagePath: '', coverImageUrl: finalCover,
      responseText: response(), audioPath: '', audioUrl: '',
      isHandwritten: false, wantsNarrated: false,
      contributorName: '',
    };
    const ok = await props.submitBook(entry, finalCover);
    setSubmitting(false);
    if (!ok) { setErrorSubmit(t('errorSubmit')); setStep('form'); return; }
    setLastSubmitted({ title: title(), author: author(), submittedAt });
    setStep('thanks');
    props.onSubmitted(entry, finalCover);
  }

  function navigateCover(dir: number) {
    const urls = coverUrls();
    if (urls.length < 2) return;
    const next = (coverIndex() + dir + urls.length) % urls.length;
    setCoverIndex(next);
    setCoverUrl(urls[next]);
  }

  function reset() {
    setTitle(''); setAuthor(''); setResponse(''); setCoverUrl('');
    setCoverState('empty');
    setErrorTitle(''); setErrorSubmit('');
    setStep('form'); setContributor('');
    setCoverUrls([]); setCoverIndex(0);
    setLastSubmitted(null);
  }

  async function handleThanksClose() {
    const name = contributor().trim();
    const last = lastSubmitted();
    if (name && last) {
      await updateContributor(last.title, last.author, last.submittedAt, name);
      props.onContributor(last.title, last.author, name);
    }
    close();
  }

  function close() {
    reset();
    props.onClose();
  }

  return (
    <div class="panel-overlay" classList={{ visible: props.open }}>
      <PromptSidebar open={props.open} step={step} lang={lang} />
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
                      <Show when={coverUrls().length > 1}>
                        <button class="cover-arrow cover-arrow-prev" onClick={() => navigateCover(-1)} aria-label="Previous cover">‹</button>
                        <button class="cover-arrow cover-arrow-next" onClick={() => navigateCover(1)} aria-label="Next cover">›</button>
                      </Show>
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
                      id="sp-title" type="text" autocomplete="off" autocorrect="off" autocapitalize="off" spellcheck={false} inputmode="text"
                      value={title()}
                      onInput={(e) => { setTitle(e.currentTarget.value); setErrorTitle(''); setCoverUrl(''); setCoverState('empty'); setCoverUrls([]); setCoverIndex(0); }}
                      placeholder={t('phTitle')}
                    />
                    <Show when={errorTitle()}>
                      <p class="field-err">{errorTitle()}</p>
                    </Show>
                  </div>
                  <div class="field-group">
                    <label class="field-label" for="sp-author">{t('labelAuthor')}</label>
                    <input
                      class="field-input"
                      id="sp-author" type="text" autocomplete="off" autocorrect="off" autocapitalize="off" spellcheck={false} inputmode="text"
                      value={author()}
                      onInput={(e) => { setAuthor(e.currentTarget.value); setCoverUrl(''); setCoverState('empty'); setCoverUrls([]); setCoverIndex(0); }}
                      placeholder={t('phAuthor')}
                    />
                  </div>
                </div>
              </div>

              <div class="response-section">
                <span class="response-label">{t('labelResponse')}</span>
                <textarea
                  ref={textareaRef!}
                  class="response-textarea"
                  autocorrect="off" autocapitalize="off" spellcheck={false} inputmode="text"
                  value={response()}
                  onInput={(e) => setResponse(e.currentTarget.value)}
                  placeholder={t('phResponse')}
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
              <div class="thanks-name-section">
                <label class="thanks-name-label" for="thanks-name">{t('labelContributor')}</label>
                <input
                  class="thanks-name-input"
                  id="thanks-name" type="text"
                  autocomplete="off" autocorrect="off" autocapitalize="off" spellcheck={false} inputmode="text"
                  value={contributor()}
                  onInput={(e) => setContributor(e.currentTarget.value)}
                  placeholder={t('phContributor')}
                />
              </div>
              <button class="thanks-close" onClick={handleThanksClose}>{t('thanksClose')}</button>
            </div>
          </Match>

        </Switch>
      </div>
    </div>
  );
}
