import { createSignal, Show } from 'solid-js';
import type { BookEntry } from '../types/BookData';
import type { Lang } from '../types/ui';
import T from '../i18n/translations.json';
import { PromptSidebar } from './PromptSidebar';
import { CoverPreview } from './CoverPreview';
import './SubmissionPanel.css';

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

  type TranslationKey = keyof typeof T.en;
  const t = (k: TranslationKey): string => T[lang()][k];
  const dateStr = () => new Date().toLocaleDateString(T[lang()].dateLocale, { year: 'numeric', month: 'long', day: 'numeric' });

  // Title/author/name are single-line-ish textareas (for wrapping + styling
  // parity with the response field). Grow their height to fit wrapped content.
  let titleRef: HTMLTextAreaElement | undefined;
  let authorRef: HTMLTextAreaElement | undefined;
  let nameRef: HTMLTextAreaElement | undefined;

  function autoGrow(el: HTMLTextAreaElement) {
    const cs = getComputedStyle(el);
    const borderY = parseFloat(cs.borderTopWidth) + parseFloat(cs.borderBottomWidth);
    el.style.height = 'auto';
    el.style.height = `${el.scrollHeight + borderY}px`;
  }

  // Keep these fields logically single-line: block Enter, strip pasted newlines.
  const stripNewlines = (s: string) => s.replace(/\r?\n/g, ' ');
  const blockEnter = (e: KeyboardEvent) => { if (e.key === 'Enter') e.preventDefault(); };

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

  async function handleSubmit() {
    if (!validate()) return;
    await doSubmit();
  }

  async function doSubmit() {
    setSubmitting(true);
    const finalCover = coverUrl();
    const submittedAt = new Date().toISOString();
    const name = contributor().trim();
    const entry: BookEntry = {
      title: title(), author: author(), submittedAt,
      coverImagePath: '', coverImageUrl: finalCover,
      responseText: response(), audioPath: '', audioUrl: '',
      isHandwritten: false, wantsNarrated: false,
      contributorName: name,
    };
    const ok = await props.submitBook(entry, finalCover);
    setSubmitting(false);
    if (!ok) { setErrorSubmit(t('errorSubmit')); return; }
    if (name) props.onContributor(title(), author(), name);
    props.onSubmitted(entry, finalCover);
    close();
  }

  function navigateCover(dir: number) {
    const urls = coverUrls();
    if (urls.length < 2) return;
    const next = (coverIndex() + dir + urls.length) % urls.length;
    setCoverIndex(next);
    setCoverUrl(urls[next]);
  }

  function clearCover() {
    setCoverUrl('');
    setCoverState('empty');
    setCoverUrls(p => p.length ? [] : p);
    setCoverIndex(0);
  }

  function reset() {
    setTitle(''); setAuthor(''); setResponse(''); setCoverUrl('');
    setCoverState('empty');
    setErrorTitle(''); setErrorSubmit('');
    setContributor('');
    setCoverUrls([]); setCoverIndex(0);
    // collapse the auto-grown fields back to a single row
    [titleRef, authorRef, nameRef].forEach((el) => { if (el) el.style.height = 'auto'; });
  }

  function close() {
    reset();
    props.onClose();
  }

  return (
    <div class="panel-overlay" classList={{ visible: props.open }}>
      <PromptSidebar
        open={props.open}
        lang={lang}
        coverUrl={coverUrl}
        coverState={coverState}
        coverUrls={coverUrls}
        onFindCover={findCover}
        onNavigateCover={navigateCover}
      />
      <div class="panel" classList={{ 'is-ko': lang() === 'ko' }}>
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

          {/* Mobile-only cover control — the sidebar (and its cover) is hidden
              below 768px, so narrow screens get the preview inside the panel. */}
          <div class="cover-mobile">
            <CoverPreview
              lang={lang}
              coverUrl={coverUrl}
              coverState={coverState}
              coverUrls={coverUrls}
              onFindCover={findCover}
              onNavigateCover={navigateCover}
              frameClass="cover-frame-mobile"
            />
          </div>

          <div class="fields-section">
            <div class="field-group">
              <label class="field-label" for="sp-title">{t('labelTitle')}</label>
              <textarea
                ref={titleRef}
                class="field-input" classList={{ 'has-error': !!errorTitle() }}
                id="sp-title" rows={1} autocomplete="off" autocorrect="off" autocapitalize="off" spellcheck={false} inputmode="text"
                value={title()}
                onKeyDown={blockEnter}
                onInput={(e) => { setTitle(stripNewlines(e.currentTarget.value)); setErrorTitle(''); clearCover(); autoGrow(e.currentTarget); }}
                placeholder={t('phTitle')}
              />
              <Show when={errorTitle()}>
                <p class="field-err">{errorTitle()}</p>
              </Show>
            </div>
            <div class="field-group">
              <label class="field-label" for="sp-author">{t('labelAuthor')}</label>
              <textarea
                ref={authorRef}
                class="field-input"
                id="sp-author" rows={1} autocomplete="off" autocorrect="off" autocapitalize="off" spellcheck={false} inputmode="text"
                value={author()}
                onKeyDown={blockEnter}
                onInput={(e) => { setAuthor(stripNewlines(e.currentTarget.value)); clearCover(); autoGrow(e.currentTarget); }}
                placeholder={t('phAuthor')}
              />
            </div>
          </div>

          <div class="response-section">
            <span class="response-label">{t('labelResponse')}</span>
            <textarea
              class="response-textarea"
              autocorrect="off" autocapitalize="off" spellcheck={false} inputmode="text"
              value={response()}
              onInput={(e) => setResponse(e.currentTarget.value)}
              placeholder={t('phResponse')}
              rows={8}
            />
          </div>

          <div class="name-section">
            <label class="field-label" for="sp-name">{t('labelContributor')}</label>
            <textarea
              ref={nameRef}
              class="field-input"
              id="sp-name" rows={1} autocomplete="off" autocorrect="off" autocapitalize="off" spellcheck={false} inputmode="text"
              value={contributor()}
              onKeyDown={blockEnter}
              onInput={(e) => { setContributor(stripNewlines(e.currentTarget.value)); autoGrow(e.currentTarget); }}
              placeholder={t('phContributor')}
            />
          </div>

          <div class="form-caption">
            <span class="form-caption-text">{t('captionLine1')}<br/>{t('captionLine2')}</span>
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
      </div>
    </div>
  );
}
