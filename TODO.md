# TODOs — Post–Outdoor Installation Refinements

Prioritized list based on observations from the first outdoor installation (Leia Lume Pad 2, Brave browser, max brightness). Bug fixes (P1) must be addressed before the next presentation; goals (P2) are aspirational enhancements.

---

## P1 — Bug Fixes

### 1. Cover Carousel
OpenLibrary returns the first result which may be a different-language edition (e.g. Spanish). Allow users to click through multiple covers.

**Approach:**
- `BookCoverService.cs`: New `FetchCovers()` method with OpenLibrary `limit=5`, parse all ISBNs/cover IDs, collect all successful cover URLs, fire new `OnCoverUrls` event with JSON array
- `V2UIBridge.cs`: Subscribe to `OnCoverUrls`, store first texture for spawning, pass all URLs to JS
- `WebGLBridge.cs` + `.jslib`: Add `JS_NotifyCoverUrls` jslib function
- `UnityBridge.ts`: Add `onCoverUrls` callback, parse JSON array, return from `fetchCover()`
- `SubmissionPanel.tsx`: Store `coverUrls[]` + `coverIndex`, show `<` `>` arrows on cover frame
- `SubmissionPanel.css`: Arrow button styles on cover frame

### 2. Hide Tablet UI
Word suggestions and autocorrect pop up during typing on tablet.

**Approach:**
- `index.html`: Add `<meta name="apple-mobile-web-app-capable" content="yes">`
- `SubmissionPanel.tsx`: Add `autocorrect="off"`, `autocapitalize="off"`, `spellcheck={false}`, `inputmode="text"` to all inputs (title, author, contributor, response textarea)

### 3. Black Flash on Button Press
Screen momentarily black when pressing buttons or tab — possibly a SolidJS re-render/layout issue.

**Approach:**
- `SubmissionPanel.css`: Add `will-change: transform, opacity` to `.panel-overlay` and `.panel`, add `backface-visibility: hidden`
- `App.tsx`: Guard against unnecessary state updates in state change handler
- **Diagnostic:** Log performance timestamps around `setPanelOpen` calls to identify layout thrash

### 4. Author Field Optional
Users don't always know the author's name or the correct spelling.

**Approach:**
- `SubmissionPanel.tsx`: Remove `!author().trim()` from `findCover()` guard and `validate()`. Remove author error state.
- `BookCoverService.cs`: Allow empty author in `FetchCover()` entry check. OpenLibrary already has title-only fallback.
- `V2UIBridge.cs`: Remove author validation in `FetchCover()`
- `WebGLBridge.cs`: Handle optional author in `ReceiveFetchCover()` JSON

### 5. Highlight Timeout (15s)
Books stay highlighted after touch/mouse interaction ends.

**Approach:**
- `V2BookInteraction.cs`: Add `static readonly List<V2BookInteraction> AllBooks` — register in `Awake()`, deregister in `OnDestroy()`. Add `static void DimAll()`.
- `V2PlayerController.cs`: Add `static float lastInteractionTime`. Track input in `Update()` (`Input.anyKeyDown`, `GetMouseButtonDown(0)`, `touchCount`). If `BROWSING` and idle > 15s, call `V2BookInteraction.DimAll()`.

### 6. UI Visibility — Larger Button + Skeuomorphism
Bottom-right "leave a book" button too subtle; text too small.

**Approach:**
- `HUD.css`: Increase `.leave-book-btn` font-size to `clamp(1.5rem, 2vw, 2.25rem)`. Add `background: rgba(32,28,21,0.5)` with `backdrop-filter: blur(4px)`, `border: 1px solid rgba(244,239,227,0.25)`, `border-radius: 8px`, `padding: 12px 20px`.
- `HUD.css`: Bump `.hud-title` font-size to `1.5rem`.
- `SubmissionPanel.css`: Bump base font sizes — `.field-label` to 0.75rem, `.field-input` to 1rem, `.find-cover-btn` to 0.75rem.

---

## P2 — Goals

### 1a. Larger Cover + Submission Form Redesign
Cover thumbnail too small to verify the right book was picked.

**Approach:**
- `SubmissionPanel.css`: Widen `.book-row` grid to `160px 1fr` (from `108px`). Enlarge `.cover-frame` to `130×185` (from `90×128`). Adjust responsive breakpoints.
- `SubmissionPanel.tsx`: Rearrange contributor below response, further from author field.

### 1b. Distinguish Author from Contributor
Author and contributor name fields look too similar.

**Approach:**
- `SubmissionPanel.css`: Add border-top + spacing above contributor section. Distinct label styling (italic, smaller, different color).
- `SubmissionPanel.tsx`: Move contributor below response textarea. Add section label "Your name (optional)".

