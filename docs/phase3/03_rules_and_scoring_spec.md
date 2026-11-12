# 03 — Rules and Scoring Specification (our ruleset **R1 v0.1**)

**Deliverable B.** · **Status:** PROPOSED (★ decisions pending, `01_decision_register.md`) · **Date:** 2026-11-12
**This is OUR game's ruleset.** It is not a description of the reference game. Each rule carries a tag from `00_README_structure.md` §2: **VRO** (verified reference observation, with Phase 2 evidence), **DD** (our decision), **PD** (provisional — a flag exists), **OQ** (not decided). A rule tagged VRO is only as strong as its Phase 2 qualifier (*visual*, *log-only*, *tutorial*). Normative words: MUST, MUST NOT, MAY.
**Engine contract.** The rules engine (doc 05) implements exactly this document; every PD rule is a ruleset flag (§13) so alternatives can be tested without code changes.

---

## 1. Terms

| Term | Meaning |
|---|---|
| **Match** | Played until a side reaches the target score. |
| **Deck round** (deck) | One pass through all 40 cards (several deals). Points persist; per-deck pile counts reset. |
| **Deal** | One distribution of cards to hands within a deck round. |
| **Seat** | Position 0–3 counter-clockwise from the local player. 1v1 uses seats 0 and 1 (the second player faces the first); 2v2 uses 0–3. |
| **Side** | 1v1: {0}, {1}. 2v2: {0, 2}, {1, 3} (partners sit opposite). |
| **Pile** | The cards a side has captured in the current deck round (shared by partners). |
| **Drop** | Playing a card that captures nothing; it stays on the table. |
| **Capture** | Playing a card whose rank is on the table; the played card and the matched table cards go to the playing side's pile. |
| **Rank order** | `1 < 2 < 3 < 4 < 5 < 6 < 7 < 10 < 11 < 12`; rank index 0–9. `next(7) = 10`; `next(12)` does not exist. |
| **Announcement** | A pair (*ronda*) or three/four of a kind (*tringla*) declared before playing those cards. |

---

## 2. Setup and flow

| ID | Rule | Tag | Evidence / decision |
|---|---|---|---|
| RL-01 | The deck MUST be the 40-card Spanish-suited set: suits coins, cups, swords, clubs × ranks 1–7, 10, 11, 12 (no 8s or 9s). | **VRO** | R01 (tutorial `017_rules_1`; card faces A041) · D-G-01 |
| RL-02 | The match target MUST be 41 points (configurable per ruleset). A side that reaches or passes the target ends the match at once (see RL-23 for the tally case). | **VRO** | R25 (A044; tutorial `017_rules_2`) · D-G-03 |
| RL-03 | Seats and sides follow §1. In 2v2 the turn order alternates sides (0 → 1 → 2 → 3), so the previous player is always an opponent. | DD | D-G-18 |
| RL-04 | The dealer MUST rotate to the next seat in turn order after each deck round; the first dealer is chosen at random by the server. | PD | D-G-05. Reference: dealer token moved between decks (R27, INFERRED: alternates in 1v1) |
| RL-05 | **Deal pattern.** 1v1: five deals of 4 cards per player (deck 32 → 24 → 16 → 8 → 0). 2v2: first deal 4 each, then 3 each, then 3 each (deck 24 → 12 → 0). A deal MUST begin only when all hands are empty. | **VRO** | R03 (A027, A029, A033, A023, A038/A039: visual), R04 (tutorial `017_rules_2`) · D-G-04 |
| RL-06 | At the start of a deck round the table MUST be empty; the table persists between deals of the same deck round. | PD | Reference: INFERRED (card accounting, Phase 2 `docs/03_game_rules_specification.md` §3.1). Our rule: yes. |
| RL-06a | The shuffle MUST be performed by the server from a server-held seed; hands and the undealt deck are hidden information. | DD | D-S-01, D-S-10 |
| RL-06b | The first player of every deal MUST be the seat after the dealer in turn order; the dealer therefore plays last in every round of plays. Turn order is counter-clockwise on screen. | PD | D-G-05 (reference: direction UNKNOWN / weakly INFERRED) |

