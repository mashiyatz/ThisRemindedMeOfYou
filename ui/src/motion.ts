declare global {
  interface Window {
    __reducedMotion?: boolean;
  }
}

// In production the WebGL template computes window.__reducedMotion before any
// script runs; in `npm run dev` (no template) we compute the same thing here.
function compute(): boolean {
  const param = new URLSearchParams(window.location.search).get('motion');
  if (param !== null) return param === 'off';
  return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

export const reducedMotion: boolean = window.__reducedMotion ?? compute();

if (reducedMotion) document.documentElement.classList.add('reduced-motion');
