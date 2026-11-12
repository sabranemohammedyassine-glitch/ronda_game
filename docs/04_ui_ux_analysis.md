# 04 — UI / UX Analysis (reference game)

**Phase:** 2 · **Status:** FINAL for Phase 2 review (observed screens added in §13; evidence limits in `02_reference_game_analysis.md` §11) · **Date:** 2026-11-12
**Subject:** "Ronda: Moroccan Card" v6.1.1 on a 20:9 phone in landscape (1600 × 720 px render, left cutout).
**Labels:** **VERIFIED** observed (*visual* = a screenshot was inspected; *log-only* = only a Driver/Observer log line) · **VERIFIED (tutorial)** stated in the in-game Tutorial · **INFERRED** strongly suggested, not conclusive · **UNKNOWN** insufficient evidence. (Earlier drafts: `[V]`, `[V-text]`, `[H]`, `[U]`.) Screenshot references are `Axxx`/`Bxxx` files in `reference_captures/`; epochs are host seconds from the Driver log. Usernames, avatars and table codes visible in the captures are deliberately not reproduced here.
All coordinates are **approximate (±5 px) measurements from captures at 1600 × 720**; they document layout proportions only. Our UI will be designed from scratch (see doc 05 clean-room checklist).

---

## 1. Global UI conventions

| Topic | Observation | Label |
|---|---|---|
| Orientation | Landscape only; the activity forces landscape even when auto-rotate is off | **VERIFIED** |
| Interaction model | Touch only. **A single tap plays a card** (no drag, no confirm); buttons are large tiles or pills | **VERIFIED (tutorial)**, **VERIFIED** |
| Modals | Cream panel with an orange title tab straddling the top edge and a **red circular X** at the top-right; background dimmed | **VERIFIED** (Settings, Leaderboard, Create Table, notice) |
| Android Back key | From *My table* returns to the menu. From the menu/dialogs: **UNKNOWN**. From the external Facebook page: unverified result (see §6.4) | **VERIFIED** / **UNKNOWN** |
| Language | 5 languages switchable in Settings: English, Arabic, Morocco (Darija), French, Spanish | **VERIFIED** |
| Sound | One ON/OFF toggle (music vs SFX split **UNKNOWN**) | **VERIFIED** |
| Account | Signed-in session persists across restarts; Settings → Log out | **VERIFIED** |
| Offline tutorial | Scripted table, no network dependency seen | **INFERRED** |
| Safe area | Profile avatar starts at x ≈ 48 (≈ the 45 px left cutout inset); bottom labels end at y ≈ 690 (inside the ≈ 694 px app bound); backgrounds are full-bleed | **VERIFIED** (measurement) |
| Feedback | Gold outline highlights; floating speech bubbles for game events; progress bars | **VERIFIED** |

---

## 2. Screen inventory

| ID | Screen | Evidence | Section |
|---|---|---|---|
| S01 | Blank/engine init | video | §3 |
| S02 | Credit splash ×2 ("Made with Unity") | video | §3 |
| S03 | Loading screen | video | §3 |
| S04 | Main menu | 006, 018 | §4 |
| S05 | Settings modal | 007 | §5 |
| S06 | Leaderboard modal | 020 | §6.1 |
| S07 | Create Table modal | 013 | §6.2 |
| S08 | Join Table | A006 (visual) | §6.3 |
| S09 | Ronda Club → external app | behaviour only | §6.4 |
| S10 | My table (layout editor) | 007b, 007c | §6.5 |
| S11 | Profile (stats / change picture / avatar picker) | A002, A003, A004, A005 (visual) | §6.6 |
| S12 | Tutorial chooser + Rules path + App path | 016, 017_*, 019_* | §7 |
| S13 | Create-table waiting room, matchmaking queue, AFK dialog | A009–A018, A020 (visual) | §8 |
| S14 | Game table (1v1, 2v2) | 000–012 | §9 |
| S15 | Result panel | 005, video | §10 |
| S16 | Post-match video ad | video | §11 |
| S17 | Multilingual notice modal | video | §3 |

