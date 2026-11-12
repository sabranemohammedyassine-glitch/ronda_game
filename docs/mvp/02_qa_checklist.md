# MVP 02 — QA Checklist (Zellige, local 1v1 vs AI)

**Owner:** Agent 5 (QA / code review) · **Date:** 2026-11-12 · **Status:** prepared before any code exists.
**Rule source:** `docs/phase3/03_rules_and_scoring_spec.md` (ruleset R1 v0.1, `RL-nn`, examples `E1–E24`). If `docs/mvp/01_verified_rules_reference.md` disagrees with a value here, that file wins and the row must be updated.
**Status column:** **V** = verified reference observation (VRO: tutorial and/or live play) · **P** = provisional (PD / DD / INFERRED) → the value MUST come from `RulesConfig`, and the test MUST read the expected value from the config or be parameterised by it · **?** = expected value blocked by an ambiguity in §7.
**Notation:** table = set of ranks (suits never matter for capture); `H` = human (seat 0), `A` = AI (seat 1); pile = cards captured this deck round.

---

## 1. Unit-test matrix — rules engine (`Zellige.Tests.EditMode`, Core only)

### 1.1 Deck, shuffle, deal

| ID | Setup | Expected | Rule | St. |
|---|---|---|---|---|
| DK-01 | Build a fresh deck | 40 cards, all (suit, rank) pairs unique | RL-01 | V |
| DK-02 | Fresh deck | 4 suits × ranks {1,2,3,4,5,6,7,10,11,12}; no 8 or 9; 4 cards per rank | RL-01 | V |
| DK-03 | Rank order helper | `next(1..6)=+1`, `next(7)=10`, `next(10)=11`, `next(11)=12`, `next(12)` = none | RL-09 | V (no wrap: P) |
| SH-01 | Shuffle with any seed | Result is a permutation of the 40 cards (same multiset, no duplicates) | RL-06a | V |
| SH-02 | Shuffle twice with the same seed | Identical order | determinism | P |
| SH-03 | Shuffle with seeds 1 and 2 | Different orders; input deck instance not mutated unexpectedly | determinism | P |
| SH-04 | Fixed seed → golden permutation vector | Matches the stored vector | determinism | ? (A-12) |
| DL-01 | Start deck round, first deal | Each hand 4, table 0, stock 32, piles 0/0 | RL-05, RL-06 | V (empty table: P, A-10) |
| DL-02 | Play a full deck round | Stock after each deal = 32, 24, 16, 8, 0; exactly 5 deals; a deal starts only when both hands are empty | RL-05 | V |
| DL-03 | Table {4, 11} when hands empty, stock > 0 | Next deal keeps table {4, 11} | RL-06 | P |
| DL-04 | Dealer = H | A plays first in every deal of that deck round | RL-06b | P |
| DL-05 | Deck round ends, no winner | Dealer switches seat; piles reset to 0/0; scores kept; table empty | RL-04, RL-22 | P |
| DL-06 | After every applied action in a full seeded match | `hands + table + piles + stock = 40`, no card in two zones | conservation | V |
| DL-07 | After every applied action | At most one card per rank on the table | RL-08a | P |

### 1.2 Capture, chain, drop

