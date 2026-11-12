# 05 — Visual Design Specification (requirements for ORIGINAL art)

**Phase:** 2 · **Status:** FINAL for Phase 2 review (measurements and new components added in §10; evidence limits in `02_reference_game_analysis.md` §11) · **Date:** 2026-11-12
**Purpose:** record the visual language of the reference game as *measurable requirements and moods*, so that we can commission or create **original** graphics and UI in Unity 6. Nothing here authorises reuse of the reference's artwork.
**Labels:** **VERIFIED** measured/observed in a screenshot (*visual*) or only in a log (*log-only*) · **INFERRED** interpretation, not conclusive · **UNKNOWN** insufficient evidence. (Earlier drafts: `[V]`, `[H]`, `[U]`.) References are `Axxx`/`Bxxx` files in `reference_captures/` and `palette.json` / `perf.csv` of the Observer; usernames and avatars visible in the captures are not reproduced. Colour values are **sampled from our own private screenshots for analysis only** (Agent B palette extraction + the Scribe's spot samples) and are ±2–3 per channel because of video/screenshot compression.

> **Clean-room rule.** The "Observed" column below is a *measurement of the reference*. The "Proposed original" column is what we will actually build with. Do not trace, re-draw closely, or sample pixels from the reference into production assets.

---

## 1. Overall visual language **VERIFIED/INFERRED**

| Aspect | Observation | Requirement for our design |
|---|---|---|
| Theme | Moroccan café courtyard: zellige tile walls, horseshoe arches, lanterns, orange banquettes, mosaic floor; at the table: dark teal felt, dark wood octagonal frame with gold geometric inlay, corner lanterns glowing | Evoke a Moroccan setting with our own illustrated scenes and motifs; avoid direct copies of the reference scenes |
| Mood | Warm, social, evening; saturated accents on a calm dark playfield | Warm-dark table for focus, warm-light menus for invitation |
| Style | Illustrated 2D, soft painterly backgrounds, flat-colour UI with a small dark outline/lip, rounded shapes | 2D illustrated backgrounds + flat/semi-flat UI kit |
| Density | Few big targets on the menu, compact HUD on the table; large hand cards | Same priorities: big targets, minimal HUD |
| Cultural markers | Arabic/Darija, French, Spanish, English UI; "Salam!" greeting; Spanish-pattern 40-card deck | Original deck design in the same *card system*; original mascot; localised from day one |

## 2. Colour system

### 2.1 Observed palette (analysis only)

| Role | Hex | Where |
|---|---|---|
| Mode tile — red | `#C13417` | 2 Vs 2 tile |
| Mode tile — blue | `#4CA6C2` | 1 Vs 1 tile, XP bar fill, level badge |
| Mode tile — yellow | `#EFB93E` | Create Table tile, XP/rank bar (yellow `#F2BD47`) |
| Mode tile — cream | `#F3C585` | Join Table tile; XP-bar track |
| Tile inner icon panel | `#DC8D7D` (red), `#9BCDDD` (blue) | Lighter inset behind icons |
| Panel cream | `#FBF0D3` | Modals (Settings, Create Table) |
| Profile card / light surface | `#EEE5CD` | Profile card |
| Title tab / header orange | `#F0AE2F` | Modal title tabs, leaderboard header |
| Close button red | `#D44A33` (white X) | Round close buttons |
| Destructive red | `#E3322B` | "Log out" |
| Toggle on | `#6AC780` | Sound ON |
| Teal UI | `#174B59` / `#164B58` | Name pills, scoreboard header & row 2, tutorial buttons |
| Cream UI | `#FCE4C3` (scoreboard row 1), `#F7DFBF` (tutorial bubble), `#F9E1C0` (pill highlight) | |
| Table felt | centre ≈ `#203E47`, edges `#0D222D … #0C1D29` (radial vignette) | Table surface |
| Table ghost slots | `#1E3A43` / `#1B333D` | Faint 5 × 2 grid |
| Wood frame | `#3A1512`, `#532E28`, gold trim `#B28C5D` | Octagon frame |
| Background vignette | `#191623`, `#231D2B`, `#332631` | Outside the table |
| Card face | `#FFFFFF` with suit/court colours (yellow `#FEDD00`, red, blue) | Cards |
| Card back | navy `#010D35`–`#1B2C4E` with pale `#9FADBF`/`#E3E8EE` pattern | Card back |
| Results panel | `#322210` brown; title gold `#FFD158` | Victory panel |
| Leaderboard rows | gold `#F5CA35`, silver `#B4BAC4`, bronze `#E1B29B`, plain `#F3E4C2` | Top-3 medals + rest |
| Menu courtyard | wall `#C58264` / `#CB8565`, floor `#746652` | Background |

### 2.2 Proposed original palette tokens (to be refined by the art lead)

Same *roles*, different identity. Use semantic tokens in Unity (`Theme` ScriptableObject) so values can change without touching prefabs.

| Token | Proposed | Note |
|---|---|---|
| `brand.saffron` | `#E8A317` | Primary accent / titles |
| `brand.terracotta` | `#B8432F` | Primary action / "2v2" |
| `brand.lagoon` | `#2F8FB0` | Secondary action / "1v1" |
| `brand.indigo` | `#14304F` | Table UI surfaces (pills, scoreboard) |
| `surface.ivory` | `#F8EDD6` | Modal panels |
| `surface.sand` | `#E9D6AE` | Cards of the UI, tracks |
| `felt.night` | `#16363F` → `#0A1A24` radial | Table |
| `wood.walnut` | `#40201A` + `gold.trim #C5A059` | Frame |
| `state.go` | `#58B368` | Turn glow, toggle on |
| `state.danger` | `#D8392B` | Destructive, close |
| `rank.bronze/silver/gold/diamond` | to be defined | Tier badges |

Contrast requirement: text on `surface.*` ≥ 4.5:1; text on `felt.*` ≥ 7:1; never rely on colour alone for turn state (add glow + icon).

## 3. Typography **VERIFIED/INFERRED**

| Aspect | Observation | Requirement |
|---|---|---|
| Display / buttons | Heavy, rounded, slightly playful sans with chunky numerals | One rounded display face for titles and numerals |
| Body | Same family at regular/medium weight, cream-on-teal or brown-on-cream | One text face with a matching **Arabic** counterpart |
| Scale (1600×720 reference) | Tile labels ≈ 28–30 px; modal titles ≈ 28 px; body/bubbles ≈ 18–22 px; scoreboard numerals ≈ 22 px; deck counter numerals ≈ 24 px | Define a type ramp in reference pixels; scale with the canvas |
| Languages | Latin + Arabic (RTL) must render with the same visual weight | Choose open-licensed families (verify OFL/Apache licences before use), e.g. a rounded Latin face plus an Arabic face with a similar rhythm |

Do **not** try to identify or reuse the reference's font files.

## 4. Components

### 4.1 Buttons

| Kind | Observed style | Requirement |
|---|---|---|
| Mode tile | ≈ 228×247, corner radius ≈ 12, flat colour, darker bottom lip ≈ 6 px, lighter inset icon panel, two-line label | Large coloured tiles with a pressed state that drops the lip |
| Pill | Teal filled with cream text, or cream with teal outline; ≈ 150 × 48 | Primary = filled, secondary = outline, destructive = red |
| Title tab | Orange rounded tab overlapping the top edge of the modal | A consistent modal header component |
| Close | Red circle ≈ 56 px with a white X, top-right, overlapping the panel corner | Same affordance, ≥ 48 px touch target |
| Toggle | Green pill with white knob and "ON" text | Labelled toggle |
| Icon + label | White glyph + small caption at the screen corners | Consistent 56 px icon grid |

### 4.2 Panels and modals
Cream panel, radius ≈ 14–20 px, no heavy shadow, dimmed backdrop; tutorial bubble: cream rounded rectangle with a pointer to the mascot, ≈ 480 × 110 px.

### 4.3 Avatars and badges
Circular portrait with a **gold ring** (≈ 80 px at the table, ≈ 80 px on the menu card), league shield badge at its corner, crown for top performer on results, ≥ 12 selectable portraits in a 4-column picker. **Requirement:** an original portrait set with a consistent style; no portraits derived from the reference.

### 4.4 Scoreboard and indicators
Compact 2-row board (≈ 143 × 140): header cell in teal with cream labels, row 1 cream, row 2 teal with cream numerals; ronda banner under it (≈ 230 × 40, draining bar); dealer token ≈ 36 px gold medallion; deck counter as a small card-back with a numeral; turn indicator = green tint/glow on the avatar.

## 5. Cards **VERIFIED**

| Property | Observation (1600×720 capture) | Requirement |
|---|---|---|
| Aspect ratio | Table card ≈ 88 × 140 px (≈ 0.63) | 5:8 portrait; vector or ≥ 512 px high source |
| Corner radius | ≈ 8 px at table size (≈ 9 %) | Rounded corners; same radius on all cards |
| Index | Rank numeral top-left, inverted bottom-right, small, dark | Large readable rank at all sizes; avoid tiny indices on the table copy |
| Suits | Coins, cups, swords, clubs | Four distinct silhouettes readable at 88 px; colour-blind safe (not red/green only) |
| Courts | 10 sota, 11 caballo, 12 rey illustrated | Original illustrations; do **not** copy any traditional deck scans or the reference's faces |
| Back | Navy with a pale ornamental medallion pattern | Original pattern, different motif |
| Size on screen | Hand ≈ 170 px wide (≈ 2× table); fan of up to 4 with ±5° rotation, overlapping, bottom clipped | Same hierarchy: big hand, smaller table cards |
| Table layout | 5 × 2 slot grid, centres ≈ x 590/700/810/920/1030, y 275/445; slot ≈ 88 × 140; placeholders always faintly visible | Keep a stable grid so players learn positions; ghost slots at ≈ 8–10 % opacity. **Observed rule (VERIFIED, visual; A027, A029, A033, A039, A041):** each rank has a fixed slot — top row ranks 1–5, bottom row 6, 7, 10, 11, 12, left to right — so the layout itself teaches the rank order and the 7 → 10 jump. (The earlier draft said "next free slot"; that was wrong.) Our design may keep, change or drop this; it is a presentation choice, not a rule |

## 6. Table and scene geometry **VERIFIED**
Octagonal table ≈ 1210 × 535 px centred in 1600 × 720; seats at the four sides with nameplates ≈ 245 × 70; the own seat bottom-left, the top seat above the grid. Corner lanterns provide a warm light source (bottom-left, top-left, bottom-right); felt is darker toward the edges (radial vignette). Light direction is implied from the lanterns; cards have a very soft contact shadow.

## 7. Lighting and depth **INFERRED**
Painterly background with baked lighting; no real-time 3D. Depth from a darker outer vignette, a lit felt centre, and thin lighter rims on UI. **Requirement:** pre-baked 2D lighting; performance budget favours static textures + a few UI shaders.

## 8. Iconography
Simple white glyphs with rounded ends: two people with "Vs", group, group + "+", group + lock, trophy, gear, book + play, palette, crosshair in a ring, speech bubble (sticker button), medals, coin medallion with a sun motif. **Requirement:** one consistent line weight (≈ 4 px at 56 px), rounded terminals, filled shapes for state (lock, plus).

## 9. Motion principles **VERIFIED**
- **Immediate acknowledgement:** a tapped card lifts and gets a gold outline before it travels.
- **Spatial continuity:** hand → slot → pile, cards never teleport; bonus bubbles appear *at the actor's seat*.
- **Short, readable beats:** call-outs ("Derba! +1", "Missa!") pop, hold ≈ 1–2 s, fade; the scoreboard changes after the move completes.
- **Modals:** fade/scale in over a dimmed background; red X closes.
- **Result:** "Calculating…" beat, then bars fill.
- **Pacing:** across 33 Observer samples the reference alternates between a ≈ 30 fps and a ≈ 60 fps regime on a low-end phone (details and the per-phase table are in doc 06 §9); the 30 fps regime was not confined to menus (it also appears in match samples), so design animations that look right at both. VERIFIED (log-only, `perf.csv`).
- **Accessibility:** provide a reduced-motion mode and don't encode turn state by motion alone.

## 10. Measurements added by the Observer (B)
Sources: the Observer's `palette.json` (already used in §2.1), `perf.csv` (33 samples, doc 06 §9), 26 event contact sheets and 17 event stills (B001–B017), and the Driver's A-series. Animation *durations* were **not** extracted from the contact sheets (UNKNOWN); only coarse bounds exist.

### 10.1 New components seen in the Driver session (original-art requirements; sizes are ≈ ±10 px, derived from half-scale views of 1600 × 720 captures)

| Component | Evidence | Observed structure | Requirement for our original UI |
|---|---|---|---|
| **AFK takeover dialog** | A020, A021, A023, A042 | cream panel ≈ 445 × 260 px centred over the dimmed table, orange title tab "You are AFK", one short sentence, one blue pill button "I'm back" (≈ 195 × 50) | Blocking but unobtrusive modal; one obvious return button; the table stays visible behind it so the player sees bots playing. Colour must not be the only cue (icon + text) |
| **Waiting Players dialog** | A010, A011, A014 | cream panel ≈ 676 × 450 px; four rounded-square seat slots (≈ 95 px) in two halves split by a vertical rule; padlock = closed seat, "+" = open seat, avatar = occupied; Table-Id row with copy button; blue "Start" pill with padlock while locked; eye icon + counter for spectators | Clear seat states (closed / open / occupied), copy affordance, locked-primary-action state |
| **Matchmaking "Searching" modal** | A016 | cream panel with title tab, two avatars around an orange "VS" medallion, one status sentence, a count-up timer, blue "Cancel" pill, red X | Show both sides, elapsed time and an always-available cancel |
| **Profile modal** | A002 | cream panel; avatar with Change button; two tinted stat panels (2 Vs 2 in warm cream, 1 Vs 1 in cool tint); league bar with bronze and silver shields; Id with Copy | Per-mode stats panels, league progress bar, copyable public id |
| **Avatar picker** | A003 | 4-column grid of portrait avatars with rounded corners, scrolls vertically, blue Save pill | ≥ 12 original portraits, selection state, explicit Save |
| **Speech bubbles** | A021, A027, A031, 003 | cream rounded rectangle with a tail towards the nameplate, dark brown text; short ("Missa!", "Derba! +1") or two-line (end-of-deck explanation) | Reusable bubble component, 1–3 lines, anchored to the actor's seat, auto-dismiss |
| **Ronda pill + countdown banner** | A029, A036, A041, A042 | dark-teal pill with cream label at the hand's lower right; cream banner under the scoreboard with a thin draining bar | Eligibility-driven button; countdown text + bar |
| **Result panel** | A044, A045, 005 | dark-brown panel with gold border; gold title; avatars with a crown for the winner; score in a dark inset; XP and rank-delta chips; yellow league bar between bronze/silver shields; orange level bar with the current and next level numbers; "Tap to continue" | Count-up animation for XP/rank values (62 → 100 and 6 → 9 across ≈ 1.2 s by burst labels), bars filling alongside |

### 10.2 Motion and rendering measurements
- Result count-up (A044 → A045): values change between frames labelled `t05` and `t17`; duration of the whole result animation is **INFERRED ≤ 1.7 s**.
- Result panel opens ≈ 0.5 s after the last play (VERIFIED, visual: A044 file time @1794482087.93 vs Driver `E4` @1794482085.39; copy times can lag).
- Frame pacing: 30 fps vs 60 fps regimes, 16.67 ms vsync; see doc 06 §9 for all 33 samples. A ≈ 33 ms median frame interval with a ≈ 50 ms p95 is what "30 fps" looks like here; "60 fps" samples have a 16.6 ms median.
- Contamination note: motion-related observations from the match phase were made while real touches were present in 32 of 61 touch windows; they concern visual behaviour only, so touch contamination matters little here, but timing relative to Driver actions does not.

## 11. Asset-need preview (feeds doc 13)
Backgrounds: menu courtyard, table surround, loading, results. UI kit: tiles, pills, modals, tabs, close, toggles, bubbles, scoreboard, counters. Icons (≈ 20). Cards: 40 faces + back. Avatars: ≥ 12 portraits. Badges: ≥ 5 tiers + medals. Mascot: poses × 6. Stickers: set TBD.

## 12. Clean-room checklist (apply to every asset)

- [ ] Created from scratch or from a licence we hold; the source and licence are recorded in the asset register.
- [ ] Not traced, re-drawn from, colour-picked from, or composited with any reference screenshot.
- [ ] No reference logos, mascot, courtyard art, card faces, card back, avatars, icons or fonts.
- [ ] Our mascot differs in species/character, outfit, silhouette and name from the reference's mascot.
- [ ] Layout is our own composition (do not mirror exact tile positions/proportions of the reference screens).
- [ ] Text strings are our own wording (do not copy the Tutorial text).
- [ ] Reviewed side-by-side with the reference by someone who did not make the asset; any "looks too close" finding is fixed.
- [ ] Screenshots of the reference stay in `docs/reference_captures/` (private) and are never imported into Unity.