---

## 3. Splash, loading and notice

| State | Layout | Behaviour |
|---|---|---|
| Engine init | Neutral pale background | ≈ 4–5 s on the test phone **VERIFIED** |
| Credit splash 1 | Centred developer credit (name omitted here), "Made with Unity" badge below | Fade in / hold / fade out, ≈ 3 s **VERIFIED** |
| Credit splash 2 | "UI/UX by" designer credit + same badge | Same pattern **VERIFIED** |
| Loading | Blurred café-courtyard background, game logo (three cards fanned behind the word mark) at upper centre, **progress bar** (≈ 300 px wide) below the logo with a teal fill, "Loading…" label, `v6.1.1` bottom-left | ≈ 3 s; progress bar fills left to right **VERIFIED** |
| Menu appears | Direct cut to the menu | **VERIFIED** (1 fps resolution) |
| Notice modal | Large cream panel, red X; four language blocks (Arabic/Darija block right-aligned, English, French, Spanish) | Appeared once over the menu; text: ranking system has issues, will be reset **VERIFIED**; trigger **UNKNOWN** |

Errors (no network, update required, maintenance): **UNKNOWN**.

---

## 4. S04 Main menu **VERIFIED**

```
┌──────────────────────────────────────────────────────────────────────────┐
│ [avatar▣ name        ]                          [🏆 LeaderBoard][⚙ Settings] │
│ [lvl ▓▓▓░ 20/120     ]                                                     │
│        ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐                       │
│        │ 2 Vs 2 │ │ 1 Vs 1 │ │ Create │ │  Join  │                       │
│        │ Match  │ │ Match  │ │ Table  │ │ Table 🔒│                       │
│        └────────┘ └────────┘ └────────┘ └────────┘                       │
│ [Ronda Club]                                      [My table] [Tutorial]   │
└──────────────────────────────────────────────────────────────────────────┘
```

| Component | Position / size (px) | Behaviour |
|---|---|---|
| Profile card | top-left, ≈ 435 × 135, tapered lower-right corner; circular-square avatar with a league-shield badge at its top-right, username, level badge (circle), XP bar `current / threshold` | Tap opens the Profile modal (stats, change picture): **VERIFIED (visual)**, A002/A003, §6.6 |
| Leaderboard | top-right, trophy icon + label | Opens modal §6.1 |
| Settings | top-right, gear + label | Opens modal §5 |
| Tiles ×4 | x ≈ 300–528, 558–785, 815–1042, 1072–1300; y ≈ 250–497; each ≈ 228 × 247, gap ≈ 30; coloured body with a darker bottom lip, a lighter inner icon panel (two people "Vs" / group), two-line label | **2 Vs 2** (red), **1 Vs 1** (blue), **Create Table** (yellow, "+" over the group icon), **Join Table** (cream, padlock over the group icon, visually quieter) |
| Ronda Club | bottom-left, group icon + label | Opens the Facebook app §6.4 |
| My table | bottom-right, palette icon + label | Opens §6.5 |
| Tutorial | bottom-right, book-with-play icon + label | Opens §7 |
| Background | Illustrated Moroccan café courtyard (arches, zellige walls, lanterns, tables and chairs), warm and slightly desaturated behind the tiles | static **VERIFIED** |

Hierarchy: the four tiles dominate; colour (red/blue/yellow/cream) encodes mode and quietness; utility actions are small icon+label pairs on the screen edges.

---

## 5. S05 Settings modal **VERIFIED**

Panel ≈ x 478–1122, y 168–550. Orange title tab "Settings" (≈ 675–925 × 132–187) overlaps the top edge; red circular close at (≈ 1128, 160).

| Row | Control |
|---|---|
| Language | 5 circular flag buttons (≈ 60 px) at y ≈ 290; the active one has an orange ring |
| Sound | Pill toggle (≈ 140 × 50) green with "ON" and a white knob when on |
| Account | Red pill "Log out" (≈ 150 × 48) **(not tapped)** |
| Footer | `Version 6.1.1` (grey-brown, centred) |

