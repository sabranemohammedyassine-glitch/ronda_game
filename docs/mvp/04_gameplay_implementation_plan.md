# MVP 04 — Gameplay Implementation Plan (next phase)

**Date:** 2026-11-13 · **Input:** `03_gameplay_fidelity_audit.md` (row numbers `#n`, evidence items `E-n`) · **Scope:** local single-player against Nadia only.
**Ground rules for this phase:**
- **Rules:** no rule is changed without evidence or an owner decision; every provisional behaviour stays switchable in `RulesConfig`.
- **Tests:** the existing 109 tests keep passing; new tests use hand-built decks (`TestDecks.Stacked`) and assert independently computed expected values, never values read back from the engine.
- **Scene and art:** the scene, UI and artwork are not part of this plan, except for the one UI item explicitly marked (B-2).

---

## A. Critical correctness work (do first, no new evidence needed)

The audit found **no implemented rule that contradicts the evidence**. The critical items below are about *proving* correctness where the suite currently only checks self-consistency, and about the project rule on configurable provisional mechanics.

| ID | Work | Why | Acceptance | Files |
|---|---|---|---|---|
| **A-1** ✅ | **Deterministic end-of-deck and match-end tests.** **Done 2026-11-13** (`Tests/EditMode/EndOfDeckTests.cs`, 12 tests, suite 121/121). | Before: 0 of 14 cases covered deterministically. After: 10 covered, ED-03 partial, 3 BLOCKED (audit §4.4). | **Covered:** ED-01, 02, 04–07, ME-01 (both seats), ME-02, ME-05, ME-06, DB-07, RN-13. ED-02 reproduces X-13 (A021: +5, +12). **Partial:** ED-03, whose X-14 decomposition was not reproduced (see A-5). **BLOCKED:** ED-08, ME-03, ME-04 (no evidence). **Deliberately not tested:** MS-03 and RN-15 (unverified rules, see B-3 and C-3). A mutation check showed the old suite misses a leftover defect that the new tests catch. | `Tests/EditMode/EndOfDeckTests.cs` (`ScriptedDeck` helper) |
| **A-5** (new) | **Test-only state setup.** An internal factory (via `InternalsVisibleTo("Zellige.Tests.EditMode")`) that starts a deal from a given table, hands, piles, scores, dealer and last play. | Only the first deck can be scripted today, so every end-of-deck fixture must be a full 40-card deck. Some cases can't be built that way: ED-03's exact numbers, and ME-03/ME-04 once M-50 is decided. Small fixtures are also easier to review. | Factory used by new tests only; the game's public API is unchanged; ED-03 reproduced with X-14's numbers (23 + 3 = 26 → +6). | `Core/RondaGame.cs` (internal constructor/factory, no behaviour change), `Core/AssemblyInfo.cs` |
| **A-2** | **Move hard-coded provisional rules into `RulesConfig`**, defaults unchanged: `AnnounceTie` (void), `BothReachTarget` (higher wins), `ChainWrap` (off), `FirstPlayer` (non-dealer). Also fix the opponent-bonus count for the non-default quad variant. | Project rule: unverified mechanics must be configurable (#10, #14, #33, #34, #43). Lets E-1…E-5 outcomes be applied by changing data, not code. | Each flag has a test for its default *and* its alternative; all 109 existing tests pass unchanged. | `Core/RulesConfig.cs`, `Core/RondaGame.cs`, `Core/Card.cs` (wrap), `Core/CaptureRules.cs` |
| **A-3** | **Fill the remaining quick test gaps.** | Several rules are implemented but untested or tested only in combination. | Missa alone (MS-01) and no Missa (MS-02); Derba + chain = +1 (DB-04); Derba across a deal (DB-05) and none at deck start (DB-06); chain upward only (CP-05) and long chain (CP-06); free choice with a capture available (CP-10); dealer alternates (DL-05); full stock sequence (DL-02); fix `NewDeal_…_AndTablePersists` to assert the table; ronda 10 vs 7 (RN-09); winner with fewer announcements (RN-07); quad (RN-08); play after match end and same card twice rejected (IL-03, IL-04). | `Tests/EditMode/RulesTests.cs` (additions only) |
| **A-4** | **Validate the seat argument** in `GetHand`, `GetAnnounceableRanks` and `Announce`. | Robustness (audit R-3): an invalid seat currently throws `IndexOutOfRangeException`. | Invalid seat → `ArgumentOutOfRangeException` or a refused result; tests IL-05, IL-06. | `Core/RondaGame.cs` |

**Exit criterion for phase A** (after A-1: 41 of 79 covered, 14 partial, 21 not covered, 3 BLOCKED):
- At least 45 of the 79 QA engine cases fully covered (A-3 alone adds more than 10).
- No hard-coded provisional rule left.
- The rules the game plays by are unchanged, so the default-configuration behaviour is identical.

---

## B. Missing core mechanics (gated by evidence or owner decision)

| ID | Mechanic | Gate (must be answered first) | Proposed design once the gate is passed | Size |
|---|---|---|---|---|
| **B-0** | **Capture model: obligation and automatic capture** (#16–18) | **E-1** (observation) and owner decision D-G-06 / D-G-07 (doc 01 §5 Q1) | **Obligation:** if the reference forces captures, set `MustCapture = true` (already implemented: engine, UI and AI already respect `GetLegalCards`). Then add a UI cue explaining why a card is refused. **Optional capture:** if the reference allows dropping a matching card, that is a **model change** (table becomes a multiset; capture rule for duplicates needed, P2 Q1). Re-plan before coding. | XS (obligation) / L (optional capture) |
| **B-1** (investigated 2026-11-17, see `05_catch_specification.md`; not implemented) | **Catch** (#35) | Owner decision (doc 01 §5 Q4), then **E-7** for details: (a) Is Catch in the MVP? (b) May Nadia hide pairs? Without that, the human can never catch her. (c) Does Nadia catch the human? (d) Lockout scope: deal or deck? (e) Value of a hidden tringla: 1 or 5? | **Engine:** `Catch(seat, target, rank)` command, allowed after the window closes. Uses the evidence rule `catchEvidence = playedThisDeal` (default, M-40) and `catchLockout` / `catchValueTringla` flags; new `ScoreReason.Catch`; events for success and failure. **AI:** a policy to catch when the human has played a rank twice without announcing, plus optionally hiding a weak pair (probability flag). **UI:** a catch control on Nadia's panel with a rank picker (UI work, separate task). | M (engine and AI) + M (UI) |
| **B-2** | **Per-pair announcement choice for the human** (#32) | Only meaningful once **B-1** exists: without Catch, announcing everything is never worse than hiding, except that it hands the opponent bonus points. Owner decision. | Button opens a small choice when several ranks are eligible; engine already supports per-rank `Announce`. **UI change**: separate task, outside this rules phase. | S |
| **B-3** | **Announcement window and credit timing** (#31) | **E-2** and owner decision (doc 01 §5 Q2) | Keep the play-count window for the local MVP. Add `AnnounceCredit = atWindowClose | immediate` if E-2 shows immediate crediting. Add a time-based window only if the owner wants reference parity. Contest scoring is unaffected either way. | S (flag) / M (timer) |

---

## C. Optional mechanics and improvements (low priority)

| ID | Item | Gate | Note |
|---|---|---|---|
| C-1 | Tutorial "same-number tie" clause: later Derbas on that rank pay the other side (#33) | E-3 + owner (doc 01 Q3) | Becomes `AnnounceTie = tutorialDerba` on top of A-2. |
| C-2 | Dealer King/Ace sentence (#39) | owner + evidence (ambiguous text, no live instance) | Keep off. |
| C-3 | Four of a kind alternatives (#34) | owner (doc 01 Q7) | Flag exists; A-2 fixes the bonus count. |
| C-4 | Golden shuffle vectors with an own PRNG (#3) | none | Only needed for replays or cross-runtime determinism. |
| C-5 | Smarter Nadia | none | **Last capture:** plan for the dealer rule and leftovers (#48). **Drops:** prefer ranks the human cannot have (cards already seen). **Difficulty:** levels via the existing evaluation weights. Must keep using engine helpers only, with no rule duplication. |
| C-6 | Explicit `ChainWrap` alternative | none | Default stays off after A-2; no evidence suggests wrapping. |

---

## D. Out of scope (multiplayer-only or platform features)

Not to be implemented in this MVP:
- 2v2 (seating, teams, partner rules);
- **Derba escalation** (1 → 5 → 10, tutorial says 2v2 only);
- 2v2 announcement aggregation;
- turn timer, autopilot / away state and forfeit (RL-24, RL-25: online fairness features);
- server-authoritative play, networking, reconnect;
- matchmaking, ranked rating, private tables, spectators, stickers;
- accounts, shops and cosmetics, leaderboards, analytics.

The engine's `PlayerCount = 2` assumption stays.

---

## Status update — M2 (2026-11-17)

| Item | Status |
|---|---|
| Android Back → "Quit Zellige?" confirmation | **Done.** `Scripts/UI/QuitDialog.cs`, added to the saved scene in place (39 objects added, 1 changed). 5 EditMode tests; Editor Play Mode verified. Device test pending (no phone connected via adb). |
| B-1 Catch | **Investigated, not implemented.** `05_catch_specification.md`: confirmed facts, unknowns UK-1…UK-11, owner decisions D-1…D-4, observations O-1…O-7. |
| A-5 test fixture | **Done 2026-11-17.** `Core/RondaGame.TestSetup.cs` (`GameSetup`, internal `RondaGame.FromSetup`), `Core/AssemblyInfo.cs`, `Tests/EditMode/StateBuilder.cs`; 11 tests; ED-03 reproduced with its observed numbers. |
| A-2 configurable uncertain rules | **Done 2026-11-17.** H-1…H-10 in `RulesConfig`. Selectable alternatives for H-1, H-3, H-4, H-5, H-6, H-9, H-10. Single defined option for H-2, H-7, H-8 (no well-defined alternative). 32 tests. Default behaviour proved unchanged by a golden master recorded before the change. |

## E. Recommended order

1. ~~A-1~~ (done) → **A-3 → A-4 → A-5 → A-2.** Finish the tests first, so the A-2 refactor is protected by deterministic expectations. A-5 is small and makes ED-03 and the future ME-03/ME-04 tests possible.
2. **Collect evidence E-1 and E-2** in one clean observation session (≈ 30–45 min). Record E-3–E-7 when they occur.
3. **Owner decisions** on doc 01 §5 Q1 (capture), Q2 (announcements) and Q4 (Catch).
4. **B-0** (likely a one-line flag change plus a UI cue), then **B-1** if Catch is approved, then **B-2 / B-3** as decided.
5. C items only when time allows.

After each step:
- Run all EditMode tests (currently 109; the count only grows).
- Check the console for zero errors.
- Run one full Play Mode match to the result screen.
- Update `03_gameplay_fidelity_audit.md` rows to the new status.
