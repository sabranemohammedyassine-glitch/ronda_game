# Product 01 — Product Decisions (central register)

**Status:** PROPOSED · **Date:** 2026-11-17 · **Scope:** the Lobby / navigation milestone and the long-term online modes.
**This file does not replace** `docs/phase3/01_decision_register.md`. Existing decisions keep their IDs there (`D-P-`, `D-G-`, `D-S-`, `D-C-`, `D-U-`, `D-A-`); this file references them, records their **current status** after the MVP, and adds only what was missing:

- **`D-L-nn`**: Lobby, navigation, local profile and settings (new).
- **`D-O-nn`**: online multiplayer product decisions (new).

Detail lives in the companion documents:

- `02_lobby_specification.md`: what the lobby does;
- `03_lobby_implementation_plan.md`: how and in which order;
- `04_online_multiplayer_architecture.md`: the backend options and phases.

---

## 0. Status vocabulary

| Status | Meaning |
|---|---|
| **CONFIRMED** | The owner stated it in a brief or explicit answer (source given), or it is implemented and accepted. |
| **PROVISIONAL** | A default is chosen so work can proceed. Nothing has been approved yet, and the default can change cheaply. |
| **UNRESOLVED** | No decision yet. Options and a recommended default are given, and the owner's approval is required before it is relied on. |
| **SUPERSEDED (MVP)** | A Phase 3 proposal that the owner's MVP constraints replaced. The Phase 3 text remains as the long-term idea only. |

★ means the decision requires owner approval before implementation that depends on it starts.

**Note on Phase 3 statuses.** Every Phase 3 decision is still PROPOSED. The approval checklist in `docs/phase3/01_decision_register.md` §4 has no owner responses recorded. In this file, "PROVISIONAL" for a Phase 3 item means "proposed, never approved".

### 0.1 Owner statements used as sources

| Ref | Owner statement | Where |
|---|---|---|
| **OB-1** | MVP brief: keep Unity 2022.3, the Built-in RP, uGUI + TextMeshPro and legacy input. No backend, multiplayer, accounts, shops or leaderboards in the MVP. Original assets only. Don't invent uncertain rules. | MVP build brief; out-of-scope list recorded in `docs/mvp/04_gameplay_implementation_plan.md` §D |
| **OB-2** | M2 brief: Android Back opens "Quit Zellige?" / "Do you want to quit the game?" / Cancel / Quit. | `docs/mvp/04_gameplay_implementation_plan.md` M2 status; `Scripts/UI/QuitDialog.cs` |
| **OB-3** | Lobby brief (2026-11-17): the game must have a complete Lobby / Main Menu before gameplay, with profile, mode choice, settings, stats/history, rules/tutorial, about and exit. | this milestone |
| **OB-4** | Online clarification (2026-11-17): Me vs Computer is the local single-player mode. 1 vs 1 is online with two human players. 2 vs 2 is online with four human players in two teams. Online multiplayer and an authoritative backend are long-term scope. Me vs Computer must stay playable without a backend. No networking code yet. Local profile data must not automatically require an online account. | this milestone |
| **OB-5** | The repository owner performs all git commits and pushes; agents never do. | this milestone |

---

## 1. Inventory of existing decisions relevant to the Lobby

Implementation status is what the Unity project actually contains on 2026-11-17 (`ronda_game/Assets/Zellige`).

### 1.1 Product and platform

