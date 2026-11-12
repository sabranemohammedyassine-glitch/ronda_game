# 02a — Phase 2 Consolidated Assessment

**Phase:** 2 (reference-game analysis) · **Status:** FINAL for owner review — nothing is implemented · **Date:** 2026-11-12
**Written for:** the project owner, who reviews this report before approving Phase 3, and the later design team.
**Subject:** "Ronda: Moroccan Card", `com.sandukhan.ronda` v6.1.1 (code 43), observed on one physical phone (Samsung SM-A045F, Android 14).
**Companion documents (updated in place):** `02_reference_game_analysis.md` (flow, evidence limits §11), `03_game_rules_specification.md` (rules, live-play register §9), `04_ui_ux_analysis.md`, `05_visual_design_specification.md`, `06_technical_investigation.md`.

**Labels.** **VERIFIED** = shown by recorded gameplay or game UI, qualified as *visual* (a screenshot was inspected), *log-only* (only a Driver/Observer log), or *(tutorial)* (the game's own scripted Tutorial text, not yet seen applied in live play). **INFERRED** = strongly suggested, not conclusive. **UNKNOWN** = insufficient evidence. **CONTAMINATED** = observed while real finger touches were present; usable only as a displayed state, never as proof of cause. **INVALID** = the test did not test what it claimed. Nothing is filled in from traditional Moroccan Ronda.

**References.** `Axxx` / `Bxxx` / `0xx_…` are files in `docs/reference_captures/` (private, never imported as assets). Epochs are host seconds written in full (`START` = 1794481151); *file time* = the screenshot's save time. Usernames, avatars, Table Ids and the profile Id visible in those files are **not** reproduced here.

---

## 1. Executive summary

- **What was observed.** The complete in-game Tutorial (31 rule steps + 22 app-tour steps), the main menu, Settings, Leaderboard, "My table", Profile and avatar picker, Join Table, Create Table with the 1v1 and 2v2 waiting rooms, the public 1v1 queue, and **one full public 1v1 match (victory 41 – 26, four deck rounds)**. In the earlier series an in-progress 2v2 match on the owner's account was observed (result 44 – 31, capture 005).
- **Established with good evidence.** The deck and deal pattern; capture and chain; Missa, Derba (call-out), Ronda (button, ≈ 15–16 s window at every deal); the end-of-deck tally (leftover to the last taker, dealer-not-last = +5 to the other side, one point per card over 20 — A021 shows the game explaining it in words); the first-to-41 end; rank-addressed table slots; the scoreboard semantics; the AFK takeover dialog; the result panel with count-up numbers; the menu/profile/waiting-room/queue UI.
- **Not established.** Forced capture (the Driver's test **E3 is INVALID**); multi-card same-rank captures; the turn-timer value; when Ronda points are credited; live tringla, catch and the 2v2 derba escalation; 2v2 partner assignment and dealer rules; loss/draw screens; reconnect and disconnect behaviour; whether public opponents can be bots; sticker panel and in-match menu.
- **Evidence limits.** **32 of the 61 touch windows (64 touches in total) contain real finger touches.** The menu/queue phase and one 150 s stretch around the end of deck 1 are clean; most of decks 2–4 are contaminated. See §3.
- **Performance.** Across all 33 Observer samples: PSS 500–625 MB, graphics 210–291 MB, 138–160 threads, 30–60 fps. A step of +82 MB PSS / +61 MB graphics / +16 threads occurs between two samples that bracket the match end and result panel; correlation only (§10).
- **Decision: READY FOR PHASE 3 WITH DOCUMENTED UNCERTAINTIES** (§14). No further screenshot review is needed. A short, clean, hands-off observation session is recommended (§13) before the rule engine is frozen, not before Phase 3 starts.

---

## 2. Scope and evidence sources

| Source | What it holds | Used for |
|---|---|---|
| In-game Tutorial captures `017_rules_1–31`, `019_howto_1–22` | the designers' stated rules and app tour | every rule marked VERIFIED (tutorial) |
| Earlier series `000–020` | 2v2 table states, 2v2 result (005), menu, Settings (007), My table (007b/c), Create Table modal (013), Leaderboard (020), tutorial entry | UI and the 2v2 observations |
| Driver `A001–A045` + `driver_log.md` (44 lines, @1794481199–@1794482110) | menus, waiting rooms, queue, one 1v1 match | live-play findings |
| Observer `B001–B017`, `observer_index.md`, 25 retained 30 s recordings, 26 contact sheets | event stills and recordings | cross-checks (not reviewed frame by frame) |
| `touch_watch.log` | 61 windows × 15 s of real-finger counts | contamination (§3) |
| `perf.csv` (33 samples), `palette.json` | performance and colours | doc 06, doc 05 |
| Package inventory (doc 01) | technology identification | doc 06 |

**Method limits (user decisions).** Static analysis was limited to identifying technologies; no IL2CPP dumping, decompilation or asset extraction (D3); no traffic interception or TLS inspection (D4); no network disconnect test was run (needs the owner's approval); Ronda stayed in the foreground; Ronda Club was not explored (it opens Facebook). **This stage collected no new data**: the phone was not touched and no agent was restarted. Evidence files were not modified or deleted.

---

## 3. Evidence quality and contamination limitations

**Detection.** The Observer counted `BTN_TOUCH DOWN` events in consecutive 15 s windows (`getevent`). Injected `adb input tap` events never appear there, so any non-zero window means a real finger. Epochs in the log are write times (± 15 s on window position).

| Phase (Driver) | Epoch range | Windows | Non-zero | Status |
|---|---|--:|--:|---|
| Menu, dialogs, waiting rooms, queue (A002–A018) | 1794481274.98 – 1794481410.65 | 10 | 0 | **CLEAN** (A001–A005 precede the watcher: Driver preflight only) |
| Match start, first AFK dialog, "no-input burst" | 1794481425.73 – 1794481546.39 | 9 | 5 | **CONTAMINATED** |
| End of deck 1, AFK re-appearance, first Ronda tap (A021, A023) | 1794481561.47 – 1794481697.16 | 10 | 0 | **CLEAN** |
| Decks 2–4 (A027 – A045, E1 – E7, E3, E4) | 1794481712.25 – 1794482179.66 | 32 | 27 | **CONTAMINATED** (up to 6 touches per window) |
| **Total** | | **61** | **32** | **64 touches** |

**Interpretation (INFERRED).** A person other than the Driver was touching the phone for most of decks 2–4, plausibly playing cards.

**Affected.** Any claim that attributes a play, a Ronda tap, an "I'm back" or a point to the Driver's injected input after 1794481712 (for example the Ronda tap at 1794481774.26 and the +1 first seen in A033); the first AFK appearance (A020, ≤ 1794481427.5) and the "no-input burst" (1794481458 – 1794481646); the Driver's "turn timer ≈ 12 s" (derived from its own read-and-decide latency); all of E1 – E7 as tests.
**Invalid.** **E3** (forced-capture test, Driver log line 39 at 1794481900.63): the Driver's own note at 1794482055.45 records that the AFK dialog was active the whole time, so bots played the cards; the window stamped 1794481893.22 also held a real touch. No rule here rests on E3.
**Unaffected.** Static UI captured in clean windows; A021 and its numbers; the AFK re-appearance after the dismissal at 1794481646.74; the 33 performance samples (passive sampler).
**Still valid in contaminated windows.** *What the game displayed at a moment* (numbers, cards, bubbles, layout) — those are game outputs whoever tapped. State-based findings (rank-addressed slots, deck counters, tally numbers, result panel) use only that.

**Other limits.** One account, one public 1v1 match, one opponent. Driver timestamps are write times (match end written at 1794482109.88; the panel was visible at file time 1794482087.93). A041/A042 share a file time; `A039_E3_t07` / `A040_E3_t2` carry an "E3" label but show the final-deal state. Screenshots were inspected as down-scaled copies; B-series stills were not individually inspected; chunk011 and chunk027 were deleted unviewed (non-Ronda focus). Earlier-series videos are not preserved.

---

## 4. Complete screen-by-screen functionality (as observed)

Details, sizes and layout: doc 04; visual requirements: doc 05.

| # | Screen / state | What it does | Evidence | Label |
|---|---|---|---|---|
| 1 | Launch → two credit splashes → loading screen (version tag, progress bar) → menu | ≈ 12–13 s on the test phone; cold start 2,040 ms to first frame | earlier video | VERIFIED (log-only/video) |
| 2 | Multilingual notice modal | ranking reset notice in Arabic/Darija, English, French, Spanish | earlier video | VERIFIED (video); trigger UNKNOWN |
| 3 | **Main menu** | profile card (avatar, league badge, level, XP bar), Leaderboard, Settings, tiles 2 Vs 2 / 1 Vs 1 / Create Table / Join Table, Ronda Club, My table, Tutorial | 006, 018, A005 | VERIFIED (visual) |
| 4 | **Settings** | language (5 flags), sound ON/OFF, Log out (not tapped), version | 007 | VERIFIED (visual) |
| 5 | **Leaderboard** | Rank · Name · Tier · Ronda Points · Win Ratio; own row pinned at the bottom (≈ 11,000th) | 020 | VERIFIED (visual) |
| 6 | **Profile** | level, EXP bar `0 / 170`, per-mode stats (rank, win ratio, played, won), league bar `1010 / 1040`, copyable id, Change | A002 (file time 1794481240.33) | VERIFIED (visual), clean |
| 7 | **Avatar picker** | 4-column scrolling grid (≥ 12 portraits), Save, X | A003 | VERIFIED (visual), clean |
| 8 | **Join Table** | one "Table Id" field + Join | A006 | VERIFIED (visual), clean; result of joining UNKNOWN |
| 9 | **Create Table** | choose 1 Vs 1 or 2 Vs 2 | 013, A008, A013 | VERIFIED (visual) |
| 10 | **Waiting Players** (1v1 / 2v2) | 4 seat slots in two halves (lock / own avatar / "+"), Table Id + copy, locked Start while alone, spectator eye counter 0 | A009–A015 | VERIFIED (visual), clean; "+" and locked-Start taps produce no visible reaction |
| 11 | **Matchmaking queue** | "Searching": two avatars around "VS", count-up timer, Cancel; match found ≤ 3 s (log-only); dealt table on screen 10.1 s after the tap | A016 (1794481364.38), A017 (1794481367.06), A018 (1794481371.57); Driver 1794481361.43 | VERIFIED (visual A016–A018); ≤ 3 s log-only |
| 12 | **Game table** (1v1 observed live, 2v2 earlier) | octagonal table, rank-addressed 5 × 2 slots, large fanned hand, nameplates, crosshair (catch) per opponent, sticker button, deck counter, dealer token, scoreboard, Ronda pill and countdown banner, gear | A020–A043, 003 | VERIFIED (visual) |
| 13 | **In-table call-outs** | cream bubbles with a tail: "Derba! +1" (A027), "Missa!" (A031), "I had a ronda! I take +1" (003), end-of-deck explanation (A021) | listed | VERIFIED (visual) |
| 14 | **AFK takeover dialog** | "You are AFK — Bots are playing for you." + "I'm back" | A020, A021, A023, A042, B007 | VERIFIED (visual); timing see §6 |
| 15 | **Result panel** | VICTORY, avatars (crown), score 41 – 26, XP and ▲ chips that count up (+62/+6 → +100/+9), league bar, level bar, "Tap to continue" | A044, A045, 005 | VERIFIED (visual) |
| 16 | **Post-match video ad** | full-screen ad ≈ 14 s then Close | earlier series only; not captured after this match | VERIFIED (earlier, once); frequency UNKNOWN |
| 17 | **My table** (layout editor) | Back, Undo, Reset, Save, "Tap anything to move or resize it", Style | 007b, 007c | VERIFIED (visual); not edited |
| 18 | **Tutorial** | Sota mascot; "Learn the rules" (31 scripted steps) and "How to use the app" (22 steps, spotlight tour, demo 2v2 table) | 016, 017_*, 019_* | VERIFIED (visual) |
| 19 | Ronda Club | opens the Facebook app (external) | earlier series | VERIFIED (behaviour); never re-tap |
| 20 | In-match gear panel, sticker panel, defeat / draw result, error dialogs | — | — | UNKNOWN |

---

## 5. Gameplay rules and scoring

Rule IDs refer to the matrix in §11. Full wording and test cases: doc 03.

**Cards and deals.** 40 Spanish cards, ranks 1–7, 10, 11, 12 (R01); chain order jumps 7 → 10 (R02). **1v1:** 4 cards per hand per deal, deck counter 32 → 24 → 16 → 8 → 0, five deals per deck (R03). **2v2:** 4, then 3, then 3 per player (R04). The table starts a deck empty (R05, INFERRED).

**Play.** One tap plays a card (R06). Same-rank take, plus a chain of the following ranks present on the table (R07, state seen A029 → A030). A non-matching card stays on the table (R08). Table slots are **rank-addressed**: top row 1–5, bottom row 6, 7, 10, 11, 12 (R09). Forced capture and multi-card same-rank behaviour are **UNKNOWN** (R10, R11).

**Points.** Missa (table emptied) +1 (R12). Derba (taking the card the previous player just played) +1, growing 1 → 5 → 10 in 2v2 per the Tutorial only (R13). Ronda (pair, announced before playing) +1, two rondas +2 (R14); Tringla 5; contest between announcements; ronda points never end a match, you stop at 40 (R17–R19). Catch +1, wrong guess blocks you until the next round (R20). First side to 41 wins and the match ends at once (R25).

**End of a deck (R21 – R24, partly verified in live play).** Leftover table cards go to the last side that took cards; if the dealer was not the last to take, the other side gets +5 (A021: "The dealer wasn't the last to take! +5 for us"); each side then scores one point per card over 20 (A021: "I collected 32 cards, I get +12"; A039 → A041: 20 → 26 points for a 26-card haul, INFERRED decomposition). The King/Ace exceptions in the Tutorial have no live instance.

**Scoreboard.** Own side on the left; top row = cards taken this deck; bottom row = match points (R26).

**Progression.** A win gave +100 XP (A045, 005); the green ▲ value was +9 (1v1) and +10 (2v2) (R31; meaning INFERRED as rank points); level 3 needs 170 XP; the league bar ran `1010 / 1040` before the match.

---

## 6. Turn timing, AFK handling and timeout behaviour

| Observation | Evidence | Label |
|---|---|---|
| Bots play for a player who misses turns behind a modal "You are AFK"; "I'm back" returns control | A020 (file time 1794481427.496), B007 (1794481458.37), A021 (1794481583.25), A023 (1794481667.17), A042 | VERIFIED (visual) |
| First dialog on screen ≤ 55.9 s after the dealt table first appeared (A018, 1794481371.57; A020 file time 1794481427.496); the Driver measured "~33 s" from its own write-time line (1794481391.999) and logged the dialog at 1794481458.368 | A020; log lines 26–27 | VERIFIED (visual) for ≤ 55.9 s; **CONTAMINATED** (touch window at 1794481425.73 holds 2 touches) |
| Re-appearance ≤ 20.4 s after "I'm back" at 1794481646.74 (A023 file time 1794481667.17), inside windows that read 0; A022 (1794481649.46) shows no dialog 2.7 s after the dismissal; the Driver logged its unsuccessful Ronda tap at 1794481664.29, a write time | log lines 29–31; A023; touch log | INFERRED (bound), clean |
| Driver's "turn timer ≈ 12 s" | log line 31 (1794481684.78) | INFERRED, low confidence |
| After "I'm back" at 1794481754.62 no dialog for ≥ 23.5 s (A029, A030) | A029, A030 | **CONTAMINATED** (a human may have been playing) |
| Bot strategy, whether bots run client- or server-side, whether repeated timeouts forfeit, timer value, whether the timer is per turn | — | UNKNOWN |

**Net.** The AFK takeover exists and keeps the match moving (VERIFIED). A missed turn triggers it (INFERRED). The turn timer is UNKNOWN; a design-grade bound is "somewhere under ≈ 20 s", and nothing above that is supported.

---

## 7. Matchmaking, table creation, joining and waiting-room behaviour

- **Public 1v1:** tap the tile → "Searching" modal (count-up timer, Cancel) → the placeholder table is on screen 5.6 s after the tap (A017) and the dealt table 10.1 s after it (A018) (tap 1794481361.43); the Driver reports the match was found ≤ 3 s (log-only). The queue UI shows a placeholder opponent portrait until the real avatar appears. Tutorial: "we find you an opponent of your level" (VERIFIED (tutorial)). Matching rules, timeout and bot fill: UNKNOWN.
- **Create Table:** choose 1 Vs 1 or 2 Vs 2 → **Waiting Players** dialog with a Table Id, copy button, locked **Start** while alone, spectator counter. 1v1 shows one open "+" seat and two padlocked seats; 2v2 shows three "+" seats (A010, A011, A014). Tapping Start while alone or "+" produced no visible reaction in the captured frames (A010, A011). What "+" does is UNKNOWN. Tutorial text says friends can join by code/link and "others can join just to watch" (VERIFIED (tutorial)).
- **Join Table:** one text field "Table Id" and a Join button (A006). No code was entered; joining and spectating are UNKNOWN.
- **2v2 public queue:** "start alone and we find you a partner" (VERIFIED (tutorial)); not played in the Driver session. In the earlier 2v2 result (005) the top-seat player is on the winner's own team, i.e. the partner sits opposite (VERIFIED (visual)); partner selection is UNKNOWN.

---

## 8. Multiplayer observations and connection behaviour

| Topic | Finding | Evidence | Label |
|---|---|---|---|
| Opponent presentation | Name, avatar, templated first-person bubbles; no free-text chat seen; stickers exist (button beside the nameplate) | A021, A027, A031, 003; 019_howto_17 | VERIFIED (visual / tutorial) |
| Human or bot opponent | not determinable | — | UNKNOWN |
| Idle player | bots + AFK dialog (§6) | A020… | VERIFIED (visual) |
| App restart during a match | the earlier force-stop returned straight to the running match with scores intact (one instance, earlier series 000–001; not re-inspected) | earlier series | VERIFIED (one instance) |
| Network loss / reconnect UX / grace period | not tested (needs owner approval) | — | UNKNOWN |
| Spectators | counter in the waiting room; tutorial: viewers can watch a started game | A010, A014; 019_howto_5–8 | VERIFIED (visual / tutorial); behaviour UNKNOWN |
| Server authority | the game explains tallies in words and shows authoritative numbers; where the logic runs is not visible | A021 | UNKNOWN (backend stack unverified, doc 06 §5) |
| Matchmaking speed | ≤ 3 s in one sample | log line 25 | VERIFIED (log-only), n = 1 |

---

## 9. UI/UX and visual design observations

**UX.** Landscape-only, one-tap play, large targets, turn glow, countdown banner, call-out bubbles that explain every score change (the end-of-deck bubble is the clearest example), modals with an orange title tab and a red X, a deliberate count-up on the result screen, a tutorial that separates "learn the rules" from "use the app". Ads and external links (Ronda Club → Facebook) interrupt flow; our design should avoid both patterns (doc 04 §14).
**Visual.** Warm Moroccan café-courtyard scenes, a dark-teal felt table with a wooden octagonal frame and lanterns, cream modals, saturated tiles (red/blue/yellow/cream), teal HUD pills, illustrated Spanish-style cards, painterly 2D art. Observed colours and sizes are *measurements for analysis only* (doc 05 §2.1, §10); every asset in our game must be original (doc 05 §12).
**New components documented from the Driver session** (doc 05 §10.1): AFK dialog, Waiting Players dialog, Searching modal, Profile, avatar picker, speech bubbles, Ronda pill and banner, result panel with count-up.
**Layout insight.** The rank-addressed slot grid (VERIFIED, visual) makes the card order visible on the table; whether we copy it is a design choice.

---

## 10. Performance findings

Source: all 33 Observer samples (`perf.csv`, 1794481323 – 1794482185; log-only). Full table and step analysis: doc 06 §6, §9.

| Metric | Range (median) |
|---|---|
| PSS | 499.7 – 624.6 MB (517.7) — reported **500–625 MB** |
| Graphics memory | 209.7 – 290.9 MB (224.0) — reported **210–291 MB** |
| Threads | 138 – 160 (142) |
| Frame rate | 30.2 – 60.3 fps (47.7) — two regimes, **30–60 fps** |
| CPU | 29.4 – 97.0 % of one core |
| Battery | 33.1 – 36.7 °C; CPU/GPU 47.1 °C in every sample (constant; not informative) |

- The "30 fps on menus, 60 fps on the table" reading of the earlier draft is **not supported**: ≈ 30 fps also appears in match samples (samples 9, 11, 15, 16, 22), and the last three samples (after the match) are a strict 30 fps lock at ≈ 30 % CPU.
- **Memory/thread step B (samples 29 → 30).** Between 1794482078.73 and 1794482105.61 (26.9 s): PSS 519.8 → 602.1 MB, graphics 224.0 → 285.3 MB, threads 143 → 159, battery 33.1 → 36.7 °C. The interval contains the last plays (Driver, 1794482085.39), the result panel (A044 file time 1794482087.93, A045 1794482091.58) and possibly the tap on "Tap to continue" (logged at 1794482109.97, a write time after sample 30). The level stays 602–625 MB for the next 80 s. A smaller step A (+21 MB PSS, +11.9 MB graphics) occurs between samples 12 and 13, after the Observer's guard saw the notification shade take focus (1794481634 – 1794481644).
- **What this does not show.** It does not show that the match end, the result panel or an ad *caused* the increase; no sample lies inside the interval, no memory profiler was used, and which window had focus during the last samples is UNKNOWN (chunk027 was deleted unviewed). Whether memory returns to the earlier level after returning to the menu is UNKNOWN.
- **Design implication.** Budget for ≈ 650 MB PSS / ≈ 300 MB graphics on low-end phones as a ceiling *for the reference*; our target should be lower, with a post-match memory check in our own tests.

---

## 11. Verified / Inferred / Unknown findings matrix

Strength: **V** visual, **L** log-only, **T** tutorial text only, **C** contaminated state-only, **X** invalid.

### 11.1 Rules and scoring

| ID | Finding | Label | Evidence | Str. |
|---|---|---|---|---|
| R01 | 40-card Spanish deck, ranks 1–7, 10–12 | VERIFIED | 017_rules_1; card faces A041 | T/V |
| R02 | Chain order jumps 7 → 10 | VERIFIED (tutorial) | 017_rules_7 | T |
| R03 | 1v1 deals of 4 per hand, deck 32 → 24 → 16 → 8 → 0 | VERIFIED | A027, A029, A033, A023, A038, A039 | V |
| R04 | 2v2 deals 4, 3, 3 per player (24 → 12 → 0) | VERIFIED (tutorial) + earlier video | 017_rules_2 | T |
| R05 | Table empty at deck start (1v1) | INFERRED | card accounting at 1794481719.36 | — |
| R06 | One tap plays a card | VERIFIED (tutorial) | 019_howto_12 | T |
| R07 | Same-rank take + chain of following ranks | VERIFIED (tutorial); live state fits (11 → 12) | 017_rules_7; A029 → A030 | T/V/C |
| R08 | Non-matching card stays on table | VERIFIED | A041 | V |
| R09 | Rank-addressed slots (top 1–5, bottom 6, 7, 10, 11, 12) | VERIFIED | A027, A029, A033, A039, A041 | V |
| R10 | Forced capture | **UNKNOWN** | E3 INVALID | X |
| R11 | Several same-rank table cards, same-rank stacking | UNKNOWN | A027 most simply an animation (INFERRED) | — |
| R12 | Missa +1 | VERIFIED | 017_rules_10; A030 → A031 (rival 22 → 23) | T/V |
| R13 | Derba +1 (call-out); 2v2 escalation 1 → 5 → 10 | VERIFIED (bubble A027); escalation VERIFIED (tutorial) only | 017_rules_23; A027 | T/V |
| R14 | Ronda +1, two rondas +2, announce before playing | VERIFIED (tutorial); announcement bubble 003 | 017_rules_13; 003 | T/V |
| R15 | Declaration window ≈ 15–16 s at every deal | INFERRED (13 s, 10 s, 9 s seen) | A041, A037, A029 | V |
| R16 | When ronda points are credited | **UNKNOWN** (video after close vs Driver 14 → 15) | A030, A033; 003 | C |
| R17 | Tringla = 5 | VERIFIED (tutorial) | 017_rules_13 | T |
| R18 | Contest: tringla > ronda; higher ronda wins; lower tringla wins; winner scores own + 1 per other | VERIFIED (tutorial) | 017_rules_16–18 | T |
| R19 | Ronda points cap at 40, tringla can win | VERIFIED (tutorial); withheld ronda not seen | 017_rules_19 | T |
| R20 | Catch +1; wrong guess blocked until next round | VERIFIED (tutorial); not played live | 017_rules_21 | T |
| R21 | Leftover table cards to the last taker | VERIFIED (tutorial); fits A039 → A041 | 017_rules_26 | T/V |
| R22 | Dealer not last to take → +5 to the other side | VERIFIED | A021 | V |
| R23 | One point per card over 20 | VERIFIED (32 cards → +12) | A021 | V |
| R24 | King / Ace dealer exceptions | UNKNOWN | 017_rules_27 text only | T |
| R25 | First to 41 wins, instant end | VERIFIED | A044 (41 – 26); earlier video (41 – 23 on a Derba) | V |
| R26 | Scoreboard: own side left; top = cards this deck; bottom = points | VERIFIED | 019_howto_18; A029, A043 | T/V |
| R27 | Dealer alternates each deck (1v1) | INFERRED | A021, A039 (own seat), A041/A043 (rival) | V |
| R28 | Who leads after a deal; turn direction | UNKNOWN (2v2 counter-clockwise INFERRED, weak) | — | — |
| R29 | Turn timer value | UNKNOWN (≤ 20.4 s INFERRED) | A023 | C |
| R30 | AFK takeover with bots and "I'm back" | VERIFIED | A020, A021, A023, A042, B007 | V |
| R31 | +100 XP per win; ▲ +9 (1v1) / ▲ +10 (2v2); counts up | VERIFIED values; meaning INFERRED | A044, A045, 005 | V |
| R32 | Both sides ≥ 41, four of a kind, tie, defeat and draw rules | UNKNOWN | — | — |

### 11.2 UI and multiplayer

| ID | Finding | Label | Evidence | Str. |
|---|---|---|---|---|
| U01 | Menu, Settings, Leaderboard, My table, Tutorial as documented | VERIFIED | 006, 007, 007b, 020, 016–019 | V |
| U02 | Profile and avatar picker layouts | VERIFIED | A002, A003 | V (clean) |
| U03 | Join Table = Table-Id field + Join | VERIFIED | A006 | V (clean) |
| U04 | Waiting Players layouts, Table Id, spectator counter, locked Start | VERIFIED | A010, A011, A014 | V (clean) |
| U05 | "+" and locked Start produce no visible reaction | VERIFIED (captured frame only) | A010, A011 | V |
| U06 | Queue modal with count-up timer and Cancel | VERIFIED | A016 | V |
| U07 | Call-out bubbles explain scoring (incl. end-of-deck text) | VERIFIED | A021, A027, A031, 003 | V |
| U08 | Result panel counts up (+62/+6 → +100/+9) | VERIFIED | A044, A045 | V |
| U09 | Profile bar ≈ 97 % vs result league bar ≈ 50–55 % | VERIFIED values; reason UNKNOWN | A002, A044, A045 | V |
| U10 | Sticker panel, gear panel, defeat/draw, error dialogs | UNKNOWN | — | — |
| M01 | Public 1v1 found ≤ 3 s | VERIFIED | log line 25 | L, n = 1 |
| M02 | Opponent is human or bot | UNKNOWN | — | — |
| M03 | Reconnect after app restart returned to the running match | VERIFIED (one instance, earlier series) | 000, 001 | not re-inspected |
| M04 | Disconnect / reconnect UX, grace period | UNKNOWN (needs approval) | — | — |
| M05 | 2v2 partner assignment, seating, dealer | UNKNOWN (top seat = partner VERIFIED) | 005 | V |

### 11.3 Technical

| ID | Finding | Label | Evidence | Str. |
|---|---|---|---|---|
| T01 | Unity 6000.3.6f1, IL2CPP, Addressables, Localization (5 languages), Firebase/UGS/WebSocket/AdMob **present** | VERIFIED (package presence); use unproven | doc 01, doc 06 | — |
| T02 | Which backend carries the game | UNKNOWN | — | — |
| T03 | PSS 500–625 MB, graphics 210–291 MB, 138–160 threads, 30–60 fps | VERIFIED | perf.csv (33) | L |
| T04 | +82 MB / +61 MB / +16 threads between samples bracketing match end | VERIFIED (numbers); cause UNKNOWN | perf.csv; A044 | L |
| T05 | Graphics API, renderer, audio streams | UNKNOWN | — | — |

---

## 12. Contradictions and unresolved questions

**Contradictions in the evidence (both observations are kept; why unresolved).**

| # | Observation A | Observation B | Status |
|---|---|---|---|
| 1 | Earlier drafts: "the table starts empty" | A041 shows 8 table cards, deck 24, counters 0 / 0 | Reconciled (INFERRED): A041 is a second deal after eight non-capturing plays (24 + 8 + 4 + 4 = 40); first deal never captured |
| 2 | Earlier video: ronda bubble appears after the window closes | Driver: points 14 (A030) → 15 (A033) around a Ronda tap whose time is after the countdown seen in A029 | **Unresolved**: contaminated window (touches 2 / 2 / 6), the +1 could be another event |
| 3 | Driver log: AFK seen at 1794481458.37 | A020 file time 1794481427.50 already shows the dialog | Reconciled: log time is a write time; onset ≤ 1794481427.50, exact onset UNKNOWN and contaminated |
| 4 | Driver: "+62 XP, +6 rank pts" | A045: +100 XP, ▲ +9 | Reconciled: count-up animation; final = A045 |
| 5 | Driver log: match end 1794482109.88 | A044 (panel visible) file time 1794482087.93 | Reconciled: write time ≈ 22 s late |
| 6 | Driver: A027 shows a stacked pair of 2s (hypothesis: same-rank stacking) | A027 also shows "Derba! +1" | **Unresolved**: most simply a capture animation (INFERRED) |
| 7 | Driver: turn timer ≈ 12 s | only bound derivable: ≤ 20.4 s | **Unresolved**; ≈ 12 s kept as INFERRED, low confidence |
| 8 | Profile bar `1010 / 1040` (≈ 97 %) | result league bar ≈ 50–55 % | **Unresolved** (different scale or different quantity) |
| 9 | A039/A040 filenames say E3 | content = final-deal state of the Driver's E6 | Label only; content cited |
| 10 | Rival's points 0 (A021) → 20 (E7, logged 1794481664.29; same state in A022, file time 1794481649.46) vs +5 +12 = 17 | +3 not itemised | INFERRED earned during deck 2; not itemised |
| 11 | Driver E4 "before" state equals A043 (file time 1794482057.89) but E4 is logged at 1794482085.39 | the last move is not separately recorded | **Unresolved**; the +9 (32 → 41) breakdown is not recoverable |

**Unresolved questions (ranked by impact on our design).** (1) Is capture compulsory, and may a matching card be dropped (R10)? (2) What happens with two or more same-rank table cards (R11)? (3) When are Ronda points credited, and is the window measured from the deal (R15/R16)? (4) What is the turn timer and the timeout policy (R29)? (5) Live tringla, catch and contest outcomes. (6) 2v2: partner assignment, dealer, who leads, derba escalation. (7) King/Ace dealer exceptions. (8) Disconnect/reconnect, grace period. (9) Human vs bot opponents. (10) Defeat/draw, rank-point formula, ad frequency, sticker panel.

---

## 13. Recommended additional observations (only if the owner wants parity data)

All sessions: **one driver, owner hands-off, touch watcher must read 0 in every window used as evidence**, Ronda foreground only, no force-stop, D3/D4 respected, usernames kept private. Items 1–5 would fit one 60–90 minute session of 3–5 public 1v1 matches; they need no new approval beyond the existing public-matchmaking consent.

| Pri. | Test | Settles | Notes |
|---|---|---|---|
| 1 | Burst-screenshot every move (≥ 2 frames/s around each play), play promptly (no AFK), look for any state where a matching card could be captured but a non-matching card is played (by anyone) and whether the UI refuses | R10, R4 | E3 repeated validly; cannot arrange hands, so opportunistic |
| 2 | Record every table that ever contains two same-rank cards and how the next matching play resolves | R11 | only possible if compulsory capture is false |
| 3 | Take a Ronda with a 4 fps recording through the window close; read points at tap, at close and 2 s later; also time the window from the deal | R15, R16 | |
| 4 | Deliberately miss one turn after the turn glow starts, with a stopwatch from the glow to the dialog; repeat ≥ 3 times | R29 | affects the opponent only through bot play; ask first |
| 5 | Post-match flow: result → ad → menu, with memory samples every 5 s through to the menu and idle menu for 60 s | step B in §10; ad frequency | |
| 6 | One 2v2 match (public queue): partner assignment, seating, dealer, who leads, a derba escalation if it happens | M05, R13, R28 | |
| 7 | Tringla and catch opportunistically; a deliberate wrong catch to confirm the lockout | R17, R20 | wrong catch affects an opponent's score minimally; ask first |
| 8 | A loss and a draw | R32, defeat screen | may be reached via AFK |
| 9 | Network disconnect / reconnect | M04 | **requires the owner's explicit approval** (D4 still applies) |
| 10 | Open the sticker panel and gear panel once | U10 | |

---

## 14. Phase 3 readiness assessment

**Decision: READY FOR PHASE 3 WITH DOCUMENTED UNCERTAINTIES.**

**Why not "READY".** Several rules that a rule engine and an AI must implement are not established in live play: forced capture, same-rank multi-card captures, Ronda credit timing, turn timer/timeout policy, live tringla/catch/contest, 2v2 specifics. The only attempt to test forced capture (E3) is invalid, and 32 of 61 touch windows were contaminated.

**Why not "NOT READY".** Phase 3 designs *our own* game. The evidence is sufficient for the core of the game: deck, deal, capture and chain, Missa, Derba, Ronda, the end-of-deck tally (three components confirmed in play), the 41 target, the scoreboard, the AFK concept, the full app flow and the UI vocabulary. Every open point can be handled in Phase 3 as an explicit, parameterised **design decision** of ours (our own rule, our own timer value) recorded in a decision log, with no claim of parity with the reference. Reference-parity questions are valuable but not blocking.

**Uncertainties that would materially affect the original game's design**

| Uncertainty | Why it matters | How Phase 3 should treat it |
|---|---|---|
| Forced vs optional capture; dropping a matching card | defines the legal-move generator, AI, hints and UI affordances | decide explicitly; make it a rule flag; if the owner wants reference parity, run test 1 first |
| Several same-rank cards on the table | capture size, table model, slot design | design our own rule; keep the rank-addressed slot idea optional |
| Ronda credit timing and window origin | hidden-information reveal, server timers, anti-cheat | make window length and award time configurable; define reveal semantics ourselves |
| Turn timer and timeout/forfeit policy | server authority, bot design, fairness | pick a value (reference upper bound ≈ 20 s); define bot behaviour ourselves |
| Tringla, contest and catch in live play | hidden-information model, scoring edge cases | take the Tutorial text as the specification candidate; mark as "design decision, reference unverified" |
| 2v2 rules (partner, dealer, derba escalation, who leads) | team model, scoring | specify ourselves from the Tutorial text; test later |
| Disconnect / reconnect and grace period | session service, resume design | design our own; the one observed resume (restart returns to the running match) is a requirement candidate |
| Human vs bot opponents; bot quality | matchmaking, fairness messaging | decide our policy (label bots clearly) |

**Recommended path.** Approve Phase 3 on this evidence. Run the short clean session of §13 (items 1–5 and 6) **before the rule engine specification is frozen**, or accept the parameterised decisions and skip it. No further screenshot reading is required.
