# MVP 05 — Catch: evidence and specification

**Date:** 2026-11-17 · **Status:** evidence review complete. **Not implemented**: the evidence covers the core of the mechanic but not the parameters that change its behaviour (§4), and a product decision is needed first (§5).
**Sources:**
- Phase 2: `docs/03_game_rules_specification.md` §4.6, Q9 and TC-CATCH-01; `docs/02a_phase2_consolidated_assessment.md` R20 and §13 item 7; `docs/02_reference_game_analysis.md` (HUD); `docs/04_ui_ux_analysis.md` (catch row).
- Primary tutorial captures, re-read for this review: `docs/reference_captures/017_rules_20.png`, `017_rules_21.png`, `017_rules_22.png`, `019_howto_16.png`.
- Phase 3 proposals: `docs/phase3/03_rules_and_scoring_spec.md` RL-18, E18–E20; `01_decision_register.md` D-G-13; `04_state_machines.md`; `05_domain_model.md`; `docs/mvp/01_verified_rules_reference.md` M-39, M-40, U-11.

**Evidence level.** Catch has **never been observed in live play** (Phase 2 played no catch, R20). Everything below marked CONFIRMED comes from the reference game's tutorial, i.e. the designers' stated intent, not observed server behaviour.

---

## 1. What the primary sources show

| Capture | What is on screen (text quoted only as evidence) |
|---|---|
| `017_rules_20` | 1v1 tutorial table. Two 4s face up in the centre (4 of swords, 4 of clubs). Scoreboard You 8 / Rival 0 (cards), You 2 / Rival 0 (points). Guide: *"Now watch the Rival's cards…"* No target icon next to the Rival yet. |
| `017_rules_21` | Same state; a **target (crosshair) icon now appears next to the Rival's name**, highlighted. Guide: *"The Rival played two 4s and never announced a ronda. Catch him: tap the target next to his name, then pick 4."* |
| `017_rules_22` | After tapping the target: a **popover anchored to the target with ten cards, ranks 1–7, 10, 11, 12** (shown with one suit), a 5 × 2 grid. Same guide text. |
| Phase 2 transcription of the next step (`tut_t5`, not in `reference_captures`) | *"Caught him! +1 point. Careful: a wrong guess blocks you until the next round."* Accuser's points row 2 → 3. |
| `019_howto_16` | 2v2 tutorial table (app tour). **Target icons beside both opponents only** (Salma, Kenza); **none beside the partner** (Omar). The local player's *Ronda* button is visible at the same time (an announcement window is open). Guide: *"Think an opponent hid a ronda? Tap the target next to their name and pick the number."* |
| Phase 2, live table (A020–A043) | A crosshair is visible beside each opponent in live 1v1 and 2v2 tables (VERIFIED visual). Never tapped. |

**Side observation (not about Catch).** In `017_rules_20/21` two cards of the same rank lie face up together. That could bear on U-2 ("at most one card per rank on the table"). It is a **scripted tutorial illustration**, possibly just displaying the cards the Rival played, and live play showed rank-addressed slots (R09). Recorded for E-1 only; it is not evidence.

---

## 2. Confirmed facts (tutorial level)