## 3. Turn and play

| ID | Rule | Tag | Evidence / decision |
|---|---|---|---|
| RL-07 | On its turn a seat MUST play exactly one card from its hand (command `PlayCard`). Playing is a single committed action; there is no undo. | DD | Reference plays on one tap (R06 tutorial). The UI's two-step confirmation is client-side only (D-U-04). |
| RL-08 | **Capture.** If the table holds a card of the played card's rank, the play MUST capture: the played card and that table card go to the playing side's pile. A capture is automatic; the player cannot decline it. | DD (derived) | D-G-06. Reference: equal-rank take (R07 tutorial), live state consistent (A029 → A030); "decline" behaviour UNKNOWN (R10, R11). |
| RL-08a | The table MUST hold at most one card per rank at any time (consequence of RL-08 and RL-10). | DD | Reference layout is rank-addressed (R09, A027/A029/A033/A039/A041: visual). |
| RL-09 | **Chain.** After an equal-rank capture the capture MUST continue upward: for `k = next(rank)`, `next(k)`, … each rank present on the table is captured; the chain stops at the first rank that is absent, and never wraps from 12. | **VRO** (+ DD for no-wrap) | R02/R07 tutorial: "3, then 4 and 5: all yours! After 7 the order jumps to 10, then 11 and 12." Live state fits an 11 → 12 chain (A029 → A030; attribution INFERRED). Wrap: our decision. D-G-08 |
| RL-10 | **Drop.** If the table holds no card of the played rank the card MUST be placed on the table. | **VRO** | R08 (A041: eight dropped cards, captured counters 0 / 0) |
| RL-11 | **Capture obligation (flag `mustCapture`, default false).** false: any card in hand is legal. true: if at least one hand card would capture, only capturing cards are legal. | PD | D-G-07. Reference: UNKNOWN (E3 invalid). |
| RL-11a | A seat with an empty hand MUST NOT be asked to play; a deal ends when all hands are empty. | DD | |

## 4. Scoring events during play

All points are recorded in the scoring ledger (RL-26) with a reason code.

