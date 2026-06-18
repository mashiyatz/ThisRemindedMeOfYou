import { createSignal, createEffect, For, onCleanup } from 'solid-js';
import type { Lang } from '../types/ui';
import { reducedMotion } from '../motion';
import WRITING_PROMPTS from '../i18n/writing_prompts.json';
import { CoverPreview } from './CoverPreview';
import './PromptSidebar.css';

type CoverState = 'empty' | 'loading' | 'loaded';

interface BubbleState {
  index: number;
  top: number;
  left: number;
  anim: 'idle' | 'out' | 'in';
  bobDelay: number;
}

interface Pos {
  top: number;
  left: number;
}

interface PromptSidebarProps {
  open: boolean;
  lang: () => Lang;
  coverUrl: () => string;
  coverState: () => CoverState;
  coverUrls: () => string[];
  onFindCover: () => void;
  onNavigateCover: (dir: number) => void;
}

const NUM_BUBBLES = 5;
const MIN_GAP = 22;
/* Ring-shaped distribution around the centered cover. The cover sits at roughly
   50%/50% and occupies a central zone; randomRingPos picks positions in a band
   around it so bubbles appear to orbit the preview rather than overlap it. */
function randomRingPos(existing: Pos[]): Pos {
  const cx = 50, cy = 50, minR = 30, maxR = 40;
  for (let attempt = 0; attempt < 120; attempt++) {
    const angle = Math.random() * 2 * Math.PI;
    const dist  = minR + Math.random() * (maxR - minR);
    const top   = cy + dist * Math.sin(angle);
    const left  = cx + dist * Math.cos(angle);
    // Clamp — avoid bubble clipping over the panel (>65% left) or offscreen
    if (top < 4 || top > 86 || left < 4 || left > 64) continue;
    const pos: Pos = { top, left };
    if (existing.every(p => Math.hypot(pos.top - p.top, pos.left - p.left) >= MIN_GAP)) {
      return pos;
    }
  }
  return { top: 20 + Math.random() * 50, left: 10 + Math.random() * 40 };
}

function randomIndex(existing: number[], promptsLen: number): number {
  if (existing.length >= promptsLen) {
    return Math.floor(Math.random() * promptsLen);
  }
  let idx: number;
  do {
    idx = Math.floor(Math.random() * promptsLen);
  } while (existing.includes(idx));
  return idx;
}

export function PromptSidebar(props: PromptSidebarProps) {
  const [bubbles, setBubbles] = createSignal<BubbleState[]>([]);
  const [stickyIdx, setStickyIdx] = createSignal(-1);

  createEffect(() => {
    if (!props.open) return;

    setStickyIdx(-1);

    const prompts = WRITING_PROMPTS[props.lang()];
    const count = Math.min(NUM_BUBBLES, prompts.length);

    const used: number[] = [];
    while (used.length < count) {
      used.push(randomIndex(used, prompts.length));
    }

    const positions: Pos[] = [];
    while (positions.length < count) {
      positions.push(randomRingPos(positions));
    }

    setBubbles(used.map((idx, i) => ({
      index: idx,
      top: positions[i].top,
      left: positions[i].left,
      anim: reducedMotion ? 'idle' : 'in',
      bobDelay: Math.random() * 3,
    })));

    // E-ink mode: prompts stay static. Every timed content/position swap is a
    // DOM mutation that forces a panel region refresh right next to the form
    // while the visitor is typing.
    if (reducedMotion) return;

    const fadeInTimers = used.map((_, i) =>
      setTimeout(() => {
        setBubbles(prev => prev.map((b, j) => j === i ? { ...b, anim: 'idle' } : b));
      }, 600 + i * 900 + 1500)
    );

    const cycleTimers: number[] = [];
    const replaceOutTimers: number[] = [];
    const replaceInTimers: number[] = [];

    used.forEach((_, i) => {
      const intervalId = setInterval(() => {
        if (stickyIdx() === i) return;
        setBubbles(prev => prev.map((b, j) => j === i ? { ...b, anim: 'out' } : b));

        const outTimer = setTimeout(() => {
          if (stickyIdx() === i) return;
          setBubbles(prev => prev.map((b, j) => {
            if (j !== i) return b;
            const others = prev.filter((_, k) => k !== i);
            const otherIndices = others.map(o => o.index);
            const newPos = randomRingPos(others.map(o => ({ top: o.top, left: o.left })));
            return {
              index: randomIndex(otherIndices, prompts.length),
              top: newPos.top,
              left: newPos.left,
              anim: 'in',
              bobDelay: Math.random() * 3,
            };
          }));
        }, 1500);

        const inTimer = setTimeout(() => {
          setBubbles(prev => prev.map((b, j) => j === i ? { ...b, anim: 'idle' } : b));
        }, 3000);

        replaceOutTimers.push(outTimer);
        replaceInTimers.push(inTimer);
      }, 12000 + Math.random() * 8000);

      cycleTimers.push(intervalId);
    });

    onCleanup(() => {
      fadeInTimers.forEach(t => clearTimeout(t));
      cycleTimers.forEach(t => clearInterval(t));
      replaceOutTimers.forEach(t => clearTimeout(t));
      replaceInTimers.forEach(t => clearTimeout(t));
    });
  });

  function handleClick(i: number) {
    setStickyIdx(prev => prev === i ? -1 : i);
  }

  return (
    <div class="cover-sidebar" classList={{ visible: props.open, 'is-ko': props.lang() === 'ko' }}>
      <For each={bubbles()}>
        {(bubble, i) => (
          <div
            class="prompt-sidebar-bubble"
            classList={{ 'anim-out': bubble.anim === 'out', 'anim-in': bubble.anim === 'in', sticky: stickyIdx() === i() }}
            style={{ top: `${bubble.top}%`, left: `${bubble.left}%`, '--bob-delay': `${bubble.bobDelay}s` }}
            onClick={() => handleClick(i())}
            role="button"
            tabindex={0}
          >
            {WRITING_PROMPTS[props.lang()][bubble.index]}
          </div>
        )}
      </For>

      <div class="cover-stage">
        <CoverPreview
          lang={props.lang}
          coverUrl={props.coverUrl}
          coverState={props.coverState}
          coverUrls={props.coverUrls}
          onFindCover={props.onFindCover}
          onNavigateCover={props.onNavigateCover}
        />
      </div>
    </div>
  );
}
