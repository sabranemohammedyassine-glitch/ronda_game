# 10 — Testing Strategy and Acceptance Criteria

**Deliverable I.** · **Status:** PROPOSED · **Date:** 2026-11-12 · Test IDs: `T-GV` golden vectors, `T-PR` property tests, `T-SIM` simulations, `T-SRV` server, `T-UNI` Unity, `T-PERF`, `T-SEC`, `T-UX`; acceptance criteria `AC-nn`.
Tools named are *candidates* and need the owner's approval and a licence check before use (D-P-06).

---

## 1. Principles

1. **The rules engine is proven outside Unity first.** Every `RL-nn` has at least one executable test before any Unity client code depends on it.
2. **Independent oracles.** Golden vectors are checked against a second, independently written implementation (e.g. a small script) so a bug cannot confirm itself.
3. **Determinism is a feature:** same seed + same commands ⇒ same events, on the server, in Unity Editor and on an Android device.
4. **Hidden information is tested adversarially** (redaction tests on every message path).
5. **Risk-based:** the uncertain mechanics (PD rules) are covered by flag-parameterised tests so changing a decision changes data, not tests.
6. **Performance and memory are acceptance criteria**, including the post-match memory gate motivated by Phase 2 T04.

## 2. Test layers

| Layer | Scope | Tooling (candidates) | Runs | Milestone |
|---|---|---|---|---|
| **L1 Unit (Core)** | rules, scoring, tally, legal moves, presence, timers as pure functions | NUnit + `dotnet test` | every commit | M1 |
| **L2 Property-based** | invariants I1–I14 under random legal play and random flag sets | FsCheck or CsCheck (licence to verify) | every commit (fast) + nightly (deep) | M1 |
| **L3 Simulation** | millions of bot-vs-bot games, balance and termination statistics, fuzzed illegal commands | `Ronda.Simulation` CLI | nightly + before each milestone | M1 |
| **L4 Determinism / replay** | seeded shuffle vectors; event-log replay hash; Linux .NET vs Unity Editor vs Android device | same NUnit tests run in Unity EditMode and on device | nightly + milestone | M1–M2 |
| **L5 Protocol / contract** | codec round-trips, version negotiation, backward-compat fixtures, decoder fuzzing | NUnit | every commit | M3 |
| **L6 Server integration** | headless bot clients over real WebSockets: full matches, reconnect, timeouts, forfeits, matchmaking, private tables, redaction | NUnit + in-process test host | every PR | M3 |
| **L7 Load / soak** | 5,000 concurrent matches, 4-hour soak, latency and memory | custom bot-load driver or k6-style tool | before M4, M6 | M4 |
| **L8 Chaos / network** | latency, jitter, loss, disconnect, node kill, DB failover | fault-injection proxy | before M3 exit, M6 | M3 |
| **L9 Unity client** | EditMode (reducers, mappings, l10n completeness), PlayMode (choreographer order, fast-forward, input gating), UI screenshot comparison, offline practice and tutorial end-to-end | Unity Test Framework | every PR / nightly | M2 |
| **L10 Localisation / RTL** | pseudo-locale, bidi, shaping, overflow, numerals | automated + native-speaker review | nightly + milestone | M2 |
| **L11 Accessibility** | contrast, text scale, reduced motion, TalkBack labels | tooling + manual | milestone | M5 |
| **L12 Performance** | budgets of doc 06 §9 on the target phone class; frame-time capture; memory before/after a match | on-device scripted replay | milestone | M2 |
| **L13 Security** | redaction, forged commands, replay, rate limits, dependency/secret scans, external pen test before beta | tests + audit | M3, M6 | M3 |
| **L14 Usability / playtests** | rules comprehension, tally comprehension, RTL, timers | moderated sessions | M2, M4, M5, M6 | M2 |
| **L15 Cultural / legal** | native-speaker cultural review, privacy review, store policy | manual | M5–M6 | M5 |

**Coverage targets:** Core line ≥ 95 %, branch ≥ 90 %; mutation score reported after M1 (Stryker.NET-style tool, candidate); server ≥ 80 % lines; every `RL-nn` and every error code referenced by at least one test (traceability §6).

## 3. Golden vectors (T-GV) — data-driven fixtures

Format: JSON/YAML `given` (ruleset flags, table, hands, history) → `action` → `expect` (state, events, ledger). The same files run in `dotnet test` and Unity. All worked examples of doc 03 §10 are fixtures:

