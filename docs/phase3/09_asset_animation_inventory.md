# 09 — Asset and Animation Inventory

**Deliverable H.** · **Status:** PROPOSED · **Date:** 2026-11-12 · Decisions: D-A-01…D-A-04, D-U-01…D-U-03, D-P-06.
**Scope.** Everything the game needs to look, move and sound right, with counts, specifications, priorities and provenance rules. **Nothing is created, downloaded or commissioned until the owner approves the art direction and sourcing (D-U-01, D-A-01).** Counts assume Direction A; B/C change surface art, not the list.
**Priority tags:** **M2** placeholder/vertical slice · **M5** content-complete for beta · **M6** release quality · **Later** post-launch.

---

## 1. Principles and sourcing (D-A-01)

1. **Original or licensed only.** Every shipped asset has a provenance row (§8): author, tool, licence, date, source files.
2. **No reference assets** (art, audio, fonts, text, icons) are copied, traced, sampled, composited or used as AI-generation input. Reference captures stay private and are never imported into Unity.
3. **AI-generated imagery** is allowed for mood boards only, unless counsel clears rights and similarity risk for specific shipped assets (recommendation of D-A-01).
4. **Placeholders first (M2):** programmer art (flat shapes, system-neutral OFL fonts, generated card faces) so gameplay and performance can be validated before art spend.
5. **Layered, themeable cards:** faces are composed from frame + pips + court art + corner indices so themes, colour-blind shapes and localisation of numerals are data, not repainting.

## 2. Art assets

### 2.1 Cards

| Asset | Count | Spec | Pri. |
|---|--:|---|---|
| Card frame (front) | 1 (+ theme variants later) | vector source; exported 9-sliced where possible | M2 |
| Suit pip sprites (coins, cups, swords, clubs) | 4 × 2 (large/small) | consistent weight, readable at 88 px height; **distinct silhouettes** for colour-blind safety | M5 |
| Court illustrations (sota 10, caballo 11, rey 12) | 12 (3 × 4 suits) | original characters in our style — **not** copies of traditional deck scans or the reference | M5 |
| Pip layouts 1–7 | data (no art) | positions per rank, composed at build/run time | M2 |
| Ace/“1” special art | 4 | optional decorative aces | M6 |
| Corner indices | font glyphs | rank numerals in the active numeral set | M2 |
| Card back | 1 default + 2 cosmetic | original motif (zellige-derived pattern), distinct from the reference | M5 |
| Card shadows / highlights | 3 | soft contact shadow, gold-ring selection, capture glow | M5 |

*Composed vs fully painted faces:* composed saves ≈ 90 % of illustration (12 court illustrations + 8 pips versus 40 paintings) and memory; fully painted faces are a **Later** cosmetic.

### 2.2 Environment and table

| Asset | Count | Spec | Pri. |
|---|--:|---|---|
| Home scene (rooftop) | 1 layered scene (4–6 layers) | 2400 × 1080 reference, parallax | M5 |
| Match table scene | 1 default | cloth + zellige ribbon band, vignette, lantern lights (URP 2D lights) | M5 |
| Rank ribbon sockets | 10 + inactive/active states | curved band with 10 socket frames; 7 → 10 gap ornament | M2 (flat) / M5 |
| Cosmetic table themes | 2 | Later | Later |
| Result backdrop, loading screen, queue backdrop | 3 | reuse scene layers where possible | M5 |

### 2.3 UI kit

| Asset | Count | Notes | Pri. |
|---|--:|---|---|
| Buttons (primary/secondary/destructive/icon) | 4 styles × 3 states | 9-slice | M2 |
| Panels, modals, bottom sheet, tab, toggle, slider, close, chips | ≈ 12 | 9-slice, USS variables | M2 |
| Speech bubbles | 1 + tails (4 directions) | 9-slice | M2 |
| Score capsule, timer ring, turn ring, deck counter, dealer token | 5 | vector/SDF where possible | M2 |
| Tier badges | 6 tiers (+ division pips) | original craft-material set | M5 |
| Avatar frames | 4 | | M5 |
| Avatars (portraits) | ≥ 16 | original illustrated portraits, no likenesses of real people | M5 |
| Icons | ≈ 40 | 56 px grid, 4 px stroke; list in §2.4 | M2 / M5 |
| Stickers | 24 (+ localised text variants where needed) | 2 sizes | M5 |
| Mascot (tea-pot guide) | 8 poses (idle, wave, point, think, cheer, warn, sad, sleep) | layered for the 2D Animation package | M5 |
| App icon, adaptive icon layers, feature graphic, 8 store screenshots × 5 locales | 1 + 1 + 1 + 40 | | M6 |
| Splash / boot logo | 1 | | M2 |

