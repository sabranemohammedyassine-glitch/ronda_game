# Product 05 — L1: Local Data and Persistence Foundation

**Status:** IMPLEMENTED, awaiting owner review · **Date:** 2026-11-17
**Scope:** data models and persistence only. There is no UI, no scene change and no gameplay change. Nothing in the running game calls this code yet; L2 connects it.
**Approvals applied:** D1–D8 (see `01_product_decisions.md`, "Owner approvals").

---

## 1. Where the code lives

- **Runtime:** `ronda_game/Assets/Zellige/Scripts/App/` (namespace `Zellige.App`).
  - The folder belongs to the existing `Zellige.Game` assembly, so no asmdef was added or edited.
  - The rules engine (`Zellige.Core`) and the AI (`Zellige.AI`) do not reference it.
- **Tests:** `ronda_game/Assets/Zellige/Tests/EditMode/LocalData/`, inside the existing test assembly.

| File | Contents |
|---|---|
| `MatchMode.cs` | `MatchMode` (VsComputer = 1, Online1v1 = 2, Online2v2 = 3), `MatchOutcome` (Victory = 1, Defeat = 2), `MatchModes` helpers (`IsPlayable`, `IsOnline`, `IsDefined`, `NameKey`) |
| `PlayerProfile.cs` | `PlayerProfile`, `AvatarCatalog`, `NameError` |
| `GameSettings.cs` | `GameSettings`, `CardPlayMode` (TwoStep = 1, OneTap = 2) |
| `MatchHistory.cs` | `MatchRecord`, `MatchStatistics`, `MatchHistory`, `RecordResult` |
| `MatchSession.cs` | `MatchConfig`, `MatchState`, `MatchSession`, `MatchHandoff` |
| `AtomicFileStore.cs` | Safe file reads and writes (`ReadStatus`) |
| `LocalDataStore.cs` | Load, validate, repair and save the three documents; `CompleteMatch` / `AbandonMatch` (`LoadStatus`) |
| `AppData.cs` | The app's single store, under `Application.persistentDataPath/save/`, created lazily |
| `TextKeys.cs` | `TextKeys`, the English table and `TextCatalog` |
| `Timestamps.cs` | UTC ISO-8601 formatting and parsing |

**No frameworks were added:**
- no DI container, no packages, no ScriptableObjects;
- no `PlayerPrefs`, no networking APIs;
- no `DontDestroyOnLoad`.

The static state is limited to `MatchHandoff` and `AppData`. Both are cleared at `RuntimeInitializeLoadType.SubsystemRegistration`, so it never survives between Editor play sessions.

---

## 2. Data models

### 2.1 Profile (`profile.json`)

| Field | Type | Rule |
|---|---|---|
| `schemaVersion` | int | 1 |
| `displayName` | string | Spaces trimmed; runs of spaces collapsed; 1–16 characters; Latin letters, digits, space and `- _ .` only. The default is "Player", taken from the text table. |
| `avatarId` | string | Must be in `AvatarCatalog.Ids`: `avatar_01` (default) and `avatar_02`, the two existing sprites |
| `createdUtc` | ISO-8601 UTC | Set once, on first launch |
| `modifiedUtc` | ISO-8601 UTC | Updated when the name or avatar actually changes |

- Changes go through `TrySetDisplayName` and `TrySetAvatar`. An invalid value changes nothing and returns the reason; each reason has a text key.
- There is no account, XP, level, currency, rank or achievement (D7).

### 2.2 Settings (`settings.json`)

| Field | Default | Why this default |
|---|---|---|
| `soundEnabled` | `true` | Today the match `AudioSource` starts unmuted |
| `cardPlayMode` | `TwoStep` (1) | Today's interaction: the first tap lifts the card and previews its captures, the second tap plays it |

L1 **stores** these values only. `SoundFx` and `GameController` do not read them yet, so the card-play behaviour is unchanged.

### 2.3 Match history (`match_history.json`)

- **`MatchRecord`** describes one **completed** match from the local player's side:
  - `matchId`, `mode`, `opponentName`;
  - `startedUtc`, `completedUtc`;
  - `outcome`, `playerScore`, `opponentScore`, `decksPlayed`.
- **No Draw outcome.** The engine always produces a winner: an exact tie at 41 plays another deck (H-3).
- **Retention:**
  - `records` holds the **latest 20**, newest first;
  - `lifetime` (`MatchStatistics`) holds `completed`, `victories`, `defeats`, `abandoned`, `bestWinningMargin` and `highestScore`.
- **Derivation:**
  - `MatchStatistics.Add(record)` is the **only** place a record is counted.
  - It feeds both the stored lifetime totals and `History.Recent`, which is recomputed from the retained records, so both always follow the same rules.
  - A test checks that the two agree while there are 20 records or fewer.
- **Abandoned matches (D6):**
  - they never create a record;
  - they never count as completed or as a defeat;
  - they do not affect the win rate (victories ÷ completed);
  - they only increase `abandoned`.
- **Online later:**
  - `mode` already has the online values, and the per-side scores can hold team totals for 2 vs 2;
  - no online record can be created today, because no online config can exist (§2.4).