| ID | Topic | Existing decision / evidence | Source | Implementation | Status |
|---|---|---|---|---|---|
| D-P-01 | Name | Codename **Zellige**; final name later. | phase3/01 §1.1 | Codename used. **Finding:** `ProjectSettings.productName` is `Zllige_ronda_game` (typo); this is the name shown under the Android icon. See D-L-18. | PROVISIONAL |
| D-P-02 | Platform | Android phones, landscape, arm64 first. | phase3/01 §1.1 | Landscape-only, IL2CPP ARM64, minSdk 26, package `com.pyramidStudio`. | CONFIRMED (in use) |
| D-P-03 | Monetisation | Deferred. No ads in or right after a match; no real money; no loot boxes. | phase3/01 §3; phase3/02 §12 | None. | UNRESOLVED (constraints PROVISIONAL) |
| D-P-04 | Languages | EN, FR, AR (MSA) at M5; Darija and ES later; RTL from M2. | phase3/01 §1.1 | **English only**, hard-coded strings. The font (TMP LiberationSans SDF) has **no Arabic glyphs**. | PROVISIONAL, not started → see D-L-13 |
| D-P-05 | Safety and social | No free-text chat; stickers; mute/report/block; guest accounts limited. | phase3/01 §1.1; phase3/02 §9 | n/a (no online). | PROVISIONAL |
| D-P-06 | Licensing | Permissive licences only; owner approves every package. | phase3/01 §1.1 | Followed (no packages added). | CONFIRMED (standing rule) |
| D-P-07 | Team, skills, budget | Unknown. Decides the backend choice. | phase3/01 §3 | — | **UNRESOLVED ★** (blocks D-O-05) |

### 1.2 Modes and gameplay scope

| ID | Topic | Existing decision / evidence | Source | Implementation | Status |
|---|---|---|---|---|---|
| D-G-02 | Launch modes | Practice vs bots offline; online 1v1, then private and ranked, then 2v2. | phase3/01 §3; phase3/02 §5 | Only local **1 human vs Nadia** exists. | Mode *definitions* **CONFIRMED by OB-4** (see D-O-01); phasing PROVISIONAL |
| D-G-04 | Deal pattern | 1v1: 5 deals × 4. 2v2: 4, 3, 3. | phase3/03 RL-05 | 1v1 only. `RondaGame.PlayerCount = 2` (hard constraint). | 1v1 CONFIRMED (verified play); 2v2 PROVISIONAL (tutorial only) |
| D-G-17 | Turn timer, autopilot, forfeit | 20 s timer; autopilot; ranked forfeit after 8 auto-plays. | phase3/01 §3 | None (MVP has no timer). | UNRESOLVED ★ (online only) |
| D-G-18 | 2v2 seating | Partners opposite; shared pile; random seats; party of 2. | phase3/01 §1.2; RL-03 | None. | PROVISIONAL |
| D-G-21 | 2v2 Derba escalation | Out of v1. | phase3/01 §2 | — | UNRESOLVED (deferred) |
| MVP §D | MVP out of scope | 2v2, timers, networking, matchmaking, private tables, accounts, shops, leaderboards, analytics. | mvp/04 §D (OB-1) | Respected. | CONFIRMED for the MVP. OB-4 moves online modes into **long-term** scope; nothing is moved into the current milestone. |
| Catch | Catch rule | Not implemented; needs evidence and decisions D-1…D-4. | mvp/05 | Not implemented. | UNRESOLVED (separate track; affects Rules pages only) |

### 1.3 Online and backend

| ID | Topic | Existing decision / evidence | Source | Status |
|---|---|---|---|---|
| D-S-01 | Authority | Server-authoritative; clients send intents only. | phase3/01 §3 | **CONFIRMED by OB-4** ("Online modes must use an authoritative backend") |
| D-S-02 | Backend platform | Custom C#/.NET server recommended. UGS and Nakama are fallbacks, sensitive to D-P-07. | phase3/07 §3 | **UNRESOLVED ★**. Re-evaluated in product/04 (Nakama, Photon Fusion and UGS, as the owner asked); see D-O-05. |
| D-S-03 / D-S-13 | Hosting, region | Managed containers; one Western-Europe region. | phase3/07 §14 | PROVISIONAL |
| D-S-04 | Transport | WebSocket + MessagePack. | phase3/07 §5 | PROVISIONAL; depends on D-O-05 |
| D-S-05 | Identity | Guest-first; link Google/Apple; managed identity provider. | phase3/01 §3 | PROVISIONAL ★; refined by D-O-04 |
| D-S-06 | Persistence | PostgreSQL + Redis; per-match event log. | phase3/07 §9 | PROVISIONAL; depends on D-O-05 |
| D-S-07 | Matchmaking and rating | Rating queue, Glicko-2, tiers. | phase3/07 §6–7 | PROVISIONAL; ranked deferred (D-O-03) |
| D-S-08 | Bots | Server-side, labelled; never in ranked. | phase3/01 §3 | **UNRESOLVED ★**: OB-4 says online modes need human players → D-O-02 |
| D-S-09 | Reconnect | Resume token; 60 s grace. | phase3/07 §8 | PROVISIONAL |
| D-S-11 | Anti-abuse | Rate limits, validation, redaction, replay audit. | phase3/07 §11 | PROVISIONAL |
| D-S-12 | Observability | Logs, metrics, traces; opt-in analytics. | phase3/07 §12 | PROVISIONAL |