| ID | Rule | Tag | Evidence / decision |
|---|---|---|---|
| RL-12 | **Missa:** a capture that leaves the table empty awards **+1** to the capturing side. The final play of a deck is included (flag `missaOnFinalPlay`, default true). | **VRO** (+PD) | R12 (tutorial `017_rules_10`; A030 → A031: bubble, empty table, rival 22 → 23: visual) · D-G-09 |
| RL-13 | **Derba:** a capture awards **+1** when the card of the played rank that is captured is the card the *previous play in turn order* has just dropped (so the previous play was a drop, by an opponent, of the same rank). A derba can occur together with chain and Missa (points add). No escalation in v1 (flag `derbaEscalation`, default false). | **VRO** (+PD) | R13 (tutorial `017_rules_23`; "Derba! +1" bubble A027: visual). Escalation 1 → 5 → 10 is tutorial text only → D-G-21. D-G-10 |
| RL-14 | **Announcement eligibility.** At the start of a deal, a seat holding ≥ 2 cards of one rank MAY announce that rank before the window closes (command `Announce(rank)`). Kind: 2 cards = **ronda** (value 1); 3 cards = **tringla** (value 5); 4 cards = tringla (flag `quadAsTringla`, default true). A seat MAY announce several ranks (two pairs = two rondas = value 2). The announcement MUST be made before any card of that rank is played; playing one first forfeits it. One announcement per rank per seat per deal. | **VRO** (+PD) | R14/R17 (tutorial `017_rules_13`, `019_howto_14`), "2 rondas" (earlier video). Four of a kind: UNKNOWN → PD. D-G-11 |
| RL-15 | **Announcement window.** At the end of dealing, the server MUST open a window of **15 s** (flag `announceWindowMs`, default 15000), running concurrently with play. Announcements are **sealed** (not shown to other seats, including partners) until the window closes. The window MUST NOT close early, to avoid revealing that nobody holds a pair. At close, all announcements are revealed and resolved once (RL-16). | **VRO** (countdown) + PD | R15 (INFERRED ≈ 15–16 s; "13s" A041, "9s" A029, "3s" A022, "10s" A037: visual), reveal after close (003: visual). Credit timing in the reference is UNKNOWN (R16) → our rule: at close. D-G-11 |
| RL-16 | **Contest.** Let each side's *best* announcement be its strongest: tringla beats ronda; between two rondas the **higher** rank wins; between two tringlas the **lower** rank wins. If one side is strictly stronger it **wins** and scores `own + other`, where *own* = the sum of the values of all announcements made by its members and *other* = **1 point per announcement** made by the opposing side. The losing side scores 0. A side that is the only one announcing scores *own*. If the best announcements of the two sides are equal in strength (same kind and same rank) the contest is a **tie**: nobody scores (flag `announceTie = void`). | **VRO** (rank rules, 1v1 scoring) + PD (2v2, tie) | R18 (tutorial `017_rules_16–18`: "tringla beats a ronda … the lower one wins … plus 1 point for each other announcement … Two tringlas? … 5 + 1 = 6 … same number: nobody scores yet"). 2v2 aggregation and the tie's later derba clause (D-G-22) are our decisions. D-G-11 |
| RL-17 | **Cap.** Points from a ronda-class award (the winning side holds no tringla) MUST NOT lift a side above target − 1 (= 40). Points from an award that includes a tringla are uncapped. | **VRO** (tutorial) | R19 (`017_rules_19`: "ronda points can never end a game … A tringla can win it!"); live withheld ronda not observed. D-G-12 |
| RL-18 | **Catch.** After the window has closed a seat MAY accuse an *opponent* of hiding a pair: `Catch(targetSeat, rank)`. It is **allowed** only if the target has played at least one card of that rank in the current deal, the accuser is not locked out, and `(target, rank, deal)` has not been caught. It is **valid** if the target's hand at that deal contained ≥ 2 cards of that rank and the target did not announce that rank. Valid: **+1** to the accuser's side (flag `catchValueTringla` = 1). Invalid: the accuser is locked out of catching until the next deal begins (flag `catchLockout = nextDeal`, alternative `nextDeck`). | **VRO** (tutorial) + PD | R20 (`019_howto_16`, `017_rules_21–22`: "+1 … a wrong guess blocks you until the next round"); not played live. Evidence rule and lockout scope: our decision. D-G-13 |

## 5. End of a deck round and of the match

| ID | Rule | Tag | Evidence / decision |
|---|---|---|---|
| RL-19 | **Leftover.** When the last card of the deck has been played, any cards left on the table MUST go to the pile of the side that made the **last capture** of that deck round. If there was no capture in the deck round, the leftover cards are discarded. | **VRO** (tutorial) | R21 (`017_rules_26`; A039 → A041 fits, decomposition INFERRED) |
| RL-20 | **Dealer rule.** If the last capture of the deck round was **not** made by the dealer's side, the other side MUST be awarded **+5**. The Tutorial's King/Ace sentence is **not** implemented in v1 (flag `dealerKingAceRule`, default off). In 2v2 the test is **side-level** (flag `dealerRuleLevel = side`, alternative `player`). | **VRO** (+PD) | R22 (A021: bubble "The dealer wasn't the last to take! +5 for us": visual). K/A exceptions: R24 UNKNOWN → D-G-15. D-G-14 |
| RL-21 | **Cards over 20.** After RL-19, each side MUST score **+1 per card** in its pile above 20 (`max(0, n − 20)`). | **VRO** | R23 (A021: "I collected 32 cards, I get +12": visual) |
| RL-22 | **Tally order.** (1) leftover to the last capturer (RL-19); (2) dealer rule (RL-20); (3) cards over 20 (RL-21). All three awards are applied, then the match is evaluated (RL-23). Per-deck pile counters reset. | DD | Reference: component order UNKNOWN (R-matrix, Q12). Results of the three components are independent of order, except for tie handling in RL-23. |
| RL-23 | **Match end.** (a) During play, the match ends at once when a side's score reaches the target (e.g. a derba taking 40 → 41). (b) After a tally: if exactly one side has reached the target it wins; if both have, the **higher score** wins; if both are equal, one more deck round is played (dealer rotates) until one side leads after a tally (flag `bothReachTarget = higherWins`). (c) Forfeit: RL-25. | **VRO** (a) + PD (b) | R25 (A044: 41 – 26 after a tally; earlier video 41 – 23 on a derba). Both-sides case: R32 UNKNOWN → D-G-16 |

