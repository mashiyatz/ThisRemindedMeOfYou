import { createEffect, createSignal, onCleanup, Show } from 'solid-js';
import type { Accessor } from 'solid-js';
import type { BookOpenData } from '../bridge/UnityBridge';
import type { Lang } from '../types/ui';
import './BookResponsePanel.css';

const DISMISS_LABEL: Record<Lang, string> = {
  en: 'Return to the room',
  ko: '방으로 돌아가기',
  es: 'Volver a la habitación',
};

const LISTEN_LABEL: Record<Lang, string> = {
  en: 'listen',
  ko: '듣기',
  es: 'escuchar',
};

const PLAYING_LABEL: Record<Lang, string> = {
  en: 'playing',
  ko: '재생 중',
  es: 'reproduciendo',
};

const REPLAY_LABEL: Record<Lang, string> = {
  en: 'replay',
  ko: '다시 듣기',
  es: 'repetir',
};

function AudioPlayer(props: { src: string; lang: Accessor<Lang> }) {
  let audioRef: HTMLAudioElement | undefined;
  const [playing, setPlaying] = createSignal(false);
  const [ended, setEnded] = createSignal(false);

  function toggle() {
    if (!audioRef) return;
    if (playing()) {
      audioRef.pause();
    } else {
      audioRef.play();
      setEnded(false);
    }
  }

  const label = () => {
    const l = props.lang();
    if (playing()) return PLAYING_LABEL[l] ?? PLAYING_LABEL.en;
    if (ended())   return REPLAY_LABEL[l]  ?? REPLAY_LABEL.en;
    return LISTEN_LABEL[l] ?? LISTEN_LABEL.en;
  };

  const icon = () => playing() ? '⏸' : (ended() ? '↺' : '▶');

  return (
    <div class="brp-audio-wrap">
      <audio
        ref={audioRef}
        src={props.src}
        onPlay={() => setPlaying(true)}
        onPause={() => setPlaying(false)}
        onEnded={() => { setPlaying(false); setEnded(true); }}
      />
      <button class="brp-audio-btn" onClick={toggle} aria-label={label()}>
        <span class="brp-audio-icon">{icon()}</span>
        <span class="brp-audio-label">{label()}</span>
      </button>
    </div>
  );
}

interface Props {
  data: BookOpenData;
  visible: boolean;
  lang: Accessor<Lang>;
  onDismiss: () => void;
  contributorName?: string;
}

export function BookResponsePanel(props: Props) {
  createEffect(() => {
    if (!props.visible) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') props.onDismiss();
    };
    window.addEventListener('keydown', onKey);
    onCleanup(() => window.removeEventListener('keydown', onKey));
  });

  return (
    <div class={`brp-overlay${props.visible ? ' visible' : ''}`} aria-hidden={!props.visible}>
      <div class="brp-content">
        <div class="brp-rule" />
        <div class="brp-byline">
          <h2 class="brp-title">{props.data.title}</h2>
          <p class="brp-author">{props.data.author}</p>
          <Show when={props.contributorName}>
            <p class="brp-contributor">— {props.contributorName}</p>
          </Show>
        </div>

        <Show when={props.data.imageUrl}>
          <img
            class="brp-image"
            src={props.data.imageUrl}
            alt={props.data.title}
          />
        </Show>

        <Show when={!props.data.imageUrl && props.data.responseText}>
          <p class="brp-text">{props.data.responseText}</p>
        </Show>

        <Show when={props.data.audioUrl}>
          <AudioPlayer src={props.data.audioUrl} lang={props.lang} />
        </Show>

        <button class="brp-dismiss" onClick={props.onDismiss}>
          <span class="brp-dismiss-arrow">←</span>
          {DISMISS_LABEL[props.lang()] ?? DISMISS_LABEL.en}
        </button>
      </div>
    </div>
  );
}