### 1.4 Client technology (Phase 3 vs the MVP)

| ID | Phase 3 proposal | What the MVP actually uses | Status |
|---|---|---|---|
| D-C-01 | Unity 6.3 LTS | **Unity 2022.3.62f3** | SUPERSEDED (MVP) by OB-1 |
| D-C-02 | URP 2D | **Built-in RP** | SUPERSEDED (MVP) by OB-1 |
| D-C-03 | UI Toolkit (spike-gated) | **uGUI + TextMeshPro**; Canvas Scaler 1920 × 1080, Expand | SUPERSEDED (MVP) by OB-1 |
| D-C-04 | Pure C# engine shared with the server | `Zellige.Core` asmdef, `noEngineReferences`, deterministic (golden master) | CONFIRMED (in use); key enabler for D-O-05 |
| D-C-05 | Events → choreographer → views | `RondaGame` returns event lists; `GameController` plays them through `GameScreen` coroutines | CONFIRMED (in use, simplified) |
| D-C-06 | Hand-written composition root; VContainer candidate | No DI. The owner asks for no unnecessary frameworks. | VContainer **rejected** for now; hand-written only |
| D-C-08 | Addressables | `Resources/` folder | SUPERSEDED (MVP) |
| D-C-09 | Unity Localization package | None | UNRESOLVED → D-L-13 |
| D-C-10 | Offline practice through the same gateway as online | `GameController` drives `RondaGame` directly; no gateway interface | PROVISIONAL; seam planned in product/04 phase O-3 |
| D-C-12 | Input System package | **Legacy input** (Android Back = `KeyCode.Escape`) | SUPERSEDED (MVP) by OB-1 |

### 1.5 UI/UX and assets

| ID / spec | Topic | Existing decision / evidence | Source | Implementation | Status |
|---|---|---|---|---|---|
| Doc 08 §4 | Information architecture | Boot → Home → Play sheet (1v1, 2v2, Practice, Private) / Profile / Ladder / Settings / Learn. **One primary Play button**, deliberately unlike the reference's four equal tiles. | phase3/08 §4, §6.2, §12 | None. The app boots straight into `Game.unity`. | PROVISIONAL; reconciled in D-L-03 |
| Doc 04 §8 | Client app FSM | Boot → Auth → Home → … → Result → Home | phase3/04 §8 | None | PROVISIONAL; the offline subset is adopted in D-L-04 |
| D-U-01 | Art direction | Direction A "Starlit riad rooftop": ink indigo, saffron, turquoise. | phase3/01 §3; phase3/08 §2 | De-facto MVP identity: navy `#0B1A3F`, gold `#E8BE5A`, orange `#E8842C`, ivory `#F6EFDF` (`UiKit.Palette`), plus the original `ui_*` sprites | **De facto in use; never formally approved** → D-L-17 |
| D-U-02 | Mascot | Original tea-pot guide "Atay". | phase3/01 | None | PROVISIONAL (not needed for the lobby) |
| D-U-04 | Card interaction | Two-step play with capture preview; "quick play" option. | phase3/01 §3 | Two-step implemented; no quick-play option | Two-step CONFIRMED (in use); quick play PROVISIONAL → D-L-13 |
| D-U-08 | Accessibility | Shape + colour suits, text scale, reduced motion, haptics. | phase3/08 §10 | Partial (original suits) | PROVISIONAL |
| D-U-09 | Onboarding | Interactive tutorial with "Learn the rules" and "Tour the app" paths. | phase3/01; phase3/02 §10 | None | PROVISIONAL; v1 scope in D-L-14 |
| D-U-10 | Post-match | Itemised breakdown; rematch; no interstitial. | phase3/01 | Result overlay: VICTORY/DEFEAT, final score, **Play again**; no Home button | Partly implemented → D-L-06 |
| Doc 08 §6.8 / §6.10 | Profile / Settings content | Avatar, name, level/XP, rating, stats, history / language, sound sliders, two-step vs quick, reduced motion, etc. | phase3/08 | Sound on/off button in the match top bar only; **not persisted** (no `PlayerPrefs` anywhere) | PROVISIONAL → D-L-10…D-L-13 |
| M2 Phase A | Android Back / quit | Back opens the "Quit Zellige?" dialog; Back again cancels. | OB-2; `QuitDialog.cs` | Implemented. 5 tests; Editor verified; **device test still pending** | CONFIRMED for the current single-scene app → D-L-08 |
| QA ME-06 / PM-14 | Restart | New match resets everything; clean restart. | mvp/02 | "New game" (top bar) and "Play again" (result) | CONFIRMED (tested) |
| VRO (reference) | Reference lobby patterns | Four mode tiles; profile card; settings (5 languages, one sound toggle, log out); profile modal with stats per mode and avatar grid; waiting room with a code; matchmaking queue; AFK dialog; result count-up; post-match ad | docs/04 §4–§11 | Research only (clean-room) | Evidence, not decisions |

