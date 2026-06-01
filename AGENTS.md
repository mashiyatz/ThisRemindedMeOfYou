# AGENTS.md

## Project Overview

"This Reminded Me of You" is an interactive Unity3D scene (WebGL build) where the player explores a living room and clicks on books to read handwritten dedications, accompanied by voice audio recordings. Created by Jo Suk and Mashi Zaman for Hypercinema Fall 2022 at ITP.

- Unity version: **Unity 6.4**
- Unity project folder: `CornellBox/`
- WebGL build output: `build/` (local testing); `docs/` is the old archived WebGL build served via GitHub Pages

## Project structure

Two production parts, two deprecated experiments.

| Directory | Status | Role | Stack |
|-----------|--------|------|-------|
| `CornellBox/` | **Active** | Unity 3D scene, built to WebGL | Unity 6.4 C# |
| `ui/` | **Active** | SolidJS overlay deployed alongside Unity WebGL output | SolidJS + Vite + TypeScript + `@supabase/supabase-js` |
| `archived/` | **Archived** | Deprecated experiments and V1 scripts | Various |
| `utils/` | **Utility** | Python scripts for asset processing | Python |

## Commands

All run from the project's own directory, not from repo root.

### `ui/` (production overlay)
- `npm run dev` — standalone Vite dev preview
- `npm run build && npm run copy` — build, then copy bundle to `build/ui/` alongside Unity WebGL output
- Env: `VITE_SUPABASE_URL`, `VITE_SUPABASE_ANON_KEY` (copy `ui/.env.example`)

### `CornellBox/` (Unity project)
- Open in Unity Hub or Unity Editor 6.4
- File → Build Settings → WebGL → Build (output to `build/`)
- After building, run `cd ui && npm run build && npm run copy` to deploy the SolidJS UI overlay alongside it

### `archived/`
- Deprecated experiments (`web/`, `babylon/`) and V1 scripts — not actively maintained. Refer to original `README.md` in each subdirectory for build instructions.

## Unity architecture (`CornellBox/`)

The active codebase uses the **V2** scripts in `CornellBox/Assets/Scripts/V2/`. V1 scripts (`PlayerController.cs`, `ClickInteraction.cs`, `BookDisplay.cs`, `ScrollThroughBooks.cs`) are still present at `CornellBox/Assets/Scripts/` but deprecated.

### State Machine (`V2PlayerController.cs`)

`V2PlayerController` owns the global state machine via a static `PlayerState` enum:

- `BROWSING` — player can hover/click books freely
- `IN` — a book is animating into view
- `READING` — book is fully displayed; click to dismiss
- `OUT` — book is animating back out
- `SUBMITTING` — submission panel is open

### Scripts

| Script | Role |
|--------|------|
| `V2PlayerController.cs` | Global state machine, triggers `V2BookDisplay.AnimateIn/Out()` on transition |
| `V2BookDisplay.cs` | Shared book UI controller — animates book into/out of view (DOTween), fades overlay, scrollable response text with two-tier overflow (auto-size → scroll), idle bobbing animation |
| `V2BookInteraction.cs` | Attached to each book mesh — `OnMouseEnter/Exit/Down/Drag/Up`, emission highlight, outline material overlay, **draggable books** constrained to round wooden table surface |
| `V2BookScroller.cs` | Keyboard navigation (Left/Right arrows + Enter/Space) through books, resets on mouse hover |
| `V2BookLoader.cs` | Loads books from `StreamingAssets/books.json` (local curator) + Supabase (visitor submissions), deduplicates by title+author, filters by MostRecent/Random, spawns with position/rotation jitter, animated book drop-in on new submissions |
| `V2UIBridge.cs` | Reactive event bus — subscribes to `V2BookDisplay` events, mirrors `V2PlayerController` state, exposes reactive properties + `OnChanged` events consumed by `WebGLBridge` |
| `WebGLBridge.cs` | **The actual C#↔JS bridge** — outbound via jslib (`JS_Notify*`), inbound via `SendMessage('WebGLBridge', 'Receive*')`. All communication with the SolidJS overlay flows through this. |
| `V2SubmissionPanel.cs` | Full submission form inside Unity (UI Toolkit + TextMeshPro) — title/author/response fields, cover fetch via `BookCoverService`, font switching per language (Lora/Fell/Noto), DOTween panel animations, confirm overlay, Supabase insert via `SupabaseService` |
| `SupabaseService.cs` | Unity-side Supabase REST client — fetches books for a room, inserts new book entries. Schema `reminded_me`, table `books`. |
| `BookData.cs` | Data classes: `BookEntry` (title, author, cover paths, response text/audio), `BookCatalog` |
| `RoomConfig.cs` | Resolves room ID from `?room=` URL param (WebGL) or `StreamingAssets/room_config.json` (Editor) |
| `HoverEffect.cs` | DOTween color fade on UI element hover (used on form buttons) |
| `SkyboxController.cs` | Cycles through skybox material phases with `Material.Lerp` |

### Bridge Architecture