### 2.4 Icon list (≈ 40)
play, practice, private table, join, ladder/trophy, learn/book, settings/gear, profile, cosmetics, back, close, check, lock/unlock, plus, copy, share, eye (spectators), timer, bell, haptics, sound on/off, music, language, high-contrast, motion, text size, ronda, tringla, catch/target, sticker, flag/report, mute, kick, rematch, replay, rating up/down, XP, tier, warning, info, wifi lost.

### 2.5 VFX sprites
Sparkles, star glow, confetti (win), dust puff, ribbon sweep, ring flash, lantern flicker, coin-less “point” pop (our own motif), tier-up shine: ≈ 10 sprites + 2 particle prefabs; budget in §7.

## 3. Animation inventory

| # | Animation | Trigger (event) | Duration | Technique | Reduced motion | Pri. |
|--:|---|---|---|---|---|---|
| 1 | Shuffle | `DeckStarted` | 900 ms | sprite flicker + tween | skip | M5 |
| 2 | Deal | `DealStarted` | 150 ms / card, staggered | tween flight, flip for own cards | instant | M2 |
| 3 | Card lift & preview | tap | 120 ms | tween + socket glow | instant | M2 |
| 4 | Card flight | `CardPlayed` | 280 ms ease-out | tween along a short arc | 120 ms | M2 |
| 5 | Drop settle | `CardDropped` | 120 ms | scale-in + shadow | instant | M2 |
| 6 | Capture sweep | `CardsCaptured` | 350 ms (+ 90 ms/card) | tween to pile with trail | simultaneous | M2 |
| 7 | Chain highlight | chain length > 1 | 90 ms / socket | glow sequence | single glow | M5 |
| 8 | Missa burst | `ScoreAwarded(MISSA)` | 700 ms | particles + bubble | bubble only | M5 |
| 9 | Derba flash | `ScoreAwarded(DERBA)` | 400 ms | ring + bubble | bubble only | M5 |
| 10 | Score tick | any `ScoreAwarded` | 300 ms | number roll | instant | M2 |
| 11 | Bubble | call-outs | 160 + 1600 + 200 ms | scale/fade | fade | M2 |
| 12 | Announce button pulse | window open and eligible | loop | scale 1.00–1.06, 1 Hz | static | M5 |
| 13 | Announcement reveal | `AnnouncementsRevealed` | 250 ms / card | flip | swap | M5 |
| 14 | Catch resolve | `CatchResolved` | 500 ms | crosshair + bubble | bubble | M5 |
| 15 | Turn glow / ring | `TurnStarted` | continuous | ring drain; last-5 s pulse | no pulse | M2 |
| 16 | Away overlay | `SeatAway` | 220 ms | fade/scale | fade | M2 |
| 17 | Tally rows | `DeckTallied` | 600 ms / row | slide + count | 200 ms | M5 |
| 18 | Result count-up and bars | `MatchEnded` | 1200 ms | number roll + bar fill | instant | M5 |
| 19 | Tier-up / level-up | rating/XP thresholds | 1500 ms | shine + badge swap | static | M6 |
| 20 | Mascot idle/talk/cheer | tutorial | loops 2–4 s | 2D Animation (bone) | static pose | M5 |
| 21 | Screen transitions | navigation | 220 ms | fade/slide | fade 120 ms | M2 |
| 22 | Queue "searching" | queue state | loop 2 s | subtle orbit | static | M2 |
| 23 | Reconnect spinner | `Reconnecting` | loop | rotation | static | M2 |
| 24 | Sticker pop | sticker | 600 ms | scale + float | fade | M5 |

All timings live in the motion-token table (doc 08 §3, §8) and are configurable per theme.

## 4. Audio inventory

Format: Ogg Vorbis (or AAC where better for Android), 44.1 kHz; SFX mono, music stereo; loudness target −16 LUFS integrated for music, −12 LUFS peak-normalised SFX (tuning at M5). Sources: **original or licensed** (D-A-02); register in §8.

