# 03 — Game Rules Specification (reference game, as observed)

**Phase:** 2 · **Status:** FINAL for Phase 2 review (live-play evidence folded in at §9; contamination limits in `02_reference_game_analysis.md` §11) · **Date:** 2026-11-12
**Subject:** "Ronda: Moroccan Card" v6.1.1 (`com.sandukhan.ronda`). **This documents what the reference does. It is not yet our game design** (that is doc 07).
**Clean-room note:** rules and mechanics are not copyrightable expression, but the wording below is quoted only to record evidence. Our own rulebook text, names and UI strings must be written independently.

## Evidence labels

| Label | Meaning |
|---|---|
| **VERIFIED (tutorial)** | Stated in the game's own scripted Tutorial text (authoritative for what the designers intend). Quoted verbatim. Source captures `017_rules_*` ("Learn the rules" path) and `019_howto_*` ("How to use the app" path); post-action lines (`tut_t1…tut_t6`) were read from screen recordings of the same scripted steps, which are **not preserved** in `reference_captures/` (log-only for those lines). |
| **VERIFIED (play)** | Directly shown in a live match: a screenshot of the A/B series that the consolidation inspected (stated as *visual*), or a Driver/Observer log line with no inspected image (stated as *log-only*). Earlier-phase screen recordings are named "video" and are not preserved either. |
| **INFERRED** | Strongly suggested by the evidence but not conclusive (state diffs, arithmetic, text that is ambiguous). Never to be used as a rule without a controlled observation. |
| **UNKNOWN** | Insufficient evidence. Listed again in §8. |

Earlier drafts used `[V]`, `[V-text]`, `[V-play]`, `[H]`, `[U]`; they map to VERIFIED, VERIFIED (tutorial), VERIFIED (play), INFERRED, UNKNOWN. A rule marked only **VERIFIED (tutorial)** is what the game says; whether the live server applies it identically is not independently confirmed unless **VERIFIED (play)** is also shown.

**Evidence references.** `Axxx`/`Bxxx` = filenames in `reference_captures/` (Driver / Observer series). `@epoch` = host epoch seconds in the Driver log (`START` = 1794481151); *file time* = the screenshot's save time, which can lag the real moment by seconds because burst frames were copied. Driver log timestamps are the time a line was **written**, which sometimes trails the moment shown in the screenshot (see §11 of doc 02). **Contamination:** 32 of 61 touch windows contain real finger touches, so live-play observations after the first minute of the match are qualified individually below; the Driver forced-capture test **E3 is INVALID** and proves nothing.

---

## 1. Vocabulary

