# 05 — Card and Match Domain Model

**Deliverable D.** · **Status:** PROPOSED · **Date:** 2026-11-12
**Purpose.** A language-neutral specification of the engine-independent domain (`Ronda.Core`, doc 06 §3): types, invariants, commands, events, views and domain services. It is the contract between rules (doc 03), state machines (doc 04), server (doc 07), client (doc 06) and tests (doc 10). **No code is given; implementation starts only after approval.**

---

## 1. Design constraints on the model

| # | Constraint | Why |
|---|---|---|
| C1 | **Pure and deterministic**: no engine types, no I/O, no wall-clock, no floats, no static mutable state, no iteration-order dependence | same engine on server, client (offline/preview) and in tests; replay equals live |
| C2 | **Immutable state, event-producing**: `Apply(state, command, now) → (state', events[])` | testability, event log, replays, reconnect |
| C3 | **Ruleset as data** (flags, doc 03 §13) | alternatives without code changes |
| C4 | **Visibility is part of the model**: every event/view says who may see it | hidden information, spectators, anti-cheat |
| C5 | **Small, closed vocabulary** (40 cards, ≤ 4 seats, ≤ 10 table ranks) | exhaustive and property testing is cheap |
| C6 | **Time is an input** (`now` in ms, monotonic server clock) | timers are testable |

## 2. Value objects

| Type | Definition |
|---|---|
| `Suit` | `Coins=0, Cups=1, Swords=2, Clubs=3` |
| `RankIndex` | 0–9 ↔ rank values `[1,2,3,4,5,6,7,10,11,12]`; `Next(i) = i+1` (none after 9) |
| `CardId` | 0–39, `id = suit·10 + rankIndex`; stable across platforms and the wire |
| `SeatId` | 0–3 (counter-clockwise from the local player); `SideOf(seat)` = `seat mod 2` in 2v2, `seat` in 1v1 |
| `SideId` | 0 or 1 |
| `Points` | signed 32-bit integer |
| `Rank kind` | `Ronda`(2 cards) · `Tringla`(3–4 cards) |
| `Millis` | 64-bit integer (server monotonic clock) |
| `Seed` | 32 bytes; per-match seed and per-deck seed |
| `RulesetRef` | `(rulesetId, version, flagsHash)` |

## 3. Entities and aggregates

The **aggregate root** is `MatchState`. All children are owned by it and mutated only through `Apply`.

| Entity / value | Fields (all immutable snapshots) | Notes |
|---|---|---|
| **`MatchState`** | `matchId`, `ruleset`, `mode(1v1\|2v2)`, `seats[]`, `status`, `deckIndex`, `dealIndex`, `dealer`, `turnSeat`, `turnDeadline`, `hands[seat]`, `undealt` (ordered), `table[rank→CardId?]`, `piles[side]`, `lastDrop?`, `lastCapturerSide?`, `scores[side]`, `window`, `announcements[]`, `catchLog[]`, `lockout[seat]`, `ledger[]`, `presence[seat]`, `eventSeq`, `commandSeq[seat]`, `result?` | `undealt` is server-only |
| **`SeatState`** | `seat`, `side`, `controller` (`Human`\|`Bot(botId, tier)`), `playerId?` | bots are labelled in views |
| **`Presence`** | `control (Human\|Autopilot)`, `connection (Connected\|Disconnected)`, `autoplayCount`, `terminal (None\|Forfeited\|Left\|Replaced)` | doc 04 §6 |
| **`TableState`** | map `RankIndex → CardId?` (≤ 1 card per rank) | the rank-addressed table is the model, not a UI choice (RL-08a) |
| **`Pile`** | list of `CardId` per side for the current deck round | count is public; contents follow from public events |
| **`LastDrop`** | `(seat, rankIndex)` — the previous play if it was a drop | cleared by any capture and at deck start (RL-13) |
| **`Window`** | `state (None\|Open\|Revealed)`, `opensAt`, `closesAt`, `dealIndex` | RL-15 |
| **`Announcement`** | `(seat, rankIndex, kind, count, dealIndex, accepted)`; stored sealed | RL-14 |
| **`CatchEntry`** | `(accuser, target, rankIndex, dealIndex, outcome)` | RL-18 |
| **`LedgerEntry`** | `(seq, side, requested, granted, reason, cause, deckIndex, dealIndex)` | RL-26; `cause` = card/seat/announcement refs |
| **`MatchResult`** | `winnerSide`, `finalScores`, `reason (Target\|Forfeit\|Abort)`, `deckCount`, `rulesetRef`, `seedReveal?` | persisted |

### 3.1 Identifier stability
`matchId` (128-bit, server generated), `eventSeq` (starts at 1, +1 per event, no gaps), `commandSeq` (client-generated per seat, strictly increasing for idempotency).

## 4. Domain services (stateless, pure)