---

## 2. New decisions (this milestone)

Each **UNRESOLVED ★** item has a full card in §3. Rows marked "→ spec" are detailed in `02_lobby_specification.md`.

### 2.1 Lobby and navigation (D-L)

| ID | Decision | Recommended default | Status |
|---|---|---|---|
| **D-L-01** | Launch destination | The app opens on the **Lobby (Home)**, not on the match. | **CONFIRMED** (OB-3); details PROVISIONAL |
| **D-L-02** | Visibility of the online modes before they exist | Show **1 vs 1** and **2 vs 2** as **locked "Online — coming later"** cards. Tapping one shows an information dialog and never starts network activity. | **UNRESOLVED ★** (§3.1) |
| **D-L-03** | Home structure | Home has one primary **Play** button that opens **Mode selection** (three cards: Me vs Computer, 1 vs 1 Online, 2 vs 2 Online), plus Profile, Stats, Rules, Settings, About and Exit. This keeps doc 08's "one primary action" and still lists the modes the owner named. | PROVISIONAL ★ (§3.2) |
| **D-L-04** | Scene structure | Two scenes: a new **`Lobby.unity`** (build index 0) and the existing **`Game.unity`** (index 1), loaded with single-scene loads. No bootstrap scene, no additive scenes. | PROVISIONAL ★ (§3.3) |
| **D-L-05** | Passing match settings to the game | A plain C# **`MatchRequest`** held by a static **`AppSession`**. `GameController` reads it at `Start`; without one (e.g. `Game.unity` opened directly in the Editor) it behaves exactly as today. | PROVISIONAL |
| **D-L-06** | After a match | The result overlay keeps **Play again** and adds **Lobby**. The match result is recorded to local stats once, when `MatchEndedEvent` is shown. | PROVISIONAL ★ (UI change to the match screen) |
| **D-L-07** | Leaving a match in progress | An in-match **Home** button and Android Back both open **"Leave the match?"** (Keep playing / Leave). Leaving returns to the Lobby; the match is recorded as *abandoned* (D-L-09). | **UNRESOLVED ★** (§3.4) |
| **D-L-08** | Android Back map | Lobby Home → "Quit Zellige?" (the existing dialog, OB-2). Any lobby sub-screen or dialog → close it / go back one level. Match → "Leave the match?" (D-L-07). Result overlay → Lobby. | **UNRESOLVED ★** (§3.4): changes the confirmed in-match behaviour of OB-2 |
| **D-L-09** | Interrupted Me vs Computer match (app killed, or left) | **v1: not resumable.** The match is lost; stats count it as *abandoned* (separate from losses). Resume via a replay log is a later option. | **UNRESOLVED ★** (§3.5) |
| **D-L-10** | Local profile | **Device-only profile, no account:** display name (1–16 characters, trimmed, no control characters, default "Player"), avatar from a bundled set, created on first launch without a blocking form. | PROVISIONAL → spec §5 |
| **D-L-11** | Local statistics and history | **Me vs Computer only:** played, won, lost, abandoned, win %, best winning margin, highest score; history of the **last 20** matches (date, mode, opponent, final score, result, decks played). Kept on the device; never uploaded; never mixed with future online stats. | PROVISIONAL → spec §6 |
| **D-L-12** | Progression (XP, levels, tiers) | **Not in the lobby v1.** GDD §8 places progression with the online product, where the server must own it. Offline XP can be farmed and would conflict later. | **UNRESOLVED ★** (§3.6) |
| **D-L-13** | Settings v1 | Sound effects on/off (existing toggle, now persisted and shared with the match); card play: two-step (default) or quick play (D-U-04 option); confirm before quitting (always on, not a setting); language shown as "English" only, until D-P-04 is planned. **No music setting** (there are no music assets). No AI difficulty (only one AI exists). | PROVISIONAL; language part **UNRESOLVED ★** (§3.7) |
| **D-L-14** | Rules and tutorial in v1 | A **static, paged "How to play"** written in our own words from the CONFIRMED rules in `mvp/01`. Provisional rules are labelled as house rules, and Catch is listed as "not in this version". The interactive tutorial (D-U-09) comes later. | PROVISIONAL ★ (owner reviews the text) |
| **D-L-15** | About / credits | Name, version (`Application.version`), "an original game", credits, third-party notices (TextMeshPro and font licence), and a clean-room statement. No external links in v1. | PROVISIONAL |
| **D-L-16** | Exit | An **Exit** entry on Home and Android Back on Home both open the existing quit dialog. | PROVISIONAL |
| **D-L-17** | Lobby visual direction | Reuse the in-game identity: the `UiKit.Palette` navy/gold/orange/ivory, the existing `ui_*` 9-slice sprites, TMP text with outline. New art (lobby background, mode icons, an avatar set of about 8) only through a separately approved asset request. | PROVISIONAL ★ (formal approval of D-U-01 in its MVP form) |
| **D-L-18** | Display name on the device | Fix `productName` "Zllige_ronda_game" to the owner's chosen display name (e.g. "Zellige"). Changing it changes the install label and the persistent-data path. | **UNRESOLVED ★** (owner picks the name; do it **before** any profile data exists) |
| **D-L-19** | Player names in the match | The HUD keeps "You" for the human; the profile name and avatar appear on the human's nameplate. The opponent stays **"Nadia"**. | PROVISIONAL |
| **D-L-20** | Rules preset for Me vs Computer | Always the default `RulesConfig` (the golden-master defaults). The lobby exposes **no rule options**, because those would change gameplay. | **CONFIRMED by constraint** ("do not change game rules") |

