# 01 — Design Decision Register

**Status:** PROPOSED · **Date:** 2026-11-12 · Classification, gates and conventions: `00_README_structure.md` §2.
**How to read.** §1 = summary table of every decision. §2 = open questions with a recommended next step. §3 = full decision cards for every ★ item (context, options, recommendation, cost of reversal, needed-by). §4 = the approval checklist.
**Evidence.** VRO references point to Phase 2 (`02a` matrix IDs `R01…R32`, `U01…`, `M01…`, `T01…`; screenshots `Axxx`).
**Web-verified facts used (2026-11-12):** Unity 6.3 LTS released 2025-12-04 and supported until December 2027; Unity 6.0 LTS standard support ends October 2026; Unity Multiplay Game Server Hosting was deprecated at the end of March 2026 (Matchmaker, Relay and Distributed Authority continue); UGS Cloud Code C# modules run server-side .NET (up to .NET 9) but cannot use Unity runtime APIs; UI Toolkit's Advanced Text Generator supports Arabic RTL while uGUI/TextMeshPro are not covered; Nakama's core is Apache-2.0 with authoritative match handlers in Go/Lua/TypeScript; Colyseus is MIT (Node.js). Sources: doc 07 §2.

---

## 1. Summary table

Class: DD our design · PD provisional · OQ open question. Gate: ★ approval needed · ○ proceed unless objection. All statuses are **PROPOSED** (OQ: OPEN). "By" = milestone (doc 11).

### 1.1 Product and process

| ID | Decision | Class | Gate | Recommended default | By |
|---|---|---|---|---|---|
| D-P-01 | Product identity and naming | DD | ★ | Codename **Zellige**; final name later, must be clearly distinct from the reference title; trademark/store search before M6 | M0 / M6 |
| D-P-02 | Platform scope | DD | ★ | Android phones, landscape, arm64 first; iOS kept possible; tablets via scalable layout; no WebGL/PC in v1 | M0 |
| D-P-03 | Monetisation stance | OQ | ★ | Defer; hard constraints: no ads during or right after a match, no real-money stakes, cosmetics-only candidate | M5 |
| D-P-04 | Launch languages | DD | ○ | EN, FR, AR (MSA) at M5; Darija (ar-MA) and ES by M6; RTL from M2 | M2 |
| D-P-05 | Safety and social baseline | DD | ○ | No free-text chat; stickers; mute/report/block; guest accounts limited | M3 |
| D-P-06 | Third-party licensing policy | DD | ○ | Permissive licences only; provenance register; owner approves every package before install | M0 |
| D-P-07 | Team, skills and budget | OQ | ★ | Unknown; assume 2–5 people comfortable with C# | M0 |

### 1.2 Gameplay (rules detail: doc 03; flags: doc 03 §14)

