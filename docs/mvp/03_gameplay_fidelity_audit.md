# MVP 03 — Gameplay Rules Fidelity Audit

**Date:** 2026-11-13 · **Scope:** local 1v1, human vs Nadia (rule-based AI) · **Type:** investigation only (no code, scene, UI or art changed).
**Updated 2026-11-13 after Phase 1** (end-of-deck and match-end tests): coverage in rows #30, #36, #38, #40–43, §4 (new §4.4) and §5. The engine is unchanged.
**Code audited:** `ronda_game/Assets/Zellige/Scripts/Core/*` (engine), `Scripts/AI/SimpleAi.cs`, `Scripts/Game/GameController.cs` (rules-relevant input flow only), `Tests/EditMode/RulesTests.cs`.
**Evidence used:** `docs/mvp/01_verified_rules_reference.md` (rule IDs `M-xx`, unresolved items `U-x`, contradictions `C-x`, examples `X-x`), which consolidates Phase 2 (`docs/03_game_rules_specification.md`, `docs/02a_phase2_consolidated_assessment.md`, matrix IDs `Rxx`) and Phase 3 (`docs/phase3/03_rules_and_scoring_spec.md`, rule IDs `RL-xx`). Planned test IDs (`CP-xx`, `ED-xx`, …) are from `docs/mvp/02_qa_checklist.md`.

---

## 0. Classification used

Every mechanic gets **two** labels, because "is the rule known?" and "is it in the code?" are separate questions.

| Axis | Label | Meaning |
|---|---|---|
| Evidence | **CONFIRMED (play)** | Seen applied in a live match of the reference game. |
| | **CONFIRMED (tutorial)** | Stated by the reference's own tutorial text; never seen applied live. Weaker. |
| | **UNKNOWN** | Insufficient evidence. Where the code still needs *some* behaviour, it uses a Phase 3 design default; that default is ours, not an observation. |
| Implementation | **IMPLEMENTED** | Present in the code and behaving as the established (or default) rule says. |
| | **SIMPLIFIED** | Deliberately implemented differently from the documented model, for the local MVP. |
| | **MISSING** | Not implemented. |

"Test coverage" says what the existing EditMode tests actually assert. **A passing suite is not treated as proof of a rule** (see §4: much of the end-of-deck logic is only checked for *self-consistency*, not against independent expected values).

---

## 1. Headline findings

1. **No implemented rule contradicts the evidence.** I traced every CONFIRMED rule through the engine and found no behaviour that disagrees with Phase 2 observations or the tutorial. I found no correctness bug in the rules as implemented.
2. **The biggest fidelity risk is unverified assumptions, not bugs.** These hold the game up but are UNKNOWN in the reference:
   - automatic capture with at most one card per rank;
   - free choice of card even when a capture is available;
   - non-dealer leads every deal;
   - the announcement timing model.

   The capture-obligation question (U-1) touches **every turn**, and the evidence to settle it is cheap to collect (§6).
3. **One confirmed mechanic is missing: Catch** (tutorial-confirmed; all its details are UNKNOWN). Because of it, an unannounced pair currently has no consequence.
4. **Announcements are simplified in three ways:**
   - the window closes after each player's first card instead of a 15 s timer;
   - the human's button announces *all* eligible pairs at once;
   - credit timing is an UNKNOWN that the docs contradict on (C-2).

   Announcements are frequent: a 4-card hand holds a pair or better **41.2 %** of the time (my own combinatorics: 37.8 % one pair, 1.8 % two pairs, 1.6 % three or four of a kind).
5. **Five provisional rules are hard-coded instead of living in `RulesConfig`.** These are tie = void, both-reach-41 = higher wins, no 12→1 wrap, non-dealer leads, and quad-as-two-rondas bonus counting. This breaks the project rule "isolate unverified mechanics in a configurable rules component".
6. **Test gaps:**
   - ~~End of deck, the dealer rule and match end have no deterministic tests.~~ **Closed in Phase 1 (2026-11-13), see §4.4.** 12 deterministic tests now cover ED-01, ED-02, ED-04…ED-07, ME-01, ME-02, ME-05, ME-06, DB-07 and RN-13. ED-03 is partial; ED-08, ME-03 and ME-04 are BLOCKED.
   - Still open: Missa *alone* is never tested.
   - Still open: one test's name claims a check it doesn't make (`…AndTablePersists` never asserts the table).

---

## 2. Rules verification matrix

Code references are `file:line` in `ronda_game/Assets/Zellige/Scripts/`. Test names are in `Tests/EditMode/RulesTests.cs`.

### 2.1 Deck, deal, turn order