| ID | Setup | Expected | Rule | St. |
|---|---|---|---|---|
| CP-01 | Table {4,7,11}; play 7 | Captures 7; pile +2; table {4,11} | RL-08 (E1) | V |
| CP-02 | Table {3,4,5,10}; play 3 | Captures 3,4,5; pile +4; table {10} | RL-09 (E2) | V |
| CP-03 | Table {2,7,10,11}; play 7 | Captures 7,10,11 (7→10 jump); pile +4; table {2} | RL-09 (E3) | V |
| CP-04 | Table {3,4,6}; play 3 | Captures 3,4; stops at missing 5; table {6} | RL-09 | V |
| CP-05 | Table {5,6}; play 6 | Captures 6 only (chain is upward only); table {5} | RL-09 | V |
| CP-06 | Table {1,6,7,10,12}; play 6 | Captures 6,7,10; stops at missing 11; pile +4; table {1,12} | RL-09 | V |
| CP-07 | Table {1,12}; play 12 | Captures 12 only; pile +2; table {1} (no wrap 12→1) | RL-09, `chainWrap=false` | P |
| CP-08 | Table {4,7}; play 2 | Drop; table {2,4,7}; piles and scores unchanged | RL-10 (E5) | V |
| CP-09 | Table empty; play 5 | Drop; table {5}; no Missa | RL-10, RL-12 | V |
| CP-10 | Table {5}; hand has 5 and 2; play 2 | Legal with `mustCapture=false`; drop | RL-11 | P |
| CP-11 | Same as CP-10 with `mustCapture=true` | Play 2 rejected; play 5 accepted | RL-11 | P |
| CP-12 | Table {7}; play 7 | Capture is automatic (no decline option in the API) | RL-08 | P |

### 1.3 Missa and Derba

| ID | Setup | Expected | Rule | St. |
|---|---|---|---|---|
| MS-01 | Table {11,12}; play 11 | Captures both; table empty; Missa +1 to capturer | RL-12 (E4) | V |
| MS-02 | Table {4,11}; play 11 | Capture; table {4}; no Missa | RL-12 | V |
| MS-03 | Last play of the deck round empties the table | Missa +1, then tally with no leftover | `missaOnFinalPlay=true` | P |
| DB-01 | Previous play: A dropped 10 → table {1,10}; H plays 10 | Capture; Derba +1 to H; table {1} | RL-13 (E6) | V |
| DB-02 | A dropped 10; H dropped 3; A dropped 5; H plays 10 | Capture; no Derba | RL-13 (E7) | V |
| DB-03 | Table {10} from A's drop just before; H plays 10 | Derba +1 and Missa +1 = +2 | RL-12, 13 (E8) | V (stacking: P) |
| DB-04 | A just dropped 10 → table {1,10,11}; H plays 10 | Captures 10,11; Derba +1 only (chain adds no extra point); table {1} | RL-13 | P |
| DB-05 | Dealer's last play of deal *k* is a drop of 5; first play of deal *k+1* is a 5 | Derba +1 | §12 edge case | P (A-4) |
| DB-06 | First play of a deck round | Never a Derba (no previous play; table empty) | §12 | P |
| DB-07 | H at 40; H makes a Derba | Score 41; match ends immediately; remaining cards are not played | RL-23a | V |

### 1.4 Ronda / Tringla (announcements)

| ID | Setup | Expected | Rule | St. |
|---|---|---|---|---|
| RN-01 | Only H announces ronda | H +1 | RL-16 (E12) | V |
| RN-02 | H ronda(7), A ronda(4) | Higher wins: H +2 (1 own + 1 per opposing announcement); A 0 | RL-16 (E10) | V |
| RN-03 | H ronda(5), A tringla(3) | A +6 (5 + 1); H 0 | RL-16 (E9) | V |
| RN-04 | H tringla(3), A tringla(6) | Lower tringla wins: H +6; A 0 | RL-16 (E11) | V |
| RN-05 | Both ronda(5) | Tie: nobody scores | `announceTie=void` | P (A-2) |
| RN-06 | H rondas (3) and (7); A ronda(5) | H's best (7) wins: 2 + 1 = +3 | RL-14, 16 (E14 adapted) | V |
| RN-07 | H rondas (2) and (4); A ronda(6) | A wins: own 1 + 2 opposing = +3; H 0 | RL-16 | V |
| RN-08 | H holds four 6s | Counts as one tringla, +5 | `quadAsTringla=true` | P |
| RN-09 | H ronda(10) vs A ronda(7) | H wins (10 ranks above 7 in rank order) | RL-16 | P (INFERRED) |
| RN-10 | Announce a rank with fewer than 2 cards in hand | Rejected; state unchanged | RL-14 | V |
| RN-11 | H plays a 5 before announcing pair of 5s | Announcement forfeited / later announce rejected | RL-14 | ? (A-1) |
| RN-12 | H at 38 wins a ronda contest worth 3 | Granted 2 → 40; match continues; ledger keeps requested 3 / granted 2 | RL-17 (E16) | V (tutorial) |
| RN-13 | H at 40, sole ronda | Granted 0; stays 40 | RL-17 | V (tutorial) |
| RN-14 | H at 38 wins a tringla award of 5 | 43; match ends | RL-17, 23 (E17) | V (tutorial) |
| RN-15 | H at 39 wins with tringla + ronda (own 6) | Uncapped (award includes a tringla): 45; match ends | RL-17 | P (A-3) |
| RN-16 | Moment the announcement points are applied | Per `RulesConfig` (start of deal vs. window close) | RL-15 | ? (A-1) |