| Term | Meaning in this document |
|---|---|
| **Deck round** | One pass through all 40 cards (the Tutorial says "this deck" / "each deck"). Points persist across deck rounds; the per-deck captured-card counter resets. |
| **Deal** | One distribution of cards to the hands within a deck round. |
| **Side** | A team: one player (1v1) or two partners sitting opposite each other (2v2). The UI calls them *Us/Them* (2v2) and *Me/Rival* (1v1); in the Tutorial, *You/Rival*. |
| **Take** | Capturing cards from the table by playing a hand card (the Tutorial's word). Captured cards go to the capturing side's pile. |
| **Ronda / Tringla** | A pair / three of a kind (same number) in one hand, announced for points. |
| **Derba, Missa, Catch** | Bonus events defined in §5–§6. |

---

## 2. Deck and card order

**VERIFIED (tutorial)** (`017_rules_1`): *"Ronda is played with a 40-card Spanish deck: coins, cups, swords and clubs, numbered 1 to 7, then 10, 11 and 12. There are no 8s or 9s."*

- Suits: **coins, cups, swords, clubs** (traditional Spanish names *oros, copas, espadas, bastos*). **VERIFIED (play)** card faces.
- Ranks, in order: `1 2 3 4 5 6 7 10 11 12`. Court cards: 10 = *sota* (jack/page), 11 = *caballo* (knight on horse), 12 = *rey* (king). **VERIFIED (play)** (card faces; the Tutorial refers to "a King (12)" and "an Ace (1)").
- **Order for chains jumps from 7 to 10** — **VERIFIED (tutorial)** (`tut_t2`): *"3, then 4 and 5: all yours! After 7 the order jumps to 10, then 11 and 12."*
- Suit never matters for taking, ronda or tringla; the Tutorial speaks only of "the same number". **VERIFIED (tutorial, implicit)** Suit-dependent rules (none seen): **UNKNOWN**.
- Whether the sequence wraps from 12 back to 1: **UNKNOWN** (nothing observed suggests it does).

---

## 3. Setup, deals and turn flow

### 3.1 Cards per deal
**VERIFIED (tutorial)** (`017_rules_2`): *"Each player gets 4 cards (in 2 vs 2, 3 after the first round of each deck). Take cards from the table and score points: the first side to reach 41 points wins."*

| Mode | Deal sizes per deck round | Deck counter after each deal | Evidence |
|---|---|---|---|
| **2 v 2** | 4 each, then 3 each, then 3 each (16 + 12 + 12 = 40) | 24 → 12 → 0 | **VERIFIED (play)** (video: counter 24, then 12, then none; hand sizes 4 → 3 → 3) + **VERIFIED (tutorial)** |
| **1 v 1** | 4 each per deal (8 cards per deal), five deals per deck round (5 × 8 = 40) | 32 → 24 → 16 → 8 → 0 | **VERIFIED (play, visual)**: 32 (A027), 24 (A029, A031, A041), 16 (A033), 8 (A023); 0 = no counter on the last deal (A038, A039). Deal size 4 per hand: A020, A041 |

- The deck-remaining counter is shown next to the dealer / the dealing seat (a rotated card-back with a number at the own seat, a card-back at the rival seat). **VERIFIED (play, visual)** A023, A027, A029, A041
- **The table starts empty at the first deal of a deck round.** 2v2: **VERIFIED (play)** (earlier video, counter 24, no table cards). 1v1: **INFERRED** from card accounting: at the first look at a new deck (Driver `E1`, @1794481719.36, deck 32) hands were 4 + 3 and one table card (the rival's lead) = 32 + 4 + 3 + 1 = 40, so no table cards were dealt.
- **Reconciled contradiction — "8 cards on the table at the start of a round".** A041 (file time @1794482026.1, name `round_start_8_on_table`) shows 8 table cards, deck 24, hands 4 + 4, captured-cards row 0 / 0. That is **not** a round start: 24 + 8 + 4 + 4 = 40 means a *second deal* in a deck where the first 8 plays (4 each) captured nothing (captured counters still 0 / 0). The filename is the Driver's label, not a rule. The 8 table cards have 8 distinct ranks (1, 2, 3, 4, 7, 10, 11, 12), which is consistent with "a card is dropped only when no table card has its rank". **INFERRED** (the first deal itself was not captured).
- Dealer rotates between deck rounds: the dealer token sat at the own seat in A021 (@1794481583, round 1) and A039 (@1794481903), at the rival seat in A041/A043 (@1794482026–057). Strict alternation in 1v1 is **INFERRED**; 2v2 rotation order is **UNKNOWN**.

### 3.2 Turn order
- Players act one at a time; a player's avatar **glows green** when it is their turn. **VERIFIED (tutorial)** (`019_howto_12`): *"Your cards are at the bottom. When your picture glows, it's your turn: tap a card to play it."* and **VERIFIED (play)**.
- Direction of play: counter-clockwise on screen in 2v2 **INFERRED** (a partial reading of dealer → next player across two deck rounds). Who plays first after a deal: **UNKNOWN**.
- Playing a card is **a single tap on a hand card** (no drag, no confirm). **VERIFIED (tutorial)** + **VERIFIED (play)** (tap on the card in the Tutorial played it).
- Turn timer length and what happens on timeout: **INFERRED only** — a missed turn hands play to bots behind an "AFK" dialog (§3.4); the exact seconds are **UNKNOWN** (see §3.4, §8 Q14).

### 3.3 Playing a card (core rule)
**VERIFIED (tutorial)** (`017_rules_3`): *"Your turn! Play a card with the same number as a card on the table to take it."* After the take: *"Well done! You took the 7."* (`tut_t1`). Result in the scoreboard: top row +2 (the played card and the matching table card). **VERIFIED (tutorial)**

1. Play a hand card.
2. If a table card has the **same number**, that card is **taken** together with the played card.
3. **Chain:** **VERIFIED (tutorial)** (`017_rules_7`): *"When you take, you also take the cards that follow in order."* e.g. play 3 with table `3,4,5,10`: *"3, then 4 and 5: all yours! After 7 the order jumps to 10, then 11 and 12."* The chain follows consecutive numbers present on the table and **stops at the first missing number** (the 10 stayed because no 6 was present; top row went 2 → 6 = played 3 + table 3,4,5). **VERIFIED (tutorial)**
4. If nothing matches, the card **remains on the table**. **VERIFIED (play, visual)** — A041 shows 8 non-captured cards after eight plays (captured counters 0 / 0).
5. **Chain in live play.** A029 → A030 (file times @1794481759 → @1794481778; Driver `E4` @1794481778.12): table `1 coins, 3 swords, 7 swords, 11 cups, 12 coins` → table `3 swords, 7 swords`; my captured-cards counter 0 → 3, rival 4 → 6. The only reading that fits is "11 played, took the 11 and the following 12" (3 cards) and the rival took the 1 with a 1 (2 cards). **VERIFIED (play, visual)** for the state change and the counter deltas; the play-by-play attribution is **INFERRED** (two moves happened between the frames; real touches were present in that interval, §11 of doc 02).

**Table layout is rank-addressed, not first-free-slot.** In A027, A029, A033, A039 and A041 every table card sits in a fixed slot by rank: top row, left to right = ranks 1, 2, 3, 4, 5; bottom row = 6, 7, 10, 11, 12. (A033: 1 and 5 on top, 6 and 10 on the bottom, each in its rank's column; A041: ranks 1-4 top, 7-12 bottom, the 6 slot empty.) **VERIFIED (play, visual).** This replaces the earlier draft claim that "played cards occupy the next free slot". Whether two table cards of the same rank can coexist in one slot is **UNKNOWN**: A027 shows two overlapped 2-of-swords cards in one slot while a "Derba! +1" bubble is on screen, which is most simply a capture animation in progress (**INFERRED**), not a persistent stack.

Not established: see §8 Q1–Q4 (multiple table cards of the same number, forced take, whether a player may decline an available take). **Forced capture is UNKNOWN**: the Driver's test `E3` (@1794481900.63) is INVALID (the AFK takeover was active and bots played the cards, @1794482055.45 note).

### 3.4 Turn timer, AFK takeover and timeout (new)
| Observation | Evidence | Label |
|---|---|---|
| A modal "You are AFK — Bots are playing for you." with one button "I'm back" covers the table. The match continues underneath; rival bubbles and score updates keep arriving | A020 (file time @1794481427.50), B007 (event still @1794481458.37), A021 (@1794481583), A023 (@1794481667), A042 | **VERIFIED (play, visual)** |
| First appearance: already on screen at A020, i.e. ≤ 55.9 s after the dealt table first appeared (A018, file time 1794481371.57). The Driver measured "~33 s" from its own `M4` line (1794481391.999) and logged the dialog at 1794481458.368; both are *write times*, not the onset (the dealt table was already on screen ≈ 20 s before `M4`) | A020 vs Driver log lines 26–27 | **VERIFIED (play, visual)** for ≤ 55.9 s; onset exactly: **UNKNOWN** |
| Real finger touches occurred in the touch windows stamped @1794481425.7 (2), 1794481471.0, 1794481501.2, 1794481516.2, 1794481546.4 (1 each). The first appearance coincides with a contaminated window, so "33 s of inactivity" is **not** a clean measurement | `touch_watch.log` | **CONTAMINATED** |
| After the Driver tapped "I'm back" (@1794481646.74) the dialog was back by A023 (file time @1794481667.17), i.e. within ≤ 20.4 s. The touch windows stamped 1794481636.9, 1794481651.9, 1794481667.0, 1794481682.1 all read 0, so this cycle is **clean**. A022 (file time 1794481649.46, 2.7 s after the dismissal) shows no dialog and "Ronda declarations close in 3s"; the Driver logged its Ronda tap at 1794481664.29 (a write time ≈ 15 s after A022) as "lost to AFK takeover", which the evidence cannot place more precisely | Driver log lines 29–31; A023; touch log | **INFERRED** (bound, not a measurement) |
| Driver's estimate "turn timer ≈ 12 s" | Driver log line 31 (@1794481684.78) | **INFERRED**, low confidence: derived from the Driver's reading latency, not from a timed experiment |
| Bots play instantly for the absent player until "I'm back" is tapped (cards disappeared from hand before the Driver's tap landed, `A027` @1794481723 vs Driver line @1794481743.61 "tapped 7 swords but bots had already played my cards") | A027; Driver log | **VERIFIED (play, log-only)** for the sequence; bot quality/strategy **UNKNOWN** |
| After "I'm back" the dialog did *not* re-appear within 23.5 s while cards were being played (A029 @1794481759.4 and A030 @1794481778.1 show no dialog after I'm back @1794481754.62), but the windows 1794481757.5 / 1794481772.6 / 1794481787.7 contain 2 / 2 / 6 real touches, so a human may have been playing in time | Driver log line 34; touch log | **CONTAMINATED** |

**Summary.** The AFK takeover exists (VERIFIED). Its trigger is *a missed turn*, not "no touch at all" (INFERRED: it re-appeared on a clean interval ≤ 20.4 s after a dismissal). The turn-timer value (≈ 12 s) and whether a limited number of timeouts leads to forfeiture are **UNKNOWN**. Whether an absent player is replaced by a bot server-side or client-side is **UNKNOWN**.

---

## 4. Bonus events

### 4.1 Missa (clear the table) — **+1**
**VERIFIED (tutorial)** (`017_rules_10`): *"Take every card and leave the table empty: that's a Missa, worth 1 point! Tap your 6."* → *"Missa! +1 point."* (`tut_t3`; top row 6 → 8, bottom row 0 → 1). **VERIFIED (play, visual)**: A030 → A031 (file times @1794481778 → @1794481782; Driver @1794481806.23): the table is emptied by the rival's capture, a "Missa!" bubble appears at the rival's seat and the rival's points go 22 → 23 (my side stays 14), the table is empty. Note the bubble is the *capturer's* call-out; beneficiary = capturer.

### 4.2 Derba — **+1**, escalating in 2v2
**VERIFIED (tutorial)** (`017_rules_23`): *"The Rival just played a 10. Take it right away with your 10: that's a Derba, +1 point! Tap your 10."* → *"Derba! +1. In 2 vs 2 it can grow: if the next players play the same number, it's worth 1, then 5, then 10 points to the last one."* (`tut_t6`; top row 8 → 10, bottom 3 → 4; the other table card (a 1) stayed). **VERIFIED (play)**: "Derba! +1" call-outs in 1v1 and 2v2 with +2 cards and +1 point (earlier video); a "Derba! +1" bubble anchored at the rival's seat is in A027 (file time @1794481723.03, **visual**); one 1v1 match ended at exactly 41 on a Derba (earlier video "passive3", not preserved). Whether a Derba also needs the *immediately* preceding play, and the 2v2 escalation (1 → 5 → 10), have **not** been observed in live play (UNKNOWN).

Definition (tutorial text): a **Derba** is taking, *immediately*, the card the previous player has *just* played, using a card of the same number. **Escalation in 2v2** (1 → 5 → 10) is described only in text; exact trigger sequence and which side scores: **UNKNOWN** (§8 Q6).

### 4.3 Ronda (pair) — **+1**, and Tringla (three of a kind) — **5**
**VERIFIED (tutorial)** (`017_rules_13`): *"Two cards of the same number in your hand make a Ronda. Announce it before you play them: tap Ronda."* → *"Ronda! +1 point. Three of a kind is a Tringla: 5 points!"* (`tut_t4`; bottom row 1 → 2).

- A **Ronda** button appears beside the hand when the dealt hand contains a pair; a **Tringla** button exists for three of a kind. **VERIFIED (tutorial)** (`019_howto_14`): *"Holding a ronda or a tringla? A button shows up: tap it to announce it before time runs out."* The My-table editor also shows separate "Ronda" and "Tringla" buttons. **VERIFIED (play)**
- **Declaration window:** a countdown banner "Ronda declarations close in Ns" with a draining bar runs after *every* deal (not only the first). **VERIFIED (play, visual)**: "13s" in A041 (2nd deal of a deck), "9s" in A029 (2nd deal), "10s" in A037 (last deal, deck 0); "3s" in A022 (file time 1794481649.46; the Driver logged its tap `E7` at 1794481664.29). The highest value ever seen is 13 s, so the total length is **≈ 15–16 s INFERRED**; measured from the deal itself: **UNKNOWN**. The pill "Ronda" (bottom-right of the hand) is present while the window is open (A029, A036, A042).
- Holding two different pairs shows **"2 rondas!"** and is worth +2. **VERIFIED (play)** (earlier video).
- A pair of the same rank in the dealt hand was seen in A029 (3, 3), A036 (10, 10) and A041/A042 (10, 10): the Ronda button was available in each. **VERIFIED (play, visual)**
- **Award timing in live play: UNRESOLVED.** Earlier video: a bubble *"I had a ronda! I take +1"* / *"+2"* appeared after the window closed (the Tutorial applies the point instantly). Driver evidence: a Ronda tap at @1794481774.26 followed by my points 14 (A030, @1794481778.1) → 15 (A033, @1794481826.05). A029 (file time @1794481759.4) already showed "9s" left, so, if the window is ≈ 15–16 s, it closed about 9 s after that file time (≈ 1794481768, derived, not logged) and would already have been over at the logged tap time; the tap may have been the *chained* tap at @1794481754.62, or a real finger (touch windows stamped 1794481772.59 = 2 and 1794481787.67 = 6). The +1 may also be a Derba or other event. **Both observations are kept; neither proves when the point is awarded.** **UNKNOWN**
- Four of a kind: **UNKNOWN** (a 4-card first deal makes it possible).

### 4.4 Contest between announcements
**VERIFIED (tutorial)** (`017_rules_16`): *"If both sides announce, the stronger one wins: a tringla beats a ronda, between two rondas the higher number wins, and between two tringlas the lower one wins."*
**VERIFIED (tutorial)** (`017_rules_17`): *"The winner scores their own announcement, plus 1 point for each other announcement, even a tringla. Two tringlas? The winner gets 5 + 1 = 6 points."*
**VERIFIED (tutorial)** (`017_rules_18`): *"Two rondas of the same number? Nobody scores yet. During the round, each time a player makes a derba on that number, the other side earns 1 point."*

| Case | Result |
|---|---|
| Tringla vs ronda | Tringla side wins: scores 5 + 1 = 6 |
| Ronda vs ronda (different numbers) | Higher number wins: scores 1 + 1 = 2 |
| Tringla vs tringla | **Lower** number wins: scores 5 + 1 = 6 |
| Ronda vs ronda (same number) | Nobody scores at announcement; each later derba on that number gives the *other* side 1 point |

How "higher" compares numbers when 10, 11, 12 are involved is presumed to follow the card order (12 highest): **INFERRED**. Multi-announcer cases in 2v2 (more than two announcers): **UNKNOWN**.

### 4.5 Ronda points never end the match
**VERIFIED (tutorial)** (`017_rules_19`): *"Good to know: ronda points can never end a game, you stop one point short of 41. A tringla can win it!"* → ronda points cap the side at **40**. **VERIFIED (play)**: in a 1v1 match the leading side's points stayed at 40 across roughly 70 s of recording and the match ended only when a *Derba* moved it to 41, so non-ronda points can end the match. We did **not** observe a ronda actually being withheld at 40, so the cap itself is **VERIFIED (tutorial)** only. Whether Missa/Catch/last-take/over-20 points can end the match: **VERIFIED (play) yes for Derba, INFERRED yes for the others**.

### 4.6 Catch (accuse an unannounced ronda) — **+1**
**VERIFIED (tutorial)** (`019_howto_16`): *"Think an opponent hid a ronda? Tap the target next to their name and pick the number."*
(`017_rules_21`): *"The Rival played two 4s and never announced a ronda. Catch him: tap the target next to his name, then pick 4."* → *"Caught him! +1 point. Careful: a wrong guess blocks you until the next round."* (`tut_t5`; bottom row 2 → 3).

- UI: a crosshair icon beside each opponent's name opens a 5 × 2 number picker (1–7, 10–12); choosing a number submits the accusation. **VERIFIED (tutorial)** (`017_rules_22`) The crosshair icons are visible on every opponent in live 2v2/1v1 tables. **VERIFIED (play)**
- A correct catch: **+1 point** to the accuser. A wrong guess: the accuser **cannot catch again until the next round** (scope of "round" **UNKNOWN**). **VERIFIED (tutorial)**
- Whether catching a hidden *tringla* is worth more: **UNKNOWN**.

---

## 5. End of a deck round

**VERIFIED (tutorial)** (`017_rules_26`): *"When the deck runs out, the cards left on the table go to the last side that took cards."*

**VERIFIED (tutorial)** (`017_rules_27`): *"The dealer must be the last player to take in the deck, or the other side gets 5 points. If the dealer's last take is with a King (12), the dealer's side gets 5; with an Ace (1), the other side does."*

**VERIFIED (tutorial)** (`017_rules_28`): *"Then each side counts its cards: every card over 20 is worth 1 point."* So a side that captured `n` cards scores `max(0, n − 20)` points.

**Live verification — three end-of-deck tallies (1v1):**

| Tally | Evidence | What is directly visible | Reading |
|---|---|---|---|
| Deck 1 | **A021** (file time @1794481583.25; read as an image). Rival speech bubble: *"The dealer wasn't the last to take! +5 for us. I collected 32 cards, I get +12"*. Scoreboard behind it: cards 8 / 32, points 2 / 0 | **VERIFIED (play, visual)** that (a) the bubble announces the dealer-not-last bonus **+5 to the other side** — the dealer token was at the own seat (me) and the +5 went to the rival; (b) 32 cards → **+12 = 32 − 20**. The rival's points were 20 by `E7` (@1794481664.29, 80 s later): 0 + 5 + 12 = 17 plus ≈ 3 earned in the next deck is **INFERRED**, not itemised |
| Deck 3 | **A039** (@1794481903.8) cards 23 / 14, points 20 / 26, 3 cards left (table 5 and 12, my hand 6) → **A041** (@1794482026.1) points **26 / 26** | **VERIFIED (play, visual)** my side +6, rival +0. **INFERRED** decomposition: leftover cards to the last side that took (me) gives 23 + 3 = 26 cards, 26 − 20 = +6; no dealer-not-last bonus because I (dealer in that deck) was last to take. The final-card counts were not captured |
| Deck 4 (match end) | A043 (@1794482057.9) cards 22 / 8, points 32 / 26 → A044 (@1794482087.9) final **41 – 26**, captured counters reset to 0 / 0 | **VERIFIED (play, visual)** points 32 → 41 (+9) in one transition; +9 = 29 − 20 fits but the breakdown (cards, leftover, bonuses) was not captured: **INFERRED** only |

Established by this: *leftover cards go to the last side that took*; *dealer-not-last = +5 to the other side*; *(cards − 20) points per side*. Still **UNKNOWN**: the King / Ace special cases quoted in `017_rules_27` (no live instance), who "the dealer" is in 2v2, the order of tally steps, what happens when both sides exceed 20 (impossible with 40 cards) or sit on exactly 20, and what happens when no side has taken any card.

> **Ambiguous sentence kept verbatim for the design phase:** "*If the dealer's last take is with a King (12), the dealer's side gets 5; with an Ace (1), the other side does.*" It is unclear whether this is an additional rule or a clarification of the 5-point penalty. Both A021 and the Tutorial agree on "dealer must be last to take". The King/Ace exceptions have no live evidence. **UNKNOWN**

---

## 6. Winning the match

- **First side to reach 41 points wins** (`017_rules_2`, **VERIFIED (tutorial)**). **VERIFIED (play)**: 1v1 ended 41–23 the instant a Derba moved 40 → 41 (mid-deck-round); a 2v2 match ended 44–31 after a deck-round tally (so the final score may exceed 41).
- Ronda points are capped at 40 (§4.5).
- **Live 1v1 end (this session):** final score 41 – 26 (A044/A045). The scoreboard's captured-cards row reads 0 / 0 behind the result panel, i.e. the deck tally had been applied before the panel opened. The result panel was on screen at file time @1794482087.93 (A044), ≈ 0.5 s after the Driver's last logged play (`E4`, @1794482085.39). **VERIFIED (play, visual).** The Driver's own match-end line (@1794482109.879) was written ≈ 22 s after the panel was already visible, so it must not be used as the end time.
- Result: "VICTORY" panel with final score; "Defeat"/draw variants **UNKNOWN** (no loss was observed).
- **Result-panel numbers animate.** A044 (burst label `t05`) reads **+62 XP, ▲ +6**; A045 (`t17`) reads **+100 XP, ▲ +9** for the same 41 – 26 result. The Driver log quotes the first frame ("+62 XP, +6 rank pts"). The final values are the A045 ones. **VERIFIED (play, visual)**; the meaning of the green ▲ number (rank points per the leaderboard/profile bar) is **INFERRED**.
- Tie at ≥ 41 for both sides (possible in a single tally) **UNKNOWN**.

---

## 7. Scoreboard semantics (**now confirmed**)

**VERIFIED (tutorial)** (`019_howto_18`): *"The scoreboard: your side on the left. Cards taken this deck on top, points below."*

| Row | Meaning | Resets | Evidence |
|---|---|---|---|
| Top | Cards captured by the side **in the current deck round** (both sides sum to ≤ 40) | At each new deck round | **VERIFIED (tutorial)** + **VERIFIED (play)** |
| Bottom | **Match points** (target 41) | Never within a match | **VERIFIED (tutorial)** + **VERIFIED (play)** |

---

## 8. Open questions (unambiguous list)

| # | Question | Why it matters | Status |
|---|---|---|---|
| Q1 | If the table holds two or more cards of the played number, are all taken? (A027 overlap is most likely a capture animation, §3.3) | Core capture function | UNKNOWN |
| Q2 | Does the chain apply only after an equal-rank take, only upward, and can it wrap 12 → 1? Does it include a card already taken? | Core capture function | Upward, stops at gap: VERIFIED (tutorial); a live 11 → 12 chain is consistent with A029 → A030 (INFERRED attribution); wrap: UNKNOWN |
| Q3 | Is a take compulsory when available? May a player instead drop a non-matching card? | Legal-move generator | **UNKNOWN** — Driver test E3 INVALID (AFK bots were playing) |
| Q4 | Can you play a card that matches but choose not to take (drop a matching rank)? | Legal-move generator | **UNKNOWN** (same reason; A041's 8 distinct-rank table cards show no duplicate rank ever left on the table, consistent with compulsory capture but equally with chance) |
| Q5 | Exact Derba trigger: only the card *just* played by the previous player? Does a chain also apply? Can it stack with Missa? | Scoring | UNKNOWN (definition: tutorial text) |
| Q6 | 2v2 Derba escalation 1 → 5 → 10: counting window, who scores ("to the last one"), reset conditions | Scoring | UNKNOWN |
| Q7 | Exact ronda declaration window length and when points are applied relative to window close | Timers, scoring | UNKNOWN (≈ 15–16 s INFERRED; award timing UNRESOLVED, §4.3) |
| Q8 | Tringla vs four of a kind; partner visibility of announcements in 2v2; more than two announcers | Scoring | UNKNOWN |
| Q9 | Catch: what exactly counts as "never announced"; effect on a hidden tringla; scope of "until the next round" | Anti-cheat feature | UNKNOWN |
| Q10 | 1v1 first deal: is the table empty? | Setup | INFERRED yes (card accounting, §3.1); first deal never captured |
| Q11 | End-of-deck dealer rule (quoted in §5): the King/Ace cases; the dealer in 2v2 | Scoring | Dealer-not-last = +5 other side: VERIFIED (play, A021). King/Ace exceptions: UNKNOWN |
| Q12 | Order of tally steps at deck end (leftover cards → last-take → dealer rule → over-20) and tie-breaks | Scoring | Components VERIFIED separately (A021, A039 → A041); order and ties UNKNOWN |
| Q13 | Who leads after a deal; dealer rotation; confirm turn direction | Turn manager | UNKNOWN / INFERRED (1v1 alternation, §3.1) |
| Q14 | Turn timer length and timeout action (auto-play? skip? forfeit) | Server authority | Action = bots play behind "You are AFK" dialog: VERIFIED (play, visual). Timer length: UNKNOWN (≤ 20.4 s INFERRED, §3.4). Forfeit rule: UNKNOWN |
| Q15 | Both sides ≥ 41 simultaneously | Match end | UNKNOWN |
| Q16 | Behaviour when a player leaves/disconnects mid-match (bot takeover? forfeit?) | Reconnect design | UNKNOWN (requires user-approved test) |

---

## 9. Live-play findings from the Driver agent (A)

Source: the Driver's log (`driver_log.md`, 44 lines), the Observer's index and touch-watch log, and the A/B screenshots. The Driver played one public **1v1** match to completion (**victory 41 – 26**, 4 deck rounds, started @1794481361.43, panel visible @1794482087.93). No 2v2 match, no loss and no tringla/catch were played in this session.

### 9.1 Timeline (host epoch; `t+` = seconds since `START` 1794481151)

| Epoch | t+ | Event | Source |
|---|--:|---|---|
| 1794481361.43 | 210 | Tap "1 Vs 1 Match" → "Searching" modal | log line 24; A016 (visual) |
| 1794481367.06 | 216 | Table loading: empty table, placeholder "Us / Them" labels | A017 (visual) |
| 1794481371.57 | 220 | Dealt table: deck 32, 4 cards each, I am dealer, labels "Me / Rival" | A018 (visual) |
| 1794481391.999 | 241 | Driver writes "match started, dealing done" (write time) | log lines 25–26; A019 file time 1794481394.33 |
| ≤ 1794481427.50 | ≤ 276 | AFK dialog already visible | A020 (visual) |
| 1794481458.37 / .485 | 307 | Driver logs AFK, taps "I'm back" | log 27–28; B007, B008 |
| 1794481583.25 | 432 | Deck 1 tally bubble (dealer not last, 32 cards, +12) while the AFK dialog is up | A021 (visual) |
| 1794481646.74 | 496 | "I'm back" again ("resume manual play") | log 29 |
| 1794481649.46 | 498 | "Ronda declarations close in 3s", Ronda pill, no AFK dialog (2.7 s after "I'm back") | A022 (visual) |
| 1794481664.29 | 513 | Driver writes "tapped Ronda with 3 s left; lost to AFK takeover" (write time) | log 30–31 |
| 1794481667.17 | 516 | AFK dialog on screen again | A023 (visual) |
| 1794481719.36 | 568 | New deck (deck 32, cards 0 / 0, points 14 / 21), 4 + 3 cards, 1 table card | log 32; B010 |
| 1794481723.03 | 572 | "Derba! +1" bubble; two overlapped 2s | A027 (visual) |
| 1794481754.62–774.26 | 603–623 | Chained "I'm back" + Ronda attempt (pair of 3s) | log 34–35; A029 (visual) |
| 1794481778.12 → 782.74 | 627 → 632 | 11-chain; rival's Missa (+1) | A030, A031 (visual); log 35–36 |
| 1794481806.23–859.88 | 655–709 | Further plays (log 36–38); points 15 / 25 → 16 / 25 | A032, A033, A035 |
| 1794481900.63 | 750 | **E3 forced-capture "test"** | log 39 — **INVALID** (log 41) |
| 1794481903.8 | 753 | Last deal, 23 / 14 cards, 20 / 26 points | A039 (visual) |
| 1794482026.14 | 875 | Deck 4: 8 cards on the table, deck 24, 26 / 26, "Ronda declarations close in 13s"; AFK dialog again | A041, A042 (visual) |
| 1794482085.39 | 934 | Driver's last logged play | log 42 |
| 1794482087.93 | 937 | VICTORY panel visible, 41 – 26 | A044 (visual) |
| 1794482091.58 | 941 | Panel final numbers (+100 XP, ▲ +9) | A045 (visual) |

### 9.2 Rule-evidence register (live play)

| Rule / mechanic | Live evidence | Strength | Notes |
|---|---|---|---|
| 1v1 deck sequence 32 → 24 → 16 → 8 → 0, 4 cards per hand per deal | A027, A029, A033, A023, A038/A039 | VERIFIED (visual) | covers all five deals |
| Rank-addressed table slots (top 1–5, bottom 6, 7, 10, 11, 12) | A027, A029, A033, A039, A041 | VERIFIED (visual) | replaces "next free slot" |
| Capture + chain (11 → 12) | A029 → A030 | VERIFIED (visual) state; attribution INFERRED | 2 moves between frames |
| Missa +1 | A030 → A031 (rival 22 → 23, bubble, empty table) | VERIFIED (visual) | |
| Derba bubble "Derba! +1" | A027 | VERIFIED (visual) bubble | beneficiary and trigger not itemised |
| Ronda button + ~15 s window each deal | A029 (9 s), A037 (10 s), A041 (13 s) | VERIFIED (visual) | length INFERRED |
| Ronda point award timing | A030 14 → A033 15 | UNKNOWN | §4.3 |
| End-of-deck: leftover to last taker, dealer-not-last +5, cards − 20 | A021, A039 → A041, A043 → A044 | VERIFIED (visual) for the first two parts of A021 and the numbers; decomposition of A039 → A041 and A043 → A044 INFERRED | |
| Match ends when a side passes 41 | A044 (41 – 26) | VERIFIED (visual) | |
| AFK takeover | A020, A021, A023, A042, B007 | VERIFIED (visual); timer INFERRED | contaminated first cycle |
| Forced capture | E3 | **INVALID — UNKNOWN** | |
| Tringla, Catch, 2v2 derba escalation, four of a kind | not played | UNKNOWN | tutorial text only |

**Residual items that stay open:** the Driver's E4 log line quotes the pre-move state of A043 (saved ≈ 27 s earlier), so the exact last move and the +9 breakdown are not recoverable from the logs. The files `A039_E3_t07`/`A040_E3_t2` carry an "E3" label but show the final-deal state used by the Driver's `E6` entry; the label is the Driver's burst name, the content is what is cited here.

---

## 10. Machine-readable test cases for the rule engine (verified cases only)

Notation: ranks only (suits irrelevant for these rules). `table` and `hand` are multisets of ranks; `top`/`bottom` are the scoreboard rows (cards taken this deck / points). Each case is independent. Evidence IDs refer to captures or scripted-tutorial recordings.

```yaml
ranks_order: [1,2,3,4,5,6,7,10,11,12]      # next(7) == 10; evidence: tut_t2 / tutorial text
cases:
  - id: TC-TAKE-01   # equal-rank take
    given: {table: [4,7,11], hand: [2,7,12], score: {top: [0,0], bottom: [0,0]}}
    action: {play: 7}
    expect: {table: [4,11], taken_by_actor: [7,7], score: {top: [2,0], bottom: [0,0]}}
    evidence: ["017_rules_3", "tut_t1 (tutorial text; also seen in live play)"]
  - id: TC-CHAIN-01  # take + following numbers, stop at the first gap
    given: {table: [3,4,5,10], hand: [3,11], score: {top: [2,0], bottom: [0,0]}}
    action: {play: 3}
    expect: {table: [10], taken_by_actor: [3,3,4,5], score: {top: [6,0], bottom: [0,0]}}
    evidence: ["017_rules_7", "tut_t2 (tutorial text)"]
  - id: TC-MISSA-01  # clearing the table = +1
    given: {table: [6], hand: [6,1], score: {top: [6,0], bottom: [0,0]}}
    action: {play: 6}
    expect: {table: [], score: {top: [8,0], bottom: [1,0]}, event: missa}
    evidence: ["017_rules_10", "tut_t3 (tutorial text); Missa call-out also live play"]
  - id: TC-RONDA-01  # announce a pair before playing: +1 (tutorial applies it immediately)
    given: {hand: [5,5,11], table: [2,12], announced: false, score: {bottom: [1,0]}}
    action: {announce: ronda}
    expect: {score: {bottom: [2,0]}, event: ronda}
    note: "award timing in live play is UNRESOLVED (section 4.3): an earlier video showed the point after the window closed; Driver data A030/A033 is contaminated. The engine must not hard-code either until observed."
    evidence: ["017_rules_13", "tut_t4 (tutorial text)"]
  - id: TC-CATCH-01  # accuse an unannounced pair
    given: {opponent_played_unannounced_pair_of: 4, score: {bottom: [2,0]}}
    action: {catch: {target: opponent, number: 4}}
    expect: {score: {bottom: [3,0]}}
    evidence: ["017_rules_21", "tut_t5 (tutorial text)"]
  - id: TC-DERBA-01  # take the card the opponent just played
    given: {table: [1,10], last_played_by_opponent: 10, hand: [10,3], score: {top: [8,0], bottom: [3,0]}}
    action: {play: 10}
    expect: {table: [1], score: {top: [10,0], bottom: [4,0]}, event: derba}
    evidence: ["017_rules_23", "tut_t6 (tutorial text); Derba call-outs live play"]
  - id: TC-DEAL-2V2  # deal sizes per deck round
    given: {mode: 2v2}
    expect: {deal_sizes: [4,3,3], deck_counter_after_each_deal: [24,12,0], table_at_start: []}
    evidence: ["017_rules_2 (tutorial text)", "video 2v2 (live play)"]
  - id: TC-DEAL-1V1
    given: {mode: 1v1}
    expect: {deal_size: 4, deck_counter_after_each_deal: [32,24,16,8,0], table_at_start: []}
    evidence: ["017_rules_2 (tutorial text)", "A027 (32), A029 (24), A033 (16), A023 (8): visual; table_at_start INFERRED, section 3.1"]
  - id: TC-ENDDECK-01   # tally at the end of a deck round (1v1)
    given: {cards_taken: [8,32], leftover_table_cards_awarded_to: last_taker, dealer: side_0, last_taker: side_1}
    expect: {points_added: [0,17], note: "side_1: 32 - 20 = 12, plus 5 because the dealer (side_0) was not the last to take"}
    evidence: ["A021 (visual bubble: 'The dealer wasn't the last to take! +5 for us. I collected 32 cards, I get +12')"]
  - id: TC-ENDDECK-02   # cards over 20 only
    given: {cards_after_leftover: [26,14], dealer_was_last_taker: true}
    expect: {points_added: [6,0]}
    evidence: ["A039 -> A041 (visual points 20 -> 26 and 26 -> 26); the 26-card figure is INFERRED"]
  - id: TC-END-01  # match ends the instant a non-ronda point reaches 41
    given: {mode: 1v1, score: {bottom: [40,23]}}
    action: {event: derba_by_side_0}
    expect: {score: {bottom: [41,23]}, match_over: true, winner: side_0}
    evidence: ["video passive3 (live play)"]
  - id: TC-CAP-01   # ronda points never push a side past 40
    given: {score: {bottom: [40,9]}}
    action: {event: ronda_awarded_to_side_0}
    expect: {score: {bottom: [40,9]}, match_over: false}
    evidence: ["017_rules_19 (tutorial text only; live play showed the score holding at 40 but a withheld ronda was not observed)"]
  - id: TC-SCOREBOARD-01
    expect: {row_top: "cards taken this deck", row_bottom: "points", left_column: "your side"}
    evidence: ["019_howto_18 (tutorial text)", "tut_t1..tut_t6 (tutorial text)"]
```

Cases **not** included because they are not verified in play: the King/Ace dealer exceptions, 2v2 derba escalation, contest outcomes between announcements, forced capture, same-rank stacking, tringla and catch scoring (tutorial text only), chain over multiple equal-rank table cards. `TC-ENDDECK-01/02` are verified for the numbers shown on screen; their input card counts are partly INFERRED as noted.
