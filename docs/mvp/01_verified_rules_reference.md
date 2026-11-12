# MVP 01 — Verified Rules Reference (1v1, human vs rule-based AI)

**Purpose.** One implementation-ready rules table for the Unity MVP that separates what Phase 2 **observed** in the reference game from what Phase 3 only **proposes**. Nothing here is new design; where the docs are silent it says so.
**Sources.** P2 = `docs/03_game_rules_specification.md` (§ / Q-numbers), `docs/02a_phase2_consolidated_assessment.md` (matrix IDs `R01…R32`). P3 = `docs/phase3/03_rules_and_scoring_spec.md` (`RL-xx`, examples `E1…E24`, flags §13), `01_decision_register.md` (`D-G-xx`), `04_state_machines.md`, `05_domain_model.md`, `10_testing_strategy.md`.
**Scope.** 1v1 only. Out of scope and not listed: 2v2, networking, server timers, AFK/autopilot, forfeit (RL-03, RL-06a, RL-24, RL-25, RL-27).

**Status legend**

| Status | Meaning |
|---|---|
| **VERIFIED-OBSERVED (play)** | Seen in a live match of the reference (screenshot inspected, or an earlier video that is *not preserved* — marked "video"). |
| **VERIFIED-OBSERVED (tutorial)** | Stated by the reference's own tutorial text only; never seen applied in live play. Weaker: it is the designers' stated intent. |
| **PROPOSED-DESIGN** | A Phase 3 decision. The reference is INFERRED, UNKNOWN or silent on it. Implement behind `RulesConfig` where a flag is named. |
| **UNRESOLVED** | The reference is UNKNOWN *and* the choice materially changes the engine/AI, or Phase 3 deviates from the tutorial. Detailed in §2. |

---

## 1. Rule table

### 1.1 Deck, deal, dealer

| ID | Rule (codeable statement) | Status | Evidence | P3 rule |
|---|---|---|---|---|
| M-01 | Deck = 40 cards: suits {coins, cups, swords, clubs} × ranks {1,2,3,4,5,6,7,10,11,12}. No 8s, no 9s. | VERIFIED-OBSERVED (tutorial + play) | R01; P2 §2 | RL-01 |
| M-02 | Rank order `1<2<3<4<5<6<7<10<11<12` (rank index 0–9); `next(7)=10`. | VERIFIED-OBSERVED (tutorial) | R02; P2 §2 | RL-09, §1 |
| M-03 | Suit never affects capture, ronda or tringla; only the number matters. | VERIFIED-OBSERVED (tutorial, implicit) | P2 §2 | (§1 terms) |
| M-04 | `next(12)` does not exist: the order does not wrap 12 → 1. | PROPOSED-DESIGN (ref UNKNOWN, "nothing suggests it does") | P2 §2, Q2 | RL-09, flag `chainWrap=false` |
| M-05 | Each deal gives 4 cards to each player; 5 deals per deck round; undealt count after each deal 32 → 24 → 16 → 8 → 0. | VERIFIED-OBSERVED (play) | R03 (A027, A029, A033, A023, A038/A039) | RL-05 |
| M-06 | A new deal starts only when both hands are empty; a player with an empty hand is never asked to play. | PROPOSED-DESIGN (consistent with R03 card accounting) | P2 §3.1 | RL-05, RL-11a |
| M-07 | No cards are dealt to the table. The table is empty at the start of every deck round. | PROPOSED-DESIGN (ref 1v1: INFERRED from card accounting; first deal never captured) | R05; P2 §3.1, Q10 | RL-06 |
| M-08 | The table is **not** cleared between deals of the same deck round; it is cleared only at deck end. | PROPOSED-DESIGN (ref INFERRED: A041 shows 8 table cards at a 2nd deal) | P2 §3.1; 02a §12 #1 | RL-06 |
| M-09 | The dealer alternates to the other player after every deck round. | PROPOSED-DESIGN (ref INFERRED: token own seat A021/A039, rival seat A041/A043) | R27 | RL-04 |
| M-10 | The first dealer of a match is chosen at random. | PROPOSED-DESIGN (ref silent; one sample: local player was dealer in deck 1, A018) | P2 §9.1 | RL-04 |
| M-11 | The first player of **every deal** is the non-dealer; players then alternate, so the dealer plays the last card of each deal and of the deck. | PROPOSED-DESIGN (ref UNKNOWN) | R28; P2 §3.2, Q13 | RL-06b |
| M-12 | Per-deck captured-card counts reset to 0 at each new deck round; match points never reset. | VERIFIED-OBSERVED (tutorial + play) | R26; P2 §7 | RL-22, §1 |

