import { createSignal, createEffect, onCleanup, Show } from 'solid-js';
import { stateManager, SceneState } from '../state/SceneState';
import type { Lang } from '../types/ui';
import HUD_LABELS from '../i18n/hud_labels.json';
import './HUD.css';

interface HUDProps {
  lang: () => Lang;
  setLang: (l: Lang) => void;
  onLeaveBook: () => void;
}

const LANG_LABELS: Record<Lang, string> = {
  en: 'EN',
  ko: '한국어',
  es: 'ES',
};

export function HUD(props: HUDProps) {
  const [hoverTitle, setHoverTitle] = createSignal('');
  const [browsing, setBrowsing] = createSignal(true);
  const [labelIndex, setLabelIndex] = createSignal(0);
  const [isFullscreen, setIsFullscreen] = createSignal(false);

  function onHighlight(e: Event) {
    setHoverTitle((e as CustomEvent<string>).detail);
  }
  function onState(e: Event) {
    const s = (e as CustomEvent<SceneState>).detail;
    setBrowsing(s === SceneState.BROWSING);
  }

  function toggleFullscreen() {
    if (!document.fullscreenElement) {
      document.documentElement.requestFullscreen();
    } else {
      document.exitFullscreen();
    }
  }

  createEffect(() => {
    if (browsing()) {
      const labels = HUD_LABELS[props.lang()];
      setLabelIndex(Math.floor(Math.random() * labels.length));
    }
  });

  createEffect(() => {
    function onChange() {
      setIsFullscreen(!!document.fullscreenElement);
    }
    document.addEventListener('fullscreenchange', onChange);
    onCleanup(() => document.removeEventListener('fullscreenchange', onChange));
  });

  stateManager.addEventListener('bookhighlight', onHighlight);
  stateManager.addEventListener('statechange', onState);
  onCleanup(() => {
    stateManager.removeEventListener('bookhighlight', onHighlight);
    stateManager.removeEventListener('statechange', onState);
  });

  const langs: Lang[] = ['en', 'ko', 'es'];
  const supportsFullscreen = 'requestFullscreen' in document.documentElement;

  return (
    <div class="hud" classList={{ hidden: !browsing() }}>

      {/* Top-left: language selector */}
      <div class="hud-lang">
        {langs.map((l, i) => (
          <>
            {i > 0 && <span class="hud-lang-sep">·</span>}
            <button
              class="hud-lang-btn"
              classList={{ active: props.lang() === l }}
              onClick={() => props.setLang(l)}
            >
              {LANG_LABELS[l]}
            </button>
          </>
        ))}
      </div>

      {/* Top-right: fullscreen toggle */}
      <Show when={supportsFullscreen}>
        <button
          class="hud-fullscreen-btn"
          onClick={toggleFullscreen}
          aria-label={isFullscreen() ? 'Exit fullscreen' : 'Enter fullscreen'}
        >
          <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M8 3H5a2 2 0 0 0-2 2v3m18 0V5a2 2 0 0 0-2-2h-3m0 18h3a2 2 0 0 0 2-2v-3M3 16v3a2 2 0 0 0 2 2h3"/>
          </svg>
        </button>
      </Show>

      {/* Bottom-left: hovered book title */}
      <Show when={hoverTitle()}>
        <div class="hud-book-title">
          <div class="hud-book-rule" />
          <p class="hud-title">{hoverTitle()}</p>
        </div>
      </Show>

      {/* Bottom-right: leave a book — cycles through phrases */}
      <button class="leave-book-btn" onClick={props.onLeaveBook}>
        {HUD_LABELS[props.lang()][labelIndex()]}
        <span class="leave-book-arrow" aria-hidden="true">→</span>
      </button>

    </div>
  );
}