## 6. Timers and presence

| ID | Rule | Tag | Evidence / decision |
|---|---|---|---|
| RL-24 | **Turn timer.** Each turn has a server deadline of **20 s** (flag `turnTimerMs`; private tables 15–60 s). On expiry the server MUST play on the seat's behalf (*autopilot*: a legal card chosen by the basic bot policy), mark the seat **Away**, and then auto-play that seat's later turns after a short humanised delay (0.8–1.5 s) until the human sends `ReturnFromAway`. The returning human regains control at their next turn. | **VRO** (takeover exists) + PD (values) | R30 (A020, A021, A023, A042, B007: visual); timer length UNKNOWN (≤ 20.4 s bound, INFERRED, clean interval A023). D-G-17 |
| RL-25 | **Forfeit and abandonment.** Ranked: a seat that is Away for **8 consecutive auto-plays** forfeits (its side loses). A voluntary leave in ranked 1v1 is a forfeit. Casual and private matches never forfeit for inactivity: a seat that leaves is replaced by a bot (labelled). In 2v2, a departed seat is replaced by a bot and the side keeps playing. Disconnects use the reconnect grace period (doc 07 §8) and then follow the same rules as Away. | PD | D-G-17, D-S-08, D-S-09. Reference: reconnect after restart resumed the running match (M03, one earlier instance); network-loss behaviour UNKNOWN. |

## 7. Scoring ledger and explanations

| ID | Rule | Tag | Evidence / decision |
|---|---|---|---|
| RL-26 | Every point change MUST be recorded as a ledger entry `(side, points, reason, cause, deckRound, deal, seq)` with a reason code from §8. The UI MUST show an explainer (bubble + log line) for each entry. | DD (VRO pattern) | D-G-19. Reference explains scores in words: A021, A027, A031 (U07: visual). |
| RL-27 | The ruleset MUST be a versioned, data-driven object (`rulesetId`, `version`, flags). The server pins one ruleset per match and stores it in the match record so replays are exact. | DD | D-G-20 |

## 8. Ledger reason codes

| Code | Points | Trigger |
|---|--:|---|
| `MISSA` | +1 | RL-12 |
| `DERBA` | +1 | RL-13 |
| `ANNOUNCE_OWN` | Σ values | RL-16 winner's own announcements (ronda 1 each, tringla 5 each) |
| `ANNOUNCE_BONUS` | +1 per opposing announcement | RL-16 |
| `CATCH` | +1 | RL-18 valid |
| `DEALER_PENALTY` | +5 (to the other side) | RL-20 |
| `OVER_20` | +n | RL-21 |
| `FORFEIT_WIN` | to target | RL-25 |

Cap rule RL-17 is applied to `ANNOUNCE_OWN`/`ANNOUNCE_BONUS` entries; a clamped entry records both `requested` and `granted`.

---

## 9. Legal commands (summary; full list in doc 05 §6)

| Command | Legal when | Effect |
|---|---|---|
| `PlayCard(cardId)` | it is the seat's turn; card is in hand; (if `mustCapture`) the card captures whenever any hand card would | RL-07…RL-13 |
| `Announce(rank)` | window open; the seat holds ≥ 2 of the rank in the **current deal's** hand; none played; not yet announced | RL-14 |
| `Catch(target, rank)` | RL-18 "allowed" | RL-18 |
| `ReturnFromAway` | seat is Away | seat is Human again at its next turn |
| `Leave` | any time | RL-25 |

---

## 10. Worked examples (all are also golden test vectors in doc 10)

Notation: table as a set of ranks; `top` = pile counts; suits are irrelevant here.