| # | Mechanic | Evidence | Implementation | Current behaviour | Expected (if established) | Code | Test coverage | Next action |
|---|---|---|---|---|---|---|---|---|
| 1 | 40-card Spanish deck, ranks 1–7, 10–12 | CONFIRMED (play+tutorial) M-01 | IMPLEMENTED | 4 suits × 10 ranks; 8/9 rejected | same | `Core/Card.cs:78` `Deck.CreateStandard`, `:23` `Ranks.IsValid` | `StandardDeck_Has40UniqueCards_WithoutEightsOrNines`, `RankOrder_…` | none |
| 2 | Rank order, `next(7)=10` | CONFIRMED (tutorial) M-02 | IMPLEMENTED | as stated | same | `Core/Card.cs:36` `Ranks.TryGetNext` | `RankOrder_JumpsFromSevenToTen_AndStopsAtTwelve` | none |
| 3 | Shuffle | (fairness: design) | IMPLEMENTED | Fisher–Yates with `System.Random(seed)`; deterministic per seed | — | `Core/Card.cs:92` | `Shuffle_IsAPermutation_AndRepeatableWithTheSameSeed` | Optional: own PRNG for cross-runtime golden vectors (QA SH-04). Not needed for the MVP. |
| 4 | 4 cards each, 5 deals per deck (32→24→16→8→0) | CONFIRMED (play) M-05 | IMPLEMENTED | `CardsPerDeal = 4` | same | `Core/RondaGame.cs:251` `Deal`, `:277` `DrawCards` | deal 1 and deal 2 only (`StartMatch_…`, `NewDeal_…`); no full-deck stock sequence | Add a full-deck test (QA DL-02). |
| 5 | New deal only when both hands empty | UNKNOWN (consistent with M-05) M-06 | IMPLEMENTED | as stated | — | `RondaGame.cs:156` | `NewDeal_StartsWhenBothHandsAreEmpty_…` | none |
| 6 | Table empty at deck start, no cards dealt to the table | UNKNOWN (inferred) M-07 | IMPLEMENTED | `table.Clear()` at deck start | — | `RondaGame.cs:225` `StartDeckRound` | `StartMatch_…` (table 0) | Evidence item E-4 (§6). |
| 7 | Table persists between deals | UNKNOWN (inferred, A041) M-08 | IMPLEMENTED | table untouched by `Deal` | — | `RondaGame.cs:251` | **not asserted**: `NewDeal_…_AndTablePersists` checks deal number, hands, stock, turn and card total, but never the table | Fix the test to assert the table contents (QA DL-03). |
| 8 | First dealer | UNKNOWN M-10 | IMPLEMENTED (random) | random via engine RNG; tests can fix it | — | `RondaGame.cs:70` `StartMatch(firstDealer)` | `SameSeed_GivesTheSameDeal` | none |
| 9 | Dealer alternates each deck | UNKNOWN (inferred, R27) M-09 | IMPLEMENTED | flag `DealerAlternates = true` | — | `RondaGame.cs:228`; `Core/RulesConfig.cs:61` | **none** (self-play only reads the dealer back from events) | Add a test (QA DL-05). |
| 10 | Non-dealer leads every deal | UNKNOWN M-11 (R28) | IMPLEMENTED, **hard-coded** | `CurrentPlayer = Other(Dealer)` | — | `RondaGame.cs:257,266` | `StartMatch_…`, `NewDeal_…` | Move into `RulesConfig` (plan A-2); evidence E-5. |
| 11 | One card per turn, committed | CONFIRMED (tutorial) M-13 | IMPLEMENTED | `PlayCard` validates turn and card ownership | same | `RondaGame.cs:109` | `IllegalPlays_AreRejected_AndChangeNothing` | Add play-after-match-end rejection and same-card-twice (QA IL-03/IL-04). |

### 2.2 Capture