```
SolidJS (browser)         Unity (WebGL)
─────────────────         ──────────────
window.__unity            WebGLBridge.cs
  .SendMessage()    ───→    .ReceiveOpenPanel()
                             .ReceiveClosePanel()
                             .ReceiveFetchCover()
                             .ReceiveDismissBook()
                             .ReceiveSubmit()

window.unityBridge    ←───  JS_Notify*() jslib calls
  .onStateChange()          V2UIBridge → WebGLBridge events
  .onBookOpen()
  .onCoverUrl()
  .onCoverLoading()
```

### Assets Layout

```
CornellBox/Assets/
  Scripts/              # V1 scripts (deprecated)
  Scripts/V2/           # Active C# MonoBehaviours
  Scenes/Scene.unity
  Audio/
    Voice/              # Per-book voice recordings (.mp3)
    SFX/                # beep, bgm, pageFlip, swoosh
  Animations/           # MainMenu fade animation + animator controller
  # Third-party assets below are in .gitignore (not tracked):
  3dizart Books Pack/   # Book 3D model + textures (third-party)
  FurnitureAssets/      # Room furniture FBX models and materials (third-party)
```

### Adding a New Book (curator)

1. Add an entry to `StreamingAssets/books.json` as a `BookEntry` JSON object
2. Place cover texture in `StreamingAssets/` (or use an existing material via `materialName`)
3. Place response image in `StreamingAssets/` (or use `Resources/`)
4. Place voice audio in `StreamingAssets/` (or use `Resources/`)
5. The `V2BookLoader` will pick it up on scene start and merge it with any Supabase visitor books

## Book cover fetching (`BookCoverService`)

`CornellBox/Assets/Scripts/BookCoverService.cs` fetches cover images for both the Canvas-based and SolidJS submission paths.

### Strategy (tried in order)

1. **In-memory ISBN cache** — normalized `"title|author"` key → first ISBN
2. **OpenLibrary search** → ISBN → `https://covers.openlibrary.org/b/isbn/{isbn}-L.jpg`
3. **OpenLibrary cover_i fallback** → `https://covers.openlibrary.org/b/id/{cover_i}-L.jpg` (if search returned a cover ID but no ISBN step succeeded)
4. **Old API fallback** → `https://bookcover.longitood.com/bookcover` (original API)
5. **Solid-color cover** — if all fail, caller generates via `BookCoverTextureComposer.GenerateSolidCover()`

### Key findings

**Search (`openlibrary.org/search.json`)**
- `UnityWebRequest` works on both Editor and WebGL — **use it everywhere**. No platform branching needed.
- `HttpWebRequest` (background thread) causes 403 in Unity Editor's Mono runtime. Setting `User-Agent` via either `req.UserAgent` property or `req.Headers["User-Agent"]` is blocked by Mono (the property silently fails; the Header collection throws `"must be modified using the appropriate property"`).
- Search requests `fields=isbn,cover_i` — both are returned in the same response.
- Verified books: "Scorched Earth" (ISBN `178478446X`, cover_i `14761400`), "Elite Capture" (ISBN `0745347851`, cover_i `12897594`).

**Cover download (`covers.openlibrary.org`)**
- Cover images exist and serve correctly (confirmed 200 OK from PowerShell with and without User-Agent).
- `UnityWebRequestTexture.GetTexture()` can fail in the **Editor** even though the URL is valid — likely a Unity Editor HTTP/TLS stack issue. Does **not** affect WebGL builds (browser handles networking).
- Not a User-Agent block — tested `UnityPlayer/6000.0.0` User-Agent and got 200 OK.
- The fallback chain (cover_i → old API → solid color) handles this gracefully.

## `ui/` architecture

See `ui/AGENTS.md` for full guidance. Summary:

- **Entrypoint**: `ui/src/entry.tsx` — mounts SolidJS, initializes Unity bridge
- **Unity bridge**: `ui/src/bridge/UnityBridge.ts` — `SendMessage` to Unity + `window.unityBridge` callbacks
- **State machine** (mirrors Unity C#): `BROWSING → IN → READING → OUT → SUBMITTING` in `ui/src/state/SceneState.ts`
- **Supabase**: `@supabase/supabase-js` client, schema `reminded_me`, table `books`. Room from `?room=` (default: `default`)
- **Submission flow**: SolidJS `SubmissionPanel` → Supabase insert → Unity `notifySpawn()`
- **i18n**: `en`/`ko`/`es` via `src/i18n/translations.json`
- **Dev mode**: Press `B` to simulate book open (only in Vite dev mode)

## TypeScript conventions (`ui/`)

- `verbatimModuleSyntax: true` — use `import type` for type-only imports
- `noUnusedLocals` / `noUnusedParameters`
- `erasableSyntaxOnly: true` — no enums, namespaces, or parameter properties
- `jsxImportSource: solid-js`
- These also apply to `archived/web/` and `archived/babylon/`

## What's missing

- No test framework in any project
- No linter (ESLint, Prettier, etc.)
- No CI workflow files

## TODOs

See [`TODO.md`](../TODO.md) for the full prioritized list of post-outdoor-installation refinements.

### Quick reference

| Priority | Items |
|----------|-------|
| **P1 — Bug fixes** | Cover carousel, tablet UI autocorrect, black flash, optional author, highlight timeout (15s), UI visibility |
| **P2 — Goals** | Larger cover + form redesign, distinguish author/contributor, prompt left sidebar, idle flourishes, beyond 8 books rotation |