| # | Situation | Result | Rules |
|---|---|---|---|
| E1 | Table {4, 7, 11}; play 7 | captures 7 (2 cards); chain: 10 absent → stop; table {4, 11} | RL-08, 09 |
| E2 | Table {3, 4, 5, 10}; play 3 | captures 3, 4, 5 (4 cards); 6 absent → stop; table {10} | RL-09 (tutorial example) |
| E3 | Table {2, 7, 10, 11}; play 7 | captures 7, then 10, 11 (jump 7 → 10), 12 absent → stop; 4 cards; table {2} | RL-09 |
| E4 | Table {11, 12}; play 11 | captures 11, 12 (3 cards); table empty → **Missa +1** | RL-09, 12 |
| E5 | Table {4, 7}; play 2 | drop; table {2, 4, 7} | RL-10 |
| E6 | Previous play: opponent dropped a 10 → table {1, 10}; play 10 | captures 10; **Derba +1**; table {1} | RL-13 |
| E7 | Opponent dropped a 10, then the next seat dropped a 3, then you play 10 | captures the old 10 but the previous play was not a drop of 10 → **no derba** | RL-13 |
| E8 | Table {10}; previous play was an opponent's drop of 10; play 10 | captures; table empty → **Derba +1** and **Missa +1** = +2 | RL-12, 13 |
| E9 | 1v1: P0 {5, 5, 2, 11} announces ronda(5); P1 {3, 3, 3, 12} announces tringla(3) | tringla beats ronda: P1 scores 5 + 1 = **6**; P0 0 | RL-16 |
| E10 | 1v1: P0 ronda(7), P1 ronda(4) | higher wins: P0 scores 1 + 1 = **2** | RL-16 |
| E11 | 1v1: P0 tringla(3), P1 tringla(6) | lower wins: P0 scores 5 + 1 = **6** | RL-16 |
| E12 | 1v1: only P0 announces ronda | **+1** | RL-16 |
| E13 | 1v1: both announce ronda(5) | tie → nobody scores | RL-16 |
| E14 | 1v1: P0 two rondas (3 and 8); P1 ronda(5) | P0's best (8) beats 5: scores own 2 + 1 = **3** | RL-14, 16 |
| E15 | 2v2: seat 0 ronda(4), seat 2 ronda(9), seat 1 ronda(6) | best of side A = 9 beats 6: side A scores own (1 + 1) + 1 = **3**; side B 0 | RL-16 |
| E16 | Side at 38 wins a ronda contest worth 3 | clamped to 40 (granted 2); match continues | RL-17 |
| E17 | Side at 38 wins a tringla award of 5 | 43 ≥ 41 → **match ends** | RL-17, 23 |
| E18 | Seat 1 played a 4 this deal, never announced; seat 0 catches (1, 4); seat 1's hand held 4, 4 | valid: **+1** to seat 0's side | RL-18 |
| E19 | Same, but seat 1 had announced the pair | invalid: seat 0 locked out until the next deal | RL-18 |
| E20 | Catch on a rank the target has not played this deal | **not allowed** (no evidence) | RL-18 |
| E21 | End of deck, 1v1, dealer P0; piles P0 8 / P1 32; last capture by P1 | dealer rule: P1 **+5**; over 20: P1 **+12**; P0 0 (matches A021 numbers) | RL-19–22 |
| E22 | End of deck, dealer P0 made the last capture; piles 23 + 3 leftover = 26 / 14 | P0 **+6**, P1 0, no dealer penalty (matches A039 → A041, decomposition INFERRED) | RL-19–22 |
| E23 | Tally: A at 38 gets +3 → 41; B at 38 gets the dealer penalty +5 → 43 | both ≥ 41 → higher wins: **B** | RL-23 |
| E24 | Tally leaves both at 41 | one more deck round | RL-23 |

---

## 11. Mapping of Phase 2 evidence to rules