| ID | Decision | Class | Gate | Recommended default | By |
|---|---|---|---|---|---|
| D-G-01 | Deck and ranks | DD (VRO R01, R02) | ○ | 40-card Spanish deck, ranks 1–7, 10–12, original art | M1 |
| D-G-02 | Launch modes | DD | ★ | 1v1 casual + ranked, 2v2 casual (ranked later), private tables, practice vs bots; spectators after beta | M3 |
| D-G-03 | Target score and end | DD (VRO R25) | ○ | 41; match ends the instant a side reaches it | M1 |
| D-G-04 | Deal pattern | DD (VRO R03, R04) | ○ | 1v1: 5 deals × 4; 2v2: 4, 3, 3 | M1 |
| D-G-05 | Dealer, first player, direction | PD | ○ | Dealer rotates each deck; first player = next after dealer; counter-clockwise | M1 |
| D-G-06 | Capture resolution and table model | DD (derived from VRO R07–R09) | ★ | Playing an equal rank always captures; at most one card per rank on the table | M1 |
| D-G-07 | Capture obligation | PD | ★ | Free play (no obligation to play a capturing card); flag `mustCapture` | M1 |
| D-G-08 | Chain | DD (VRO R02, R07) | ○ | Upward through the rank order, stops at the first missing rank, no wrap | M1 |
| D-G-09 | Missa | DD (VRO R12) | ○ | +1 for any capture that empties the table, including the final play of a deck | M1 |
| D-G-10 | Derba | PD | ★ | +1 when you capture, with the same rank, the card the previous player just dropped (with alternating 2v2 seating that is always an opponent); no 2v2 escalation in v1 | M1 |
| D-G-11 | Announcements (ronda/tringla) | PD | ★ | Sealed, 15 s window concurrent with play, resolved at close; contest per Tutorial text; side-level scoring; same-number tie voids | M1 |
| D-G-12 | Ronda point cap | DD (VRO R19, tutorial) | ○ | Ronda-class points cap a side at 40; tringla points are uncapped | M1 |
| D-G-13 | Catch | PD | ★ | +1; needs evidence (target has played that rank this deal); wrong guess locks you out until the next deal | M1 |
| D-G-14 | End-of-deck tally | DD (VRO R21–R23) | ○ | Leftover to last taker; dealer's side not last → +5 other side; +1 per card over 20 | M1 |
| D-G-15 | Dealer King/Ace exceptions | OQ | ○ | Disabled in v1 (flag) | M1 |
| D-G-16 | Both sides ≥ 41 in one tally | PD | ○ | Higher score wins; exact tie → one more deck | M1 |
| D-G-17 | Turn timer, timeout, forfeit | PD | ★ | 20 s turn timer; autopilot plays until "I'm back"; ranked forfeit after 8 consecutive auto-plays | M3 |
| D-G-18 | 2v2 teams and seating | PD | ○ | Partners opposite, shared pile, random seat assignment, party-of-2 option | M5 |
| D-G-19 | Score explanations | DD (VRO U07) | ○ | Every score change carries a reason code, an explainer bubble and a log entry | M2 |
| D-G-20 | Ruleset versioning and flags | DD | ○ | Versioned data-driven ruleset; every PD rule is a flag; the server pins the ruleset per match | M1 |
| D-G-21 | 2v2 derba escalation (1 → 5 → 10 per Tutorial) | OQ | ○ | Out of v1; optional variant after observation | after M5 |
| D-G-22 | Tutorial's same-number-tie derba clause | OQ | ○ | Not implemented in v1 | after M5 |

### 1.3 Server and multiplayer (detail: doc 07)

| ID | Decision | Class | Gate | Recommended default | By |
|---|---|---|---|---|---|
| D-S-01 | Authority model | DD (project rule) | ★ | Server-authoritative; clients send intents only | M0 |
| D-S-02 | Backend platform | PD | ★ | Custom C#/.NET authoritative game server hosting the shared rules assembly | M0 |
| D-S-03 | Hosting | PD | ★ | Containers on one managed cloud region; provider chosen before M3 | M3 |
| D-S-04 | Transport and serialisation | PD | ○ | WebSocket over TLS, binary MessagePack (JSON in dev) | M3 |
| D-S-05 | Identity and accounts | PD | ★ | Guest-first; link Google/Apple; managed identity provider; server verifies tokens | M3 |
| D-S-06 | Persistence | DD | ○ | PostgreSQL + Redis; per-match event log | M3 |
| D-S-07 | Matchmaking and rating | PD | ○ | Rating-based queue with expanding window; Glicko-2 style rating; tiers | M4 |
| D-S-08 | Bot policy | PD | ★ | Server-side bots; labelled; autopilot; casual-only queue fill; ranked never bot-filled | M3 |
| D-S-09 | Reconnect | DD | ○ | Resume token, 60 s grace (config), event replay or snapshot | M3 |
| D-S-10 | Provable shuffle | PD | ○ | Optional commit–reveal of the shuffle seed | M4 |
| D-S-11 | Anti-abuse baseline | DD | ○ | Rate limits, command validation, redaction, replay audit, collusion heuristics | M3 |
| D-S-12 | Observability and telemetry | DD | ○ | Structured logs, metrics, traces; client analytics opt-in | M3 |
| D-S-13 | Region and latency | PD | ○ | Single region (Western Europe) for MVP | M3 |

