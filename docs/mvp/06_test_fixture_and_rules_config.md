# MVP 06 — Deterministic test fixture (A-5) and configurable uncertain rules (A-2)

**Date:** 2026-11-17 · **Status:** design, ready to implement; **nothing in this document is built yet**.
**Inputs:** `04_gameplay_implementation_plan.md` (A-2, A-5), `03_gameplay_fidelity_audit.md` (rows #10, #14, #33, #34, #43; R-2), `01_verified_rules_reference.md` (U-1…U-12), the engine (`Scripts/Core/RondaGame.cs`, `RulesConfig.cs`, `Card.cs`).
**Constraint for both parts:** current gameplay behaviour must not change. Every new default equals today's behaviour, and the full EditMode suite (126 tests) must pass unchanged.

---

## Part C — Test-only state fixture (plan item A-5)

### C.1 Problem

Through the public API a test can only fix the **first deck** (`StartMatch(firstDealer, firstDeckOrder)`), so every end-of-deck or match-end test must script a complete 40-card deck. That cost two things in Phase 1:
- **ED-03 not reproduced:** its observed numbers (23 + 3 leftover = 26 → +6) could not be reproduced at all;
- **ME-03 / ME-04 not buildable:** these will need arbitrary pre-scores once M-50 is decided.

Full-deck fixtures are also long to review.

### C.2 Design

**An internal factory on the engine, visible only to the EditMode test assembly.**

```csharp
// Scripts/Core/AssemblyInfo.cs (new)
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Zellige.Tests.EditMode")]

// Scripts/Core/GameSetup.cs (new) — plain data, internal
internal sealed class GameSetup
{
    public RulesConfig Rules;                 // required
    public int Seed = 1;                      // used for every later shuffle
    public int Dealer;                        // 0 or 1
    public int CurrentPlayer;                 // whose turn it is
    public int DeckRound = 1, DealNumber = 1;
    public List<Card>[] Hands = { new List<Card>(), new List<Card>() };
    public List<Card> Table = new List<Card>();
    public List<Card>[] Piles = { new List<Card>(), new List<Card>() };  // captured cards this deck
    public List<Card> Stock = new List<Card>();     // undealt, top first
    public int[] Scores = new int[2];
    public int LastCapturer = -1;                    // -1 = nobody yet this deck
    public (int Seat, Card Card, bool WasDrop)? LastPlay;   // for Derba
    public bool AnnounceWindowOpen;                  // defaults to "closed": no announcements pending
    public int PlaysThisDeal;
}

// RondaGame.cs (addition only)
internal static RondaGame FromSetup(GameSetup setup) { ... }
```

**Validation** inside `FromSetup` (throw `ArgumentException` with a clear message):
- all 40 cards appear exactly once across hands, table, piles and stock;
- hands hold ≤ 4 cards each, and the current player has at least one;
- the table holds at most one card per rank (current capture model, M-18);
- scores are below the target (the match is not already over);
- `LastPlay`, if given, names a card that is in a pile or on the table.

**Behaviour after setup:** unchanged. The game is driven only through the existing public commands (`PlayCard`, `Announce`), so every rule under test runs through production code. The factory only fills private fields that `StartMatch` normally fills.

**Test helper** (`Tests/EditMode/StateBuilder.cs`), a fluent wrapper that keeps fixtures short:

```csharp
var game = new StateBuilder()
    .Dealer(H).Turn(H)
    .Scores(h: 30, a: 12)
    .Hand(H, "5O").Hand(A, "7C")
    .Table("3E", "11B")
    .LastCapturer(H)
    .RestToPiles(h: 20)          // fills the remaining cards into piles / stock so the count is 40
    .Build();
```

**Why this cannot reach the production game:**
- **Compile-time isolation:** `FromSetup` and `GameSetup` are `internal` to `Zellige.Core`, and only `Zellige.Tests.EditMode` (an Editor-only assembly with `UNITY_INCLUDE_TESTS`) is granted access. The game's own code (`Zellige.Game`, `Zellige.AI`) cannot call them.
- **No UI:** there is no menu, button, cheat key or scene object, so nothing to remove before release.
- IL2CPP may keep the unused internal code in the binary, but it is unreachable.

**First tests to write with it:**
1. ED-03 with X-14's numbers (dealer took last, 23 + 3 = 26 → +6, no +5);
2. a small leftover-ownership table test (3 short cases instead of full decks);
3. re-express ED-02 as a 2-play fixture and check it agrees with the full-deck version (proves the factory is faithful);
4. later, ME-03 / ME-04 once M-50 is decided.

**Effort:** small (≈ 150 lines engine and helper, plus tests). **Risk:** the factory must stay in sync with engine fields. This is mitigated by test 3 (agreement with the scripted-deck tests) and by keeping the factory next to the fields it sets.

---

## Part D — Rules that are hard-coded although unverified (plan item A-2)

### D.1 Inventory

"Unverified" = UNKNOWN in `01_verified_rules_reference.md` (reference behaviour not observed). Line numbers refer to the current files.

**Hard-coded, should move to `RulesConfig`:**

| # | Assumption in the code today | Where | Evidence status | Doc refs |
|---|---|---|---|---|
| H-1 | The **non-dealer** plays first in every deal. | `RondaGame.cs:257` (`int first = Other(Dealer)`) | UNKNOWN | M-11, U-8 |
| H-2 | Same kind and same rank announced on both sides → **nobody scores, nothing else happens**. | `RondaGame.cs:329` (`comparison == 0 → -1`) | UNKNOWN, deviates from the tutorial's "not yet" clause | M-36, U-4, C-3 |
| H-3 | After a tally, if any player has ≥ 41, the **higher** score wins; **equal** scores play another deck. | `RondaGame.cs:385-390` | UNKNOWN | M-50, U-7 |
| H-4 | The upward chain **stops at 12** (no 12 → 1 wrap). | `Card.cs:38` (`Ranks.TryGetNext`) | UNKNOWN (nothing suggests wrapping) | M-04, U-10 |
| H-5 | A **Derba carries across a deal boundary** (the dealer's last drop can be taken with a Derba by the first play of the next deal); the memory is cleared only at a new deck. | `RondaGame.cs:143-146, 244` | UNKNOWN | M-23 |
| H-6 | Once a score reaches 41 during play, **later points on the same play are not awarded** (e.g. a Missa after a match-winning Derba). | `RondaGame.cs:395` (`Award`: `if (IsMatchOver) return`) | UNKNOWN (41 vs 42) | M-48 note |
| H-7 | Announcements still **pending when the match ends are discarded**. | `RondaGame.cs:147, 153` | UNKNOWN (Phase 3 edge case) | doc 03 §12 |
| H-8 | An announcement award containing a tringla is **uncapped as a whole**, including its ronda values and bonuses. | `RondaGame.cs:316` | UNKNOWN (tutorial: "A tringla can win it!") | M-38, U-12 |
| H-9 | With `QuadCountsAsTringla = false`, four of a kind is **one** announcement worth 2, so the opponent's bonus counts it **once**. | `RondaGame.cs:213-217` | UNKNOWN, and possibly inconsistent with "two rondas" | M-29, U-5, audit R-6 |
| H-10 | The **first dealer is random**. | `RondaGame.cs:76` | UNKNOWN; already overridable per call (`StartMatch(firstDealer)`) | M-10 |

**Not proposed for configuration now:**

| # | Assumption | Why not now |
|---|---|---|
| N-1 | Matching rank is **captured automatically**, at most one card per rank on the table. | A model-level change (table becomes a multiset), and this phase must not change the capture model. Gated by E-1 and D-G-06. |
| N-2 | The **table is empty** at the start of each deck; no cards are dealt to it. | No documented alternative to choose between yet. |
| N-3 | **Tally order** (leftover → dealer rule → over-20). | The values don't depend on the order; only the event order would change. |

**Already configurable (no change needed):**
- `MustCapture` (U-1);
- `MissaOnFinalPlay` (M-21);
- `QuadCountsAsTringla` (U-5);
- `AnnounceWindowPlays` (U-3, the local simplification);
- `RondaCannotWin` (M-37);
- `DealerAlternates` (M-09);
- the scoring values and `TargetScore`.

### D.2 Proposed structure

Keep the existing **flat `RulesConfig` class**, with no new classes, interfaces or dependency injection. Add one field per assumption. Each field's documentation states that its default is the current provisional behaviour, not a verified rule. Enums are used only where Phase 3 already documents a second option; otherwise the field has a single documented value and becomes an enum when evidence names an alternative. This avoids inventing behaviours that nobody has described.

```csharp
public sealed class RulesConfig
{
    // ... existing fields unchanged ...

    // ---- Provisional assumptions (reference behaviour UNKNOWN; defaults = current behaviour) ----

    /// <summary>H-1 (M-11). Who plays first in every deal.</summary>
    public FirstPlayer FirstPlayer = FirstPlayer.NonDealer;          // alternative documented: Dealer

    /// <summary>H-2 (M-36, U-4). Both sides' best announcements equal.</summary>
    public AnnouncementTie AnnouncementTie = AnnouncementTie.NobodyScores;
                                                // alternative documented (tutorial): LaterDerbasPayOtherSide

    /// <summary>H-3 (M-50, U-7). Both players at or above the target after one tally.</summary>
    public BothReachTarget BothReachTarget = BothReachTarget.HigherScoreWinsTieReplays;
                                                // alternative documented: AlwaysPlayAnotherDeck

    /// <summary>H-4 (M-04). Chain continues from 12 to 1.</summary>
    public bool ChainWrapsAfterTwelve = false;

    /// <summary>H-5 (M-23). A Derba can take the previous deal's last drop.</summary>
    public bool DerbaAcrossDeals = true;

    /// <summary>H-6 (M-48). Points after the match-winning point on the same play still count.</summary>
    public bool PointsAfterWinningPointCount = false;

    /// <summary>H-7 (doc 03 §12). Pending announcements are scored when the match ends.</summary>
    public bool ScorePendingAnnouncementsAtMatchEnd = false;

    /// <summary>H-8 (M-38, U-12). A tringla makes the whole announcement award uncapped.</summary>
    public bool TringlaUncapsWholeAward = true;                     // alternative: only the tringla's own value

    /// <summary>H-9 (M-29, U-5). With QuadCountsAsTringla off: four of a kind counts as two announcements.</summary>
    public bool QuadCountsAsTwoAnnouncements = false;               // current: one announcement worth 2
}
```

H-10 needs no field: tests and the game already pass `firstDealer` when they need a fixed dealer.

### D.3 Implementation rules (for when A-2 is approved)

1. **Defaults reproduce today exactly.** The 126 existing tests must pass without edits. That is the regression proof that behaviour did not change.
2. **Every flag gets two tests:** its default and each documented alternative, written with `ScriptedDeck` or the A-5 fixture.
3. **Alternatives that need more than a few lines are deferred:** e.g. `AnnouncementTie.LaterDerbasPayOtherSide`, which needs per-rank tie memory. The enum value is added only when it is implemented, never as a stub that throws at runtime.
4. **One change per commit:** e.g. "A-2: make first player configurable (default unchanged)", to keep review easy.
5. **The values of unknown rules are not decided here.** They are settled later by the evidence items E-1…E-7 (audit §6) and O-1…O-7 (`05_catch_specification.md`), or by the product owner.