### 1.5 End of deck round, match end

| ID | Setup | Expected | Rule | St. |
|---|---|---|---|---|
| ED-01 | Last card played; table {5,12}; last capture by H | Both cards go to H's pile; table empty; piles sum to 40 | RL-19 | V |
| ED-02 | Dealer H; final piles H 8 / A 32; last capture by A | A +5 (dealer rule) and +12 (over 20) = +17; H 0 | RL-20, 21 (E21) | V |
| ED-03 | Dealer H; H made last capture; H pile 23 + 3 leftover = 26; A 14 | H +6; A 0; no +5 | RL-19–22 (E22) | V (decomposition INFERRED) |
| ED-04 | Piles 20 / 20; dealer made last capture | Nobody scores at tally | RL-21 | V |
| ED-05 | Piles 20 / 20; non-dealer made last capture | Non-dealer +5 only | RL-20 | V |
| ED-06 | Dealer H; H pile 30; last capture by A (A pile 10) | H +10; A +5 | RL-20, 21 | V |
| ED-07 | Leftover moves a pile from 20 to 22 | Over-20 counted after leftover: +2 | RL-22 order | P |
| ED-08 | No capture at all in a (hand-built) deck round | Leftover discarded; +5 behaviour per config | RL-19, 20 | ? (A-5) |
| ME-01 | Score reaches exactly 41 by Missa mid-deal | Match ended, winner set, `MatchEnded` raised once | RL-02, 23a | V |
| ME-02 | Tally lifts H from 35 to 43, A stays 30 | H wins after the full tally | RL-23b | V |
| ME-03 | Tally: H 38 +3 = 41; A 38 +5 = 43 | Both ≥ 41 → higher wins: A | `bothReachTarget` (E23) | P |
| ME-04 | Tally leaves 41 / 41 | No winner; one more deck round, dealer rotates | RL-23b (E24) | P |
| ME-05 | `targetScore` set to 21 in config | Match ends at 21; ronda cap becomes 20 | RL-02, 17 | P |
| ME-06 | Restart / new match | Scores 0/0, piles 0/0, table empty, fresh 40-card deck, ledger empty, new seed honoured | — | V |

### 1.6 Illegal actions (each: rejected with a reason, state hash unchanged, no events)

| ID | Setup | Expected | Rule | St. |
|---|---|---|---|---|
| IL-01 | H plays while it is A's turn | Rejected `NotYourTurn` | RL-07 | V |
| IL-02 | H plays a card not in H's hand (in A's hand / on table / in stock) | Rejected `CardNotInHand` | RL-07 | V |
| IL-03 | H plays the same card twice | Second call rejected | RL-07 | V |
| IL-04 | Any play after match end | Rejected `MatchOver` | RL-23 | V |
| IL-05 | Play before the match/deal has started, or with a null/invalid card id | Rejected; no exception escapes the engine | — | V |
| IL-06 | Seat index out of range | Rejected | — | V |

