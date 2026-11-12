# 04 — Gameplay State Machines and Transitions

**Deliverable C.** · **Status:** PROPOSED · **Date:** 2026-11-12
**Scope.** Authoritative (server) machines for match, deck round, deal/turn, announcement window and seat presence; lobby and matchmaking machines; client application and presentation machines; two sequence diagrams (play a card, reconnect). Rules referenced as `RL-nn` (doc 03), commands/events as in doc 05 §6–§7. **All timings are ruleset flags**; defaults from doc 03 §13.
**Conventions.** `[guard]` · `/ action` · events in `CamelCase` · times use the server clock only.

---

## 1. Layering

```mermaid
flowchart TB
  subgraph Server["Authoritative (server or offline host)"]
    M[Match FSM] --> D[Deck-round FSM] --> L[Deal & Turn FSM]
    L --- W[Announcement-window FSM]
    S[Seat presence FSM x seats]
  end
  subgraph Client
    A[App FSM] --> V[Match-view FSM] --> C[Choreographer queue]
  end
  Server -- events --> Client
  Client -- commands --> Server
```

The **engine is a pure function**: `Apply(state, command, now) → (state', events[])`. The machines below describe the states encoded in `MatchState` and the transitions the engine performs; timers are *inputs* (`TimeoutTurn`, `CloseWindow` commands issued by the host's scheduler), never hidden side effects.

---

## 2. Match FSM (server)

```mermaid
stateDiagram-v2
  [*] --> Created
  Created --> Seating: SeatsReserved
  Seating --> InProgress: AllSeatsFilled - MatchStarted, ruleset pinned, dealer drawn
  Seating --> Cancelled: SeatingTimeout
  InProgress --> Finished: MatchEnded by target or forfeit
  InProgress --> Aborted: UnrecoverableServerError
  Finished --> [*]
  Cancelled --> [*]
  Aborted --> [*]
```

| From | Event | Guard | Action | To |
|---|---|---|---|---|
| Created | SeatsReserved | matchmaking/lobby produced a roster | create `MatchState`, ruleset pinned | Seating |
| Seating | AllSeatsFilled | every seat has a human or a labelled bot | draw first dealer; emit `MatchStarted` | InProgress |
| Seating | SeatingTimeout | no quorum within the limit | release seats; notify | Cancelled |
| InProgress | MatchEnded | RL-23 or RL-25 | write result, ratings job, close room | Finished |
| InProgress | UnrecoverableServerError | state cannot be restored | mark void, no rating change, notify | Aborted |

*Match is Finished only through the engine's `MatchEnded` event (RL-23); the host never edits scores.*

---

## 3. Deck-round FSM (inside InProgress)

```mermaid
stateDiagram-v2
  [*] --> Shuffling
  Shuffling --> Dealing: DeckShuffled - DeckStarted
  Dealing --> DealPlay: DealCompleted - open announcement window
  DealPlay --> Dealing: DealEnded with cards left in the deck
  DealPlay --> Tally: DealEnded with the deck empty
  Tally --> Evaluate: TallyApplied
  Evaluate --> Shuffling: NoWinner or TieAtTarget - rotate dealer
  Evaluate --> [*]: WinnerDecided (MatchEnded)
```

| From | Event | Guard | Action | To |
|---|---|---|---|---|
| Shuffling | DeckShuffled | seed drawn server-side (RL-06a) | clear table, piles, per-deck counters; rotate dealer (except first deck) | Dealing |
| Dealing | DealCompleted | all hands filled per RL-05 | emit `DealStarted` (hands redacted per seat), set first player (RL-06b), open window (RL-15) | DealPlay |
| DealPlay | DealEnded | all hands empty | emit `DealEnded` | Dealing / Tally |
| Tally | TallyApplied | RL-19 → RL-20 → RL-21 | emit `DeckTallied` with itemised ledger entries | Evaluate |
| Evaluate | WinnerDecided | RL-23 | emit `MatchEnded` | — |
| Evaluate | NoWinner | no side at target, or exact tie at target (`bothReachTarget`) | — | Shuffling |

Mid-play, `MatchEnded` can be emitted from DealPlay at any `ScoreAwarded` that reaches the target (RL-23 (a)); the machine then leaves directly to `[*]`.

---

## 4. Deal and turn FSM

```mermaid
stateDiagram-v2
  [*] --> AwaitingMove
  AwaitingMove --> Resolving: PlayCard (legal)
  AwaitingMove --> Resolving: TimeoutTurn - autopilot picks a legal card, seat becomes Away
  AwaitingMove --> AwaitingMove: InvalidCommand - emit CommandRejected
  Resolving --> Scoring: CaptureOrDrop resolved (RL-08..10)
  Scoring --> MatchOver: score reaches target
  Scoring --> TurnEnded: otherwise
  TurnEnded --> AwaitingMove: next seat has cards
  TurnEnded --> [*]: all hands empty
```

| State | Entry actions | Exit conditions |
|---|---|---|
| **AwaitingMove** | set `turnSeat`, `deadline = now + turnTimerMs`; emit `TurnStarted(seat, deadline)`; if the seat is **Away**, schedule an immediate humanised autopilot move (0.8–1.5 s) | `PlayCard` valid · `TimeoutTurn` |
| **Resolving** | remove the card from hand; RL-08/09 capture or RL-10 drop; emit `CardPlayed`, `CardsCaptured` or `CardDropped`; update `lastDrop` (cleared by any capture, set by any drop) | always → Scoring |
| **Scoring** | evaluate Missa (RL-12), Derba (RL-13); each award → `ScoreAwarded(reason)`; check RL-23 (a) after each | MatchOver or TurnEnded |
| **TurnEnded** | emit `TurnEnded`; advance `turnSeat` to the next seat with cards (counter-clockwise) | next turn / deal end |

Commands that are **not** turn-bound (can arrive in any state of DealPlay): `Announce` (window open), `Catch`, `ReturnFromAway`, `Leave`, sticker sends (cosmetic, ignored by the engine).

**Idempotency.** Each command carries `(matchId, seat, commandSeq)`; a repeated `commandSeq` returns the original outcome without effect (needed for reconnect).

---

## 5. Announcement-window FSM (parallel with turns)

```mermaid
stateDiagram-v2
  [*] --> NoWindow
  NoWindow --> Open: DealCompleted - deadline now plus announceWindowMs, WindowOpened
  Open --> Open: Announce when eligible - stored sealed, ack to the seat only
  Open --> Open: PlayCard of an announceable rank by a seat that has not announced - rank forfeited
  Open --> Resolving: CloseWindow by timer
  Resolving --> Revealed: reveal all and apply RL-16
  Revealed --> NoWindow: DealEnded
```

- **Sealed**: `AnnouncementAccepted` goes only to the announcing seat (ack); other seats learn nothing until `AnnouncementsRevealed` (RL-15).
- **Catch** becomes available in **Revealed** (RL-18), until `DealEnded`; a successful or failed catch is recorded per `(target, rank, deal)`.
- A window exists for **every deal** (VRO R15: visual in A029, A037, A041, A022).
- If the match ends while Open/Resolving, pending announcements are discarded.

---

## 6. Seat presence — two orthogonal flags

**Control** ∈ {Human, Autopilot} and **Connection** ∈ {Connected, Disconnected}; plus a terminal result {None, Forfeited, Left, Replaced}.

```mermaid
stateDiagram-v2
  state Control {
    [*] --> Human
    Human --> Autopilot: TurnTimeout or GraceExpired
    Autopilot --> Human: ReturnFromAway when Connected
  }
  state Connection {
    [*] --> Connected
    Connected --> Disconnected: SocketLost - start grace timer
    Disconnected --> Connected: Reconnected - resync
  }
```

| Event | Effect | Notes |
|---|---|---|
| `TurnTimeout` | Control: Human → Autopilot; autoplay counter = 1; emit `SeatAway(seat)` | VRO R30: bots play, "I'm back" (visual) |
| Autopilot turn | autoplay counter += 1 | ranked: counter ≥ 8 → `Forfeited` (RL-25) |
| `ReturnFromAway` | Autopilot → Human at the seat's **next** turn; counter reset | requires Connected |
| `SocketLost` | Connection: Disconnected; grace timer `reconnectGraceMs` (default 60 000); emit `SeatDisconnected` | the seat's turns continue normally until its timer expires → autopilot |
| `GraceExpired` | Control → Autopilot | the seat is still reconnectable until `Forfeited` or match end |
| `Reconnected` | Connection: Connected; the client receives a **snapshot** (§10) | Control stays Autopilot until the human returns |
| `Leave` | ranked 1v1 → `Forfeited`; casual/private/2v2 → `Replaced` by a labelled bot | RL-25 |

---

## 7. Lobby and matchmaking machines

### 7.1 Private table (room)

```mermaid
stateDiagram-v2
  [*] --> Open
  Open --> Filling: PlayerJoined
  Filling --> Filling: PlayerJoined or PlayerLeft or BotAdded
  Filling --> Ready: AllSeatsFilled
  Ready --> Filling: PlayerLeft
  Ready --> Started: HostStart - create Match
  Open --> Expired: TTL
  Filling --> Expired: TTL
  Started --> [*]
  Expired --> [*]
```

Guards: only the host may `Start`; 1v1 needs 2 filled seats, 2v2 needs 4; a code is valid only while Open/Filling/Ready; spectators (post-beta) attach to Started rooms read-only.

### 7.2 Matchmaking ticket

```mermaid
stateDiagram-v2
  [*] --> Searching
  Searching --> Matched: CompatibleTicketsFound
  Searching --> BotFillOffered: WaitedTooLong in casual only
  BotFillOffered --> Matched: Accept or auto after 5 s
  Searching --> Cancelled: PlayerCancel
  Searching --> Expired: MaxWait
  Matched --> [*]
  Cancelled --> [*]
  Expired --> [*]
```

Parameters (doc 07 §6): rating window starts ±100 and widens by ±50 every 5 s; casual bot fill after 12 s; ranked never bot-filled (D-S-08).

---

## 8. Client application FSM

```mermaid
stateDiagram-v2
  [*] --> Boot
  Boot --> Auth: ServicesReady
  Auth --> Home: Authenticated or GuestCreated
  Auth --> Offline: NoNetwork
  Home --> Queue: PlayOnline
  Home --> PracticeSetup: PlayPractice
  Home --> TableLobby: CreateOrJoinTable
  Home --> Tutorial: StartTutorial
  Queue --> Match: MatchFound
  Queue --> Home: Cancel
  TableLobby --> Match: HostStarted
  PracticeSetup --> Match: Start (local host)
  Match --> Result: MatchEnded
  Match --> Reconnecting: ConnectionLost
  Reconnecting --> Match: Resynced
  Reconnecting --> Home: GraceOver or MatchGone
  Result --> Queue: Rematch
  Result --> Home: Continue
  Offline --> Home: NetworkBack
  Tutorial --> Home: Done
```

`Match` has a nested **match-view FSM**: `Connecting → Syncing → Live ⇄ Animating → Live`, plus `Away` overlay (AFK dialog) and `Paused` (app backgrounded). In offline practice the same `Match` state is driven by a local host (D-C-10).

### 8.1 Match-view FSM and input gating

| State | Meaning | Input |
|---|---|---|
| Syncing | applying a snapshot or replaying events without animation | disabled |
| Live-Idle | event queue empty, state consistent | enabled if it is my turn |
| Animating | the choreographer is playing queued events | disabled for card play; announce/catch buttons stay available when legal |
| AwaitingConfirm | (two-step play) a card is lifted with a capture preview | tap again to play, tap elsewhere to cancel |
| Away | autopilot active, AFK dialog shown | only "I'm back" |
| Reconnecting | socket down | disabled; banner |

The client never changes game state; it applies **events** from the host in order. Optimistic UI is limited to lifting a card and showing the preview; a play is only *shown* after `CardPlayed` arrives.

---

## 9. Sequence — play a card

```mermaid
sequenceDiagram
  participant C as Client (seat 0)
  participant S as Server (match room)
  participant O as Other clients
  C->>S: PlayCard(cardId, commandSeq)
  S->>S: validate turn, legality, idempotency
  S->>S: Apply(state, cmd) -> events
  S-->>C: CardPlayed, CardsCaptured or CardDropped, ScoreAwarded x n, TurnEnded, TurnStarted(next)
  S-->>O: same events, hands redacted
  Note over S,O: events carry a monotonically increasing sequence number
```

If validation fails the server answers `CommandRejected(reason)` and state is unchanged.

## 10. Sequence — reconnect

```mermaid
sequenceDiagram
  participant C as Client
  participant G as Gateway
  participant R as Match room
  C->>G: Connect(token) ... socket lost
  Note over R: SocketLost -> grace timer; turns continue
  C->>G: Resume(sessionId, lastEventSeq)
  G->>R: attach
  alt gap is small, at most N events and retained
    R-->>C: events (lastEventSeq+1 ..)
  else
    R-->>C: Snapshot(PlayerView at seq K) + events (K+1 ..)
  end
  R-->>C: SeatState (Autopilot? deadline?)
  C->>C: Syncing -> Live; if Away: show "I'm back"
```

Snapshot = the seat's redacted `PlayerView` (doc 05 §8): hand, table, piles counts, scores, turn, deadlines, announcement state (own sealed announcements only), lockout flags.

## 11. Invariants checked by every machine (full list: doc 05 §5)

Card conservation (40 cards always partitioned); at most one card per rank on the table; the current `turnSeat` holds ≥ 1 card; exactly one announcement window per deal; the ledger sum equals the displayed score; `MatchEnded` is emitted exactly once.