### 1.4 Client / Unity (detail: doc 06)

| ID | Decision | Class | Gate | Recommended default | By |
|---|---|---|---|---|---|
| D-C-01 | Unity version | PD | ★ | Unity 6.3 LTS (6000.3.x); Android module install needs approval | M0 |
| D-C-02 | Render pipeline | PD | ○ | URP with the 2D Renderer | M2 |
| D-C-03 | UI technology | PD | ★ | Spike-gated: UI Toolkit (+ ATG for RTL) for menus/HUD, 2D world objects for cards | M2 (SP-1) |
| D-C-04 | Core engine packaging | DD | ○ | Pure C# (.NET Standard 2.1), one shared source tree consumed by Unity and by the server | M0 |
| D-C-05 | Presentation architecture | DD | ○ | Server events → choreographer queue → views; light MVVM | M2 |
| D-C-06 | Composition / DI | PD | ○ | Hand-written composition root; VContainer (MIT) candidate | M2 |
| D-C-07 | Animation / tweening | PD | ○ | Built-in Awaitable + small in-house sequencer; PrimeTween (MIT) candidate | M2 |
| D-C-08 | Content delivery | DD | ○ | Addressables, local-first | M2 |
| D-C-09 | Localisation | DD | ○ | Unity Localization package; ICU plurals | M2 |
| D-C-10 | Offline practice mode | DD | ○ | Local authority host running the same engine + bots behind the same interface as the online path | M2 |
| D-C-11 | Android build settings | PD | ○ | arm64 IL2CPP; minSdk and device matrix fixed after SP-3 | M2 |
| D-C-12 | Input and accessibility plumbing | DD | ○ | Input System package; haptics and reduced-motion hooks | M2 |

### 1.5 UI/UX and assets (detail: docs 08, 09)

| ID | Decision | Class | Gate | Recommended default | By |
|---|---|---|---|---|---|
| D-U-01 | Art direction | DD | ★ | Direction A "Starlit riad rooftop" (3 options in §3) | M0 |
| D-U-02 | Mascot / guide | DD | ○ | Original mint-tea-pot guide (working name "Atay") | M2 |
| D-U-03 | Table layout | PD | ★ | 10-socket curved **rank ribbon** (one row), not the reference's two-row 5 × 2 grid | M2 |
| D-U-04 | Card interaction | PD | ★ | Two-step: tap lifts and previews the capture, second tap plays; "quick play" option | M2 |
| D-U-05 | Turn-timer UI | DD | ○ | Ring around the portrait + cue in the last 5 s | M2 |
| D-U-06 | Announcement and catch UX | DD | ○ | Contextual buttons; catch via portrait long-press and rank picker | M2 |
| D-U-07 | Social surface | DD | ○ | ≈ 24 stickers, mute, report | M3 |
| D-U-08 | Accessibility | DD | ○ | Shape+colour suits, text scale, reduced motion, haptics toggle, full RTL | M2 |
| D-U-09 | Onboarding | DD | ○ | Interactive tutorial: "Learn the rules" and "Tour the app" paths | M5 |
| D-U-10 | Post-match | DD | ○ | Itemised score breakdown, rematch; no forced interstitial | M2 |
| D-A-01 | Art sourcing and provenance | OQ | ★ | Commissioned/in-house original art; AI-generated imagery only for concept boards unless legal review clears it; provenance register | M0 |
| D-A-02 | Audio sourcing | PD | ○ | Original or licensed (CC0/commercial licence), recorded in the register | M5 |
| D-A-03 | Fonts | PD | ○ | OFL families for Latin and Arabic; licence verified at selection | M2 |
| D-A-04 | 2D animation tooling | PD | ○ | Unity 2D Animation + tweens; no paid middleware unless approved | M2 |

