# 06 — Technical Investigation (reference game)

**Phase:** 2 · **Status:** FINAL for Phase 2 review (all 33 Observer samples reconciled in §6 and §9; evidence limits in `02_reference_game_analysis.md` §11) · **Date:** 2026-11-12
**Subject:** "Ronda: Moroccan Card" `com.sandukhan.ronda` v6.1.1 (versionCode 43).
**Labels:** **VERIFIED** confirmed by direct evidence (measurements come from the Observer's `perf.csv`, a log: *log-only*; screenshots are *visual*) · **VERIFIED (package presence)** the component is present in the shipped package, but runtime use is unproven · **INFERRED** strongly suggested, not conclusive · **UNKNOWN** insufficient evidence. (Earlier drafts: `[V]`, `[V-presence]`, `[H]`, `[U]`.) Epochs are host seconds (`START` = 1794481151).
**Boundaries (user decisions D3/D4):** technology *identification* only. No IL2CPP dumping, no decompilation of `libil2cpp.so`, no asset extraction, no network interception or TLS inspection, no disconnect tests. Everything below comes from package metadata (doc 01), manifest/config inspection, and non-invasive runtime sampling of the owner's phone.

---

## 1. Summary

| Question | Answer |
|---|---|
| Engine | **Unity 6000.3.6f1** (revision `bbb010bdb8a3`) **VERIFIED** |
| Scripting backend | **IL2CPP** (`libil2cpp.so` 40 MB + `global-metadata.dat` 11.9 MB, metadata version 39) **VERIFIED** |
| Activity | `com.unity3d.player.UnityPlayerGameActivity` (GameActivity-based player) **VERIFIED** |
| Content system | **Addressables** (`catalog.bin`, bundles) **VERIFIED** |
| Localization | **Unity Localization**: 5 string tables (Arabic, Darija ar-MA, English, French, Spanish) **VERIFIED** |
| Services | Firebase (Auth, Realtime Database, Remote Config, Analytics, Crashlytics), Unity Gaming Services assemblies, WebSocket libraries, AdMob **VERIFIED (package presence)** |
| Which backend carries the game traffic | **UNKNOWN** (hypotheses H1–H3, §5) |
| Observed runtime footprint (test phone, 33 samples) | PSS 500–625 MB, graphics 210–291 MB, 138–160 threads, 30–60 fps (two regimes, not tied to a single screen type); a step up of ≈ +80 MB PSS / +61 MB graphics / +16 threads occurs between two samples that bracket the match end and the result panel (§9). Correlation only |

---

## 2. Package and platform facts (from doc 01, re-stated)

| Item | Value | Label |
|---|---|---|
| Package / version | `com.sandukhan.ronda`, 6.1.1 (43) | **VERIFIED** |
| SDK levels | min 25, target 36, compile 36 | **VERIFIED** |
| Orientation | `sensorLandscape` (observed: forced landscape) | **VERIFIED** |
| Installed splits (test phone, Play) | base + `split_config.arm64_v8a` | **VERIFIED** |
| XAPK analysed offline | base + `config.armeabi_v7a`; native libs `libunity`, `libil2cpp`, `libmain`, `libgame`, `lib_burst_generated`, `libswappywrapper`, Firebase C++ libs, Crashlytics libs; NDK r27c, Android API 25 | **VERIFIED** |
| Burst | `lib_burst_generated.so` present | **VERIFIED** |
| Frame pacing library | `libswappywrapper.so` (Android Frame Pacing / Swappy) | **VERIFIED (package presence)** |
| DEX | 3 DEX files (≈ 23 MB), mostly SDK code (Firebase, GMS, AndroidX, AdMob) | **VERIFIED** |
| Permissions | INTERNET, ACCESS_NETWORK_STATE, WAKE_LOCK, FOREGROUND_SERVICE, AD_ID, ADSERVICES_*, install-referrer; **no billing permission** | **VERIFIED** |
| URI schemes | `playronda`, `https`, plus SDK schemes (`genericidp`, `recaptcha`, `sms`) | **VERIFIED** (manifest) |
| Signing | APK signature verifies (v1+, one signer) | **VERIFIED** |

## 3. Unity project structure (observable only)

| Aspect | Finding | Label |
|---|---|---|
| Boot config | `gfx-threading-mode=4`, `androidStartInFullscreen=1`, `android-preferred-data-location=1`, `gc-max-time-slice=3`, `hdr-display-enabled=0` | **VERIFIED** |
| Addressables | `assets/aa/catalog.bin`, `settings.json`, 8 bundles in `aa/Android/` (localization tables + monoscripts); settings keys include catalog locations and request timeout | **VERIFIED**; remote catalog use **UNKNOWN** |
| Managed assemblies | 156 listed in `ScriptingAssemblies.json`, including `Assembly-CSharp`, `DOTween`, `Newtonsoft.Json`, `Unity.InputSystem`, `Unity.TextMeshPro`, `Unity.Localization`, `Unity.Addressables`, `Unity.2D.{Animation,IK,SpriteShape,PixelPerfect}`, `Unity.Burst` | **VERIFIED (package presence)** |
| UI technology | `UnityEngine.UIModule` + `UIElementsModule` + `TextMeshPro` are present; the in-game UI (buttons, modals, scoreboard) looks like **uGUI with TMP**, not UI Toolkit | **INFERRED** |
| Animation | `DOTween` present; observed motion is tween-like (lift, slide, fade, bubble pop); `Unity.2D.Animation/IK` present (mascot poses may be sprite-skinned) | **INFERRED** |
| Audio | `AudioModule` present; one Sound ON/OFF toggle in Settings; channel split and format **UNKNOWN** | **VERIFIED (package presence)** / **UNKNOWN** |
| Scenes and prefabs | Not inspected (inside `data.unity3d`; D3) | **UNKNOWN** |
| Rendering pipeline (URP/Built-in) | Not determined (would require unpacking assets); Vulkan feature is declared in the manifest | **UNKNOWN** |
| Graphics API in use at runtime | See §6 (Observer measurement) | **UNKNOWN** until §9 |

## 4. Localization and content
Five Unity Localization string tables ship **inside the APK**: Arabic, Darija (`ar-MA`), English, French, Spanish **VERIFIED**; the in-app language selector shows five flags matching them **VERIFIED**. Locale-aware layout (Arabic RTL): **UNKNOWN**. The multilingual notice modal (ranking reset) appeared with all four languages' text at once; its source (Firebase Remote Config / database / bundled) **UNKNOWN**.

---

## 5. Networking and backend indicators

### 5.1 Presence-only evidence (doc 01)

| Area | Present | Label |
|---|---|---|
| Firebase | `Firebase.{App, Auth, Database, RemoteConfig, Analytics, Crashlytics}` + C++ libs (Auth, Database, Remote Config, Analytics, Crashlytics, App) | **VERIFIED (package presence)** |
| Unity Gaming Services | `Unity.Services.{Core, Authentication, Authentication.PlayerAccounts, CloudCode, CloudSave, Leaderboards, Multiplayer, QoS, Wire.Internal}`, `UnityServicesProjectConfiguration.json` | **VERIFIED (package presence)** |
| Transport | `Unity.Networking.Transport`; `websocket-sharp`, `unity-websocket-sharp` | **VERIFIED (package presence)** |
| Sign-in (Android side) | Firebase Auth generic-IdP and reCAPTCHA activities, Google Sign-In hub, AndroidX Credentials | **VERIFIED (package presence)** |
| Ads | AdMob + UMP consent assemblies; post-match interstitial observed | **VERIFIED** (ad) |

### 5.2 Hypotheses on the stack (unchanged)
- **H1:** Firebase Auth + Realtime Database carry identity and room/game state. **INFERRED**
- **H2:** UGS Authentication / Cloud Save / Leaderboards carry profile and ranking data. **INFERRED**
- **H3:** a `websocket-sharp` channel carries real-time play. **INFERRED**
Any of these may be an unused dependency. They cannot be settled without traffic inspection, which we will **not** do (D4).

### 5.3 What the observed *behaviour* implies (feature-level, not protocol)

| Observed behaviour | Evidence | Implication for our requirements |
|---|---|---|
| A force-stopped app returned directly into the still-running match with scores intact | **VERIFIED** (one instance) | Match state lives on a server (or durable store), not only on the client **INFERRED**; reconnect must restore the live state |
| Public matchmaking for 1v1 found an opponent in ≈ ≤ 3 s in our sample (one instance); a queue modal ("Searching", count-up timer, Cancel) exists | **VERIFIED (visual)** modal: A016 (@1794481364.4); duration ≤ 3 s is **log-only**, while A017 (@1794481367.06) shows the placeholder table 5.6 s after the tap and A018 (@1794481371.57) the dealt table (**VERIFIED (visual)**) (Driver log line 25, @1794481391.99); one sample only | Fast matching; queue UI with timer and cancel |
| Tutorial: 1v1 opponents are matched "of your level"; ranked ladder with Ronda Points; tiers (Bronze … Diamond) | **VERIFIED (tutorial)**, **VERIFIED** (leaderboard) | Rating-based matchmaking + leaderboard service |
| 2v2: start alone and be assigned a partner, or share a room code | **VERIFIED (tutorial)** | Party/partner assignment service |
| Private tables with a shareable code/link and a spectator mode | **VERIFIED (tutorial)**; waiting room shows a Table Id and a spectator counter **VERIFIED** (Driver capture) | Room service with IDs, invitations, spectator channel |
| A player who misses turns gets a dialog "You are AFK — Bots are playing for you — I'm back" and play continues with bots until "I'm back" is tapped | **VERIFIED (visual)** the dialog and continued play: A020 (@1794481427.5), A021, A023, A042, B007; onset ≤ 55.9 s after the dealt table appeared (A018 file time 1794481371.57 → A020 1794481427.50); **re-appearance ≤ 20.4 s after a dismissal in a clean (no real touch) interval** is **INFERRED**; the turn timer value is **UNKNOWN**. The first appearance coincides with a contaminated touch window (doc 02 §11) | Server-side (or client-side) bot takeover for idle players, plus a way to return; the game continues without stalling. Which side performs the takeover is **UNKNOWN** |
| Leaderboard with ≥ 11,000 ranked players; per-mode stats (2v2 / 1v1) on the profile | **VERIFIED** | Global leaderboard + per-mode stats |
| "Catch" accusation requires the server to know which players announced which pairs | **VERIFIED (tutorial)** | Authoritative server must track hidden hands and announcements (anti-cheat by design) |
| Stickers instead of free text; per-player mute | **VERIFIED (tutorial)** | Moderation-friendly social layer |
| Match ends instantly at 41 and shows an authoritative result panel, XP and rank deltas | **VERIFIED** | Server computes results/rewards |

### 5.4 Unknowns (cannot be answered without traffic inspection, out of scope)
Wire protocol; message formats; authentication token flow; whether the server is authoritative or a client is host; anti-cheat; region routing; reconnect grace period.

---

## 6. Observed performance (non-invasive sampling on the test phone)

**Device:** Samsung SM-A045F (low-end, MediaTek), Android 14, 720 × 1600 panel. **Source:** all **33** samples of the Observer's `perf.csv` (epochs 1794481323–1794482185, i.e. t+172 s to t+1034 s, one sample every ≈ 26–27 s; each sample = `dumpsys meminfo`, `top`, SurfaceFlinger frame intervals, thermal sensors). The first draft of this section used only the first 11 samples; those cover samples 1–12 (before the second step in §9) and are consistent with the table below. Units: **MB = 1000 kB** as logged. They describe the **reference game on one low-end phone**, not a benchmark. All figures are **VERIFIED (log-only)**; no performance screenshot exists.

| Metric | Range over 33 samples (median) | Label |
|---|---|---|
| Cold start to first activity frame | **2,040 ms** (`am start -W`, `LaunchState: COLD`; earlier series) | **VERIFIED** |
| Time from launch to main menu | ≈ 12–13 s (video, ± 1 s; earlier series) | **VERIFIED** |
| Process memory (PSS) | **499.7–624.6 MB** (median 517.7); reported range 500–625 MB | **VERIFIED (log-only)** |
| RSS | 463.2–604.8 MB (median 471.2) | **VERIFIED (log-only)** |
| Java heap / native heap | 31.3–39.3 MB / 13.0–26.7 MB | **VERIFIED (log-only)** |
| Graphics memory | **209.7–290.9 MB** (median 224.0); reported range 210–291 MB | **VERIFIED (log-only)** |
| Threads | **138.0–160.0** (median 142.0) | **VERIFIED (log-only)** |
| CPU | 29.4–97.0 % of one core (median 70.5) | **VERIFIED (log-only)** |
| Frame rate | **30.2–60.3 fps** (median 47.7); two regimes: ≈ 30 fps (p50 33.1 ms, p95 ≈ 50 ms or 33 ms) and ≈ 60 fps (p50 16.6 ms, p95 16.6–33 ms); the worst single frame gap logged is 149 ms | **VERIFIED (log-only)** |
| Vsync period | 16.67 ms (60 Hz panel) | **VERIFIED** |
| Battery temperature | 33.1–36.7 °C (median 33.1) | **VERIFIED (log-only)** |
| CPU and GPU temperature | **47.1 °C in all 33 samples** | the value never changes, so the sensor is **INFERRED** not to be informative; do not use it |

**Memory levels.** Samples 1–12 (t+1721794481465): PSS 499.7–510 MB, graphics 209.7–212.9 MB, 138–141 threads. Samples 13–29 (t+5011794481928): PSS 516–524 MB, graphics a plateau at ≈ 223.6–224.0 MB, 141–143 threads. Samples 30–33 (t+9551794482034): PSS 602–625 MB, graphics 275–291 MB, 157–160 threads (§9).

**Which screens map to which frame-rate regime.** The ≈ 30 fps samples are 1–4 (waiting rooms, queue, table loading, A010–A019), 9, 11, 15, 16, 22 and 28 (partly) — i.e. *also during matches* — and the last three samples (31–33). So "30 fps on menus, 60 fps on the table" (earlier draft) is **not** supported: the table alternates between the regimes. What decides the regime is **UNKNOWN** (sample-to-sample 3–5 s windows, no per-sample screen label). HWUI `gfxinfo` (earlier series: p50 22 ms, 7.7 % janky) sees only the Android window layer, not Unity's renderer.

**Interpretation INFERRED:** the 30 fps regime looks like a frame-rate cap (a lock at exactly 33.1 ms p50 with p95 33.2 ms in samples 31–33 where CPU is ≈ 30 %), while mid-match samples at 50–90 % CPU reach 60 fps. Swappy/frame pacing is shipped (package presence). Sample sizes are small; do not treat as a performance budget until repeated.

**Implications for our build:** target 60 fps on the table with a 30 fps fallback; budget ≲ 500–625 MB total and ≲ 300 MB graphics memory on low-end phones, with an explicit look at what is held after a match; keep thread count modest; atlas the UI; avoid per-frame allocations.

## 7. Rendering and audio facts (runtime)

The Observer's `perf.csv` has **no** renderer, graphics-API or audio fields and no such data was collected by the Driver or in consolidation (no new collection was allowed in this stage). Therefore **UNKNOWN**: graphics API in use at runtime, GPU renderer string, render pipeline, audio output (sample rate, channels, music/SFX split), audio focus behaviour. What is on record: a Vulkan feature declaration in the manifest (**VERIFIED**, doc 01), Swappy library presence (**VERIFIED (package presence)**), one Sound ON/OFF toggle in Settings (**VERIFIED (visual)**, 007), and a 60 Hz panel with 16.67 ms vsync (**VERIFIED (log-only)**). Optional non-invasive follow-ups are listed in `02a_phase2_consolidated_assessment.md` §13.

---

## 8. Limits and non-goals
1. No access to server code, protocol, or traffic (D4). Backend design in docs 09–12 will come from **our own requirements** and public documentation.
2. No inspection of Unity assets or IL2CPP code (D3); scene hierarchy, shaders and audio assets remain unknown.
3. Performance numbers come from one low-end device and a few minutes of sampling.
4. The app version is a single build (6.1.1); server-side behaviour (rankings, rewards, AFK rules) may change with server updates.
5. Presence of an assembly is **not** evidence of its use.

## 9. Final measurements from the Observer (B) — all 33 samples

All 33 samples (`perf.csv`; the `fps` column is frames over a 2–4 s SurfaceFlinger window; `pid` 18287 in every sample, so the process never restarted):

| # | epoch | t+ s | fps | p50 ms | p95 ms | PSS MB | graphics MB | native MB | threads | CPU % of a core | battery °C |
|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| 1 | 1794481323.1 | 172 | 30.4 | 33.1 | 49.8 | 509.6 | 212.9 | 13.0 | 139 | 53.1 | 33.1 |
| 2 | 1794481349.7 | 199 | 30.3 | 33.1 | 49.8 | 509.1 | 212.6 | 13.2 | 138 | 46.8 | 33.1 |
| 3 | 1794481376.2 | 225 | 31.5 | 33.1 | 49.5 | 499.7 | 209.7 | 14.1 | 140 | 59.3 | 33.1 |
| 4 | 1794481402.7 | 252 | 31.9 | 33.1 | 49.7 | 500.5 | 209.7 | 14.1 | 140 | 56.2 | 33.1 |
| 5 | 1794481429.2 | 278 | 60.3 | 16.6 | 16.7 | 500.7 | 209.7 | 14.2 | 141 | 59.3 | 33.1 |
| 6 | 1794481455.9 | 305 | 51.0 | 16.6 | 33.3 | 504.1 | 212.1 | 14.7 | 141 | 76.4 | 33.1 |
| 7 | 1794481482.5 | 332 | 59.9 | 16.6 | 16.7 | 503.8 | 212.1 | 14.7 | 141 | 76.4 | 33.1 |
| 8 | 1794481509.2 | 358 | 55.5 | 16.6 | 32.6 | 503.9 | 211.7 | 14.6 | 141 | 69.6 | 33.1 |
| 9 | 1794481535.8 | 385 | 30.2 | 33.1 | 49.8 | 504.3 | 212.1 | 14.8 | 140 | 51.5 | 33.1 |
| 10 | 1794481562.4 | 411 | 46.0 | 16.6 | 33.2 | 503.2 | 211.7 | 14.7 | 141 | 70.5 | 33.1 |
| 11 | 1794481589.0 | 438 | 30.9 | 33.1 | 49.8 | 502.0 | 212.1 | 14.8 | 141 | 65.6 | 33.1 |
| 12 | 1794481615.5 | 465 | 52.1 | 16.6 | 33.2 | 503.4 | 212.1 | 14.7 | 141 | 59.3 | 33.1 |
| 13 | 1794481652.3 | 501 | 57.2 | 16.6 | 16.9 | 524.5 | 224.0 | 20.7 | 143 | 87.8 | 33.1 |
| 14 | 1794481679.0 | 528 | 53.5 | 16.6 | 33.1 | 519.3 | 224.0 | 16.3 | 143 | 81.8 | 33.1 |
| 15 | 1794481705.6 | 555 | 35.0 | 33.1 | 49.7 | 517.1 | 224.0 | 16.1 | 142 | 52.9 | 33.1 |
| 16 | 1794481732.1 | 581 | 30.3 | 33.1 | 49.8 | 518.0 | 224.0 | 16.1 | 143 | 53.1 | 33.1 |
| 17 | 1794481759.0 | 608 | 60.3 | 16.6 | 16.6 | 518.8 | 224.0 | 16.1 | 141 | 91.1 | 33.1 |
| 18 | 1794481785.7 | 635 | 47.7 | 16.6 | 33.4 | 518.8 | 223.6 | 16.1 | 142 | 76.4 | 33.1 |
| 19 | 1794481812.6 | 662 | 42.2 | 16.6 | 33.5 | 519.0 | 223.6 | 16.1 | 141 | 78.7 | 33.1 |
| 20 | 1794481839.2 | 688 | 59.9 | 16.6 | 16.7 | 519.0 | 224.0 | 16.0 | 141 | 84.8 | 33.1 |
| 21 | 1794481865.8 | 715 | 59.9 | 16.6 | 16.6 | 515.9 | 223.6 | 16.1 | 141 | 81.8 | 33.1 |
| 22 | 1794481892.3 | 741 | 30.4 | 33.2 | 49.7 | 517.2 | 224.0 | 16.2 | 143 | 56.2 | 33.1 |
| 23 | 1794481918.9 | 768 | 46.6 | 16.6 | 33.4 | 518.3 | 224.0 | 16.2 | 142 | 84.3 | 33.1 |
| 24 | 1794481945.6 | 795 | 60.3 | 16.6 | 16.6 | 518.2 | 224.0 | 16.1 | 143 | 87.8 | 33.1 |
| 25 | 1794481972.4 | 821 | 59.9 | 16.6 | 16.7 | 518.1 | 224.0 | 13.9 | 143 | 73.5 | 33.1 |
| 26 | 1794481998.9 | 848 | 60.3 | 16.6 | 16.6 | 518.1 | 224.0 | 13.9 | 142 | 90.9 | 33.1 |
| 27 | 1794482025.4 | 874 | 54.3 | 16.6 | 33.1 | 517.2 | 224.0 | 13.9 | 143 | 75.0 | 33.1 |
| 28 | 1794482052.1 | 901 | 38.0 | 16.7 | 49.7 | 517.7 | 223.6 | 14.0 | 142 | 82.3 | 33.1 |
| 29 | 1794482078.7 | 928 | 60.3 | 16.6 | 16.6 | 519.8 | 224.0 | 13.9 | 143 | 69.6 | 33.1 |
| 30 | 1794482105.6 | 955 | 55.5 | 16.6 | 16.6 | 602.1 | 285.3 | 20.6 | 159 | 97.0 | 36.7 |
| 31 | 1794482132.2 | 981 | 30.2 | 33.1 | 33.2 | 605.8 | 275.1 | 22.1 | 160 | 29.4 | 36.7 |
| 32 | 1794482158.7 | 1008 | 30.2 | 33.1 | 33.2 | 602.3 | 275.1 | 21.8 | 158 | 31.2 | 36.7 |
| 33 | 1794482185.4 | 1034 | 30.2 | 33.1 | 33.2 | 624.6 | 290.9 | 26.7 | 157 | 33.3 | 36.7 |

### 9.1 Two steps in memory and thread count

| Step | Samples | Epoch gap | PSS | Graphics | Native | Threads | Other |
|---|---|---|---|---|---|---|---|
| **A** (small) | 12 → 13 | 1794481615.5 → 1794481652.3 (**37 s**, longer than the usual 26–27 s) | 503.4 → 524.5 MB (+21.0) | 212.1 → 224.0 (+11.9) | 14.7 → 20.7 | 141 → 143 | CPU 59.3 → 87.8 %; PSS then settles at 516–520 MB |
| **B** (large) | 29 → 30 | 1794482078.7 → 1794482105.6 (26.9 s) | 519.8 → 602.1 MB (+82.2) | 224.0 → 285.3 (+61.3) | 13.9 → 20.6 | 143 → 159 (+16) | CPU 69.6 → 97.0 %; battery 33.1 → 36.7 °C |
| after B | 30 → 33 | to 1794482185.4 | stays 602–625 MB; sample 33 rises again to 624.6 MB | 275 → 290.9 MB | | 157–160 | fps locked at 30.2, CPU 29–33 % |

### 9.2 Temporal relationship of step B to the match end (no causation inferred)

Using the screenshot *file times* (the Driver's log lines are written late, so the log time of the match end is not the time it happened):

| Epoch | t+ | Event | Source |
|---|--:|---|---|
| 1794482057.9 | 907 | state before the last moves (cards 22 / 8, points 32 / 26) | A043 |
| **1794482078.73** | 928 | **sample 29: PSS 519.8 MB, graphics 224.0 MB, 143 threads** | perf.csv |
| 1794482085.39 | 934 | Driver's last logged play (`E4`) | log line 42 |
| 1794482087.93 | 937 | VICTORY panel visible (+62 XP, ▲ +6) | A044 |
| 1794482091.58 | 941 | panel final values (+100 XP, ▲ +9) | A045 |
| **1794482105.61** | 955 | **sample 30: PSS 602.1 MB, graphics 285.3 MB, 159 threads, CPU 97 %** | perf.csv |
| 1794482109.88 / .97 | 959 | Driver writes the match-end line and "tapped Tap to continue" | log lines 43–44 (write time; the panel had been visible for ≈ 22 s) |

**What can be said:** step B happened inside the 26.9 s interval between samples 29 and 30. That interval contains the last plays, the end-of-deck tally, the result panel (visible from ≈ 937 s) and possibly the tap on "Tap to continue" (its true time is unknown; the logged time is later than sample 30). No sample lies between them, so it is **UNKNOWN** whether the increase began at the final move, when the result panel opened, or when the player left the panel. The level then stayed 80–105 MB above the in-match level for the remaining 80 s of sampling, i.e. there is no evidence of release within that time.

**What cannot be said:** that the match end, the result panel, an advertisement, the loading of a different screen or garbage collection *caused* the increase. The evidence contains only the memory/thread numbers and the clock; no memory profiler, no screen label per sample and no sample after returning to the menu. Which window had focus during samples 31–33 is **UNKNOWN**: the Observer recorded chunks 025–026 (Ronda focus, not reviewed during consolidation) but **deleted chunk027** (epoch 1794482153.98–185.64, containing sample 33) unviewed because a non-Ronda window held focus. The battery temperature step (33.1 → 36.7 °C in 27 s) occurs in the same interval and is likewise unexplained.

**Step A** (samples 12 → 13, +21 MB PSS) follows a gap in which the Observer's guard refused to record because the notification shade had focus (observer_index lines @1794481634–644; chunk011 deleted unviewed) and the Driver tapped "I'm back" @1794481646.74. Same status: correlation only.

### 9.3 Contamination relevance
Performance data are independent of the touch contamination in `02_reference_game_analysis.md` §11 (they were sampled by a passive agent), but their *interpretation by screen* depends on the Driver's logged actions, whose timestamps are write times. Treat the per-phase mapping as approximate (± 30 s).

## 10. Requirements carried forward to the architecture phase

| # | Requirement (derived from observed *features*) | Feeds doc |
|---|---|---|
| R1 | Server-authoritative match state with durable storage, so an app restart resumes the live match | 09, 10, 12 |
| R2 | Matchmaking by rating/level with a short target time and a visible queue with cancel | 10 |
| R3 | Party/partner assignment for 2v2 and private tables with shareable codes/links | 10, 12 |
| R4 | Spectator channel with a viewer counter | 10, 12 |
| R5 | Idle detection → bot takeover → "I'm back" return, without stalling the match | 07, 08, 12 |
| R6 | Hidden-information handling (hands, announcements) so "catch" can be adjudicated by the server | 10 |
| R7 | Per-mode stats, global leaderboard, tiers, XP/level | 10, 11 |
| R8 | Localization incl. Arabic RTL; content bundles (Addressables) | 09 |
| R9 | Ads and consent, if used, outside the match flow | 09 |
| R10 | Telemetry/crash reporting with consent | 09 |