| Service | Responsibility | Rules |
|---|---|---|
| `Shuffler` | deterministic permutation of the 40 `CardId`s from a `Seed` | RL-06a; §9 |
| `Dealer` | distribute cards per RL-05, set `turnSeat` per RL-06b | RL-05, 06b |
| `CaptureResolver` | given table, played card → `Capture(cards[])` or `Drop`; chain per RL-09 | RL-08–10 |
| `LegalMoveGenerator` | legal `PlayCard` set for a seat (applies `mustCapture`), announceable ranks, catchable `(target, rank)` pairs | RL-11, 14, 18 |
| `ScoreCalculator` | Missa, Derba and ledger entries with the cap (RL-17) | RL-12, 13, 17, 26 |
| `AnnouncementResolver` | strength ordering, contest, aggregation, tie | RL-14–16 |
| `CatchResolver` | evidence, validity, lockout | RL-18 |
| `TallyCalculator` | leftover, dealer rule, over-20, match evaluation | RL-19–23 |
| `TurnScheduler` | next seat with cards, deadlines (inputs only) | RL-06b, 24 |
| `AutopilotPolicy` | picks a legal card when a seat times out (basic, deterministic given a seed) | RL-24 |
| `RulesetProvider` | loads a versioned ruleset object | RL-27 |
| `ViewProjector` | `MatchState` → `PlayerView` / `SpectatorView` / `ReplayView` with redaction | C4 |

## 5. Invariants (must hold after every `Apply`)

| # | Invariant | Checked by |
|---|---|---|
| I1 | The 40 cards are partitioned exactly once among: `undealt`, hands, table, piles | property test |
| I2 | `table` has ≤ 1 card per rank | property test |
| I3 | A hand never contains a card also in table/piles/undealt | property test |
| I4 | Hand sizes follow RL-05 at the start of each deal | unit |
| I5 | `turnSeat` has ≥ 1 card while the deal is in progress | property test |
| I6 | `scores[side] = Σ ledger[side].granted` | property test |
| I7 | Scores never decrease; no ledger entry is negative | property test |
| I8 | Ronda-class awards never lift a side above `target − 1` (RL-17) | unit/property |
| I9 | At most one window per deal; announcements only while it is Open; one per `(seat, rank, deal)` | unit |
| I10 | `MatchEnded` is emitted exactly once, after which every command returns `MATCH_OVER` | unit |
| I11 | `eventSeq` increases by exactly 1 per event | property test |
| I12 | Replaying the event log from the seed reproduces the final state hash | replay test |
| I13 | `Σ pile sizes + leftover = 40` at deck end | unit |
| I14 | No `PlayerView` contains a card of another seat's hand or an `undealt` card | redaction test |

## 6. Commands

All commands carry `(matchId, seat, commandSeq)`. A repeated `commandSeq` returns the original result without effect.

| Command | Fields | Validation → error code |
|---|---|---|
| `PlayCard` | `cardId` | match in progress `MATCH_OVER` · `seat == turnSeat` `NOT_YOUR_TURN` · card in hand `CARD_NOT_IN_HAND` · `mustCapture` check `MUST_CAPTURE` |
| `Announce` | `rankIndex` | window Open `WINDOW_CLOSED` · seat holds ≥ 2 of the rank in this deal's hand `NOT_ELIGIBLE` · none played `NOT_ELIGIBLE` · not yet announced `ALREADY_ANNOUNCED` |
| `Catch` | `targetSeat`, `rankIndex` | window Revealed `WINDOW_NOT_REVEALED` · target is an opponent `BAD_TARGET` · not locked out `LOCKED_OUT` · evidence per `catchEvidence` `NO_EVIDENCE` · not already caught `ALREADY_CAUGHT` |
| `ReturnFromAway` | — | control is Autopilot and connected `NOT_AWAY` |
| `Leave` | — | any time; RL-25 |
| *host/system commands (not client-issued)* | `StartMatch(roster, rulesetRef, seed, optionalDeckOrder)` (the explicit deck order is for tutorials and tests only), `TimeoutTurn(seat)`, `CloseWindow`, `SocketLost(seat)`, `Reconnected(seat)`, `GraceExpired(seat)`, `AutopilotMove(seat)` | issued by the scheduler/gateway; same idempotent semantics |

## 7. Events

`seq` is the global event sequence. **Visibility:** **P** public (all seats and spectators) · **O** owner seat only · **S** server only (audit log). Events are the only things the UI animates.