---

## 2. Open questions with a recommended next step

| ID | Question | Why it matters | Recommended next step |
|---|---|---|---|
| D-P-03 | Monetisation model | store policy, UX, data collection | Decide before beta; design hooks only (cosmetic catalogue) |
| D-P-07 | Team size, backend skills, budget | backend risk, roadmap length | Owner answers; recommendation direction unchanged |
| D-A-01 | Who produces the art; stance on AI-assisted assets | legal safety, cost, style | Owner decision; commission a style sheet first |
| D-G-15 | Dealer King/Ace exceptions | scoring edge case | Disable; observe the reference in a clean session if parity matters |
| D-G-21 | 2v2 derba escalation | 2v2 scoring depth | Out of v1; evaluate after M5 playtests |
| D-G-22 | Same-number tie clause | scoring edge case | Not implemented; same note |
| OQ-R1 | Do we want **reference parity** on UNKNOWN mechanics? | decides whether the clean observation session (02a §13) is run | If no: ignore the session. If yes: run items 1–6 before M1 closes |

---

## 3. Decision cards for every ★ item

Each card: context · options · recommendation · cost of reversal (L/M/H) · needed-by.

### D-P-01 Product identity and naming
**Context.** A distinct identity is required for legal and clean-room reasons (the reference name and mascot must not be echoed). **Options.** (a) Codename **Zellige** now, marketing name later; (b) pick a final name now; (c) a descriptive name ("Ronda …"). **Recommendation.** (a); the generic word *Ronda* is the name of a traditional game and is fine as a *descriptor*, not as the brand. Search trademarks and app stores before M6. **Reversal:** L. **By:** M0 (codename), M6 (name).

### D-P-02 Platform scope
**Options.** (a) Android only v1; (b) Android + iOS together; (c) include WebGL. **Recommendation.** (a) with iOS kept possible (no Android-only APIs in Core, UI or protocol; avoid per-platform forks); the test phone and Phase 2 evidence are Android. WebGL is rejected: low-end memory, WebSocket/Arabic text issues, and ad/billing constraints are unknown. **Reversal:** M (iOS needs a Mac build pipeline and store accounts). **By:** M0.

### D-P-03 Monetisation stance (OQ)
**Options.** (a) Premium paid; (b) free with cosmetic IAP; (c) free with ads; (d) none for v1. **Recommendation.** decide before M5; design constraints regardless: no interstitial inside or immediately after a match flow, no real-money wagering, no loot boxes. Phase 2 observed a full-screen video ad after the result (reference, one observation) — we treat that as a pattern to avoid. **Reversal:** H if ads/IAP are bolted on late (consent flows, SDKs, UX). **By:** M5.

### D-P-07 Team, skills and budget (OQ)
**Context.** Unknown. **Impact.** A small C# team favours the custom .NET server. If there is **no capacity to run servers** or speed matters most, the weighted comparison flips to UGS-centric (3.85) or Nakama (3.75) versus custom .NET (3.35) (doc 07 §3.3); a Go/TypeScript team could also prefer Nakama. **Ask.** Team size, who writes the server, monthly infra budget. **By:** M0.

### D-G-02 Launch modes
**Options.** (a) 1v1 only at launch, 2v2 later; (b) 1v1 + 2v2 at launch; (c) 1v1 + 2v2 + private + practice. **Recommendation.** engine supports 1–4 players from M1; **online** 1v1 at M3, private tables and ranked at M4, **2v2 at M5**; practice vs bots offline from M2 (it is also our test harness). **Reversal:** M. **By:** M3.