| Phase 2 finding | Used in | Strength carried |
|---|---|---|
| R01–R02 deck, rank order | RL-01, 09 | tutorial + visual |
| R03–R04 deals | RL-05 | visual (1v1), tutorial (2v2) |
| R05 empty table at start | RL-06 | INFERRED → our rule |
| R07–R09 capture, chain, slots | RL-08–10, UI D-U-03 | tutorial + visual |
| R10–R11 forced capture, stacking | RL-08, 11 | UNKNOWN → decisions |
| R12 Missa | RL-12 | tutorial + visual |
| R13 Derba (call-out; escalation text) | RL-13 | tutorial + visual; escalation → D-G-21 |
| R14–R18 announcements | RL-14–16 | tutorial |
| R15–R16 window and credit timing | RL-15 | visual / UNKNOWN → our rule |
| R19 cap | RL-17 | tutorial |
| R20 catch | RL-18 | tutorial |
| R21–R23 end of deck | RL-19–21 | visual (A021, A039 → A041) |
| R24 K/A exceptions | RL-20 flag | UNKNOWN → off |
| R25 41 / instant end | RL-02, 23 | visual |
| R28 first player/direction | RL-06b | UNKNOWN → our rule |
| R29–R30 timer, AFK | RL-24 | timer UNKNOWN; takeover visual |
| R31 XP/rank deltas | not a rule (progression, doc 02 §7) | visual |
| R32 ties, quad, defeat | RL-14, 23 | UNKNOWN → decisions |

## 12. Edge cases

| Case | Rule |
|---|---|
| A seat's turn arrives but the deal's hands are empty | the deal ends; the next deal starts (RL-05) or the deck ends (RL-19) |
| A capture empties the table during the final play of a deck | Missa is awarded (default), then the tally runs; leftover is empty |
| A derba in the first play of a new deal | the previous play (the dealer's last play of the previous deal) still counts; no previous play exists at the start of a deck round |
| An announcement is made and the match ends before the window closes | the pending announcements are discarded |
| A seat plays a card of rank *r* before announcing a pair of *r* | the announcement is forfeited (RL-14) |
| Both partners announce the same rank | both are announcements of the same side; the side's `own` includes both values (E15 logic) |
| A seat times out while the window is open | autopilot may play a card of an announceable rank; the announcement is lost (the autopilot never announces in v1; flag `autopilotAnnounces`, default false) |
| Catch is attempted while the window is open | not allowed |
| Duplicate command (resent after reconnect) | idempotent by `(matchId, seat, commandSeq)` — doc 05 §6 |

## 13. Ruleset flags (all default values are PROPOSED)

| Flag | Default | Alternatives | Decision |
|---|---|---|---|
| `targetScore` | 41 | any | D-G-03 |
| `mustCapture` | false | true | D-G-07 |
| `chainWrap` | false | true | D-G-08 |
| `missaOnFinalPlay` | true | false | D-G-09 |
| `derbaEscalation` | false | tutorial-style 1/5/10 once understood | D-G-21 |
| `quadAsTringla` | true | quad = 2 rondas; quad = own kind | D-G-11 |
| `announceWindowMs` | 15000 | 10000–20000 | D-G-11 |
| `announceTie` | void | tutorial-style derba compensation | D-G-11, D-G-22 |
| `contestAggregation2v2` | sideSum | strongestOnly | D-G-11 |
| `rondaCap` | 40 | off | D-G-12 |
| `catchEvidence` | playedThisDeal | none (blind) | D-G-13 |
| `catchLockout` | nextDeal | nextDeck | D-G-13 |
| `catchValueTringla` | 1 | 5 | D-G-13 |
| `dealerRuleLevel` | side | player | D-G-14 |
| `dealerKingAceRule` | off | literal reading | D-G-15 |
| `bothReachTarget` | higherWins | extraDeck | D-G-16 |
| `turnTimerMs` | 20000 | 15000–60000 | D-G-17 |
| `rankedForfeitAutoPlays` | 8 | 4–12 | D-G-17 |
| `autopilotAnnounces` | false | true | D-G-17 |

## 14. Explicitly not in v1

2v2 derba escalation (D-G-21); the same-number-tie derba clause (D-G-22); the dealer King/Ace sentence (D-G-15); optional/declinable capture; mid-match rule variants for private tables other than timers (a private table MAY change `turnTimerMs` and `targetScore` only).