| ID | Source | Covers |
|---|---|---|
| T-GV-01…05 | E1–E5 | capture, chain (incl. 7 → 10), Missa, drop |
| T-GV-06…08 | E6–E8 | derba, no derba, derba + Missa |
| T-GV-09…15 | E9–E15 | contest, tie, aggregation (1v1, 2v2) |
| T-GV-16, 17 | E16, E17 | ronda cap, tringla can finish |
| T-GV-18…20 | E18–E20 | catch valid / invalid / not allowed |
| T-GV-21…24 | E21–E24 | tally with dealer penalty; dealer last; both ≥ 41; tie |
| T-GV-25…28 | deal pattern | 1v1 deck counters 32/24/16/8/0 and 2v2 24/12/0 (visual evidence A027/A029/A033/A023; tutorial) |
| T-GV-29 | A021 | tally numbers +5 and 32 → +12 |
| T-GV-30 | A039 → A041 | 26 cards → +6 (decomposition INFERRED: marked "evidence-derived") |
| T-GV-31…40 | rank order | `next()` table for all ranks; no wrap at 12 |
| T-GV-41…46 | shuffle | seed → permutation vectors, **generated once and cross-checked by an independent implementation** |
| T-GV-47…52 | flags | the same scenarios with `mustCapture`, `chainWrap`, `catchEvidence=none`, `dealerRuleLevel=player`, `announceTie`, `missaOnFinalPlay=false` |

## 4. Property tests (T-PR) and simulations (T-SIM)

| ID | Property / campaign | Oracle |
|---|---|---|
| T-PR-01…14 | invariants I1–I14 hold after every `Apply` under random legal play, random seeds, random flag sets, 1v1 and 2v2 | invariant checker |
| T-PR-15 | illegal commands never change state and always yield a rejection | state hash |
| T-PR-16 | replaying events from the seed reproduces the final state hash | replay |
| T-PR-17 | no `PlayerView` for seat *s* contains a card of another hand or the undealt deck (any event prefix) | redaction checker |
| T-PR-18 | autopilot and bots always return a legal command | legal-move generator |
| T-SIM-01 | 1M random-vs-random games terminate; length distribution recorded | termination |
| T-SIM-02 | 1M Normal-vs-Normal games per flag set: tally component frequencies, margin distribution, first-player advantage | statistics vs thresholds |
| T-SIM-03 | A/B for D-G-07: `mustCapture` true vs false (game length, swing, derba frequency) — **input to the owner's decision** | report |
| T-SIM-04 | A/B for D-G-13 evidence rule and D-G-11 aggregation variants | report |
| T-SIM-05 | fuzzed command streams (illegal order, duplicates, late announcements) never crash the engine | crash-free |

## 5. Acceptance criteria (Given / When / Then)

### 5.1 Rules (verified by L1–L4; unchanged by UI)
| ID | Criterion | Rules |
|---|---|---|
| AC-01 | **Given** table {3,4,5,10} **when** a seat plays a 3 **then** it captures 3, 4, 5 (4 cards), 10 stays, and the events are `CardPlayed → CardsCaptured` | RL-08, 09 |
| AC-02 | **Given** table {7,10,11} **when** a 7 is played **then** 7, 10, 11 are captured (jump 7 → 10) | RL-09 |
| AC-03 | **Given** the previous play was a drop of a 10 **when** the next seat captures with a 10 **then** `ScoreAwarded(DERBA, +1)` and, if the table becomes empty, also `MISSA` | RL-12, 13 |
| AC-04 | **Given** a pair in hand **when** the window is open and the seat announces **then** it is accepted sealed; **when** it closes **then** all announcements are revealed and scored once | RL-14–16 |
| AC-05 | **Given** two equal rondas on different sides **when** the window closes **then** nobody scores | RL-16 |
| AC-06 | **Given** a side at 38 **when** it wins a ronda contest worth 3 **then** it ends on 40 and the match continues; **given** a tringla worth 5 **then** the match ends | RL-17, 23 |
| AC-07 | **Given** the target has played a rank this deal and held a pair unannounced **when** an opponent catches **then** +1; a wrong catch locks the accuser until the next deal | RL-18 |
| AC-08 | **Given** the last card is played and the dealer's side did not make the last capture **then** the other side gets +5, then +1 per card over 20 (e.g. 32 → +12), leftover cards go to the last capturer | RL-19–22 |
| AC-09 | **Given** a turn deadline passes **then** autopilot plays a legal card and the seat is Away; **when** the human sends `ReturnFromAway` **then** control returns at their next turn | RL-24 |
| AC-10 | **Given** ranked and 8 consecutive autoplays **then** the seat forfeits | RL-25 |

### 5.2 Online and server
| ID | Criterion |
|---|---|
| AC-11 | Two real devices on different networks complete a full 1v1 match; every client's state hash equals the server's after each event |
| AC-12 | **Reconnect:** a client that loses its socket mid-turn resumes within the grace period and sees the live match; no command is applied twice |
| AC-13 | No message sent to seat *s* contains another seat's hand or the undealt deck (adversarial test over all message types) |
| AC-14 | Ranked queues never contain bots; casual fill after 12 s is labelled; rating changes only for ranked matches |
| AC-15 | Killing a server node mid-match: the room resurrects on another node and clients reconnect (or the match is `Aborted` with no rating change) |
| AC-16 | 5,000 concurrent matches for 4 h: command→event p95 ≤ 150 ms, no memory growth trend |