### D-G-06 Capture resolution and table model
**Context.** VRO: equal-rank take plus chain (R07), rank-addressed 10-slot layout on the reference table (R09), non-matching card stays (R08); Phase 2 did **not** establish whether a matching card may be dropped (R10, R11 UNKNOWN). **Options.** (a) **Automatic capture**: playing a card whose rank is on the table always captures; the table holds ≤ 1 card per rank. (b) **Optional capture**: the player may drop a matching card (requires a "capture or drop" choice and allows duplicates on the table). **Recommendation.** (a): no extra UI choice, a small state space (10 ranks), and the rank-addressed layout becomes a *consequence*, not a convention. **Cost of reversal:** H (state model, UI, bots, tests). **By:** M1.

### D-G-07 Capture obligation
**Context.** Whether a player holding a capturing card must play one is UNKNOWN (E3 invalid). **Options.** (a) **Free play**: any card may be played. (b) **Must capture**: if any hand card captures, only capturing cards are legal. **Implications.** (a) simpler UI, richer bluffing/avoidance (e.g. not feeding a derba), more state for AI; (b) fewer legal moves, hints trivial, UI must disable cards, stronger tempo. **Recommendation.** (a) as default; implement as flag `mustCapture` so both can be playtested; reconsider after M2 playtests. **Reversal:** L (flag) provided both are tested from M1. **By:** M1.

### D-G-10 Derba
**Context.** VRO: Derba = take, right away, the card the previous player just played, with an equal rank, +1 (tutorial R13; call-out A027). 2v2 escalation 1 → 5 → 10 is tutorial text only. **Options.** (a) Derba on the previous play only, no escalation; (b) (a) plus the Tutorial-style 2v2 escalation (1 → 5 → 10, mechanics unclear); (c) a longer window (any card dropped earlier). **Recommendation.** (a): one sentence to explain, immediate, and with alternating 2v2 seating (0 → 1 → 2 → 3, partners opposite) the previous player is always an opponent, so no "partner farming" case exists. Escalation is D-G-21. **Reversal:** L (flag `derbaEscalation`). **By:** M1.

### D-G-11 Announcements
**Context.** VRO (tutorial): ronda +1, two rondas +2, tringla 5, contest rules, announce before playing (R14, R17, R18); the reference shows a ≈ 15–16 s countdown at every deal (R15, A029/A037/A041) and a reveal bubble after it closes (003); credit timing UNKNOWN (R16). Four of a kind, 2v2 resolution and same-number ties UNKNOWN. **Our rule (doc 03 RL-14–RL-17).** Announcements are sealed; the window is 15 s and runs concurrently with play; eligibility is lost as soon as a card of the announced rank is played; at close all announcements are revealed and resolved once. **Options for credit timing.** (a) at window close (recommended: contest needs all announcements); (b) immediately at announcement (breaks the contest rule). **Options for ties.** (a) tie voids both; (b) implement the tutorial's derba clause (D-G-22). **Reversal:** M. **By:** M1.

### D-G-13 Catch
**Context.** VRO (tutorial): target icon + number picker, +1, wrong guess blocks you until the next round (R20). Live play not observed. **Options.** (a) No evidence requirement (blind guesses allowed); (b) evidence requirement (the target has played ≥ 1 card of that rank in the current deal). **Recommendation.** (b): with (a) a blind guess is a nearly free lottery ticket; lockout until the **next deal** (alternative: next deck). Catch value +1 for pairs; hidden tringla value +1 by default (alternative: 5). **Reversal:** L (flags). **By:** M1.

### D-G-17 Turn timer, timeout and forfeit
**Context.** VRO: a missed turn leads to bots playing for the player behind a "You are AFK / I'm back" dialog (R30); the timer length is UNKNOWN (≤ 20.4 s bound, INFERRED; contaminated evidence). **Options.** (a) autopilot (reference-like); (b) skip the turn; (c) forfeit on first timeout. **Recommendation.** (a): 20 s per turn (casual private tables configurable 15–60 s), autopilot plays a legal card with a basic policy, the seat is *Away* until the player returns; **ranked** forfeits after 8 consecutive auto-plays; casual never forfeits. Visible countdown ring (D-U-05). **Reversal:** L (config). **By:** M3.

