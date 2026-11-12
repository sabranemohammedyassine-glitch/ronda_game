# Sample Review 01: Visual identity and first asset set

Reviewer: Agent 6 (self-review), followed by a lead review (direction approved).
Method: every PNG was inspected visually at full size, on a navy preview sheet, in the 1920 × 1080 mock-up, and on the 140/120 px contact sheet. Contrast was computed with the WCAG formula.

## What works

* **Coherence:** one palette, one motif (the 8-point star), and the same line weights and shadow model run through the background, cards, UI kit and illustrations. The mock-up reads as a single product.
* **Card legibility at 140 px:** rank numerals (about 18 px cap height at that size) and suit silhouettes stay readable. Suits are separable in greyscale: round coin, chalice, vertical sword, and diagonal club. Index contrast on ivory is at least 6:1.
* **Cards on the table:** ivory against navy is about 11:1, and the soft contact shadows lift the cards off the felt.
* **The full deck is data-driven:** all 40 faces come from one command, with no manual edits (`deck_preview_all40.png`).
* **No text in assets:** call-outs, labels and scores are TMP only. The only exception is the card numerals.
* **Clean-room:** everything is procedural and original. No reference captures were opened, and no scans or existing artwork were used.

## Known weaknesses (honest list)

1. **Court cards are emblematic, not illustrated.** The 10/11/12 cards in one suit differ only by the top ornament (star, horseshoe, crown) and the 1–3 diamonds. That is readable once learned, but players used to Spanish decks expect figures. At 120 px the horseshoe (11) is the weakest ornament; it is small and silver on ivory.
2. **The coin pip is visually heavier than the other suits.** It fills its bounding box, so it was scaled to 88% in pip grids. 7 of Oros is still the densest card.
3. **The Bastos symbol is the least "traditional".** A diagonal club was chosen for silhouette distinctness. Some players may at first read it as a bottle or gourd. Its wood-grain detail is minimal.
4. **Card numerals use Cambria Bold (a Microsoft font) rasterised into the PNGs.** This is fine for an academic MVP, but switching to an OFL serif (Lora Bold) with `--font` is recommended before any public release. The step is a one-line change.
5. **Avatars are simple flat vector portraits.** They are charming at 104 px, but the faces are basic: there are no expressions or states (thinking, winning), and only two characters exist.
6. **Victory/defeat emblems are decent but generic.** The laurel is built from geometric lozenges and looks sparse around the medallion. The defeat emblem relies on desaturation; it does not use a strong shape change.
7. **The background frame band is busy at the screen edge.** Its 40 px star chain is fine at 1080p but may look noisy on small phones. A calmer variant (larger pitch or lower contrast) could be added. Faint stretching of the vignette is visible on ultra-wide aspect ratios. The background is 16:9 only, so use Envelope Parent or extend it with a solid `navy_deep` colour.
8. **There are no pressed/disabled sprite variants.** Buttons rely on Unity colour tinting. That is acceptable, but a pressed sprite with no lip would feel more tactile.
9. **Minification without mipmaps.** Cards are shown at about 0.4× their source size. They look clean in static renders, but a fast scaling animation could shimmer (see the inventory notes).
10. **The table-card row holds about 6 cards.** Ronda can have more cards on the table, so Unity layout code must wrap to a second row or tighten the step. The art does not dictate this.
11. **Missing icons** for later phases: close, back, help/rules, music. They are not yet generated; the same 128 px white-glyph recipe applies.

## Fixes made during iteration

* The Bastos symbol was redrawn twice (from a cactus-like club, to a tapered bottle shape, to a heavy diagonal club).
* The 7-pip layout changed to 2-3-2 to avoid overlaps.
* The numeral font changed from Georgia (old-style figures sat on different baselines) to Cambria Bold (lining figures). Two-digit indices were enlarged.
* A radial-gradient seam in the background (from a PIL built-in gradient ending early) was replaced with an exact computed gradient.
* Mock-up collisions (score capsule over the role label, overlapping hand cards, dealer chip behind its panel) were fixed.
* The victory rays were clipped at the top edge; the radius was reduced. The laurel was rearranged tangentially, and the opponent avatar's expression was softened.
