import { stateManager, SceneState } from '../state/SceneState';
import type { BookEntry } from '../types/BookData';

declare global {
  interface Window {
    __unity?: { SendMessage(gameObject: string, method: string, param?: string): void };
    unityBridge?: UnityBridgeCallbacks;
  }
}

export interface BookOpenData {
  title:        string;
  author:       string;
  responseText: string;
  imageUrl:     string;
  audioUrl:     string;
}

interface UnityBridgeCallbacks {
  onStateChange:   (state: string)    => void;
  onBookHighlight: (title: string)    => void;
  onCoverUrl:      (url: string)      => void;
  onCoverLoading:  (loading: boolean) => void;
  onBookOpen:      (json: string)     => void;
  onCoverUrls:     (json: string)     => void;
}

type CoverUrlListener     = (url: string)     => void;
type CoverLoadingListener = (loading: boolean) => void;
type CoverUrlsListener    = (urls: string[])   => void;

const coverUrlListeners     = new Set<CoverUrlListener>();
const coverLoadingListeners = new Set<CoverLoadingListener>();
const coverUrlsListeners    = new Set<CoverUrlsListener>();

/** Call once before mounting SolidJS — registers window.unityBridge and feeds stateManager. */
export function initUnityBridge(): void {
  window.unityBridge = {
    onStateChange:   (s)       => stateManager.transition(s as SceneState),
    onBookHighlight: (t)       => stateManager.dispatchEvent(new CustomEvent('bookhighlight', { detail: t })),
    onCoverUrl:      (url)     => coverUrlListeners.forEach(fn => fn(url)),
    onCoverLoading:  (loading) => coverLoadingListeners.forEach(fn => fn(loading)),
    onCoverUrls:     (json)    => {
      try {
        const urls = JSON.parse(json) as string[];
        coverUrlsListeners.forEach(fn => fn(urls));
      } catch { /* ignore malformed JSON */ }
    },
    onBookOpen:      (json)    => stateManager.dispatchEvent(
      new CustomEvent('bookopen', { detail: JSON.parse(json) as BookOpenData })
    ),
  };
}

function getUnity() {
  return window.__unity ?? null;
}

export const unityBridge = {
  openPanel() {
    getUnity()?.SendMessage('WebGLBridge', 'ReceiveOpenPanel');
  },

  closePanel() {
    getUnity()?.SendMessage('WebGLBridge', 'ReceiveClosePanel');
  },

  /** Asks Unity to fetch book covers via BookCoverService. Resolves with all URLs (empty if none). */
  fetchCover(title: string, author: string): Promise<string[]> {
    return new Promise((resolve) => {
      let collectedUrls: string[] = [];
      let hasResolved = false;

      const urlsListener: CoverUrlsListener = (urls) => {
        if (urls.length > 0) collectedUrls = urls;
      };
      const loadingListener: CoverLoadingListener = (loading) => {
        if (!loading && !hasResolved) {
          hasResolved = true;
          coverUrlsListeners.delete(urlsListener);
          coverLoadingListeners.delete(loadingListener);
          resolve(collectedUrls);
        }
      };

      const unity = getUnity();
      if (!unity) {
        resolve([]);
        return;
      }

      coverUrlsListeners.add(urlsListener);
      coverLoadingListeners.add(loadingListener);
      unity.SendMessage('WebGLBridge', 'ReceiveFetchCover', JSON.stringify({ title, author }));
    });
  },

  dismissBook() {
    getUnity()?.SendMessage('WebGLBridge', 'ReceiveDismissBook');
  },

  /** Tells Unity to prepare the scene spawn for a newly submitted book. Fire-and-forget. */
  notifySpawn(entry: BookEntry): void {
    getUnity()?.SendMessage(
      'WebGLBridge',
      'ReceiveSubmit',
      JSON.stringify({
        title:         entry.title,
        author:        entry.author,
        response:      entry.responseText,
        isHandwritten: entry.isHandwritten,
        wantsNarrated: entry.wantsNarrated,
      }),
    );
  },
};