### D-S-01 Authority model
**Recommendation.** Server-authoritative; clients send **intents** (play card, announce, catch, return, leave); the server validates, mutates state, emits events; hidden information (other hands, undealt deck) never leaves the server. Confirms the standing project rule. **Reversal:** H. **By:** M0.

### D-S-02 Backend platform
Options and weighted comparison: doc 07 §3. **Recommendation.** **Custom C#/.NET authoritative game server** (base-case score 3.95 vs 3.70 Nakama and 3.65 UGS; **sensitive to D-P-07**, doc 07 §3.3) using the same rules assembly as the client, WebSocket transport, PostgreSQL + Redis, a managed identity provider; build only what differentiates (rules, matches, bots, rating), rent commodity services (identity, push, crash). **Why not the others (summary).** UGS-centric: Cloud Code C# modules *can* host the rules but are request/response, real-time push to opponents is unproven for this design, and Multiplay Hosting was deprecated in 2026; Nakama/Colyseus: rules would be re-implemented in Go/TS (two sources of truth); Photon-style host/relay: not authoritative for hidden information; Firebase-only: awkward for hidden state and timers. **Reversal:** H after M3. **By:** M0.

### D-S-03 Hosting
**Options.** (a) managed containers (a PaaS or managed Kubernetes); (b) VMs; (c) game-server orchestration product. **Recommendation.** (a) in one Western-Europe region for MVP; the owner chooses a provider before M3 (budget, payment methods, latency to Morocco/Europe); nothing is created without approval. **Reversal:** M. **By:** M3.

### D-S-05 Identity and accounts
**Options.** (a) own username/password; (b) managed identity provider (Firebase Auth or UGS Authentication) with guest + Google/Apple linking; (c) device-only identity. **Recommendation.** (b); the server validates provider tokens and maps them to its own `PlayerId`; guest accounts can play casual and be upgraded; ranked requires a linked account. **Reversal:** M. **By:** M3.

### D-S-08 Bot policy
**Context.** VRO: bots act for an AFK player (R30); whether public opponents are bots is UNKNOWN (M02). **Recommendation.** Bots are server-side, labelled in the UI, used for (1) autopilot, (2) practice, (3) casual queue fill after 12 s, (4) tests; **never** in ranked; difficulty tiers Easy/Normal/Hard; bots see only their own hand and public information. **Reversal:** L. **By:** M3.

### D-C-01 Unity version
**Facts.** Unity 6.3 LTS supported until December 2027; 6.0 LTS standard support ends October 2026; the installed editor is 6000.6.4f1 (a newer *Supported Update* release) with no Android module (Phase 1 report `docs/01_environment_report.md`). **Options.** (a) **6.3 LTS** (6000.3.x); (b) the installed 6000.6.4f1; (c) 6.0 LTS. **Recommendation.** (a): LTS stability for a multi-month project; installing the editor and the Android module is a system change that needs the owner's approval. **Reversal:** M. **By:** M0.

### D-C-03 UI technology
**Facts.** UI Toolkit's Advanced Text Generator supports Arabic shaping and RTL; uGUI/TextMeshPro is not covered by that support; some rich-text tags (e.g. `<sprite>`) are not yet supported. **Options.** (a) UI Toolkit for menus/HUD + 2D world objects for cards; (b) uGUI + TextMeshPro + an RTL plugin; (c) UI Toolkit for everything. **Recommendation.** (a), **gated by spike SP-1** (render a localised screen in EN/FR/AR with mixed digits and a card hand on a low-end device). If SP-1 fails, fall back to (b). **Reversal:** H after M2. **By:** M2.

