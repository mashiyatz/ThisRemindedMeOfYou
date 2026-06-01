export const SceneState = {
  BROWSING: 'BROWSING',
  IN: 'IN',
  READING: 'READING',
  OUT: 'OUT',
  SUBMITTING: 'SUBMITTING',
} as const;

export type SceneState = typeof SceneState[keyof typeof SceneState];

class StateManager extends EventTarget {
  currentState: SceneState = SceneState.BROWSING;
  timeOfTransition: number = 0;

  transition(next: SceneState): void {
    this.currentState = next;
    this.timeOfTransition = performance.now();
    this.dispatchEvent(new CustomEvent('statechange', { detail: next }));
  }
}

export const stateManager = new StateManager();