| Category | Items | Count | Pri. |
|---|---|--:|---|
| Card SFX | deal, flip, lift, place/drop, capture sweep, chain tick (3 pitches), shuffle | 10 | M2 (placeholder) / M5 |
| Scoring SFX | Missa, Derba, Ronda, Tringla, Catch success, Catch fail, tally tick, score up | 8 | M5 |
| UI SFX | button, back, toggle, error, notification, sticker, copy | 7 | M2 / M5 |
| Timer SFX | soft tick (last 5 s), time-up | 2 | M5 |
| Match SFX | turn start, your-turn nudge, rival joined, reconnect, away | 5 | M5 |
| Result SFX | win sting, lose sting, tier/level up | 4 | M5 |
| Music | home loop, match ambience (2 low-intensity loops), result motifs (2 stings), tutorial loop | 6 | M5 |
| Ambience | rooftop night bed (loop), soft street ambience | 2 | M6 |
| Voice | none in v1 (text only); optional localised mascot lines | 0 | Later |

Rules: SFX never exceed music by more than 6 dB; ducking on bubbles; independent music/SFX sliders; all cues have a visual equivalent; haptics map to scoring events.

## 5. Localisation assets

| Item | Estimate |
|---|---|
| UI + game strings | ≈ 600 (MVP) × 5 languages |
| Tutorial scripts | ≈ 40 steps × 5 languages |
| Store listings | 5 locales (title, short, long, 8 screenshots) |
| Legal | privacy policy, terms (reviewed by counsel) × 5 |
| Fonts | Latin + Arabic families (dynamic font assets) |
| RTL QA | pseudo-locale + native review per language |

## 6. Fonts (D-A-03)

Selection criteria: **licence permits embedding/redistribution in games (OFL or equivalent, verified at selection)**; Arabic and Latin harmony; legible numerals; weights 400/700 (+ display); good Unity ATG/dynamic font support. **Candidates to evaluate (licence to be verified, none selected):** rounded Latin display families and Arabic families from open font libraries (e.g. Cairo, Tajawal, Almarai, Baloo-family, Fredoka-family). We do **not** try to identify or reuse the reference's fonts.

## 7. Technical specifications and budgets (feeds doc 06 §9)

| Item | Spec |
|---|---|
| Source resolution | UI at reference 1080 px height; cards authored as vectors or ≥ 512 px tall; scenes 2400 × 1080 @1x with 2x optional |
| Texture formats | ASTC 6×6 (UI/cards), ASTC 8×8 (backgrounds); RGBA32 only for tiny gradients |
| Atlases | `cards` 2048², `ui` 2048², `icons` 1024², per-theme `table` 2048²; one atlas page per draw batch |
| Texture memory | ≤ 80 MB in a match (all atlases + scene) |
| Sprite count in match | ≤ 120 visible sprites; ≤ 80 draw calls |
| Audio memory | SFX decompress-on-load ≤ 12 MB; music streamed |
| Particles | ≤ 2 active systems, ≤ 150 particles each |
| Download | base ≤ 100 MB; cosmetics via Addressables on demand |
| Naming | `ui_btn_primary_pressed`, `card_front_frame`, `sfx_card_place_01` — lowercase, underscores, no spaces |
| Folders | `Art/{Cards,Table,UI,Icons,Avatars,Mascot,VFX,Audio}`, `Themes/<theme>/…` |
| Import | atlas-packed sprites, mipmaps off for UI, crunch off for ASTC, audio quality 70 % |

## 8. Provenance register and review process

**Register columns:** `assetId · path · type · author/vendor · tool · licence & terms · source files path · date · reviewer · reference-similarity check (pass/fix) · notes`. No asset enters `main` without a row.
**Review:** (1) author declares sources; (2) a second person compares side-by-side with the reference captures (kept outside the Unity project) using the doc 08 §11 checklist; (3) third-party assets: licence text archived; attribution list generated for credits; (4) AI-assisted work: tool, prompt, outputs and rights analysis recorded; shipped only if cleared (D-A-01).

## 9. Production plan by milestone

| Milestone | Deliverable |
|---|---|
| M2 | placeholder UI kit (flat), generated card faces, placeholder SFX, motion tokens implemented |
| M5 | final cards, table, UI kit, avatars, stickers, mascot, SFX/music, localisation tables |
| M6 | store assets, ambience, tier/level-up polish, performance pass on art (atlases, overdraw) |
| Later | cosmetic themes, card backs, painted faces, voice lines |

## 10. Counts summary (Direction A, MVP + release)

Cards 12 court illustrations + 8 pip sprites + 3 backs · environment ≈ 8 layered images · UI kit ≈ 60 sprites · icons ≈ 40 · avatars ≥ 16 · stickers 24 · mascot 8 poses · VFX ≈ 10 sprites · animations 24 entries · SFX ≈ 36 · music 6 · ambience 2 · store graphics ≈ 42.
