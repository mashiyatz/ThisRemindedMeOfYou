import { createSignal, createEffect, createResource, onCleanup, Show } from 'solid-js';
import { HUD } from './ui/HUD';
import { SubmissionPanel } from './ui/SubmissionPanel';
import { BookResponsePanel } from './ui/BookResponsePanel';
import { stateManager, SceneState } from './state/SceneState';
import { unityBridge } from './bridge/UnityBridge';
import type { BookOpenData } from './bridge/UnityBridge';
import type { BookEntry } from './types/BookData';
import type { Lang } from './types/ui';
import { submitBook as supabaseSubmitBook } from './supabase/submitBook';
import { fetchContributors, addContributor, lookupContributor } from './supabase/fetchContributors';
import T from './i18n/translations.json';
import './ContextLossOverlay.css';

export function App() {
  const [panelOpen, setPanelOpen] = createSignal(false);
  const [lang, setLang] = createSignal<Lang>('en');
  const [bookData, setBookData] = createSignal<BookOpenData | null>(null);
  const [bookVisible, setBookVisible] = createSignal(false);
  const [contributorMap] = createResource(fetchContributors);
  const [contextLost, setContextLost] = createSignal(false);

  type TranslationKey = keyof typeof T.en;
  const t = (k: TranslationKey): string => T[lang()][k];

  createEffect(() => {
    function onState(e: Event) {
      const s = (e as CustomEvent<SceneState>).detail;
      if (panelOpen() !== (s === SceneState.SUBMITTING))
        setPanelOpen(s === SceneState.SUBMITTING);
      if (s === SceneState.READING && !bookVisible()) setBookVisible(true);
      if ((s === SceneState.BROWSING || s === SceneState.SUBMITTING || s === SceneState.OUT) && bookVisible()) setBookVisible(false);
    }
    stateManager.addEventListener('statechange', onState);
    onCleanup(() => stateManager.removeEventListener('statechange', onState));
  });

  createEffect(() => {
    function onBookOpen(e: Event) {
      setBookData((e as CustomEvent<BookOpenData>).detail);
    }
    stateManager.addEventListener('bookopen', onBookOpen);
    onCleanup(() => stateManager.removeEventListener('bookopen', onBookOpen));
  });

  createEffect(() => {
    function onContextLost() {
      setContextLost(true);
    }
    window.addEventListener('unitycontextlost', onContextLost);
    onCleanup(() => window.removeEventListener('unitycontextlost', onContextLost));
  });

  const hasUnity = () => !!window.__unity;

  if (import.meta.env.DEV) {
    createEffect(() => {
      const onKey = (e: KeyboardEvent) => {
        if (e.key === 'b' || e.key === 'B') {
          stateManager.dispatchEvent(new CustomEvent('bookopen', {
            detail: {
              title: 'Men Without Women',
              author: 'Haruki Murakami',
              responseText: 'This book made me think of you immediately — the quiet loneliness between people who almost understood each other. I hope you find something in these pages that feels familiar.',
              imageUrl: '',
              audioUrl: '',
            } satisfies BookOpenData,
          }));
          stateManager.transition(SceneState.READING);
        }
      };
      window.addEventListener('keydown', onKey);
      onCleanup(() => window.removeEventListener('keydown', onKey));
    });
  }

  function handleClose() {
    if (hasUnity()) unityBridge.closePanel();
    else stateManager.transition(SceneState.BROWSING);
  }

  function handleDismissBook() {
    setBookVisible(false);
    if (hasUnity()) unityBridge.dismissBook();
    else stateManager.transition(SceneState.BROWSING);
  }

  function handleFetchCover(title: string, author: string): Promise<string[]> {
    if (hasUnity()) return unityBridge.fetchCover(title, author);
    return Promise.resolve([]);
  }

  async function handleSubmitBook(entry: BookEntry, _coverUrl: string): Promise<boolean> {
    const ok = await supabaseSubmitBook(entry);
    if (!ok) return false;
    if (entry.contributorName) {
      const map = contributorMap();
      if (map) addContributor(map, entry.title, entry.author, entry.contributorName);
    }
    if (hasUnity()) unityBridge.notifySpawn(entry);
    return true;
  }

  function reloadPage() {
    location.reload();
  }

  return (
    <div id="ui">
      <HUD
        lang={lang}
        setLang={setLang}
        onLeaveBook={() => hasUnity() ? unityBridge.openPanel() : stateManager.transition(SceneState.SUBMITTING)}
      />
      <SubmissionPanel
        open={panelOpen()}
        lang={lang}
        setLang={setLang}
        onClose={handleClose}
        onSubmitted={() => {}}
        fetchCover={handleFetchCover}
        submitBook={handleSubmitBook}
        onContributor={(title, author, name) => {
          const map = contributorMap();
          if (map) addContributor(map, title, author, name);
        }}
      />
      <Show when={bookData()}>
        {data => (
          <BookResponsePanel
            data={data()}
            visible={bookVisible()}
            lang={lang}
            onDismiss={handleDismissBook}
            contributorName={lookupContributor(contributorMap() ?? new Map(), data().title, data().author)}
          />
        )}
      </Show>

      <Show when={contextLost()}>
        <div class="context-loss-overlay" onClick={reloadPage}>
          <div class="context-loss-card">
            <h2 class="context-loss-title">{t('contextLostTitle')}</h2>
            <p class="context-loss-body">{t('contextLostBody')}</p>
            <span class="context-loss-action">{t('contextLostAction')}</span>
          </div>
        </div>
      </Show>
    </div>
  );
}