### 5.3 Client, UX and quality
| ID | Criterion |
|---|---|
| AC-17 | Offline practice 1v1 vs each bot tier completes on the target device; the tutorial (both paths) completes without network |
| AC-18 | Every `ScoreAwarded` shows a localised explainer bubble and a log line with the same reason code |
| AC-19 | EN/FR/AR UI shows no clipped or mis-ordered text at 100 % and 150 % text size; AR digits/punctuation follow the numerals setting (SP-1 pass criteria) |
| AC-20 | Performance on the target phone class: match median ≥ 50 fps, min ≥ 30 fps, PSS ≤ 450 MB, graphics ≤ 180 MB, no GC pause > 3 ms |
| AC-21 | **Post-match memory gate:** 30 s after returning Home, PSS is within +40 MB of the pre-match baseline |
| AC-22 | Accessibility: all states have non-colour cues; reduced-motion setting removes all motion > 120 ms; TalkBack reads cards and actions |
| AC-23 | A first-time player completes "Learn the rules" and then explains the end-of-deck tally correctly in ≥ 70 % of playtest sessions |
| AC-24 | No interstitial or external app opens inside the match or result flow |

## 6. Traceability (rule → tests)

| Rule | Tests |
|---|---|
| RL-01, 05, 06 | T-GV-25…28, T-PR-01/03/04, T-SIM-01 |
| RL-08…11 | T-GV-01…05, 31…40, 47, T-PR-02/15, AC-01/02, T-SIM-03 |
| RL-12, 13 | T-GV-04, 06…08, AC-03 |
| RL-14…17 | T-GV-09…17, 48, AC-04…06, T-SIM-04 |
| RL-18 | T-GV-18…20, 49, AC-07 |
| RL-19…23 | T-GV-21…24, 29, 30, AC-08 |
| RL-24, 25 | T-SRV-timers, AC-09, 10, AC-12 |
| RL-26 | T-PR-05 (ledger), AC-18 |
| RL-27 | T-GV-47…52 (flags), replay tests |

## 7. Server and Unity test inventory (selected)

| ID | Test |
|---|---|
| T-SRV-01 | full 1v1 and 2v2 matches with 2/4 bot clients |
| T-SRV-02 | reconnect mid-turn; after grace; duplicate session; stale `lastEventSeq` → snapshot |
| T-SRV-03 | idempotent commands (resend `commandSeq`) |
| T-SRV-04 | announcement window and catch timing under latency |
| T-SRV-05 | matchmaking windows widen as designed; party handling; cancel races |
| T-SRV-06 | private table lifecycle and code expiry |
| T-SRV-07 | resurrection after node kill; timers re-armed without extension |
| T-SRV-08 | rate limits and malformed frames |
| T-UNI-01 | view-model reducer equals Core `PlayerView` for recorded event logs |
| T-UNI-02 | choreographer ordering and fast-forward produce the same final state |
| T-UNI-03 | input gating and two-step play preview equals `CaptureResolver` result |
| T-UNI-04 | rank-socket mapping and RTL/LTR layout snapshots |
| T-UNI-05 | localisation tables complete for all locales; pseudo-locale layout |
| T-PERF-01 | scripted match replay on device: frame-time histogram, memory curve, thermals (30 min) |
| T-PERF-02 | pre/post-match memory comparison (AC-21) |

## 8. Performance, device matrix and load

- **Device matrix:** the Phase 2 test-phone class (low-end, 20:9, Android 14) as the **minimum**, one mid-range, one high-end, one tablet; arm64 (armv7 only if SP-3 requires). Final minSdk after SP-3.
- **Budgets:** doc 06 §9. A regression of > 10 % in any budget fails the milestone gate.
- **Load:** 5,000 concurrent matches (4 h) before M4; 20,000 target modelled before launch; chaos runs per §2 L8.

## 9. Usability and playtests (L14)

| Round | When | Participants | Focus | Success |
|---|---|---|---|---|
| 1 | M2 | 5 internal/friendly | core loop, two-step play, timers | task success ≥ 80 % |
| 2 | M4 | 8–12 friends/family incl. non-gamers | queue, private tables, rating | SUS-style score and observed errors |
| 3 | M5 | 12+ with Arabic/French/Darija speakers, mixed ages | tutorial, RTL, tally comprehension | AC-23 |
| 4 | M6 | closed beta (50–200) | stability, balance, telemetry | KPIs doc 02 §13 |

## 10. Optional reference-parity tests (only if OQ-R1 = yes)

Phase 2's recommended clean session (`02a` §13) is a *research* activity, not a product test: run items 1–6 hands-off with the touch watcher at 0, then compare outcomes to our flags (capture obligation, ronda credit timing, turn timer, 2v2 specifics). Any discovered behaviour becomes a **flag value**, never silent code.

## 11. Environments, gates and definition of done

- **CI:** `dotnet test` (L1–L6) on every commit; Unity EditMode on every PR; nightly PlayMode, L3/L4, device smoke; milestone runs of L7, L8, L12.
- **Definition of done (feature):** rule/UX spec updated · tests written and green · traceability row · localisation keys · accessibility check · performance check if presentation-heavy · telemetry events reviewed.
- **Severity:** S1 data/ranking corruption, hidden-info leak, crash loop; S2 match cannot complete, wrong score; S3 UX defect with workaround; S4 cosmetic. **Release gate:** no open S1/S2; S3 reviewed.