| ID | Fact | Source |
|---|---|---|
| CF-1 | Catch exists: a player can accuse an opponent of holding an **unannounced ronda** (a pair it did not declare). | `019_howto_16`, `017_rules_21` |
| CF-2 | **Who can be targeted:** opponents only. In 2v2 the partner has no target icon. In 1v1, the single rival. | `019_howto_16` (visual), live tables (visual) |
| CF-3 | **Who can initiate:** a player at the table (the tutorial's human). Whether bots/autopilot catch is not shown. | `017_rules_21` |
| CF-4 | **How:** tap the target icon beside the opponent, then pick **one rank** from all ten ranks. | `017_rules_21/22` |
| CF-5 | **Success** (the guess is right): **+1 point to the accuser.** | Phase 2 transcription (`tut_t5`): 2 → 3 |
| CF-6 | **Failure** (wrong guess): the accuser **"cannot catch again until the next round"**. | Phase 2 transcription (`tut_t5`) |
| CF-7 | The tutorial's success case: the rival had **played two cards of the rank** and **never announced** it. | `017_rules_21` |

---

## 3. Unknown behaviour (must not be invented)

| ID | Question | Why it matters | Phase 3 provisional proposal (design, **not** evidence) |
|---|---|---|---|
| UK-1 | **When** is Catch allowed: any time, only after the announcement window closes, only until the end of the deal? | Too early would let a player "catch" a pair that is still allowed to be announced. | After the window closes, until the deal ends (RL-18, state machine "Revealed"). |
| UK-2 | **What counts as proof:** must the target have *played* the rank this deal (tutorial example), or are blind guesses allowed (*"Think an opponent hid a ronda?"*)? | Decides whether a catch can succeed before any card of the pair is shown. | Evidence required: the target has played ≥ 1 card of the rank this deal (`catchEvidence = playedThisDeal`). |
| UK-3 | **What is a valid hidden ronda:** two of the rank in the hand **of that deal**? Cards played over several deals? A pair the target still holds? | The validity test. | The dealt hand of that deal held ≥ 2 of the rank and the target did not announce it. |
| UK-4 | **Scope of the lockout ("next round")**: next deal, or next deck? | Lockout length. | Next deal (`catchLockout = nextDeal`). |
| UK-5 | **Hidden tringla**: is catching it worth 1, or more? | Value. | 1 (`catchValueTringla = 1`). |
| UK-6 | Can the same hidden pair be caught twice, or by both opponents in 2v2? | Duplicate scoring. | Once per `(target, rank, deal)`. |
| UK-7 | Does a Catch take a **turn**, need the accuser's turn, or pause play? | Turn order. | Not turn-bound; does not change the turn (doc 04 §6). |
| UK-8 | Does a successful catch affect the **caught** player (penalty, loss of points, voiding a contest)? | Scoring. | No (only +1 to the accuser). |
| UK-9 | Is a Catch point subject to the **40 cap** that applies to ronda points, and can a Catch point **win the match** at 40? | Match end. | Not stated; uncapped by omission (doc 01 U-12). |
| UK-10 | Does a catch interact with the **announcement contest** of that deal (e.g. a hidden pair that would have won it)? | Scoring. | No interaction stated. |
| UK-11 | Do bots (or the autopilot) catch, and do they ever hide pairs? | The AI design. | "Normal bot announces/catches correctly" (doc 05); no policy for hiding. |

**Conclusion.** The core (CF-1…CF-7) is enough to describe Catch to a player, but not to implement it faithfully. UK-1, UK-2, UK-4 and UK-9 change what is legal and how the score moves, and implementing them now would mean choosing values the reference doesn't establish.

---

## 4. Why Catch is not implemented in this phase

1. **Rule parameters unknown.** UK-1, UK-2, UK-4 and UK-9 are unknown (above). The Phase 3 defaults exist, but D-G-13 is a ★ decision with no owner approval recorded.
2. **It would do nothing in the current single-player game.** Nadia always announces every pair (`SimpleAi.ChooseAnnouncements`), so the human could never catch her (every catch would fail). Only the reverse direction, Nadia catching the human, has an effect. That needs an AI policy nobody has specified (UK-11).
3. **No current behaviour is wrong because of it.** The game simply has no penalty for a hidden pair (audit row #35, MISSING). That is a known gap, not a defect.

---

## 5. Decisions needed from the product owner

| # | Decision | Options |
|---|---|---|
| D-1 | Is Catch in the MVP at all? | (a) No: document the gap; (b) yes, human → Nadia only; (c) yes, both directions. |
| D-2 | If (b) or (c): may Nadia **hide** a pair? | (a) never, so the human's catch can only fail (pointless); (b) sometimes, at a configurable rate per difficulty. |
| D-3 | If (c): when does Nadia **catch** the human? | (a) whenever the evidence rule proves a hidden pair (perfect play); (b) with a probability (more human-like). |
| D-4 | Accept the Phase 3 defaults for UK-1/2/4/5/6/7/8/9 provisionally, behind `RulesConfig` flags? | yes / no / wait for observation O-1…O-6. |

---

## 6. Observations that would settle the unknowns

One clean session on the reference game (doc 02a §13 item 7), with a recording and timestamps. Preferably a private table against a consenting second player (a wrong catch costs an opponent nothing, but ask first).

| ID | Do | Watch for | Settles |
|---|---|---|---|
| O-1 | Hold a pair, do **not** announce, play one card of it; ask the other player to tap the target and pick that rank **before** and **after** the announcement countdown ends. | Is the target enabled during the countdown? Is the catch accepted? | UK-1, UK-2 |
| O-2 | Hold a pair, do not announce, play **neither** card; the other player catches that rank after the countdown. | Blind catch accepted or refused; points. | UK-2, UK-3 |
| O-3 | The other player makes a **wrong** catch in deal *n*; try again later in deal *n*, in deal *n+1*, and in the next deck. | When the target re-enables. | UK-4 (CF-6 scope) |
| O-4 | Hide a **tringla**; get caught. | Points awarded. | UK-5 |
| O-5 | Get caught twice for the same rank, or once by each opponent in 2v2. | Second catch accepted or refused. | UK-6 |
| O-6 | Catch at 40 points (if achievable). | Does it end the match? | UK-9 |
| O-7 | Note whether the catch happens on the accuser's turn only, and whether play pauses. | Turn effect. | UK-7 |

---

## 7. Implementation outline (for when D-1…D-4 are answered)

Kept here so the work can start immediately; nothing below is built.
- **`RulesConfig`** (provisional, defaults from Phase 3 unless the observations say otherwise):
  - `CatchEnabled` (bool);
  - `CatchAllowedFrom` (`WindowClosed` / `Always`);
  - `CatchNeedsEvidence` (bool: the target played the rank this deal);
  - `CatchLockout` (`NextDeal` / `NextDeck`);
  - `CatchPoints` (1);
  - `CatchHiddenTringlaPoints` (1);
  - `CatchCappedLikeRonda` (bool).
- **Engine** (`RondaGame`):
  - Record each seat's **dealt hand** per deal.
  - `CanCatch(accuser, target, rank)` and `Catch(...)`; a `CatchResolvedEvent` (accuser, target, rank, success, lockedUntil).
  - `ScoreReason.Catch`, awarded through the existing `Award` so the 41 rule applies.
  - Not turn-bound, so `CurrentPlayer` is unchanged.
- **AI** (`SimpleAi`):
  - `ChooseCatch(game, seat)`, using only public information (cards the human has played this deal and whether they announced).
  - Optional pair hiding (D-2).
- **UI:** a target button on Nadia's panel plus a 10-rank picker (separate UI task; reuse the existing panel and dialog sprites).
- **Tests (deterministic, `ScriptedDeck` or the A-5 state factory):**
  - success +1;
  - wrong guess with lockout;
  - catch refused before the window closes or with no evidence (per flags);
  - no catch of an announced pair;
  - no double catch;
  - lockout lifts at the configured boundary;
  - a catch reaching 41 (per `CatchCappedLikeRonda`).