### 1.7 Self-play / property tests

| ID | Setup | Expected | St. |
|---|---|---|---|
| SP-01 | 1,000 AI-vs-AI matches, seeds 0–999, default config | Every match terminates with a winner within a hard cap (e.g. 200 deck rounds); test fails on cap | V |
| SP-02 | Same runs, after every action | DL-06, DL-07; current player holds ≥ 1 card; scores never decrease; ledger sum = score per side | V |
| SP-03 | Same runs, at match end | Winner ≥ target and strictly ahead; loser's ronda-only awards never crossed 40; `MatchEnded` exactly once | V |
| SP-04 | Same seed twice | Identical event log and final state | P |
| SP-05 | Random-legal player vs AI; also `mustCapture=true`, `missaOnFinalPlay=false` | SP-01…03 still hold | P |
| SP-06 | Fuzz: random illegal commands interleaved | No exception; state unchanged by each rejected command | V |
| SP-07 | Each deck round of every run | Exactly 40 plays, 5 deals, tally piles sum to 40 | V |

**Total: 79 engine cases** (14 + 12 + 10 + 16 + 14 + 6 + 7), plus 10 AI cases in §2 and 20 play-mode scenarios in §4.

## 2. AI tests (`Zellige.AI`, EditMode)

| ID | Test | Expected |
|---|---|---|
| AI-01 | 10,000 random legal states | Returned card is always in the AI's own hand and is legal under the current config |
| AI-02 | Table {4,7}; hand {7,2,11,1} | Returns the 7 (capture preferred over drop) |
| AI-03 | Table {3,4,5}; hand {3,5} | Returns the 3 (longer chain preferred), if the policy ranks captures — else document the policy |
| AI-04 | No capture available | Returns a legal drop; never throws |
| AI-05 | Same state + same seed, called twice | Same card; different seed may differ only where the policy is random |
| AI-06 | Snapshot/hash of state before and after `ChooseCard` | Identical: the AI never mutates state |
| AI-07 | One card in hand | Returns that card |
| AI-08 | Static check | AI reads only its own hand, the table and public info (never the human's hand or the stock order) |
| AI-09 | Capture decision | Uses the Core capture resolver; expected capture set equals the engine result for the returned card |
| AI-10 | AI holds a pair/triple | Announcement behaviour as defined (A-1); never announces a rank it does not hold |

## 3. Code-review checklist (architecture-specific)

- [ ] `Zellige.Core.asmdef`: `noEngineReferences: true`, no references; no `using UnityEngine`/`UnityEditor` anywhere under `Scripts/Core`.
- [ ] `Zellige.AI` references Core only; `Zellige.Game` references Core + AI + TMP/UI; no reverse reference; test asmdef is Editor-only with `UNITY_INCLUDE_TESTS`.
- [ ] One implementation of capture/chain, scoring and tally — in Core. UI previews and AI evaluation call it; no re-implemented rank maths (`7→10`) in UI or AI.
- [ ] Every **P** rule in §1 reads from `RulesConfig` (target 41, ronda cap, `mustCapture`, `chainWrap`, `missaOnFinalPlay`, `quadAsTringla`, tie handling, dealer +5, first player, both-reach-target). No magic `41`, `40`, `20`, `5` literals outside the config defaults; each default has a comment naming it provisional and its `RL-nn`.
- [ ] `RulesConfig` is immutable once a match starts and is passed in (constructor), not fetched from a global.
- [ ] No static mutable state (no singletons holding game state, no static `Random`); randomness is injected and seeded; `UnityEngine.Random` is never used for rules.
- [ ] Engine API is command → result: illegal commands return a rejection, never throw for expected cases, never partially apply.
- [ ] UI consumes events and read-only snapshots; no public mutable collections exposed (hands, table, piles are `IReadOnlyList`/copies); UI never writes engine state or computes scores itself.
- [ ] The AI receives a read-only view limited to public information plus its own hand.
- [ ] MonoBehaviours hold presentation state only; input gating lives in one place (turn + animation busy flag), not scattered per button.
- [ ] Score text is rendered from engine state/ledger, not accumulated separately in the UI.
- [ ] Restart builds a new engine instance; nothing survives (event subscriptions removed, coroutines stopped, pooled card views reset).
- [ ] Naming: PascalCase types/methods, `_camelCase` private fields, namespaces match asmdefs (`Zellige.Core`, …); file name = type name; card identity is a value type or immutable class with equality.
- [ ] No dead code, commented-out blocks, unused `using`s, empty `Update()`, leftover `Debug.Log` spam; no `SampleScene` leftovers in build settings.
- [ ] Tests do not load scenes, use `GameObject`s or depend on time/frame count; no test depends on execution order or an unseeded random.
- [ ] No per-frame allocations/LINQ in `Update`; no `FindObjectOfType` in hot paths; references wired by serialized fields.
- [ ] All `.meta` files committed with their assets; no `Resources` folder abuse; strings shown to the player are not scattered literals.

## 4. Play Mode / manual scenarios (integrated game)

| ID | Scenario | Pass when |
|---|---|---|
| PM-01 | Launch → match start | 4 face-up cards for the player, 4 face-down for the AI, empty table, deck counter 32, scores 0–0 |
| PM-02 | Select + play | Selected card is visibly highlighted; play moves it to the table or pile; hand count drops by 1 |
| PM-03 | Input during AI turn | Taps on own cards do nothing (no selection, no queued play); no error |
| PM-04 | Input during deal / capture / tally animation | Blocked; rapid double-tap cannot play two cards |
| PM-05 | Simple capture | Player can see which table card was taken and by whom; pile count increases by the right amount |
| PM-06 | Chain capture incl. 7→10 | All chained cards visibly leave the table; count matches engine |
| PM-07 | Drop | Card stays on the table in a stable position; no score change |
| PM-08 | Missa / Derba / Ronda / Tringla | A readable call-out names the event and the points; score changes by the same amount |
| PM-09 | Score display | After every event, on-screen points and pile counts equal engine state (use a debug overlay or log compare) |
| PM-10 | Deck counter | Shows 32 → 24 → 16 → 8 → 0 across deals; new deal animates when both hands are empty |
| PM-11 | Turn indicator | Always shows the correct side; not colour-only (shape/label/motion) |
| PM-12 | End of deck round | Leftover, +5 and over-20 points are shown and explained; new round starts with dealer switched and piles reset |
| PM-13 | Match end | Stops at ≥ 41 (also mid-deal); correct Victory / Defeat screen; no further input accepted on the table |
| PM-14 | Restart | Scores, piles, table, hands, deck counter, call-outs, turn indicator all reset; second match plays normally; play 3 matches in a row |
| PM-15 | AI pacing | AI plays after a short visible delay; never plays out of turn; never stalls |
| PM-16 | Resolutions 16:9 (1920×1080), 20:9 (2400×1080), 4:3 (1024×768) | Nothing clipped or overlapping; card ranks, scores and buttons readable; hand fully tappable |
| PM-17 | Console | No errors, exceptions or missing-reference warnings during a full match and restart |
| PM-18 | Focus loss / pause (alt-tab) mid-animation | Game resumes in a consistent state |
| PM-19 | Accessibility basics | Suits distinguishable by shape; text contrast adequate on the table; targets comfortably clickable |
| PM-20 | Standalone build smoke | A Windows build starts, plays a full match, quits cleanly |

## 5. Asset integration checklist

- [ ] 40 face sprites present (4 suits × 10 ranks; no 8/9) + 1 back; names follow one pattern (e.g. `card_<suit>_<rank>`); a test or editor check asserts all 41 resolve.
- [ ] All card sprites share the same pixel size, aspect ratio, pivot and pixels-per-unit; corners/transparent margins consistent.
- [ ] Import settings: Texture Type *Sprite (2D and UI)*, Single mode, sRGB on, alpha is transparency, mip maps off, bilinear filter, compression chosen deliberately (no visible artefacts on ranks), max size not above source.
- [ ] No dynamic text baked into images (scores, names, "Your turn", button labels) — these are TextMeshPro; rank numerals on card faces are allowed.
- [ ] Fonts and any audio have a recorded licence; TMP font asset contains every glyph used.
- [ ] **Clean-room:** nothing from `docs/reference_captures/`, the reference APK/XAPK, or its card faces, mascot, icons, sounds or strings exists under `ronda_game/Assets` (check by file search and visual review, UI spec §11).
- [ ] Card art is original or has a documented permissive licence; source files (if any) kept outside `Assets` or in an ignored folder.
- [ ] No unused imported assets or default template leftovers in the build.

## 6. Repository hygiene — findings and recommendations (nothing changed)

| # | Finding | Recommendation |
|---|---|---|
| H-1 | At the start of this review the committed root `.gitignore` held only `.claude` and the `.xapk` name, with no Unity ignores. During the review another agent added (uncommitted) rules for `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Build(s)/`, `MemoryCaptures/`, `*.csproj`, `*.sln`, `*.suo`, `*.user`, `.vs/`, `*.apk`, `*.aab`. `ronda_game/` is still wholly untracked (`Library/` alone is 1.7 GB). Remaining gaps: `.plastic/`, `.idea/`, generic `*.xapk`, `__pycache__/` (present in `UI-Design/Generated/tools/`), `*.unitypackage`, crash/`sysinfo.txt` files. | Commit the updated `.gitignore` **before** the first `git add ronda_game`; add the remaining patterns; verify with `git status --ignored` and `git check-ignore -v ronda_game/Library`. |
| H-2 | `ronda_game/ignore.conf` and `ronda_game/.plastic/` exist: the project was created with Unity Version Control (Plastic). `ignore.conf` is not read by git. | Ignore `.plastic/`; decide on one VCS (git) and optionally remove `com.unity.collab-proxy`. |
| H-3 | The 75 MB reference `.xapk` was committed in `9d497f4` and deleted in `c63bb22`: it is still in history (pack ≈ 181 MB) and on the GitHub remote. It is a third-party binary. | Owner decision: purge from history (filter-repo + force push) or accept; never re-add. The file is no longer on disk. |
| H-4 | `docs/reference_captures/` — 142 tracked third-party screenshots (154 MB); per `02a` they show other users' names/avatars/IDs. | Owner decision: keep private only, or untrack and store outside the repo; must not be public or shipped. |
| H-5 | Generated `ronda_game/ronda_game.sln` and `.vsconfig` in the project root. | Ignore `*.sln`/`*.csproj`; `.vsconfig` may be committed. |
| H-6 | No `.gitattributes`. | Add line-ending normalisation and (optionally) Unity YAML merge / LFS rules for `*.png`, `*.psd`, audio. |
| H-7 | `Packages/manifest.json`: `com.coplaydev.unity-mcp` is a git-URL dev tool (tag-pinned, needs network on a fresh clone); unused `visualscripting`, `timeline`, `feature.2d`, `collab-proxy`. | Keep MCP for development but note it; remove unused packages before submission to cut import time. `packages-lock.json` should be committed. |
| H-8 | `ProjectSettings.asset` defaults: `companyName: DefaultCompany`, `productName: ronda_game`, identifier `com.DefaultCompany.2DProject`, default orientation = auto-rotation with portrait allowed (design is landscape-only). Legacy input (`activeInputHandler: 0`), force-text serialization and visible meta files are correct. | Set product/company names and landscape-only before a build. |
| H-9 | Build settings list only `Assets/Scenes/SampleScene.unity`. | Replace with the real match scene when it exists. |
| H-10 | Root `README` is empty; `UI-Design/*/help` are placeholder files. | Fill README (how to open, Unity version, how to run tests). |

## 7. Rule ambiguities that make expected test values uncertain

| # | Ambiguity | Tests affected | Suggested provisional default |
|---|---|---|---|
| A-1 | **Announcement flow in a local turn-based MVP.** The spec defines a 15 s sealed real-time window and scoring at window close. Not stated for the MVP: is announcing manual (button) or automatic, when are points applied, does the AI announce, and does playing the rank first forfeit? The reference's own credit timing is UNKNOWN (R16). | RN-11, RN-16, AI-10, PM-08 | Auto-resolve both hands at the start of each deal; flag in `RulesConfig`. |
| A-2 | **Equal announcements.** Tutorial says "nobody scores *yet*" (a later derba clause exists, D-G-22); spec voids the tie. | RN-05 | `announceTie = void`. |
| A-3 | **Ronda cap scope.** Cap applies to "ronda-class" awards; a mixed tringla + ronda award is uncapped by the spec's wording, but the tutorial only says "ronda points can never end a game". The cap was never seen live. | RN-12…15, SP-03 | Spec wording (whole award uncapped if it contains a tringla). |
| A-4 | **Derba details.** Across a deal boundary (spec: counts), stacking with Missa and with a chain (spec: yes / +1 only) — all tutorial-text or our decision; Q5 open. | DB-03, DB-04, DB-05 | As in spec §12. |
| A-5 | **Deck round with no capture.** Spec: leftover discarded; whether the non-dealer still gets +5 is not stated. Unreachable in a real 1v1 deck (table holds ≤ 10 ranks, 40 cards are played), so it only matters for hand-built states — and "discarded" cards would need a zone for the conservation invariant. | ED-08, DL-06 | Treat as unreachable; assert it in SP-07 and drop ED-08, or define explicitly. |
| A-6 | **Dealer rule extras.** King/Ace sentence (tutorial `017_rules_27`) is not understood → off. Tally step order is UNKNOWN in the reference; it only matters via A-7. | ED-02, ED-05, ED-06 | `dealerKingAceRule = off`. |
| A-7 | **Both sides ≥ 41 after one tally**, and equal scores at ≥ 41. Reference UNKNOWN. | ME-03, ME-04 | Higher wins; tie → extra deck round. |
| A-8 | **Missa on the final play of a deck round** — provisional. | MS-03 | true. |
| A-9 | **First dealer, who leads, dealer rotation.** Reference: lead UNKNOWN, alternation INFERRED. Tests must inject the dealer rather than rely on a random draw. | DL-04, DL-05, ED-* | Non-dealer leads; dealer alternates per deck round. |
| A-10 | **Empty table at deck start in 1v1** is INFERRED from card accounting (some traditional variants deal table cards). Also means two same-rank table cards never occur (Q1 UNKNOWN stays unreachable). | DL-01, DL-07 | Empty table. |
| A-11 | **Capture obligation and declining a capture** — UNKNOWN (reference test E3 invalid). | CP-10, CP-11, CP-12, AI-01 | `mustCapture = false`, capture automatic. |
| A-12 | **Shuffle algorithm / PRNG not specified.** Golden permutation vectors cannot be written until the PRNG is fixed; relying on `System.Random` ties vectors to the runtime's implementation. | SH-04, SP-04 | Own small PRNG + Fisher–Yates in Core; generate vectors once and cross-check. |
| A-13 | **Four of a kind** value and **ronda rank comparison across 7/10** — UNKNOWN / INFERRED. | RN-08, RN-09 | Quad = tringla (5); compare by rank order. |
| A-14 | **Catch (accusing a hidden pair)** is in the ruleset but not in the MVP scope list; if announcing is manual and Catch is absent, an unannounced pair has no consequence. | RN-11 | Out of MVP; confirm. |