### 1.2 Turn and capture

| ID | Rule (codeable statement) | Status | Evidence | P3 rule |
|---|---|---|---|---|
| M-13 | On its turn a player plays exactly one hand card; the play is committed (no undo in the engine). | VERIFIED-OBSERVED (tutorial: one tap plays) | R06; P2 §3.2 | RL-07 |
| M-14 | **Capture.** If the table holds a card of the played rank, the played card and that table card go to the player's pile (pile +2). | VERIFIED-OBSERVED (tutorial; live state consistent) | R07; P2 §3.3 (`017_rules_3`) | RL-08 |
| M-15 | **Chain.** After an equal-rank capture, for `k = next(rank), next(k), …` each rank present on the table is also captured; stop at the first absent rank. A chain never starts without the equal-rank capture. | VERIFIED-OBSERVED (tutorial); live 11 → 12 state fits (A029 → A030, attribution INFERRED) | R07; P2 §3.3 (`017_rules_7`) | RL-09 |
| M-16 | **Drop.** If no table card has the played rank, the card stays on the table. | VERIFIED-OBSERVED (play) | R08 (A041) | RL-10 |
| M-17 | Capture is **automatic**: a played card whose rank is on the table always captures; the player cannot choose to drop it. | PROPOSED-DESIGN (ref UNKNOWN, Q4; D-G-06 is ★ awaiting approval) | R10, R11 | RL-08 |
| M-18 | The table holds at most one card per rank (model `rank → card?`, 10 slots). Follows from M-07 + M-16 + M-17; a duplicate can never arise, so no duplicate-resolution rule exists. | PROPOSED-DESIGN (ref: rank-addressed slots VERIFIED play, R09; two same-rank cards coexisting UNKNOWN, R11) | P2 §3.3, Q1 | RL-08a |
| M-19 | **Capture obligation.** Any hand card is legal, even when another hand card would capture (`mustCapture=false`). | **UNRESOLVED** (ref UNKNOWN; test E3 INVALID) | R10; P2 Q3 | RL-11 |

### 1.3 Scoring during play