| # | Mechanic | Evidence | Implementation | Current behaviour | Expected (if established) | Code | Test coverage | Next action |
|---|---|---|---|---|---|---|---|---|
| 12 ★1 | **Capture by matching rank** | CONFIRMED (tutorial; live state consistent) M-14 | IMPLEMENTED | played card + table card of same rank → capturer's pile | same | `Core/CaptureRules.cs:16` `FindCaptures`; `RondaGame.cs:121-130` | `MatchingRank_CapturesOnlyThatCard_…` (E1) | none |
| 13 | Upward chain (7→10 jump, stops at first gap) | CONFIRMED (tutorial) M-15 | IMPLEMENTED | as stated | same | `CaptureRules.cs:16-25` | `Chain_ContinuesUpward_…` (E2), `Chain_JumpsFromSevenToTen` (E3) | Add "chain is upward only" (QA CP-05) and a long chain (CP-06). |
| 14 | No wrap 12→1 | UNKNOWN M-04 | IMPLEMENTED, **hard-coded** | chain stops at 12 | — | `Card.cs:36` | `Chain_DoesNotWrapFromTwelveToOne` | Optional flag `ChainWrap` (plan A-2). |
| 15 | Drop when no rank matches | CONFIRMED (play) M-16 | IMPLEMENTED | card added to table | same | `RondaGame.cs:138-141` | `NoMatchingRank_CapturesNothing`, `Drop_PutsCardOnTable_…` | none |
| 16 | **Capture is automatic** (cannot drop a matching card) | UNKNOWN M-17 (R10/R11; D-G-06 unapproved) | IMPLEMENTED (design default) | a matching card always captures | — | `RondaGame.cs:121` | implicit | **Evidence E-1** (§6). Decision D-G-06. |
| 17 ★2 | **Multiple matching cards on the table** | UNKNOWN M-18 (R11) | IMPLEMENTED by construction: **cannot occur** | Rule 16 and an empty starting table mean at most 1 card per rank, so a duplicate never arises. If one existed, `FindCaptures` would take only the first. | — | `CaptureRules.cs:30` `TryFindRank` | invariant asserted after every play in 80 self-play matches (`FullMatchTests`) | Keep. Revisit only if E-1 shows optional capture (then the reference's duplicate rule is needed, P2 Q1). |
| 18 | **Capture obligation** (must capture when able) | UNKNOWN M-19 (U-1; P2 test E3 invalid) | IMPLEMENTED as flag, default off | any hand card legal (`MustCapture = false`); `true` restricts to capturing cards | — | `RulesConfig.cs:20`; `RondaGame.cs:82` `GetLegalCards` | `MustCapture_LimitsLegalCardsToCapturingOnes` (flag on); free choice **with a capture available** not tested (QA CP-10) | **Evidence E-1, highest priority** (§6). UI and AI already consult `GetLegalCards`/`CanPlay`, so flipping the flag needs no other code. |

### 2.3 Scoring during play

| # | Mechanic | Evidence | Implementation | Current behaviour | Expected (if established) | Code | Test coverage | Next action |
|---|---|---|---|---|---|---|---|---|
| 19 ★4 | **Missa** (capture empties table → +1) | CONFIRMED (play+tutorial) M-20 | IMPLEMENTED | +1 after any capture that leaves the table empty | same | `RondaGame.cs:133-136` | only combined with Derba (`CapturingTheCardJustDropped_…`, E8). **Missa alone never tested** (E4/QA MS-01); "no Missa when cards remain" (MS-02) untested | Add MS-01, MS-02. |
| 20 | Missa on the final play of a deck | UNKNOWN M-21 | IMPLEMENTED as flag `MissaOnFinalPlay = true` | awarded | — | `RondaGame.cs:124,133`; `RulesConfig.cs:26` | **none** (MS-03) | Add test for both flag values. |
| 21 ★3 | **Derba** (capture the card the opponent just dropped → +1) | CONFIRMED (tutorial definition; "Derba! +1" seen live, trigger not itemised) M-22, C-4 | IMPLEMENTED | +1 when the previous play was the opponent's drop of the same rank | same | `RondaGame.cs:117,132`, `:180` `WouldBeDerba` | `…ScoresDerba_AndMissa…` (E8), `CapturingAnOlderDrop_IsNotDerba` (E7), `Derba_WithoutClearingTable_ScoresOne` (E6) | Add Derba + chain = +1 only (QA DB-04). |
| 22 | Derba carries across a deal boundary; reset at deck start | UNKNOWN M-23 | IMPLEMENTED | the last play persists across deals and is cleared in `StartDeckRound` | — | `RondaGame.cs:143-146, 244` | **none** (QA DB-05, DB-06) | Add tests; evidence E-6. |
| 23 | Derba + Missa + chain on one play add up | UNKNOWN M-24 | IMPLEMENTED | Derba then Missa awarded separately | — | `RondaGame.cs:132-136` | Derba+Missa only | none |
| 24 | No Derba escalation in 1v1 | CONFIRMED (tutorial: 2v2 only) M-25 | IMPLEMENTED (by absence) | — | same | — | — | Out of scope (2v2). |
| 25 ★5 | **Ronda: announce pair, value 1** | CONFIRMED (tutorial; button seen live) M-26 | IMPLEMENTED | `Announce(seat, rank)` with ≥ 2 of the rank, before playing one | same | `RondaGame.cs:189` `GetAnnounceableRanks`, `:208` `Announce` | `LoneRonda_ScoresOne_…` (E12), `Announce_IsRefused_…` | none for the value; see rows 28–31 for the model |
| 26 | Two pairs = two rondas (value 2) | CONFIRMED (play, unpreserved video) M-27 | IMPLEMENTED | each rank announced separately | same | as above | `TwoRondas_ScoreTwoPlusBonus` (E14 with valid ranks) | none |
| 27 ★6 | **Tringla: three of a kind, value 5** | CONFIRMED (tutorial) M-28 | IMPLEMENTED | `TringlaPoints = 5` | same | `RondaGame.cs:213-216`; `RulesConfig.cs:38` | `TringlaBeatsRonda_…` (E9), `LowerTringlaWins` (E11), `TringlaPoints_CanWinTheMatch` (E17, scaled) | none |
| 28 | Contest: tringla > ronda; higher ronda wins; **lower** tringla wins; compare strongest | CONFIRMED (tutorial) M-33 | IMPLEMENTED | as stated, using rank order (12 highest) | same | `RondaGame.cs:321` `FindContestWinner`, `:348` `CompareStrength` | E9, E10, E11, E14 | Add a ronda 10 vs 7 test across the 7→10 jump (QA RN-09). |
| 29 | Contest scoring: winner gets own values + 1 per opposing announcement; loser 0 | CONFIRMED (tutorial) M-34 | IMPLEMENTED | two ledger entries (`Announcement`, `AnnouncementBonus`) | same | `RondaGame.cs:300-318` | E9, E10, E11, E14 | Add the case where the winner announced fewer than the loser (QA RN-07). |
| 30 | Ronda cap at 40; tringla award uncapped | CONFIRMED (tutorial) M-37 / UNKNOWN M-38 | IMPLEMENTED | cap applied when winner holds no tringla; cap before the 41 check (C-9) | same | `RondaGame.cs:316, 393-414`; `RulesConfig.cs:52` | `RondaPoints_CannotReachTheTarget` (scaled), `TringlaPoints_CanWinTheMatch` (scaled); **RN-13 at the real target: `ME01_MissaToExactly41` (two rondas at 40 → requested 2, granted 0)** | RN-15 (tringla+ronda uncapped) still open; it rests on the unverified M-38. |
| 31 | **Announcement window and credit timing** | UNKNOWN M-31, M-32 (U-3; C-2: Phase 2 test credits instantly, Phase 3 at window close) | **SIMPLIFIED** | Window opens at each deal and closes after **2 plays** (each player one card). Announcements are sealed until it closes, then resolved and credited once. The reference uses a ≈ 15 s timer running during play. | not established | `RondaGame.cs:149-154, 265, 287`; `RulesConfig.cs:49` `AnnounceWindowPlays` | implicit in all announcement tests | **Evidence E-2** (§6) + owner decision (doc 01 Q2). |
| 32 | Human chooses which pairs to announce | (tutorial: per-pair announcement) | **SIMPLIFIED** | the "RONDA!/TRINGLA!" button announces **every** eligible rank at once; only on the human's own turn while the window is open | — | `Game/GameController.cs:84` `OnAnnounceClicked`, `:156` `RefreshPrompt` | none (UI) | Decide whether per-pair choice matters (it only matters once Catch exists; plan B-2). |
| 33 | Same kind and rank on both sides | UNKNOWN M-36 (U-4; C-3: tutorial says "nobody scores *yet*" + a derba clause) | IMPLEMENTED (void), **hard-coded** | nobody scores, nothing further | not established | `RondaGame.cs:329` | `EqualRondas_ScoreNothing` | Move into `RulesConfig` (`AnnounceTie`); evidence E-3. |
| 34 | Four of a kind | UNKNOWN M-29 (U-5) | IMPLEMENTED as flag `QuadCountsAsTringla = true` | one tringla, 5 | — | `RondaGame.cs:213-216` | **none** (QA RN-08) | Add a test. Note: with the flag **off** the engine records **one** announcement worth 2, so the opponent's bonus counts it as 1 announcement, not 2. Fix if that variant is ever enabled (plan C-3). |
| 35 ★7 | **Catch** (accuse an unannounced pair: +1, or lockout) | CONFIRMED (tutorial, never played live) M-39; details UNKNOWN M-40 (U-11) | **MISSING** | an unannounced pair has no consequence | core: correct → +1 to accuser; wrong → locked out "until the next round" | — | — | **Investigated 2026-11-17** (`05_catch_specification.md`). Core confirmed at tutorial level; timing, proof, lockout scope, tringla value and cap unknown. **Not implemented**: owner decisions D-1…D-4 and observations O-1…O-7 needed. |

### 2.4 End of deck and match

| # | Mechanic | Evidence | Implementation | Current behaviour | Expected (if established) | Code | Test coverage | Next action |
|---|---|---|---|---|---|---|---|---|
| 36 ★8 | **Cards left on the table at deck end → last capturer** | CONFIRMED (tutorial; A039→A041 fits) M-42 | IMPLEMENTED | leftover added to `lastCapturer`'s pile before scoring | same | `RondaGame.cs:356-371` `TallyDeck` | **Deterministic (Phase 1):** `ED01_LeftoverCards_GoToTheLastCapturer` (leftover {5,12} to the dealer) and the leftover assertions in ED-02, ED-04, ED-05, ED-06, ME-02. ED-03's exact observed numbers are not reproduced (§4.4). | none |
| 37 | No capture in a whole deck → leftover discarded | (RL-19) | IMPLEMENTED (unreachable) | discarded; no dealer penalty | — | `RondaGame.cs:362, 373` | none | Leave; unreachable under row 16 (doc 01 derived fact). |
| 38 ★9 | **Dealer rule: dealer not last to capture → non-dealer +5** | CONFIRMED (play, A021) M-43 | IMPLEMENTED | +5 to `Other(Dealer)` when `lastCapturer != Dealer` | same | `RondaGame.cs:373-376` | **Deterministic (Phase 1):** ED-02 (reproduces A021: +5 and 32 cards → +12), ED-04 (dealer took last: nothing), ED-05 (+5 only), ED-06 (+5 to one player, +10 to the other), ME-02 | none |
| 39 | Dealer King/Ace sentence | UNKNOWN M-46 (U-6) | MISSING (by decision) | ignored | not established | — | — | Leave off; evidence only if wanted. |
| 40 | +1 per captured card over 20 | CONFIRMED (play: 32 → +12) M-44 | IMPLEMENTED | `max(0, pile − 20)` after leftover | same | `RondaGame.cs:377-381` | **Deterministic (Phase 1):** ED-02 (+12), ED-06 (+10), ED-07 (leftover lifts 20 → 22: +2), ED-04/ED-05 (exactly 20: +0), ME-02 (+14) | none |
| 41 | Tally order leftover → dealer → over-20, then evaluate | UNKNOWN M-45 | IMPLEMENTED | as stated; no mid-tally match end | — | `RondaGame.cs:356-391` | Phase 1 tests assert the **values** of each tally award and that the winner is decided after the whole tally (ME-02). They do **not** assert the order of the awards, because the order is unverified. | none until evidence |
| 42 ★10 | **Reaching 41 during play ends the match at once** | CONFIRMED (play, unpreserved video) M-48 | IMPLEMENTED | `Award` ends the match when a non-capped award reaches 41; later awards on the same play are skipped | same | `RondaGame.cs:393-414, 417` | **Deterministic at the real 41 (Phase 1):** `ME01_MissaToExactly41` (human wins / human loses; 40 before, 41 after, no tally, no further play) and `DB07_DerbaToExactly41`. The deciding captures are built so no second point lands on the same play, because whether a simultaneous Missa still counts after 41 is unknown. | none |
| 43 | 41 reached at the tally; both ≥ 41 → higher wins; tie → another deck | CONFIRMED for one player reaching 41 (M-47, M-49) / UNKNOWN for both (M-50, U-7) | IMPLEMENTED, **hard-coded** | as stated | — | `RondaGame.cs:385-390` | **Deterministic (Phase 1):** `ME02_TallyLiftsPast41_…` (40 is not a win; 40 → 54 after the whole tally). **Both ≥ 41 (ME-03) and the exact tie (ME-04) are BLOCKED:** no evidence. | Evidence or owner decision for M-50; then add ME-03/ME-04 with `BothReachTarget` (plan A-2). |
| 44 | Score ledger with reasons | design (U07 pattern) M-51 | IMPLEMENTED | `ScoreAwardedEvent{Reason, Points, Requested}` | — | `Core/GameEvents.cs` | `FullMatchTests` (sum of events = score) | none |

### 2.5 AI (Nadia)

| # | Aspect | Status | Current behaviour | Code | Tests | Next action |
|---|---|---|---|---|---|---|
| 45 | Legal moves only, no duplicated rules | IMPLEMENTED | Uses `GetLegalCards`, `CaptureRules.FindCaptures` and `WouldBeDerba` from the engine. Exception: it re-derives "Missa" as "captures every table card" and ignores `MissaOnFinalPlay`, a harmless heuristic difference. | `AI/SimpleAi.cs:28-68` | `Ai_ChoosesALegalCard_…`, 40 AI self-play matches | none |
| 46 | Capture preference | IMPLEMENTED | `100 + 10/card`, +60 Missa, +60 Derba; drops avoid feeding chains | `SimpleAi.cs:48-68` | `Ai_PrefersACapture_OverADrop` | Optional: prefer the longer chain test (QA AI-03). |
| 47 | Announcements | IMPLEMENTED (always announces everything) | never hides a pair, so it is immune to a future Catch | `SimpleAi.cs:22` | `Ai_AnnouncesItsPairs` | Revisit with Catch (B-1). |
| 48 | End-of-deck awareness (dealer rule, last capture) | not considered | ignores who will make the last capture | — | — | Optional AI improvement (C-5). |

---

## 3. Code review notes (rules engine)

| # | Finding | Severity | Detail |
|---|---|---|---|
| R-1 | No behaviour contradicts the evidence | — | Every CONFIRMED rule in rows 1–44 traces to code that behaves as documented. |
| R-2 | Provisional rules hard-coded (**resolved 2026-11-17**: H-1…H-10 are `RulesConfig` settings, defaults unchanged; see doc 06) | Medium (process) | Rows 10, 14, 33, 43 and the quad-off bonus count (row 34) are UNKNOWN rules fixed in code, not in `RulesConfig`. The MVP brief requires unverified mechanics to be isolated in configuration. |
| R-3 | Invalid seat index throws `IndexOutOfRangeException` | Low (robustness) | `GetHand(seat)`, `GetAnnounceableRanks(seat)` and `Announce(seat, …)` index arrays without validating `seat` (QA IL-06). `PlayCard` is safe (returns illegal). Not reachable from the UI. |
| R-4 | Announcement resolution order on the 2nd play | Low (unknown rule) | On the play that closes the window, Derba/Missa are scored **before** the announcement contest. If that Derba/Missa reaches 41 the pending announcements are discarded. Phase 3 lists this case (doc 03 §12) and it is consistent with it, but with a real 15 s window the order could differ. Depends on E-2. |
| R-5 | First player's announcement window is shorter than the second's | Low (simplification) | With `AnnounceWindowPlays = 2` the first player must announce before their first card; the second player may see one card first. The reference's timer gives both roughly the same time. |
| R-6 | Quad-off variant counts one announcement | Low (non-default) | See row 34. **Now configurable (H-9, 2026-11-17).** |
| F-1 | After an exact tie at or above the target, the next deck ends at its first point | Medium (unverified rule, documented 2026-11-17) | Both players are already past 41, so the during-play check "reaching the target ends the match" fires on the first non-capped point of the extra deck. Phase 3 RL-23(b) describes the extra deck as decided "after a tally". Both are proposals; the reference is UNKNOWN (M-50). **Not changed:** pinned by the characterisation test `H3_AfterATieAtTheTarget_TheFirstPointOfTheNextDeckWins_CurrentBehaviour`. Resolve together with M-50 (ME-03/ME-04). |

---

## 4. What the 109 EditMode tests actually cover

### 4.1 Breakdown

| Group | Tests | What they prove |
|---|---|---|
| `DeckTests` | 3 | deck composition, shuffle determinism and permutation, rank order |
| `CaptureTests` | 5 | `FindCaptures` in isolation (no engine state): E1, E2, E3, E5, no wrap |
| `EngineTests` | 9 | first deal and turn, determinism, illegal plays, drop, Derba (E6, E7, E8), `MustCapture`, second deal |
| `AnnouncementTests` | 9 | lone ronda (E12), contest rules (E9, E10, E11, E13, E14), refusal cases, ronda cap and tringla win (scaled targets) |
| `FullMatchTests` | **80** (2 tests × 40 seeds) | **properties**, not rules: legal plays, 40-card conservation, ≤ 1 card per rank, termination, score = sum of awards, winner ≥ 41 and ahead, tally formulas consistent with the engine's *own* reported last capturer, dealer and pile counts |
| `AiTests` | 3 | capture preference, no state mutation, announces pairs |

**29 hand-written cases + 80 parameterised self-play runs = 109.** The self-play runs are strong invariant checks, but they compare the engine with itself. A wrong "last capturer" or a leftover sent to the wrong player would still pass, because the check reads those values from the engine's own events. **Phase 1 demonstrated this with a deliberate defect (§4.4).**

**Phase 1 additions (`Tests/EditMode/EndOfDeckTests.cs`, 12 tests, suite now 121):**

| Group | Tests | What they prove |
|---|---|---|
| `EndOfDeckTests` | 6 | ED-01, ED-02, ED-04, ED-05, ED-06, ED-07 on complete scripted 40-card decks: leftover destination, dealer rule and over-20 values against hand-computed results (ED-02 reproduces the live A021 numbers) |
| `MatchEndTests` | 6 | ME-01 (human wins and human loses at exactly 41 by Missa, with RN-13), DB-07 (Derba to exactly 41), ME-02 (tally lifts 40 → 54, winner after the whole tally), ME-05 (configured target 21 and cap 20), ME-06 (clean restart, identical replay) |

### 4.2 Requested edge cases

| Edge case | Covered? | Where / gap |
|---|---|---|
| Empty table | Partly | a drop onto an empty table (`Drop_PutsCardOnTable_…`) is covered, but "no Missa on a drop" isn't asserted (QA CP-09) |
| No matching card | Yes | `NoMatchingRank_CapturesNothing`, `Drop_…` |
| One matching card | Yes | E1 |
| Multiple matching cards | By invariant only | cannot occur (row 17); the ≤ 1-per-rank invariant is asserted after every play of 80 matches |
| Last card of a deal | Partly | the transition to deal 2 is tested, but the table isn't asserted, and Derba across the boundary (DB-05) isn't tested |
| Last card of the deck | **Yes (Phase 1)**, except Missa on the final play | ED-01, ED-02, ED-04…ED-07, ME-02 play the 40th card of a scripted deck. MS-03 (Missa on the final play) stays untested **on purpose**: the rule is unverified (M-21) and every fixture avoids it. |
| Simultaneous scoring events | Mostly (Phase 1) | Derba + Missa (E8). Two tally awards on one deck to different players: ED-06, ME-02. Dealer +5 and over-20 to the same player: ED-02. Still missing: Derba + chain (DB-04); capture scoring and the announcement contest on the same play (R-4) |
| Winning/losing at a scoring boundary | Mostly (Phase 1) | Exactly 41 by Missa (ME-01, human wins and loses) and by Derba (DB-07); 40 is not a win and the tally lifts past 41 (ME-02); a ronda at 40 granting 0 (RN-13 in ME-01); configured target (ME-05). **BLOCKED:** both ≥ 41 after a tally (ME-03) and an exact tie (ME-04), M-50 unresolved |

### 4.3 Against the QA plan (`02_qa_checklist.md`, 79 engine cases)

| Section | Covered | Partial | Not covered |
|---|---|---|---|
| 1.1 Deck, shuffle, deal (14) | 8 | 4 (SH-03, DL-02, DL-03, DL-04) | 2 (SH-04, DL-05) |
| 1.2 Capture (12) | 6 | 2 (CP-09, CP-12) | 4 (CP-04, CP-05, CP-06, CP-10) |
| 1.3 Missa, Derba (10) | 3 | 0 | 7 (MS-01, MS-02, MS-03, DB-04, DB-05, DB-06, DB-07) |
| 1.4 Announcements (16) | 10 | 1 (RN-16) | 5 (RN-07, RN-08, RN-09, RN-13, RN-15) |
| 1.5 End of deck, match (14) | **0** | 6 (ED-02, ED-04, ED-05, ED-06, ME-02, ME-05) | 8 (ED-01, ED-03, ED-07, ED-08, ME-01, ME-03, ME-04, ME-06) |
| 1.6 Illegal actions (6) | 2 | 1 (IL-04) | 3 (IL-03, IL-05, IL-06) |
| 1.7 Self-play (7) | 0 | 5 | 2 (SP-04, SP-06) |
| **Total (79), before Phase 1** | **29** | **19** | **31** |

Before Phase 1 the largest gap was **end of deck and match end: 0 of 14 cases were covered deterministically**. This is the scoring that decides most matches: the dealer +5 and over-20 points routinely move a score by 5–17.

**After Phase 1:**

| Section | Covered | Partial | Not covered | BLOCKED |
|---|---|---|---|---|
| 1.3 Missa, Derba (10) | 4 (+DB-07) | 0 | 6 | — |
| 1.4 Announcements (16) | 11 (+RN-13) | 1 | 4 | — |
| 1.5 End of deck, match (14) | **11** (ED-01…07, ME-01, 02, 05, 06; ED-03 since 2026-11-17) | 0 | 0 | 3 (ED-08, ME-03, ME-04) |
| Other sections | unchanged | unchanged | unchanged | — |
| **Total (79)** | **42** | **13** | **21** | **3** |

**2026-11-17 additions (suite 126 → 175, including the 4 code-review tests):**
- `GoldenMasterTests` (2): event-stream fingerprints of 40 full matches under the default rules, recorded before any Phase D change.
- `StateFactoryTests` (11): the A-5 factory, including ED-03.
- `RulesSettingsTests` (32): H-1 … H-10.

### 4.4 Phase 1 results (2026-11-13)

**Method.**
- **Fixtures:** only the first deck can be fixed through the public API (`StartMatch(firstDealer, firstDeckOrder)`), so every end-of-deck fixture is a complete scripted 40-card deck.
- **Readability:** the decks are built from three repeating deal patterns plus one final deal, so each one can be checked by hand. `ScriptedDeck` asserts the expected player before every play.
- **Expected values:** worked out by hand from the confirmed rules (M-42, M-43, M-44, M-47, M-48 and the announcement values), and cross-checked with an independent Python model of the documented rules, not with the engine.
- **Unverified rules avoided:** no fixture depends on Missa on the final play, Derba across a deal boundary, four of a kind, the order of tally awards, or both players reaching 41.

**Results:** 121 / 121 pass (109 previous + 12 new). **No engine defect found.**

**Sensitivity check (mutation test, engine restored byte-for-byte afterwards, MD5 verified):**

| Deliberate defect | New tests failing | Old suite (109) |
|---|---|---|
| Leftover cards sent to the wrong player | 7: ED-01, 02, 04, 05, 06, 07, ME-02 | **all 109 still passed**: the self-play invariants cannot see it |
| Match not decided after a tally | ME-02 | 2 of the 80 self-play runs failed, by chance of the seed |

**Case status:**

| Case | Status | Note |
|---|---|---|
| ED-01, 02, 04, 05, 06, 07 | Covered | ED-02 reproduces the live-play numbers of A021 (8 / 32 cards; +5 and +12). |
| ED-03 | **Covered (2026-11-17)** | Reproduced with the exact observed numbers (23 + 3 leftover = 26 → +6; other 14; no +5) by `StateFactoryTests.ED03_…`, using the A-5 state factory. (Earlier: an exhaustive search had found no first-deck script for it.) The observed decomposition itself remains INFERRED. |
| ED-08 | **BLOCKED** | "No capture in a whole deck" cannot happen under the current capture model (≤ 1 card per rank, so the table holds at most 10 cards). The dealer-rule outcome for that case is also unspecified (C-7). It needs the capture-model decision (E-1) and a rule. |
| ME-01, 02, 05, 06; DB-07; RN-13 | Covered | ME-01 runs for both seats: human victory and human defeat. |
| ME-03 | **BLOCKED** | Both players ≥ 41 after one tally (M-50 / U-7, reference UNKNOWN). The current "higher wins" is a Phase 3 proposal. |
| ME-04 | **BLOCKED** | Exact tie at or above 41 after a tally (M-50, U-7). The current "play another deck" is a proposal. |

---

## 5. The ten focus mechanics

| ★ | Mechanic | Verdict |
|---|---|---|
| 1 | Capturing matching ranks | CONFIRMED + IMPLEMENTED + tested (E1–E3). The *automatic* capture and *free choice* around it are UNKNOWN (rows 16, 18). |
| 2 | Multiple matching cards | UNKNOWN in the reference; impossible in our model by construction; invariant-tested. Depends on E-1. |
| 3 | Derba | CONFIRMED (tutorial definition) + IMPLEMENTED + tested (E6–E8). Behaviour across deals and in combination with a chain is UNKNOWN/untested. |
| 4 | Missa | CONFIRMED + IMPLEMENTED; **tested only combined with Derba**; final-play behaviour is a flag with no test. |
| 5 | Ronda | Value and contest CONFIRMED (tutorial) + IMPLEMENTED + tested. Window and credit timing UNKNOWN and **SIMPLIFIED**; human announces all pairs at once (SIMPLIFIED). |
| 6 | Tringla | CONFIRMED (tutorial) + IMPLEMENTED + tested; quad UNKNOWN (flag, untested). |
| 7 | Catch | CONFIRMED (tutorial) + **MISSING**; all details UNKNOWN. |
| 8 | Cards left at deck end | CONFIRMED + IMPLEMENTED; **deterministically tested (Phase 1)**. |
| 9 | Dealer scoring | +5 rule CONFIRMED (play) + IMPLEMENTED, **deterministically tested (Phase 1)**, including the live A021 numbers. Alternation and "non-dealer leads" are UNKNOWN; King/Ace sentence UNKNOWN and not implemented. |
| 10 | Reaching 41 | Mid-play end CONFIRMED (video) + IMPLEMENTED, **tested at the real 41 by Missa and by Derba, for both seats (Phase 1)**. A single player passing 41 at the tally is tested; both ≥ 41 and an exact tie are BLOCKED (UNKNOWN, hard-coded). |

---

## 6. Evidence needed (to collect before changing behaviour)

Collected in a clean observation session of the reference (Phase 2 `02a` §13, "OQ-R1"), each item with a screenshot or recording timestamp. **E-1 is the highest priority.**

| ID | Question | Decides | How to observe |
|---|---|---|---|
| **E-1** | With a hand card that matches a table card, can the player play a **different** card? And can a matching card ever be placed without capturing (two cards of one rank on the table)? | rows 16, 17, 18 (U-1, U-2) | Deal where the human holds a capturing card: (a) tap a non-capturing card. Is it accepted or refused? (b) Watch the table for a duplicate rank. Repeat ≥ 3 times. The Phase 2 attempt (P2 test E3) was invalid. |
| **E-2** | When do ronda/tringla points appear on the scoreboard (at the tap, or when the countdown ends)? Is the opponent's announcement visible before the countdown ends? Can a player announce after playing their first card of the deal? | rows 31, 32 (U-3, C-2) | Record the scoreboard, the countdown and the opponent area through 3–5 deals where the human holds a pair; announce at different moments. |
| E-3 | Both players announce the same rank: what happens now and later in that deal? | row 33 (U-4, C-3) | Requires a rare deal; collect opportunistically. |
| E-4 | Is the table empty when a deck starts? Are cards ever dealt face-up to the table? | row 6 | First frame after each deck's deal. |
| E-5 | Who plays first in each deal, relative to the dealer token? | row 10 (U-8) | Note the token and the first mover for 3 deals. |
| E-6 | Does a Derba apply to the first play of a new deal (on the dealer's last drop)? | row 22 | Opportunistic. |
| E-7 | Catch: the button or gesture, when it is enabled, what a correct and a wrong catch show, and how long the lockout lasts | row 35 (M-40) | Leave a pair unannounced and watch the opponent; try one catch. |

The product-owner questions in `01_verified_rules_reference.md` §5 remain open. Q1 (capture model), Q2 (announcement model for the local game) and Q4 (Catch in the MVP) gate the plan in `04_gameplay_implementation_plan.md`.