Dependent screens (language change effect on layout/RTL): **UNKNOWN**.

---

## 6. Secondary screens

### 6.1 S06 Leaderboard modal **VERIFIED** (020)
Panel ≈ x 265–1335, y 60–660, orange title tab "LeaderBoard", red X at (≈ 1334, 60). Table header: **Rank · Name · Tier · Ronda Points · Win Ratio**. Rows ≈ 68 px high; the **top three rows use gold, silver and bronze fills with medal icons** (rank number replaced by a medal), rows 4+ use cream with the rank number; each row has avatar + name, a tier shield + tier name (Diamond for the top; **Bronze** for the test account), points, and a win ratio with two decimals. **The player's own row is pinned at the bottom** with its absolute rank (≈ 11,000+, so the ladder has at least that many players). Scrolling/paging, friends or region filters: **UNKNOWN**.

### 6.2 S07 Create Table modal **VERIFIED** (013)
Panel ≈ x 516–1084, y 207–513, orange title "Create Table", red X at (≈ 1088, 200). Two large buttons: **1 Vs 1** (blue) and **2 Vs 2** (red). **What happens next UNKNOWN** (waiting room, room code/link generation, invitations). Per the tutorial: *"send the link to your friends. Others can join just to watch."* **VERIFIED (tutorial)** Share actions open the system share sheet **INFERRED**, which we do not explore (privacy).

### 6.3 S08 Join Table **VERIFIED (visual)** (A006, file time @1794481275.03; Driver @1794481270.67)
Tutorial: *"Got a code or a link? Tap here to sit at your friends' table, or to watch their game if it has already started."* **VERIFIED (tutorial)** The dialog is a small cream modal with an orange title tab "Join Table", a red round X, **one text field with the placeholder "Table Id"** and a blue **"Join"** button. Nothing was typed and no code was entered (text input and clipboard are out of scope for privacy). The rule that a Table Id can be shared by link (`playronda` scheme / https filters in doc 01) is **INFERRED**; the result of joining a started table (spectator mode) is **UNKNOWN**. Closed with the X (A007).

### 6.4 S09 Ronda Club **VERIFIED** (behaviour only)
Tapping opens the **Facebook app** (an immersive activity of the Facebook package) on top of Ronda, i.e. an **external community link**. Facebook content was neither captured nor used. After pressing the Android Back key once the window focus was momentarily null; the final return path was not confirmed by this document's sources. **Design implication:** external links should open in the system browser/custom tab, label the destination, and never trap the user.