| ID | Rule (codeable statement) | Status | Evidence | P3 rule |
|---|---|---|---|---|
| M-20 | **Missa.** A capture that leaves the table empty gives the capturer +1. | VERIFIED-OBSERVED (tutorial + play) | R12 (A030 → A031) | RL-12 |
| M-21 | Missa is also awarded on the final play of a deck round. | PROPOSED-DESIGN (ref not observed) | — | RL-12, flag `missaOnFinalPlay=true` |
| M-22 | **Derba.** A capture gives +1 when the equal-rank table card captured is the card the opponent dropped on the immediately preceding play. | VERIFIED-OBSERVED (tutorial for the definition; "Derba! +1" bubble in play, trigger not itemised) | R13; P2 §4.2 (`017_rules_23`), A027 | RL-13 |
| M-23 | Derba bookkeeping: keep `lastDrop = (player, rank)`; set by every drop, cleared by every capture and at deck start. It carries across deals inside a deck (the dealer's last drop of a deal can be derba'd by the first play of the next deal). | PROPOSED-DESIGN (ref UNKNOWN, Q5) | P3 doc 03 §12; doc 04 §4; doc 05 §3 | RL-13 |
| M-24 | Derba, chain and Missa combine on one play; points add (Derba +1 and Missa +1 = +2). | PROPOSED-DESIGN (ref UNKNOWN, Q5) | P2 Q5 | RL-13, E8 |
| M-25 | No Derba escalation in 1v1 (the tutorial's 1 → 5 → 10 text is 2v2-only). | VERIFIED-OBSERVED (tutorial wording "In 2 vs 2 it can grow") | P2 §4.2 | RL-13, flag `derbaEscalation=false` |
| M-26 | **Ronda.** Two hand cards of one rank, announced before either is played: value 1. | VERIFIED-OBSERVED (tutorial; Ronda button seen in play A029/A036/A041) | R14; P2 §4.3 | RL-14 |
| M-27 | Two different pairs in one hand = two rondas, value 2. | VERIFIED-OBSERVED (play, earlier video — not preserved) | R14; P2 §4.3 | RL-14 |
| M-28 | **Tringla.** Three hand cards of one rank, announced: value 5. | VERIFIED-OBSERVED (tutorial) | R17 | RL-14 |
| M-29 | Four of a kind counts as one tringla (value 5). | **UNRESOLVED** (ref UNKNOWN) | R32; P2 Q8 | RL-14, flag `quadAsTringla=true` |
| M-30 | An announcement is possible in every deal, for the hand of that deal only; one announcement per rank; playing a card of that rank first forfeits it. | PROPOSED-DESIGN (derived from tutorial "announce it before you play them"; window at every deal is VERIFIED play, R15) | P2 §4.3 | RL-14 |
| M-31 | The announcement window opens when the deal completes, lasts 15 s, runs while play continues, and never closes early. | PROPOSED-DESIGN (ref: countdown VERIFIED play, max seen 13 s; length ≈ 15–16 s INFERRED; origin UNKNOWN) | R15; P2 §4.3, Q7 | RL-15, flag `announceWindowMs=15000` |
| M-32 | Announcements are hidden from the opponent until the window closes; points are credited once, at close. | **UNRESOLVED** (ref UNKNOWN: tutorial credits instantly, an earlier video showed the bubble after close) | R16; P2 §4.3; 02a §12 #2 | RL-15 |
| M-33 | **Contest (both announced).** Tringla beats ronda; ronda vs ronda → higher rank wins; tringla vs tringla → **lower** rank wins. Each side is compared by its strongest announcement. | VERIFIED-OBSERVED (tutorial) | R18 (`017_rules_16`) | RL-16 |
| M-34 | Contest scoring: winner gets the sum of its own announcement values + 1 per opposing announcement (even a tringla); loser gets 0. Only one side announced → it scores its own values. | VERIFIED-OBSERVED (tutorial) | R18 (`017_rules_17`) | RL-16 |
| M-35 | "Higher/lower" uses the M-02 order (12 highest). | PROPOSED-DESIGN (ref INFERRED) | P2 §4.4 | RL-16 |
| M-36 | Same kind and same rank on both sides → nobody scores, and nothing further happens (`announceTie=void`). | **UNRESOLVED** (deviates from tutorial, see §3 C-3) | R18 (`017_rules_18`); D-G-22 | RL-16 |
| M-37 | **Ronda cap.** Announcement points from an award with no tringla on the winning side cannot lift a score above 40; the excess is lost (ledger keeps requested/granted). | VERIFIED-OBSERVED (tutorial; a withheld ronda was never seen live) | R19 (`017_rules_19`) | RL-17, flag `rondaCap=40` |
| M-38 | An award where the winner holds a tringla is uncapped as a whole (including its ronda values and +1 bonuses) and can end the match. | PROPOSED-DESIGN (tutorial only says "A tringla can win it!") | R19 | RL-17 |
| M-39 | **Catch.** After the window closes a player may accuse the opponent of an unannounced pair of rank `r`: correct → +1 to the accuser; wrong → accuser cannot catch again "until the next round". | VERIFIED-OBSERVED (tutorial; never played live) | R20 (`017_rules_21`, `019_howto_16`) | RL-18 |
| M-40 | Catch details: allowed only if the opponent has played ≥ 1 card of `r` this deal and `(target, r, deal)` was not already caught; valid if the dealt hand held ≥ 2 of `r` unannounced; hidden tringla also +1; lockout lasts until the next **deal**. | PROPOSED-DESIGN (ref UNKNOWN, Q9) | D-G-13 | RL-18, flags `catchEvidence`, `catchLockout`, `catchValueTringla` |

### 1.4 End of deck, end of match

| ID | Rule (codeable statement) | Status | Evidence | P3 rule |
|---|---|---|---|---|
| M-41 | Deck end = the 40th card has been played (5th deal finished). There is no end-of-deal processing other than dealing again (M-06, M-08). | PROPOSED-DESIGN (deck sequence itself is VERIFIED play, M-05; "nothing else happens at deal end" is the P3 model) | R03; P3 doc 04 §3 | RL-05, RL-19 |
| M-42 | **Leftover.** Cards still on the table at deck end go to the pile of the player who made the last capture of that deck. | VERIFIED-OBSERVED (tutorial; A039 → A041 fits, decomposition INFERRED) | R21 (`017_rules_26`) | RL-19 |
| M-43 | **Dealer rule.** If the last capture of the deck was not made by the dealer, the non-dealer gets +5. If the dealer made it, nobody gets the 5. | VERIFIED-OBSERVED (play, A021 bubble; "dealer last → no bonus" INFERRED from A039 → A041) | R22 | RL-20 |
| M-44 | **Over 20.** After leftovers, each player scores `max(0, pileCount − 20)`. | VERIFIED-OBSERVED (play: 32 cards → +12) | R23 (A021) | RL-21 |
| M-45 | Tally order: leftover → dealer rule → over-20; all three applied before the match-end check. | PROPOSED-DESIGN (ref order UNKNOWN, Q12) | P2 §5 | RL-22 |
| M-46 | The tutorial's King/Ace sentence about the dealer's last take is not implemented. | **UNRESOLVED** (ref text ambiguous, no live instance) | R24 (`017_rules_27`); D-G-15 | RL-20, flag `dealerKingAceRule=off` |
| M-47 | Target = 41. First player to reach or pass 41 wins. | VERIFIED-OBSERVED (tutorial + play, A044 41–26) | R25 | RL-02 |
| M-48 | During play the match ends immediately when a score reaches 41 (no tally of the unfinished deck). | VERIFIED-OBSERVED (play, earlier video — not preserved: 40 → 41 on a Derba ended it mid-deck). For Missa/Catch points: INFERRED. | R25; P2 §4.5, §6 | RL-23(a) |
| M-49 | After a tally the final score may exceed 41. | VERIFIED-OBSERVED (play, 2v2 only: 44–31) | P2 §6 | RL-23(b) |
| M-50 | Both players ≥ 41 after one tally → higher score wins; equal scores → play one more deck round (dealer alternates), repeat until one leads after a tally. | **UNRESOLVED** (ref UNKNOWN) | R32; P2 Q15 | RL-23(b), flag `bothReachTarget=higherWins` |
| M-51 | Every point change is recorded with a reason code (`MISSA`, `DERBA`, `ANNOUNCE_OWN`, `ANNOUNCE_BONUS`, `CATCH`, `DEALER_PENALTY`, `OVER_20`) so the UI can explain it. | PROPOSED-DESIGN (pattern seen in the reference's explanation bubbles) | U07 | RL-26, §8 |

**Derived fact (useful for tests).** With M-17/M-18 the table can hold at most 10 cards, so every 40-card deck round contains at least one capture; the "no capture in the deck → leftovers discarded" branch of RL-19 is unreachable in the default ruleset.

---

## 2. UNRESOLVED mechanics → `RulesConfig` flags

Flag names are Phase 3's (doc 03 §13) unless marked *(new)* — those are suggestions of this document only. U-1 to U-7 are the mechanics that most change the engine; U-8 to U-12 are PROPOSED rules worth isolating as flags because the reference gives nothing firm.

| # | What is unknown | Options | Phase 3 provisional default | Flag |
|---|---|---|---|---|
| U-1 | Must a player capture when able? (M-19) | (a) free play; (b) if any hand card captures, only capturing cards are legal | (a) | `mustCapture = false` |
| U-2 | May a matching card be dropped, creating duplicates on the table? (M-17, M-18) | (a) automatic capture, ≤ 1 per rank; (b) optional capture, duplicates allowed (then "how many same-rank cards does a capture take" is also unknown, P2 Q1) | (a). Reversal cost rated **High** in D-G-06; P3 doc 03 §14 puts (b) out of v1, so **no flag exists** | *(new)* `autoCapture = true` — only if the owner wants (b) kept possible |
| U-3 | When are ronda/tringla points credited, and are announcements hidden? (M-32) | (a) sealed, credited at window close; (b) credited immediately at announcement (tutorial behaviour; cannot honour the contest rule) | (a) | *(new)* `announceCredit = atWindowClose`; `announceWindowMs = 15000` |
| U-4 | Both announce the same kind and rank (M-36) | (a) void; (b) tutorial: nobody scores yet, then each derba on that rank gives the *other* side +1 | (a) | `announceTie = void` |
| U-5 | Four of a kind (M-29) | (a) one tringla (5); (b) two rondas (2); (c) own kind | (a) | `quadAsTringla = true` |
| U-6 | Dealer King/Ace sentence (M-46) | (a) ignore; (b) literal: dealer's last take with a 12 → dealer +5; with a 1 → other side +5 | (a) | `dealerKingAceRule = off` |
| U-7 | Both ≥ 41 in one tally (M-50) | (a) higher wins, exact tie → extra deck; (b) always extra deck | (a) | `bothReachTarget = higherWins` |
| U-8 | First player of a deal and first dealer (M-10, M-11) | non-dealer leads / dealer leads; random / fixed first dealer | non-dealer leads; random first dealer | *(new)* `firstPlayer = nonDealer`, `firstDealer = random` |
| U-9 | Missa on the last play of a deck (M-21) | award / do not award | award | `missaOnFinalPlay = true` |
| U-10 | Chain wrap 12 → 1 (M-04) | no wrap / wrap | no wrap | `chainWrap = false` |
| U-11 | Catch specifics (M-40): evidence requirement, lockout scope, hidden-tringla value | evidence `playedThisDeal` / none; lockout `nextDeal` / `nextDeck`; value 1 / 5 | first of each | `catchEvidence`, `catchLockout`, `catchValueTringla` |
| U-12 | Scope of the 40-cap when a tringla is involved (M-38); whether a Catch point is capped | whole award uncapped / only the tringla's 5 uncapped | whole award uncapped; Catch not mentioned by the cap rule, so uncapped by omission | `rondaCap = 40` (no finer flag) |

**Docs are silent on (no rule exists anywhere):** the physical dealing order (who receives cards first, one at a time or in packets) — it cannot change outcomes with a shuffled deck; how the AI announces and catches beyond "Normal tier announces/catches correctly" (P3 doc 05); a drawn match (unreachable under M-50).

---

## 3. Contradictions and inconsistencies

| # | Between | Finding | Suggested handling |
|---|---|---|---|
| C-1 | P3 doc 03 §10 E14, E15 vs RL-01 | E14 uses a ronda of rank **8** and E15 a ronda of rank **9**; neither rank exists in the deck. The same examples are golden vectors T-GV-14/15 in doc 10. | Do not use E14/E15 as written; §4 X-10 substitutes valid ranks. |
| C-2 | P2 `TC-RONDA-01` vs P3 RL-15 | P2's test case credits the ronda immediately (tutorial); P3 credits at window close. P2 itself marks the timing UNRESOLVED and says "the engine must not hard-code either". | U-3; keep the timing configurable. |
| C-3 | P2 §4.4 (`017_rules_18`) vs P3 RL-16 | Tutorial: same-number rondas → "nobody scores **yet**", later derbas on that number pay the other side. P3 voids the tie outright (D-G-22). A deliberate deviation, but RL-16 is tagged "VRO". | U-4. |
| C-4 | P3 RL-13 tag vs D-G-10 / P2 §4.2 | RL-13 is tagged **VRO**, but the register classes Derba as **PD ★**, and P2 says the "immediately preceding play" requirement was not observed live (tutorial text only). | Treat the trigger as tutorial-level (M-22) plus design (M-23, M-24). |
| C-5 | P3 RL-08 / RL-08a vs P2 R10, R11 | Automatic capture and ≤ 1 card per rank are **★ decisions not yet approved** (checklist items 6–7 blank); P2 has them UNKNOWN. P3 docs 04/05 already treat the one-card-per-rank table as an invariant. | U-1, U-2. |
| C-6 | P3 doc 03 header vs §13 | The header says "every PD rule is a ruleset flag", but RL-04, RL-06 and RL-06b (dealer rotation, empty table, first player) are PD with no flag in §13. | U-8; add flags only if wanted. |
| C-7 | P3 RL-19 vs RL-08a | RL-19 defines "no capture in the deck round → leftovers discarded"; unreachable under automatic capture (§1 derived fact). RL-20 does not say who gets +5 in that case. | Harmless by default; matters only if U-2(b) is ever enabled. |
| C-8 | P3 doc 01 §1.2 header vs doc 03 | The register points to "flags: doc 03 §14"; the flags are in §13. | Cosmetic. |
| C-9 | P3 RL-02 vs RL-17 | RL-02 says reaching the target "ends the match at once"; RL-17 clamps ronda-class points at 40. No conflict if the clamp is applied **before** the end check — that order is implied, not stated. | Apply cap, then check for 41. |

**Evidence note (not a contradiction).** P2 lists "who plays first" as UNKNOWN. Its own data are at least consistent with M-11: in the deck that began at t+568 the rival had led (hands 4 + 3, one table card; P2 §3.1) and the dealer token was at the local seat later in that same deck (A039). This is a reading made for this document from a contaminated window, not a Phase 2 finding; M-11 stays PROPOSED.

---

## 4. Worked examples (unit-test candidates)

Notation: table and hands as rank lists; `me`/`ai` = the two players; pile = cards captured this deck. Defaults of §2 apply.

| # | Given | Action | Expected | Reason | Rules | Source |
|---|---|---|---|---|---|---|
| X-1 | table [4,7,11]; hand [2,7,12]; pile 0 | play 7 | captured [7,7]; table [4,11]; pile 2; +0 | equal-rank capture; 10 absent → chain stops | M-14, M-15 | P2 TC-TAKE-01; P3 E1 |
| X-2 | table [3,4,5,10]; hand [3,11]; pile 2 | play 3 | captured [3,3,4,5]; table [10]; pile 6; +0 | chain 3 → 4 → 5; 6 absent, so 10 stays | M-15 | P2 TC-CHAIN-01; P3 E2 |
| X-3 | table [2,7,10,11] | play 7 | captured [7,7,10,11]; table [2]; pile +4; +0 | chain jumps 7 → 10 → 11; 12 absent | M-02, M-15 | P3 E3 |
| X-4 | table [11,12] | play 11 | captured [11,11,12]; table []; pile +3; **+1 MISSA** | chain ends at 12 (no wrap); table emptied | M-04, M-15, M-20 | P3 E4 |
| X-5 | table [4,7]; hand [2,…] | play 2 | captured []; table [2,4,7]; +0; `lastDrop=(me,2)` | no matching rank → drop | M-16, M-23 | P3 E5 |
| X-6 | ai just dropped a 10 → table [1,10]; hand [10,3]; pile 8; score 3 | play 10 | captured [10,10]; table [1]; pile 10; score 4 (**+1 DERBA**) | captured the card dropped on the previous play | M-22 | P2 TC-DERBA-01; P3 E6 |
| X-7 | table [10]; previous play = ai dropped that 10 | play 10 | captured [10,10]; table []; **+1 DERBA, +1 MISSA** = +2 | derba and missa add | M-20, M-22, M-24 | P3 E8 |
| X-8 | ai dropped a 10; I then dropped a 3; ai then dropped a 5 → table [3,5,10] | play 10 | captured [10,10]; table [3,5]; **+0** | previous play was not a drop of 10 → no derba | M-22, M-23 | P3 E7 (adapted to 1v1) |
| X-9 | deal hands: me [5,5,2,11] announces ronda(5); ai [3,3,3,12] announces tringla(3) | window closes | ai +6 (5 own + 1 bonus); me +0 | tringla beats ronda | M-28, M-33, M-34 | P3 E9 |
| X-10 | me holds pairs of 3 and 7 and announces both; ai announces ronda(5) | window closes | me +3 (2 own + 1 bonus); ai +0 | best ronda 7 > 5; two rondas = 2 | M-27, M-33, M-34 | P3 E14 with valid ranks (see C-1) |
| X-11 | score me 38; me wins a ronda-only contest worth 3 | window closes | me = 40 (requested 3, granted 2); match continues | ronda points cannot pass 40 | M-37 | P3 E16; P2 TC-CAP-01 |
| X-12 | score me 38; me wins with a tringla (+5) | window closes | me = 43; **match ends**, me wins | tringla award is uncapped | M-38, M-47, M-48 | P3 E17 |
| X-13 | deck end; dealer = me; piles me 8 / ai 32; last capture by ai; table empty | tally | ai +5 (`DEALER_PENALTY`) and +12 (`OVER_20`) = +17; me +0 | dealer was not last to take; 32 − 20 = 12 | M-43, M-44, M-45 | A021; P2 TC-ENDDECK-01; P3 E21 |
| X-14 | deck end; dealer = me; piles me 23 / ai 14; table holds 3 cards; last capture by me | tally | leftover → me (26); me +6; ai +0; no dealer bonus | leftover to last capturer; 26 − 20 = 6; dealer was last | M-42, M-43, M-44 | A039 → A041 (decomposition INFERRED); P3 E22 |
| X-15 | mid-deck; score me 40 / ai 23; I make a derba | play | me = 41; match ends immediately, no tally | non-ronda point reaches target mid-play | M-48 | P2 TC-END-01 |
| X-16 | tally: me 38 gets +3 over-20; ai 38 gets +5 dealer rule | evaluate | 41 vs 43 → **ai wins** | both ≥ 41 → higher score | M-50 | P3 E23 |

Not offered as tests because nothing verifies them: King/Ace dealer cases, four of a kind, forced capture, duplicate-rank captures, same-number announcement tie beyond "nobody scores".

---

## 5. Questions for the product owner (each changes the implementation)

1. **Capture model (U-1, U-2).** Approve "playing a matching rank always captures, one card per rank on the table" and "no obligation to play a capturing card"? These are register items 6–7 (D-G-06, D-G-07), still unanswered, and the first is expensive to reverse.
2. **Announcements in a local single-player game (U-3).** Keep the Phase 3 model (hidden announcements, 15 s window running during play, points at window close), or simplify for the MVP (for example resolve before the first card of the deal)? Phase 3 only specifies the networked model.
3. **Same-rank announcement tie (U-4).** Void (Phase 3) or the tutorial's "later derbas pay the other side" clause?
4. **Catch in the MVP.** Is Catch in scope for human vs AI at all? If yes, may the AI deliberately hide a pair, and does it catch the human? The docs only say a Normal bot "announces/catches correctly".
5. **First player and first dealer (U-8).** Accept "non-dealer leads every deal; first dealer random", given the reference is UNKNOWN?
6. **Reference parity (OQ-R1).** Run the clean observation session (02a §13 items 1–3) before the engine is frozen, or accept the provisional defaults above as our own rules?
7. **Both ≥ 41 (U-7) and four of a kind (U-5).** Accept the defaults (higher score wins, extra deck on an exact tie; four of a kind = one tringla)?
