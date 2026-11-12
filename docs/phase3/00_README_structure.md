# Phase 3 — Original Game Design: structure and conventions

**Status:** PROPOSED — nothing here is final until the owner approves the ★ decisions in `01_decision_register.md`. · **Date:** 2026-11-12
**Scope:** design and technical specification for our own original Moroccan Ronda game in Unity 6. **No implementation, no Unity scripts, no engine code.** Pseudocode and tables only.
**Inputs:** Phase 2 docs `docs/02`–`docs/06` and `docs/02a_phase2_consolidated_assessment.md` (accepted with documented uncertainties).

---

## 1. Principles (restated from the brief)

1. **Original game.** Not a clone. The reference is research about observable gameplay, UX patterns and technical considerations; nothing is copied (code, art, sound, text, branding, layout composition).
2. **Never invent reference-game rules.** Where Phase 2 left a mechanic UNKNOWN, *we* choose a rule as an **our-design** decision, say so, and keep it switchable (ruleset flags).
3. **Server-authoritative**, with a rules engine that is engine-independent and unit-testable (standing project rule).
4. **No unapproved installs or system changes.** Any package, SDK, Unity module or cloud account named in these documents is a *candidate* that needs the owner's approval before it is installed or created.
5. **Approval before finalising.** Important decisions are listed as ★ in the register and stay PROPOSED until approved.

## 2. Classification used in every Phase 3 document

| Tag | Meaning | How it is written |
|---|---|---|
| **VRO** | **Verified reference observation** from Phase 2, with its evidence (e.g. `R22`, `A021`, `017_rules_19`). Strength qualifiers from Phase 2 stay: *visual*, *log-only*, *tutorial*. | Always cited; never upgraded. |
| **DD** | **Our design decision.** We decide; reference parity is not claimed. | Normative text ("the engine MUST …"). |
| **PD** | **Provisional decision.** A default is chosen so design can proceed; alternatives and consequences are documented; a decision is needed before a named milestone. | Marked with the register ID and a ruleset flag where applicable. |
| **OQ** | **Open question.** Not decided; options and a recommended next step are given. | Listed in the register. |

*Evidence labels from Phase 2 (VERIFIED / INFERRED / UNKNOWN) are not used as design classes. An INFERRED or UNKNOWN reference behaviour can only appear here as a DD, PD or OQ.*

**Approval gates.** **★** = the owner's approval is required before the decision is treated as final. **○** = proceed on the default unless the owner objects (listed for visibility). **Status:** PROPOSED (all, at this point), APPROVED, REVISED, OPEN.

## 3. Document set (docs/phase3/)

| # | File | Deliverable | Content |
|---|---|---|---|
| 00 | `00_README_structure.md` | — | this file |
| 01 | `01_decision_register.md` | — | **all decisions**, options, recommendations, approval checklist |
| 02 | `02_game_design_document.md` | **A** GDD | vision, pillars, audience, modes, core loop, progression, social, safety, content, KPIs, risks |
| 03 | `03_rules_and_scoring_spec.md` | **B** rules | normative ruleset v0.1 with rule IDs `RL-xx`, worked examples, flag table |
| 04 | `04_state_machines.md` | **C** state machines | match / deck / deal / turn / announcement window / presence / room / matchmaking / client app FSMs |
| 05 | `05_domain_model.md` | **D** domain model | entities, value objects, invariants, commands, events, views, scoring ledger |
| 06 | `06_unity_architecture.md` | **E** Unity 6 architecture | repo layout, assemblies, layers, presentation pipeline, offline mode, performance budgets |
| 07 | `07_multiplayer_backend_architecture.md` | **F** multiplayer/backend | options comparison, recommendation, protocol, matchmaking, reconnect, bots, profiles, security |
| 08 | `08_ui_ux_design_spec.md` | **G** UI/UX | IA, screens, interaction, motion, accessibility, RTL, differentiation checklist |
| 09 | `09_asset_animation_inventory.md` | **H** assets | art, animation, audio, fonts, localization, provenance, budgets |
| 10 | `10_testing_strategy.md` | **I** testing | layers, golden vectors, simulation, acceptance criteria, performance gates, traceability |
| 11 | `11_implementation_roadmap.md` | **J** roadmap | milestones M0–M7, spikes, exit criteria, risks, decision deadlines |

## 4. Reading order

New reader: `02` → `03` → `01` (the ★ items) → `07`. Engineers: `03` → `05` → `04` → `06` → `07` → `10`. Designers/artists: `02` → `08` → `09`.

## 5. Conventions

- **Rule IDs** `RL-nn` (doc 03), **decision IDs** `D-X-nn` (doc 01; X = P product, G gameplay, S server, C client, U UI/UX, A assets), **test IDs** `T-…` (doc 10), **milestones** `M0`–`M7` (doc 11), **spikes** `SP-n` (doc 11).
- **Seats** are indexed 0–3 counter-clockwise on screen starting at the local player (0 = bottom, 1 = right, 2 = top, 3 = left). **Sides**: 1v1 = {0}, {1}; 2v2 = {0, 2}, {1, 3}.
- **Ranks** are written as numbers 1–7, 10, 11, 12; **rank index** 0–9 follows the order `1 2 3 4 5 6 7 10 11 12`. **Suits**: coins, cups, swords, clubs (the traditional Spanish-suited set; the *names* are generic, our *artwork* is original).
- **Document naming:** in Phase 3 files, "doc NN" means `docs/phase3/NN_*`; Phase 1–2 documents are always written as `docs/NN_*`, "Phase 2 doc NN" or `02a`.
- Reference-game captures stay private in `docs/reference_captures/` and are never imported into Unity.
- Dates are absolute; calendar plans are not given because team size is unknown (OQ D-P-07).

## 6. Input needed from the owner

1. Approve, revise or reject the **★ decisions** in `01_decision_register.md` §3 (22 items; recommended defaults given).
2. Answer **D-P-07** (team size, backend skills, budget) — it changes the backend recommendation's risk, not its direction.
3. Confirm that the Unity Android module / Unity Hub installs and any package or cloud-account creation will be approved case by case (standing rule).
