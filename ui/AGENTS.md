# Unity UI — Agent Guidance

Compact, repo-specific instructions for working with this Vite + Solid.js UI overlay.

## Stack

- **Solid.js 1.9** + **Vite 8** + **TypeScript 6**
- **Supabase** client SDK for backend
- No test framework, no ESLint, no Prettier configured
- i18n via JSON (en/ko/es)
- Plain CSS (co-located `.css` files, no framework)

## Commands

```bash
npm run dev     # Vite dev server
npm run build   # tsc -b && vite build (typecheck THEN bundle)
npm run copy    # Copy dist outputs to ../build/ui/ for Unity integration
```

**Verification**: `npm run build` is the only check — no tests or linters configured.

## Architecture

- **Single-page app overlaid on Unity WebGL canvas** (`#unity-canvas` in `index.html`)
- **Entry**: `src/entry.tsx` (has `/* @refresh reload */` for Solid HMR)
- **Unity bridge**: Bi-directional JS ↔ Unity via `window.__unity` and `window.unityBridge`
  - `src/bridge/UnityBridge.ts` — Unity calls JS callbacks; JS calls `SendMessage` to Unity
  - `src/state/SceneState.ts` — Custom EventTarget-based state machine: `BROWSING → IN → READING → OUT → SUBMITTING`
- **Room ID**: Read from `?room=` query param; defaults to `'default'` (`src/supabase/submitBook.ts:4`)
- **Supabase**: `src/supabase/client.ts` + `submitBook.ts` (schema `reminded_me`, table `books`)
- **UI components**: `src/ui/` — HUD, SubmissionPanel, BookResponsePanel (each with co-located CSS)

## TypeScript Quirks (TS 6)

- `erasableSyntaxOnly: true` — **no enums, no namespaces** (use const objects or unions)
- `verbatimModuleSyntax: true` — **must use `import type`** for type-only imports
- `noUnusedLocals` + `noUnusedParameters` enabled — will fail build on unused vars

## Dev Mode

- Press `B` key to simulate a book opening event (only in `import.meta.env.DEV` mode, see `App.tsx:39`)
- Vite dev server auto-reloads on changes

## i18n

- Translations in `src/i18n/translations.json`
- Supported: `en`, `ko`, `es`
- Language switching via HUD top-left and submission panel header

## Deploy

- `npm run copy` outputs to `../build/ui/bundle.js` + `bundle-entry.css`
- Unity project (parent directory) consumes these bundles
- Build emits ES modules (`format: 'es'` in `vite.config.ts`)

## Common Gotchas

- **Build order matters**: `tsc -b` runs **before** `vite build` (already in `package.json` script)
- **No HMR for Unity bridge**: Changes to `UnityBridge.ts` or state logic may require full page reload
- **Cover fetching**: `fetchCover()` resolves via Unity's `BookCoverService` (C# side) — returns `null` in dev mode without Unity
- **Supabase env**: `VITE_SUPABASE_URL` and `VITE_SUPABASE_ANON_KEY` must be set in `.env` (see `.env.example`)