### D-U-01 Art direction
Three original options (detail in doc 08 §2): **A. Starlit riad rooftop** — night sky, lanterns, ink-blue felt with saffron/turquoise accents; **B. Zellige workshop** — bright, tile-pattern-led, daylight; **C. Souk lantern night** — warm amber, dense textures. **Recommendation.** A: strongest contrast for cards and for reduced eye strain; clearly different from the reference's café-courtyard + teal-felt look. The owner picks one before any art is commissioned. **Reversal:** H after art starts. **By:** M0.

### D-U-03 Table layout
**Options.** (a) 10-socket **curved rank ribbon**, one row; (b) two rows of five (the reference's arrangement); (c) free-form card pile. **Recommendation.** (a): preserves the useful property (the rank order, and the 7 → 10 jump, is visible and each rank has one place) while being visibly our own composition; (b) is rejected as too close to the reference; (c) hides the chain mechanic. Depends on D-G-06(a). **Reversal:** M. **By:** M2.

### D-U-04 Card interaction
**Context.** The reference plays a card with a single tap (R06, tutorial). **Options.** (a) single tap plays; (b) two-step: tap lifts the card and previews what it would capture, a second tap (or swipe-up) plays; (c) drag to the table. **Recommendation.** (b) default with a "quick play" setting (a); mis-taps cost a card with a 20 s timer and a server-authoritative game, and the preview teaches the chain rule. **Reversal:** L. **By:** M2.

### D-A-01 Art sourcing and provenance (OQ)
**Options.** (a) commissioned original art; (b) in-house; (c) licensed asset packs; (d) AI-generated. **Recommendation.** (a)/(b) for everything shipped; (c) only with licences recorded; (d) for mood boards only unless counsel clears the output's rights and style similarity to the reference; every shipped asset has a row in the provenance register (doc 09 §8). **Reversal:** H. **By:** M0.

---

## 4. Approval checklist (★ items)

Mark each Approve / Revise (with the change) / Reject. Recommended defaults are in §1.

| # | ID | Decision | Default | Owner response |
|--:|---|---|---|---|
| 1 | D-P-01 | Codename Zellige, name later | approve | |
| 2 | D-P-02 | Android-first, iOS possible | approve | |
| 3 | D-P-03 | Monetisation deferred, constraints fixed | approve | |
| 4 | D-P-07 | Team / skills / budget — **answer needed** | — | |
| 5 | D-G-02 | Modes: 1v1 → private/ranked → 2v2, practice | approve | |
| 6 | D-G-06 | Automatic capture, ≤ 1 card per rank | approve | |
| 7 | D-G-07 | Free play (flag for mandatory capture) | approve | |
| 8 | D-G-10 | Derba on opponent's drop, +1, no escalation | approve | |
| 9 | D-G-11 | Sealed 15 s announcements, resolved at close | approve | |
| 10 | D-G-13 | Catch with evidence rule, lockout to next deal | approve | |
| 11 | D-G-17 | 20 s timer, autopilot, ranked forfeit after 8 | approve | |
| 12 | D-S-01 | Server-authoritative | approve | |
| 13 | D-S-02 | Custom .NET authoritative server | approve | |
| 14 | D-S-03 | Managed containers, one EU region (provider TBD) | approve | |
| 15 | D-S-05 | Guest-first + managed identity provider | approve | |
| 16 | D-S-08 | Server-side labelled bots, never in ranked | approve | |
| 17 | D-C-01 | Unity 6.3 LTS | approve | |
| 18 | D-C-03 | UI Toolkit hybrid, gated by spike SP-1 | approve | |
| 19 | D-U-01 | Art direction A | approve / pick B or C | |
| 20 | D-U-03 | Curved rank ribbon | approve | |
| 21 | D-U-04 | Two-step card play, quick-play option | approve | |
| 22 | D-A-01 | Original commissioned/in-house art; AI for mood only | approve | |

Plus **OQ-R1**: do we want reference parity on UNKNOWN mechanics (decides whether the clean observation session of `02a` §13 is run)?