| Event | Key payload | Vis. | UI use |
|---|---|:--:|---|
| `MatchStarted` | roster (humans, labelled bots), ruleset ref, mode, seatMap, dealer, seedCommitment? | P | intro |
| `DeckStarted` | `deckIndex`, dealer | P | shuffle animation |
| `DealStarted` | `dealIndex`, hand sizes; **own cards in the `O` variant** | P / O | deal animation |
| `WindowOpened` | `closesAt` | P | countdown banner |
| `TurnStarted` | `seat`, `deadline` | P | turn glow + timer ring |
| `CardPlayed` | `seat`, `cardId` | P | card flight |
| `CardsCaptured` | `seat`, `captured[]` (played + table cards, in chain order), `chainLength` | P | capture sweep, chain highlight |
| `CardDropped` | `seat`, `cardId`, `rankIndex` | P | settle into the rank socket |
| `ScoreAwarded` | `side`, `requested`, `granted`, `reason`, `cause`, `newScore` | P | bubble, scoreboard tick, log line |
| `AnnouncementAccepted` | `rankIndex`, `kind` | O | button feedback |
| `AnnouncementsRevealed` | all announcements, contest result, tie? | P | reveal bubbles |
| `CatchResolved` | `accuser`, `target`, `rankIndex`, `outcome`, `lockedUntil?` | P | "Caught!" / lockout |
| `TurnEnded` | `seat` | P | advance the turn glow |
| `DealEnded` | `dealIndex` | P | — |
| `DeckTallied` | itemised steps: leftover assigned, dealer penalty, over-20, per-side pile counts | P | step-by-step tally screen |
| `SeatAway` / `SeatBack` / `SeatDisconnected` / `SeatReconnected` / `SeatReplaced` / `SeatForfeited` | `seat` | P | presence badges, AFK overlay for the owner |
| `CommandRejected` | `commandSeq`, `error` | O | toast |
| `MatchEnded` | `result` incl. final scores, XP/rating deltas attached by the server | P | result flow |
| `SeedRevealed` | `matchSeed` | P (after end) | provably-fair audit (D-S-10) |

**Ordering guarantees.** Events from one `Apply` are emitted atomically in this order: `CardPlayed` → `CardsCaptured | CardDropped` → `ScoreAwarded*` (Missa, Derba) → `TurnEnded` → `TurnStarted` (or `DealEnded`/`DeckTallied`/`MatchEnded`).

## 8. Views and redaction

| View | Audience | Contains | Never contains |
|---|---|---|---|
| `PlayerView(seat)` | one player | own hand, own sealed announcements, other seats' hand **sizes**, table, pile counts, scores, dealer, turn, deadlines, window state, own lockout, presence of all seats, ruleset ref | other hands, `undealt`, other sealed announcements |
| `SpectatorView` | spectators | as above without any hand | all hands |
| `ReplayView` | post-match | complete state per event including hands (after match end only) | — |
| Server snapshot | server | full `MatchState` | — |

A **snapshot message** to a reconnecting client is its `PlayerView` at event `K` plus events `K+1…`.

## 9. Randomness and fairness

- **Deck seed** per deck round: `deckSeed = SHA-256(matchSeed ‖ deckIndex)` (32 bytes). `matchSeed` is 32 bytes from a CSPRNG on the server and is never sent to clients during the match.
- **Shuffle:** Fisher–Yates from the end of the 40-card array; each swap index is drawn from a deterministic generator initialised from `deckSeed` (xoshiro256** state loaded from the 32 bytes; bounded draws by rejection sampling, so no modulo bias). Golden vectors fix exact outputs (doc 10).
- **Commit–reveal (optional D-S-10):** at match start publish `SHA-256(matchSeed)`; at match end publish `matchSeed`; anyone can reproduce all shuffles and verify that dealing was predetermined.
- The same seeds make bots, autopilot and simulations reproducible.

## 10. Bot and autopilot interfaces (contract only)

| Interface | Input | Output | Constraint |
|---|---|---|---|
| `BotPolicy` | `PlayerView`, legal-move set, own seeded RNG | one command (`PlayCard`, optionally `Announce`/`Catch`) | sees only what a human at that seat sees |
| `AutopilotPolicy` | same | one legal `PlayCard` (no `Announce` unless `autopilotAnnounces`) | deterministic given the seed; weaker than `Normal` bots |

Difficulty tiers (D-S-08): **Easy** — random legal; **Normal** — greedy capture value, avoids feeding an obvious derba, announces/catches correctly; **Hard** — adds card counting over public events and one-ply lookahead or sampled rollouts over unseen cards.

## 11. Mapping to persistence (doc 07 §9)

`MatchRecord(matchId, rulesetRef, mode, seats, seedCommitment, startedAt, endedAt, result)` · `MatchEvent(matchId, seq, type, payload, visibility)` (append-only; snapshots every N events) · `RatingChange(matchId, playerId, mode, before, after)` · `StatsRollup(playerId, mode, …)`. The event log plus the `matchSeed` is sufficient to rebuild any state.

## 12. Error codes (client-visible)

`MATCH_OVER`, `NOT_YOUR_TURN`, `CARD_NOT_IN_HAND`, `MUST_CAPTURE`, `WINDOW_CLOSED`, `WINDOW_NOT_REVEALED`, `NOT_ELIGIBLE`, `ALREADY_ANNOUNCED`, `BAD_TARGET`, `LOCKED_OUT`, `NO_EVIDENCE`, `ALREADY_CAUGHT`, `NOT_AWAY`, `DUPLICATE_COMMAND`, `RATE_LIMITED`, `UNKNOWN_MATCH`, `UNAUTHORIZED`. Localised messages live in the client (doc 08 §9).

## 13. What the domain deliberately does not know

Networking, serialization format, UI, animation timing, persistence, ratings (computed by the server after `MatchEnded`), matchmaking, chat/stickers (cosmetic events relayed by the room, not by the engine).
