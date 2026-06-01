import { createSignal, createEffect, For, onCleanup } from 'solid-js';
import type { Lang } from '../types/ui';
import WRITING_PROMPTS from '../i18n/writing_prompts.json';
import './PromptSidebar.css';

type Step = 'form' | 'confirm' | 'thanks';
type BubbleSize = 'small' | 'medium' | 'large';

interface BubbleState {
  index: number;
  top: number;
  left: number;
  anim: 'idle' | 'out' | 'in';
  size: BubbleSize;
  bobDelay: number;
}

interface Pos {
  top: number;
  left: number;
}

interface PromptSidebarProps {
  open: boolean;
  step: () => Step;
  lang: () => Lang;
}

const SIZES: BubbleSize[] = ['small', 'medium', 'large'];
const NUM_BUBBLES = 5;
const MIN_GAP = 22;
const TOP_RANGE = 60; // 6-66%
const LEFT_RANGE = 52; // 3-55%

function randomSize(): BubbleSize {
  return SIZES[Math.floor(Math.random() * SIZES.length)];
}

function randomPos(existing: Pos[]): Pos {
  for (let attempt = 0; attempt < 100; attempt++) {
    const pos: Pos = { top: 6 + Math.random() * TOP_RANGE, left: 3 + Math.random() * LEFT_RANGE };
    if (existing.every(p => Math.hypot(pos.top - p.top, pos.left - p.left) >= MIN_GAP)) {
      return pos;
    }
  }
  return { top: 6 + Math.random() * TOP_RANGE, left: 3 + Math.random() * LEFT_RANGE };
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
    if (!props.open || props.step() !== 'form') return;

    setStickyIdx(-1);

    const prompts = WRITING_PROMPTS[props.lang()];
    const count = Math.min(NUM_BUBBLES, prompts.length);

    const used: number[] = [];
    while (used.length < count) {
      used.push(randomIndex(used, prompts.length));
    }

    const positions: Pos[] = [];
    while (positions.length < count) {
      positions.push(randomPos(positions));
    }

    setBubbles(used.map((idx, i) => ({
      index: idx,
      top: positions[i].top,
      left: positions[i].left,
      anim: 'in',
      size: randomSize(),
      bobDelay: Math.random() * 3,
    })));

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
            const newPos = randomPos(others.map(o => ({ top: o.top, left: o.left })));
            return {
              index: randomIndex(otherIndices, prompts.length),
              top: newPos.top,
              left: newPos.left,
              anim: 'in',
              size: randomSize(),
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
    <div class="prompt-sidebar" classList={{ visible: props.open && props.step() === 'form', 'is-ko': props.lang() === 'ko' }}>
      <For each={bubbles()}>
        {(bubble, i) => (
          <div
            class={`prompt-sidebar-bubble size-${bubble.size}`}
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
    </div>
  );
}
