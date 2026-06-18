import { Show } from 'solid-js';
import type { Lang } from '../types/ui';
import T from '../i18n/translations.json';

type CoverState = 'empty' | 'loading' | 'loaded';

interface CoverPreviewProps {
  lang: () => Lang;
  coverUrl: () => string;
  coverState: () => CoverState;
  coverUrls: () => string[];
  onFindCover: () => void;
  onNavigateCover: (dir: number) => void;
  /** Extra class on the frame element to control sizing per context. */
  frameClass?: string;
}

/** Shared book-cover preview: find-cover placeholder, loader, and image carousel. */
export function CoverPreview(props: CoverPreviewProps) {
  type TranslationKey = keyof typeof T.en;
  const t = (k: TranslationKey): string => T[props.lang()][k];

  return (
    <div class={`cover-frame ${props.frameClass ?? ''}`} classList={{ 'cover-frame--find': props.coverState() === 'empty' }}>
      <Show when={props.coverState() === 'empty'}>
        <button class="cover-find-btn" onClick={props.onFindCover}>
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1"
               stroke-linecap="round" stroke-linejoin="round">
            <rect x="4" y="2" width="16" height="20" rx="1"/>
            <line x1="4" y1="6" x2="20" y2="6"/>
            <line x1="8" y1="2" x2="8" y2="22"/>
          </svg>
          <span class="cover-find-label">{t('findCover')}</span>
        </button>
      </Show>
      <Show when={props.coverState() === 'loading'}>
        <div class="cover-loader">
          <div class="cover-dot" />
          <div class="cover-dot" />
          <div class="cover-dot" />
        </div>
      </Show>
      <Show when={props.coverState() === 'loaded'}>
        <img src={props.coverUrl()} alt="book cover" />
        <Show when={props.coverUrls().length > 1}>
          <button class="cover-arrow cover-arrow-prev" onClick={() => props.onNavigateCover(-1)} aria-label="Previous cover">‹</button>
          <button class="cover-arrow cover-arrow-next" onClick={() => props.onNavigateCover(1)} aria-label="Next cover">›</button>
        </Show>
      </Show>
    </div>
  );
}