- **Extra statistics:** `bestWinningMargin` and `highestScore` come from D-L-11 (Statistics screen, spec §4.4). No other statistics were added.

### 2.4 Match configuration and lifecycle

- **`MatchConfig`** (immutable): `Mode`, `MatchId` (a new GUID each time) and `OpponentName` ("Nadia").
  - It carries **no rule options** (D-L-20).
  - `MatchConfig.TryCreate(mode)` succeeds only for `VsComputer`. Online 1v1/2v2 return false with `mode.not_available`, and no object is created (D1).
- **`MatchSession`** moves through `Created → InProgress → Completed | Abandoned`.
  - Both end states are final.
  - `TryComplete` builds the record from `MatchEndedEvent.Winner` / `Scores` and the last deck round.
  - Invalid input (wrong seat, scores or deck count) throws and leaves the session unchanged.
- **`LocalDataStore.CompleteMatch` / `AbandonMatch`** are the only ways a result reaches the history. They provide exactly-once behaviour:
  - a second completion → `Duplicate`;
  - abandon after completion → refused;
  - complete after abandon → refused;
  - the history also refuses a known `matchId`.
- **`MatchHandoff`** carries the Lobby → Game hand-off.
  - `Offer(config)` accepts playable configs only.
  - `TakePendingOrDefault()` returns the pending config **and clears it**.
  - Without a pending config it returns a fresh Me vs Computer config, which is today's behaviour when `Game.unity` is opened directly.
  - "Play again" therefore always gets a new match ID.

---

## 3. Persistence

- **Location:** `Application.persistentDataPath/save/`, one JSON file per document.
  - Serialisation uses Unity's `JsonUtility`: built in, works under IL2CPP, and uses private `[SerializeField]` fields.
  - Enums are stored as integers, and the integer values are fixed.
- **Atomic write** (`AtomicFileStore`):
  1. Write `name.json.tmp` and flush it to disk.
  2. Move `name.json` to `name.json.old`.
  3. Move `.tmp` to `name.json`.
  4. Delete `.old`.
- **Recovery on the next read:**
  - no file but an `.old` copy → restore it;
  - both a file and an `.old` copy → keep the file and delete `.old`;
  - a stray `.tmp` → delete it.
  - A failed write puts the previous file back and returns `false`.
- **Load outcomes** (`LoadStatus`):

  | Situation | Status | What happens |
  |---|---|---|
  | No file | `Created` | Defaults are created and written |
  | Valid file | `Loaded` | Used as is; **the file is not rewritten** |
  | Readable, but some fields invalid | `Repaired` | Invalid fields → defaults, valid fields kept, original copied to `name.corrupt.json`, repaired file saved |
  | Unparseable, empty, not an object, or no `schemaVersion` | `Reset` | Original copied to `name.corrupt.json`, defaults written |
  | `schemaVersion` newer than this build (e.g. after a downgrade) | `Protected` | Defaults in memory; **saving disabled**; file untouched |
  | I/O or permission error | `Protected` | Same: the file is never overwritten |

- **History repair:**
  - drops invalid and repeated records;
  - trims to 20;
  - rebuilds the totals from the records if the stored totals contradict them, keeping a valid `abandoned` count.
- **Schema migration:**
  - every document carries `schemaVersion` (1 today);
  - a future version 2 migrates version-1 data in `LocalDataStore.Load`, where the place is marked;
  - version 0 (missing) is treated as corrupt;
  - two tests fix the version-1 field names and values, so an accidental rename fails the build.
- **What is not stored:** credentials, tokens and personal data beyond the chosen display name. Nothing leaves the device.
- **`PlayerPrefs`:** not used, so there is a single source of truth.

---

## 4. Tests (78 new, in `Tests/EditMode/LocalData/`)

| File | Tests | Covers |
|---|--:|---|
| `ProfileTests` | 20 | First-launch default; save and reload; a valid file is never rewritten; 5 valid and 9 invalid names; invalid rename or avatar changes nothing; field-level repair with backup; unknown avatar |
| `SettingsTests` | 8 | Defaults; save and reload; valid and invalid card-play values; repair of an undefined value on disk |
| `MatchHistoryTests` | 10 | Victory and defeat recording; latest 20, newest first, with lifetime 25; derived statistics; win rate; abandoned excluded; duplicates; complete/abandon exclusivity; invalid records; repair of a bad history file |
| `MatchSessionTests` | 16 | Config creation; online and unknown modes refused; mode flags; full lifecycle; final states; invalid results; clock moved back; hand-off default; hand-off consumed once; separate sessions per match |
| `PersistenceTests` | 19 | Missing files; 7 kinds of corrupt file; corruption isolated to one file; newer-version file protected; failed save keeps the old file; failed history save reported; both interrupted-write cases; locked file protected (Windows Editor); use before loading; version-1 files load; saved field names; timestamps |
| `TextCatalogTests` | 5 | Every key has English text; keys are unique; missing-key marker; every mode and name error has text; default name |

- **Isolation:** each test uses its own folder under the system temp folder and deletes it afterwards. No test touches `AppData` or `persistentDataPath`.
- **Determinism:** times are fixed and IDs are supplied by the tests.