### 2.2 Online multiplayer (D-O). Long-term scope, nothing implemented now

| ID | Decision | Recommended default | Status |
|---|---|---|---|
| **D-O-01** | Mode definitions | Me vs Computer = local, offline. 1 vs 1 = online, 2 humans. 2 vs 2 = online, 4 humans, partners opposite. | **CONFIRMED** (OB-4) |
| **D-O-02** | Bots in online modes | No bot-fill of empty seats. A **labelled autopilot** plays only for a disconnected or idle human, until they return or the match ends (D-G-17 / D-S-08 narrowed). | **UNRESOLVED ★** (product/04 §7) |
| **D-O-03** | Ranked vs casual at launch | **Casual only** at the first online release. Ratings, tiers and leaderboards come later (D-S-07). | **UNRESOLVED ★** |
| **D-O-04** | Account model | Online play creates an **anonymous (guest) account** on first online use. Linking Google later. The local profile name and avatar are **copied** to the online profile. Local Me vs Computer stats stay local and are never uploaded. Me vs Computer never needs an account. | PROVISIONAL ★ (refines D-S-05) |
| **D-O-05** | Backend platform | Evaluate Nakama, Photon Fusion and UGS (plus the Phase 3 custom .NET baseline). **Recommended default: UGS (Authentication, Cloud Code C# modules running `Zellige.Core`, Lobby, Matchmaker, Cloud Save), gated by a spike; fallback: custom .NET with the same engine.** | **UNRESOLVED ★** (product/04 §4) |
| **D-O-06** | Private rooms and invitations | Host creates a room → short join code → shared through the **system share sheet**. Join by code. No friends list or in-app invitations in the first release. | PROVISIONAL |
| **D-O-07** | Public matchmaking | Quick match 1v1. 2v2: solo players are grouped into teams; parties of 2 come later. | PROVISIONAL |
| **D-O-08** | Disconnect and reconnect | Server-held match state; grace period (60 s, D-S-09); autopilot while away; resume by fetching the current per-seat view. | PROVISIONAL |
| **D-O-09** | Turn timer value | 20 s per turn (D-G-17). The announcement window becomes time-based online. | **UNRESOLVED ★** (gameplay-affecting; needs playtest) |
| **D-O-10** | 2v2 rules | RL-03, RL-05, RL-16 (side aggregation), RL-20 (side-level dealer rule); no Derba escalation. Requires engine generalisation from 2 to 4 seats. Several 2v2 rules are tutorial-only evidence. | **UNRESOLVED ★** (rules decisions before engine work) |
| **D-O-11** | Hosting, cost and region | One EU region; a written cost model before any account is created; nothing provisioned without approval. | **UNRESOLVED ★** (tied to D-P-07 and D-O-05) |
| **D-O-12** | Score integrity | Online clients send commands only; the server computes every score; hands and the deck never leave the server; local stats are never accepted as online data. | **CONFIRMED** (OB-4 + D-S-01) |
| **D-O-13** | Online stats and history | Owned by the backend; the client only caches them for display. | PROVISIONAL |
| **D-O-14** | Privacy and legal | Privacy policy, data export/deletion and consent before the first online data is stored. Counsel check of GDPR / Moroccan Law 09-08 (D-P-05). | **UNRESOLVED ★** |

---

## 3. Decision cards for the unresolved lobby items

Each card covers: what is missing · options · implications · recommendation · approval.

### 3.1 D-L-02: online modes before online exists
**Missing.** OB-3 lists 1 vs 1 and 2 vs 2 in the menu, and OB-4 says they are online. No backend exists, and none is approved.

**Options.**
- (a) Hide them until online works.
- (b) Show them locked, labelled "Online — coming later", with an info dialog.
- (c) Show them, and let them start a local placeholder such as hot-seat or vs bots.

**Implications.**
- (a) is honest but hides the product direction.
- (b) shows the roadmap and costs one dialog, but buttons that cannot be used can frustrate players in a public release.
- (c) contradicts OB-4: these modes are human online modes, and offering anything else under their name would mislead players.

**Recommendation.** (b) for internal and academic builds. Revisit (a) before any public store release.
**Approval:** owner ★.

### 3.2 D-L-03: Home layout
**Missing.** Doc 08 recommends one "Play" button plus a sheet. OB-3 lists the modes as menu items.

**Options.**
- (a) Play → Mode selection screen.
- (b) Three mode tiles directly on Home.

**Implications.**
- (b) mirrors the reference's tile menu, which doc 08 §12 deliberately avoids (clean-room differentiation). With two of the three tiles locked, it also makes Home look mostly unavailable.
- (a) costs one extra tap and gives a calmer Home.

**Recommendation.** (a).
**Approval:** owner ★.

### 3.3 D-L-04: separate Lobby scene or extend `Game.unity`
**Options.**
- (a) A new `Lobby.unity` plus the existing `Game.unity` (single loads).
- (b) Lobby panels added into `Game.unity`, with the match started or hidden on demand.
- (c) A bootstrap scene with additive Home and Match scenes (Phase 3 doc 06 §6).

**Implications.**
- (b) couples two screens in one scene. `GameUiBuilder` rebuilds `Game.unity`'s UI and would have to preserve lobby objects, and the match's `Start()` auto-starts a game. (b) also keeps match memory alive while in the lobby.
- (c) is built for online sessions and Addressables, which is premature here.
- (a) keeps `Game.unity` almost unchanged, keeps it playable on its own in the Editor, frees match memory when leaving (the post-match memory concern in phase3/06 G6), and needs no framework.

**Recommendation.** (a).
**Approval:** owner ★ (architecture).

### 3.4 D-L-07 / D-L-08: Back and leave inside a match
**Missing.** Today Back in a match offers to quit the app (OB-2, confirmed when the match was the whole app). With a lobby, quitting from mid-match skips the natural "go back" step.

**Options.**
- (a) Back in the match → "Leave the match?" → Lobby.
- (b) Keep "Quit Zellige?" in the match.
- (c) Back in the match does nothing, and leaving happens only through a Home button.

**Implications.**
- (a) matches Android conventions (Back goes up one level) and reuses the `QuitDialog` component with different text. It changes a confirmed behaviour, so the 5 `QuitDialogTests` keep their current meaning only for the Lobby Home.
- (b) means the user loses the match and the app in one step.
- (c) breaks the Android expectation that Back does something.

**Recommendation.** (a). The quit dialog moves to the Lobby Home. The existing tests stay valid; new tests cover "Leave the match?".
**Approval:** owner ★, because it changes OB-2.

### 3.5 D-L-09: interrupted Me vs Computer match
**Options.**
- (a) Discard, counted as *abandoned*.
- (b) Counted as a loss.
- (c) Resume: save the seed plus the ordered player commands and replay them through the deterministic engine. The golden master proves replay is exact.
- (d) Snapshot the full state.

**Implications.**
- (b) penalises app kills by the OS.
- (c) is cheap in the engine (no engine change, only a command log), but needs UI re-sync logic (rebuilding the screen from replayed events without animation) and a resume prompt.
- (d) needs engine serialisation, the same work online needs (product/04), so it is better done once there.

**Recommendation.** (a) in lobby v1; (c) as a later lobby phase.
**Approval:** owner ★ (affects stats meaning).

### 3.6 D-L-12: progression
**Options.**
- (a) None in v1.
- (b) Local XP and levels.
- (c) Wait for online progression.

**Implications.**
- (b) is farmable offline. Once online exists, the two progressions either conflict or the local one is thrown away, which feels like a loss to players.

**Recommendation.** (a), and design progression with the online product (c).
**Approval:** owner ★.

### 3.7 D-L-13: language
**Missing.** D-P-04 plans EN/FR/AR, but nothing is implemented. The current font has no Arabic glyphs. uGUI/TMP has no built-in Arabic shaping or RTL (phase3/01 D-C-03 facts), so Arabic needs an RTL solution and a font. That is an install and asset decision (D-P-06).

**Options.**
- (a) English only in lobby v1. Strings are kept in one table (a simple string catalogue) so they can be translated later.
- (b) Add French now (Latin script, no RTL; needs translated text).
- (c) Add Arabic now (needs an Arabic font plus an RTL/shaping solution and native review).

**Recommendation.** (a) with the string catalogue. Plan (b) and (c) as a dedicated localisation milestone.
**Approval:** owner ★.

---

## 4. Approval checklist (this milestone)

| # | ID | Question | Recommended |
|--:|---|---|---|
| 1 | D-L-02 | Online modes shown locked "coming later"? | Yes |
| 2 | D-L-03 | Home = Play → Mode selection? | Yes |
| 3 | D-L-04 | Separate `Lobby.unity`? | Yes |
| 4 | D-L-06 | Add a "Lobby" button to the result overlay? | Yes |
| 5 | D-L-07/08 | In a match, Back → "Leave the match?"; the quit dialog moves to Home? | Yes |
| 6 | D-L-09 | Interrupted matches are not resumable in v1, and count as *abandoned*? | Yes |
| 7 | D-L-12 | No XP/levels in lobby v1? | Yes |
| 8 | D-L-13 | English only for now, with a string catalogue? | Yes |
| 9 | D-L-14 | Static "How to play" pages; owner reviews the wording? | Yes |
| 10 | D-L-17 | Lobby reuses the current navy/gold identity; new art by separate request? | Yes |
| 11 | D-L-18 | Display name for the app (fixes "Zllige")? | Owner to name |
| 12 | D-P-07 | Team, skills, budget for online | Owner to answer |
| 13 | D-O-02, 03, 05, 09, 10, 11, 14 | Online product and backend decisions | See product/04; not needed before the lobby |

Items 1–11 gate lobby implementation. Items 12–13 gate only the online track.