### 2. Prompt Redesign — Left Side, Large Font, More Inspiration
Prompt tool went unused. Use left screen real estate for larger prompt display.

**Approach:**
- `SubmissionPanel.tsx`: Add `promptSidebar` div positioned on the left side of `.panel-overlay`. Shows current prompt in large text. Click inserts into response textarea.
- `SubmissionPanel.css`: Style left sidebar — `max-width: 40vw`, large serif font, poetic presentation.
- `writing_prompts.json`: Optionally expand with more "why you liked" / "why you'd recommend" prompts.

### 3. Visual Flourishes During Inactivity
Scene is too still when no one is interacting.

**Approach:**
- `V2BookDisplay.cs`: Add subtle idle "breathing" animation (scale/color pulse) for books in BROWSING state.
- `V2PlayerController.cs`: When idle > 30s, randomly glow/highlight a book to attract attention.
- `HUD.css`: Add slow pulsing animation (`breathPulse`) to `.leave-book-btn` during BROWSING.

### 4. Beyond 8 Books — Shelf Rotation
Arbitrary anchor-based cap. Shelf books could rotate onto the table.

**Approach:**
- `V2BookLoader.cs`: Increase `maxBooks` default from 5 to 10. Implement rotation coroutine: every 60s swap a table book with a pool reserve, animate the swap.
- Unity scene: Add 2–3 more anchor children under `V2BookLoader` GameObject on the shelf.

---

## File Change Summary

| Priority | File | Scope |
|----------|------|-------|
| P1-1 | `CornellBox/Assets/Scripts/BookCoverService.cs` | Multi-cover fetching, new event |
| P1-1 | `CornellBox/Assets/Scripts/V2/V2UIBridge.cs` | Multi-cover bridge |
| P1-1 | `CornellBox/Assets/Scripts/V2/WebGLBridge.cs` | New jslib call |
| P1-1 | `CornellBox/Assets/Plugins/WebGL/WebGLBridge.jslib` | New JS_NotifyCoverUrls |
| P1-1 | `ui/src/bridge/UnityBridge.ts` | onCoverUrls callback |
| P1-1 | `ui/src/ui/SubmissionPanel.tsx` | Cover carousel UI |
| P1-1 | `ui/src/ui/SubmissionPanel.css` | Arrow button styles |
| P1-2 | `ui/index.html` | Meta tag |
| P1-2 | `ui/src/ui/SubmissionPanel.tsx` | Input attrs |
| P1-3 | `ui/src/ui/SubmissionPanel.css` | will-change |
| P1-3 | `ui/src/App.tsx` | State guard |
| P1-4 | `ui/src/ui/SubmissionPanel.tsx` | Author optional |
| P1-4 | `CornellBox/Assets/Scripts/BookCoverService.cs` | Allow empty author |
| P1-4 | `CornellBox/Assets/Scripts/V2/V2UIBridge.cs` | Remove author validation |
| P1-4 | `CornellBox/Assets/Scripts/V2/WebGLBridge.cs` | Handle optional author |
| P1-5 | `CornellBox/Assets/Scripts/V2/V2BookInteraction.cs` | Static book registry + DimAll |
| P1-5 | `CornellBox/Assets/Scripts/V2/V2PlayerController.cs` | Inactivity timer |
| P1-6 | `ui/src/ui/HUD.css` | Larger button, skeuomorphism |
| P1-6 | `ui/src/ui/SubmissionPanel.css` | Larger fonts |
| P2-1a | `ui/src/ui/SubmissionPanel.css` | Larger cover frame |
| P2-1a | `ui/src/ui/SubmissionPanel.tsx` | Contributor placement |
| P2-1b | `ui/src/ui/SubmissionPanel.css` | Contributor visual distinction |
| P2-2 | `ui/src/ui/SubmissionPanel.tsx` | Prompt sidebar |
| P2-2 | `ui/src/ui/SubmissionPanel.css` | Sidebar styles |
| P2-2 | `ui/src/i18n/writing_prompts.json` | Optional expansion |
| P2-3 | `CornellBox/Assets/Scripts/V2/V2BookDisplay.cs` | Idle animation |
| P2-3 | `CornellBox/Assets/Scripts/V2/V2PlayerController.cs` | Attract attention timer |
| P2-3 | `ui/src/ui/HUD.css` | Button pulse animation |
| P2-4 | `CornellBox/Assets/Scripts/V2/V2BookLoader.cs` | Rotation, increased limit |
| P2-4 | Unity scene | Additional anchors |