### 6.5 S10 My table (layout editor) **VERIFIED** (007b, 007c)
Full-screen replica of the game table in an editing mode: top bar **Back** (top-left), **Undo / Reset / Save** (top-right; Save in saffron yellow), hint banner *"Tap anything to move or resize it"* centred, **Style** (yellow pill) bottom-centre. Placeholder seat labels "You", "Left player", "Partner", "Right player"; sample bubbles "Ronda!"; **Ronda** and **Tringla** buttons; card-fan thumbnails near each seat (the "revealed ronda" display). Android Back returns to the menu **VERIFIED**. Persistence, what Style changes (card back, felt, theme?), and resize limits: **UNKNOWN** (not edited to avoid changing the owner's settings).

### 6.6 S11 Profile **VERIFIED (visual)** (A002 @1794481240.33 / Driver @1794481236.12; A003 @1794481255.73; closed A004, A005)
*"This is you: your name, picture and level. Tap it to see your stats or change your picture."* **VERIFIED (tutorial)**. The Profile modal (title tab "Profile", red X) contains: avatar with a **Change** button; **Level** and **name** labels; an **EXP bar** (`0 / 170` at level 3); two stat panels, **2 Vs 2** (rows *Rank* — Bronze —, *Win Ratio*, *G. Played*, *G. Won*) and **1 Vs 1** (rows *Win Ratio*, *G. Played*, *G. Won*; no Rank row is visible in the 1 Vs 1 panel); a **league progress bar** between a bronze shield and a silver shield labelled `1010 / 1040`; and an **Id** string with a **Copy** button (the Id is a personal identifier and is not reproduced). Both modes read 100.00 %, 1 played, 1 won. **Choose a picture (A003):** a modal with a 4-column grid of at least 12 portrait avatars (the fourth row is cut off, so the grid scrolls), a blue **Save** button and a red X; nothing was saved (closed with the X, A004). Avatar unlock rules and whether the 1 Vs 1 panel has a rank row elsewhere: **UNKNOWN**. Note: the profile bar `1010 / 1040` is ≈ 97 % full whereas the league bar in the result panel (A044/A045) is ≈ 50–55 % full; why they differ is **UNKNOWN**.

---

## 7. S12 Tutorial system **VERIFIED**

| Element | Detail |
|---|---|
| Entry | Menu → Tutorial: menu dims; mascot **Sota** (the jack card as a cheerful Moroccan page in a striped robe and red hat) stands at the bottom-left with a speech bubble: *"Salam! I'm Sota. What would you like to learn?"*; buttons **How to use the app** (outline) and **Learn the rules** (filled teal); **Skip** label at the top-centre throughout. |
| Bubble | Cream rounded rectangle (≈ 480 × 110 px) with a pointer to the mascot, teal text/buttons; **Back** (outline) and **Next** (filled) at the bottom; the mascot and bubble **move to a corner away from the highlighted element** |
| Spotlight | Rest of the screen dimmed; the target element gets a **gold rounded outline** and a lighter plate |
| Interactive steps | Prompt names the exact card or button; the rest of the UI ignores input until the correct tap; after the action the mascot cheers and a confirmation bubble appears |
| Mascot poses | Waving (greeting), index finger up (teaching), pointing (directing), thinking with "?" (caution), cheering (success), holding fanned cards (ronda topics) |
| Rules path | Scripted 2-seat table with a **You / Rival** scoreboard and a captured-pile counter at the bottom-left; 31 steps, ends with *"You know the rules! Ready for a real game?"* [Close] [Play 1 vs 1] |
| App path | Real-menu spotlight tour (profile, 1v1, 2v2, Create Table, Join Table, Leaderboard, Settings) then a scripted demo 2v2 table (hand, table centre, ronda button, catch target, stickers, scoreboard, in-match menu); ends with *"That's it! Do you want to learn the rules now?"* [Close] [Learn the rules]. **Ronda Club and My table are not covered.** |
| Content | All rule sentences are quoted in doc 03; app-tour sentences in doc 02 §4 and doc 03 §3–4 |

Voice/tone: friendly, short sentences, a Moroccan greeting ("Salam!"), uses the game's own terms (ronda, tringla, missa, derba).

---

## 8. S13 Create-table waiting room, matchmaking queue and AFK dialog **VERIFIED (visual)**

Expected from the Tutorial: level-based opponent search for 1v1; "start alone and we find you a partner" for 2v2; private-table waiting room with a shareable code. Observed (Driver session; all screens reached with injected taps, so these captures are touch-clean, `touch_watch.log` windows @1794481274.98–1794481410.65 read 0):

| Screen | Evidence | Content |
|---|---|---|
| Create Table → choose mode | A008 (@1794481288.90), A013 | modal "Create Table": **1 Vs 1** (blue) and **2 Vs 2** (red) |
| **Waiting Players** (1v1) | A009–A012 (A010 after tapping the locked Start; A011 after tapping "+") | title tab "Waiting Players", red X; **four seat slots** in two halves divided by a vertical line: `[lock][own avatar] │ [+][lock]` — only one open "+" seat exists in 1v1; **"Table Id:"** with a 6-character code and a copy button (the code is not reproduced); a blue **"Start"** button with a padlock icon (locked while alone); an **eye counter "0"** (spectators) |
| **Waiting Players** (2v2) | A014 (@1794481341.93), closed A015 | same layout with **three "+" seats**: `[+][own avatar] │ [+][+]` |
| Tap on locked **Start** while alone | A010 | no visible change (no error text, no toast) |
| Tap on a **"+"** seat | A011 (@1794481319.54) | **no visible change** (no share sheet, no dialog) in the captured frame; what "+" does is **UNKNOWN** (an invite/share action may need more time or a different gesture) |
| **Matchmaking queue** | A016–A018 (A016 @1794481364.38, "0:01") | modal "Searching": own avatar, an orange **"VS"** medallion, a placeholder portrait for the unknown opponent, the text *"Looking for a player for you"*, a count-up timer (`0:01`), **Cancel** (blue) and a red X |
| Queue → table | Driver log lines 24–26 (log-only for the duration); A017, A018, A019 | table loads with placeholder labels "Us / Them" then "Me / Rival"; A017 (file time 1794481367.06, 5.6 s after the tap) already shows the empty table with placeholder "Us / Them" labels and A018 (file time 1794481371.57, 10.1 s after the tap) the dealt table (deck 32, 4 cards, "Me / Rival"); the Driver logged "dealing done" only at 1794481391.999 (a write time). The queue lasted **≤ 3 s** according to the Driver (log-only; the Observer stills B005/B006 were not individually inspected here) |
| **AFK dialog** | A020 (@1794481427.50), A021, A023, A042, B007 | modal "You are AFK": body *"Bots are playing for you."*, one blue button **"I'm back"**; the table remains visible behind it; see doc 03 §3.4 |

Left/right halves of the waiting room are **INFERRED** to be the two teams. Whether the 1 Vs 1 table has hidden seats (the two padlocks) or is simply drawn in the 4-seat template is **UNKNOWN**. Timeout, cancel flow after a match is found, and bot filling of empty seats are **UNKNOWN**. Whether the public-queue opponent was a human or a bot is **UNKNOWN** (the queue showed a generic placeholder portrait before the real avatar replaced it; the opponent behaved like a normal player, including templated speech bubbles).

---

## 9. S14 Game table **VERIFIED**

### 9.1 Layout (1600 × 720)

| Element | Position (px) | Notes |
|---|---|---|
| Table frame | octagon, outer ≈ x 205–1415, y 95–630 | Wood frame with a geometric gold inlay; dark teal felt |
| Card slot grid | 5 columns × 2 rows; column centres ≈ 590, 700, 810, 920, 1030; row centres ≈ 275 and 445; each slot ≈ 88 × 140 | Faint rectangles always visible; **each rank has a fixed slot**: top row = ranks 1–5, bottom row = 6, 7, 10, 11, 12, left to right (A027, A029, A033, A039, A041 — VERIFIED visual; corrects the earlier "next free slot" reading) |
| Own hand | fanned, centred ≈ x 800, bottom ≈ 530–720; each card ≈ 170 px wide (≈ 2× table cards) | Overlapping, slight rotation (±5°), bottom partly clipped |
| Own nameplate | ≈ x 195–440, y 555–630 | Circular avatar (≈ 80 px) + name pill; sticker button ≈ x 445–500 |
| Top seat | avatar ≈ (983, 95); pill to its right; hand stack left of the avatar | 2v2: partner; 1v1: opponent |
| Left seat | avatar ≈ (210, 228); crosshair button ≈ (446, 230); hand stack below | 2v2 opponent |
| Right seat | avatar ≈ (1268, 477); crosshair ≈ (1193, 480); hand stack above | 2v2 opponent |
| Scoreboard | ≈ x 1265–1408, y 45–185; header (Us/Them), cream row 1, teal row 2 | Top row cards taken, bottom row points |
| Ronda banner | below the scoreboard, ≈ 230 × 40 | Text + draining bar |
| Gear | ≈ x 186–250, y 40–105 | In-match menu |
| Dealer token | ≈ 36 px gold medallion beside the dealer pill | |
| Deck counter | card-back ≈ 78 × 50 (rotated at the own seat) with a number | |
| Ronda / Tringla button | beside the own hand, bottom-right of it (≈ x 1030–1240, y 595–650) | Appears only when eligible |

### 9.2 Interactions

| Action | Gesture | Feedback |
|---|---|---|
| Play a card | One tap on a hand card (only on own turn) | Card lifts with a gold highlight, flies to a slot, takes resolve; bubbles; score update |
| Announce ronda/tringla | Tap the dark-teal **"Ronda"** pill at the lower right of the hand (A029, A036, A042) before the window closes | Bubble over the seat (*"I had a ronda! I take +1"*, seen in 003 together with the banner "Ronda declarations closed"); when the point is credited is **UNKNOWN** (doc 03 §4.3) |
| Catch | Tap the crosshair beside an opponent → 5 × 2 number picker (a popover with a pointer, ranks 1–7, 10–12) → tap a number | +1 or lockout (per Tutorial); live-play effect **UNKNOWN** |
| Stickers | Tap the sticker button | Panel **UNKNOWN**; tapping a player's avatar shows profile / mute stickers **VERIFIED (tutorial)** |
| In-match menu | Tap the gear | Share (for spectators), Settings, Leave table **VERIFIED (tutorial)**; panel layout **UNKNOWN** |

### 9.3 States and indicators
Turn: avatar glow (green tint). Waiting: opponent avatars not glowing. Declaration window: banner with seconds counting down ("Ronda declarations close in 13s", A041; "close in 9s", A029; "close in 10s", A037; "Ronda declarations closed", 003). Bonus events: cream speech bubble with a tail pointing at the actor's nameplate — "Derba! +1" (A027), "Missa!" (A031), "I had a ronda! I take +1" (003). **End-of-deck bubble** (A021): a longer bubble from the beneficiary — *"The dealer wasn't the last to take! +5 for us / I collected 32 cards, I get +12"* — so the tally is explained in first person text, not only by number changes. **AFK takeover:** modal over the table (see §8). New deal: deck counter changes and the hands refill; the declaration banner restarts at every deal (A041). The pile counter that shows captured-card count appears at the own seat in the Tutorial (card-back with a number); in live matches the equivalent is the scoreboard top row.

---

## 10. S15 Result panel **VERIFIED**
Panel ≈ x 367–1232, y 95–625 over the dimmed table; title **"VICTORY"** (gold, display type). Row 1: team avatars with league-shield badges (own team left, a crown above the top performer **INFERRED: MVP**), large score box (e.g. 44 – 31) with divider ticks, opposing team on the right. Row 2: **+100 XP** and a green "▲ +N" value. Row 3: league-shield progress bar (bronze shield → next tier shield). Row 4: level bar with level numbers at both ends. Footer **"Tap to continue"**. In 1v1 an intermediate state **"Calculating…"** shows before the numbers fill in. **The numbers count up:** the same 41 – 26 result reads **+62 XP / ▲ +6** in A044 (burst label `t05`, file time @1794482087.93) and **+100 XP / ▲ +9** in A045 (`t17`, @1794482091.58); the bars fill between them (VERIFIED, visual). The 2v2 result of the earlier series (005, 44 – 31) shows **four avatars (two per team)**, **+100 XP / ▲ +10**, level bar 2 → 3 and a crown above one avatar of the winning team; in 1v1 (A044) the crown is above the own avatar. The Driver log line quoting "+62 XP, +6 rank pts" is the **mid-animation** frame, not the final result. The panel opens ≈ 0.5 s after the last play; the captured-cards row behind it already reads 0 / 0. After "Tap to continue" the game returns to the menu (via the ad in §11 in the observed case). Defeat, draw and level-up variants: **UNKNOWN**.

## 11. S16 Post-match ad **VERIFIED** (observed once)
Immediately after the result and before the menu, a **full-screen video ad** (a Google Play install ad in this instance) played for ≈ 14 s with a small skip/forward icon, then a **Close** control at the top-right; a short black frame precedes it. Ad provider/format is consistent with the declared AdMob integration (doc 01). Frequency (every match? every N?), rewarded placements, and what happens if an ad fails to load: **UNKNOWN**.

---

## 12. Responsive behaviour and localisation

| Topic | Finding |
|---|---|
| Aspect ratio | Tested only on 20:9 (1600 × 720) with a left cutout; other ratios, tablets and foldables **UNKNOWN** |
| Scaling | UI appears proportional to screen height (fixed 720 px layout units) **INFERRED** |
| Cutout | HUD starts after the cutout inset; backgrounds extend under it **VERIFIED** (measurement) |
| Hands | Own hand is deliberately large and clipped by the bottom edge to maximise tap targets on small screens **VERIFIED** |
| Arabic | Arabic is a first-class option and the notice shows an Arabic block right-aligned; whether the whole UI mirrors (RTL) when Arabic is selected **UNKNOWN** |
| Fonts | One rounded display typeface across Latin; Arabic glyph rendering in the UI chrome **UNKNOWN** |

## 13. Items resolved by the parallel observation (A/B)
Resolved by the Driver (A) and Observer (B) sessions; each row states how it is known. Contamination: see `02_reference_game_analysis.md` §11 (32 of 61 touch windows have real touches; menu exploration and queue are clean).

| Item | Result | Evidence | Strength |
|---|---|---|---|
| Profile screen | fields and layout in §6.6 | A002, A003 (A004/A005 closes) | VERIFIED (visual), clean window |
| Join Table | Table-Id field + Join (§6.3) | A006, A007 | VERIFIED (visual), clean |
| Create Table → waiting room (1v1, 2v2) | layouts in §8 | A008–A015, B001–B004 | VERIFIED (visual for A010/A011/A014), clean; B stills not individually inspected |
| "+" seat tap, locked Start tap | no visible reaction | A010, A011 | VERIFIED (visual) for "no reaction in the captured frame" |
| Matchmaking queue | "Searching" modal, count-up timer, Cancel | A016–A018, B005 | VERIFIED (visual A016–A018): placeholder table at +5.6 s, dealt table at +10.1 s; ≤ 3 s is log-only |
| In-match gear / sticker panels | **not opened in the Driver session** (the gear was only tapped in the earlier series, whose captures show the table without a panel: 003, 004) | — | **UNKNOWN** |
| AFK dialog | §8 | A020, A021, A023, A042, B007, B008 | VERIFIED (visual); timing INFERRED/CONTAMINATED |
| Table slot rule | rank-addressed slots | A027, A029, A033, A039, A041 | VERIFIED (visual) |
| Result count-up | +62/+6 → +100/+9 | A044, A045 | VERIFIED (visual) |
| Opponent seat | top seat in 2v2 is the **partner** (005 lists own avatar + top-seat avatar as one team) | 005 (earlier series) | VERIFIED (visual) |
| Animation durations | not extracted from the 26 Observer sheets; only coarse bounds: result count-up completes within the 1.2 s separating burst labels `t05` and `t17`; panel appears ≈ 0.5 s after the last play | A044, A045 | INFERRED |
| Post-match ad after the 1v1 win | **not captured** in the A/B series (the Observer deleted the last chunk because a non-Ronda window had focus); the single earlier observation stays in §11 | observer_index (chunk027) | UNKNOWN for this match |

## 14. UX requirements derived for our implementation (not copying)

1. Landscape-only, safe-area aware, scale from a reference height; support Arabic RTL from the start.
2. One-tap play with clear turn indication; large touch targets for the hand.
3. A first-run interactive tutorial with an original mascot and short, scripted steps; separate "how to use the app" and "learn the rules" paths.
4. Observable bonus events (call-outs) for every scoring action so players can audit the score; a captured-pile or score breakdown for end-of-deck tallies.
5. A deliberate "calculating" beat before results; clear XP/rank feedback.
6. External links open the system browser, never an unexpected app; never include share flows that expose the user's contacts without clear intent.
7. Ads, if any, must not interrupt a match and must have a deterministic close affordance.
8. A reconnect path that returns the user to the live match state (the reference resumed a running match after an app restart).