---

## 5. Localisation foundation (D8)

- **Keys:** `TextKeys` holds stable keys of the form `area.item`.
- **Lookup:** `TextCatalog.Get(key)` returns English text, or `[key]` when a key is missing, which is visible but never crashes.
- **Data code returns keys, never sentences.** This covers name errors, the reason a mode is refused, mode names and the default name.
- **Adding a language later** means adding one table and a selection point in `TextCatalog`. Screens do not change.
- **Not migrated in L1:** the existing match-screen strings. That would mean editing `GameScreen` and `QuitDialog`.

---

## 6. Contradictions found between the documents and the approvals

| # | Documents said | Approval / finding | Resolution |
|--:|---|---|---|
| C1 | D-L-03, spec §4.1–4.2, plan L2: Home → **Play** → Mode selection | **D2:** the three modes go directly on Home | D-L-03 superseded (recorded in 01). Spec and plan to be revised at L2. |
| C2 | Spec §4.2 / D-3: "Online — coming later" | **D1:** "Coming Soon" | `mode.coming_soon` = "Coming Soon" |
| C3 | Spec §4.4 / AC-L-05: history rows include **Abandoned** | **D6** and the L1 brief: keep the latest 20 **completed** matches | History holds completed matches only; abandoned is a separate counter |
| C4 | Plan R6, spec §5: changing `productName` moves `persistentDataPath` | **On Android the path comes from the package ID** (`com.pyramidStudio`), not the product name. The product name affects only the Editor, Windows and macOS paths. | The display name (D-L-18) can still change on Android without losing data. **Changing the package ID would orphan saved data.** |
| C5 | Plan L1: "fix `productName` if approved" | The L1 brief forbids changing the display name or package | Not done; D-L-18 stays open |
| C6 | Plan §2.2 names: `AppSession`, `MatchRequest`, `PlayerStats`, `MatchRecorder`, `IDataStore` / `JsonFileStore` / `MemoryStore`, `Strings` | Simpler shapes chosen while implementing | `AppData`, `MatchConfig`, `MatchHistory` / `MatchStatistics`, `LocalDataStore.CompleteMatch` / `AbandonMatch`, `AtomicFileStore`, `TextKeys` / `TextCatalog`. There is no in-memory store: tests use real temp folders, so the real file code is exercised. |
| C7 | Spec §4.3: names allow "letters" | The font (LiberationSans SDF) has no Arabic glyphs (R10) | Latin letters only for now (see §8) |

---

## 7. Unchanged (verified)

- **Code and assets:** no existing script, scene, prefab, asmdef, asset, project setting or test was modified. `git diff` shows only the Unity Version Control caches (`ronda_game/.plastic/*`), which the plugin rewrites by itself.
- **Game scene and behaviour:** `Game.unity` is untouched, and `GameController` does not call the new code, so the game behaves exactly as before.
- **Tests:** the 175 original tests pass, including the two golden-master fingerprints.

---

## 8. Open decisions for the owner (before or during L2/L3)

| # | Question | Recommended default |
|--:|---|---|
| L1-1 | Should the Statistics screen show **lifetime** totals, or only the **last 20** matches? | Lifetime totals, with the last 20 in the history list. Both are available. |
| L1-2 | Keep a separate **abandoned** counter (D-L-11), or record nothing at all for abandoned matches? | Keep it, shown apart from wins and losses. |
| L1-3 | In-match **New game** before the end: does it count as abandoned (D6)? | Yes, after the same confirmation as Leave. |
| L1-4 | **App killed mid-match:** record nothing (current), or save an "in progress" marker so the next launch counts an abandon? | Record nothing in v1. |
| L1-5 | **Name characters:** Latin only until a font with Arabic glyphs (and RTL support) is approved? | Yes. |
| L1-6 | **D-L-18:** the app display name, "Zllige_ronda_game" today | Owner to name. On Android, data is safe across a rename (C4). |

---

## 9. Next step: L2 (not started)

L2 is the navigation skeleton. It needs owner approval first.

1. **Lobby scene:** `Lobby.unity` built by a `LobbyUiBuilder` editor script. Home shows the three mode cards directly (D2), with the two online cards locked as "Coming Soon" (D1), plus the quit dialog. The scene goes first in Build Settings (D3).
2. **Starting a match:**
   - `GameController.Start` calls `MatchHandoff.TakePendingOrDefault()` and creates a `MatchSession`.
   - On `MatchEndedEvent` it calls `AppData.Store.CompleteMatch(...)`.
   - On "Leave the match?" → Leave, it calls `AbandonMatch(...)` and loads the Lobby (D5).
3. **Result overlay:** a **Lobby** button (D4).
4. **Back button:** one handler per scene, so the existing `QuitDialog` and the new Leave dialog never both react to the same press.
5. **Settings wiring:** the sound toggle reads and writes `GameSettings`.

L2 modifies `GameController`, `GameScreen`, `GameUiBuilder`, `QuitDialog` and `Game.unity`, all additively. Per the L1 rules, it needs owner approval first.
