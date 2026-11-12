# 01 — Environment Report

**Phase:** 1 (Environment Discovery)
**Date:** 2026-11-12
**Status:** Complete. Awaiting approval before Phase 2.
**Conventions:** **[VERIFIED]** = directly observed with a tool, **[HYPOTHESIS]** = inferred, not yet confirmed, **[UNKNOWN]** = could not be determined. Tool provenance is given as *REA* (formal REA Evidence ID), *SDK tool* (Android SDK binary run via shell), or *shell*.

---

## 1. Summary

| Question | Answer |
|---|---|
| Reference app located? | Yes, `Ronda_+Moroccan+Card+Game_6.1.1_APKPure.xapk` in the project root (a **split bundle**, not a plain APK) |
| Package | `com.sandukhan.ronda`, v6.1.1 (code 43) |
| Built with Unity? | **Yes. Unity 6000.3.6f1, IL2CPP**, ARMv7 (32-bit) native libs only |
| Unity installed locally? | Yes. Editor **6000.6.4f1**, but **no Android build module** |
| REA usable? | Yes for archive/ADB/Frida/Hopper. **Android code tools (JADX) and apktool are unavailable** |
| Device for runtime observation? | **None attached.** Two x86_64 AVDs exist, but the app is ARM32-only (see §7) |
| Anything installed or modified? | No |

---

## 2. Host

| Item | Value | Source |
|---|---|---|
| OS | Ubuntu 26.04.1 LTS, kernel 7.0.0-43-generic, x86_64 | shell |
| CPU / RAM | 18 logical cores, 14 GiB RAM (~7.4 GiB available at probe time) | shell |
| Disk | 468 GB total, 179 GB free | shell |
| KVM | `/dev/kvm` read/write OK; emulator reports "KVM (version 12) is installed and usable" | shell |
| Working dir | `/home/taha/taha/projects/Ronda project` (not a git repository; contained only the XAPK before this report) | shell |

## 3. Toolchain

| Tool | Status | Version / Path |
|---|---|---|
| Java (JRE + `javac`) | present | OpenJDK 21.0.13 (`/usr/bin/java`); `JAVA_HOME` unset |
| adb | present | 1.0.41 / 37.0.1-15733141 at `~/Android/Sdk/platform-tools/adb` (REA `ev_3f30ef0d…`) |
| Android SDK | present | `ANDROID_HOME=~/Android/Sdk`. Platforms 35, 36, 37.0. Build-tools 36.0.0. cmdline-tools `latest`. Emulator. NDK 28.2.13676358 |
| SDK binaries used | present, **not on PATH** | `build-tools/36.0.0/{aapt,aapt2,apksigner,zipalign,d8,dexdump}`, `cmdline-tools/latest/bin/{apkanalyzer,avdmanager,sdkmanager}` |
| AVDs | present | `Medium_Phone`, `Pixel_10_Pro`: both **x86_64**, 2048 MB RAM, image `android-37.0/google_apis_playstore/x86_64` |
| Node / npm | present | Node v24.22.0 |
| Python | present | 3.14.4 |
| git | present | 2.53.0 |
| unzip, 7z, file, strings, objdump, readelf | present | system |
| **JADX / jadx-gui** | **absent** | no binary and no JADX MCP JAR found; `REA_JADX_MCP_JAR` unset |
| **APKTool** | **absent** | REA: `capability_unavailable`, set `REA_APKTOOL_COMMAND` |
| Frida CLI / frida-server | absent | no `frida`, `frida-ps` on PATH |
| Ghidra | absent | `GHIDRA_INSTALL_DIR` not set |
| Il2CppDumper / similar | absent | n/a (see §8 on whether we should use one) |
| tshark / Wireshark | absent | no packet analysis possible |
| .NET SDK | absent | not needed so far |
| Hopper Disassembler | **present** | `/opt/hopper/bin/Hopper` per REA provider diagnostics; not launched or license-checked |
| Docker | group membership only | user is in the `docker` group; installation not checked |

## 4. Unity installation

| Item | Value |
|---|---|
| Unity Hub | config at `~/.config/unityhub` (Hub binary not located on PATH) |
| Installed Editor | **6000.6.4f1 (12bfff696524)** at `~/Unity/Hub/Editor/6000.6.4f1/Editor/Unity` |
| Installed platform support | Linux standalone (bundled) + **WebGL** |
| Hub `modules.json` — selected | `webgl`, `documentation` |
| Hub `modules.json` — **not** selected | `android`, `android-open-jdk-17.0.18+8`, `android-sdk-ndk-tools`, `android-ndk-r27c`, `android-sdk-*`, `linux-il2cpp`, `ios`, … |
| Existing Unity project | none in the working directory |

**Consequence:** the editor cannot currently build an Android APK. The Android module (or a pointer to the existing `~/Android/Sdk`/NDK) will be needed before Phase 6 milestone 12. Not needed for analysis.

**Version note [VERIFIED]:** the reference game was built with **6000.3.6f1**, a different release line from the installed 6000.6.4f1. Matching the reference's engine version has no functional benefit for a clean-room rewrite; this is a Phase 4 decision, not a blocker.

## 5. Reference artifact

### 5.1 Container

| Item | Value |
|---|---|
| File | `Ronda_+Moroccan+Card+Game_6.1.1_APKPure.xapk` |
| Size | 75,201,800 bytes |
| SHA-256 | `efba61c93761431d56c124554ea7227558320d99e871a133764131ba263ed3a7` |
| Format | ZIP, stored (no compression). XAPK v2, repackaged by **APKPure** (each member carries the comment "ApkPure.com") |
| Integrity after analysis | SHA-256 re-checked: **unchanged** |

| Member | Size | SHA-256 |
|---|---|---|
| `com.sandukhan.ronda.apk` (base) | 50,577,881 | `fe7a5004e9512e4ed7483c6a5946a6de07b1821121e64c40cde7052bf6c6b59b` |
| `config.armeabi_v7a.apk` (ABI split) | 24,557,366 | `e68deb7a5d192a6075a754f5344c0cc8ad69b1648057224b78e766899525db85` |
| `icon.png` | 63,921 | `3f1de14a33748de432426544bba9c3001e5b030518fc9b80dcacde282c8b55d0` |
| `manifest.json` | 2,066 | `b7c8647885d4ee61eb2878121142e86c2f4b8353b7ccba8532813df1bdcf8116` |

Working copy for analysis: extracted into the session scratchpad (`…/scratchpad/xapk_extracted/`). The original XAPK was never modified. The scratchpad is session-scoped, so re-extract when needed.

### 5.2 Application identity (SDK tool `aapt2 dump badging`, `apkanalyzer`)

| Field | Value |
|---|---|
| Package | `com.sandukhan.ronda` |
| Version | 6.1.1 (versionCode 43) |
| minSdk / targetSdk / compileSdk | 25 / 36 / 36 |
| Orientation | `screenOrientation=6` → sensor-landscape (Android constant) |
| Install location | preferExternal |
| Declared features | Vulkan version, touchscreen, multitouch |
| Permissions | INTERNET, ACCESS_NETWORK_STATE, WAKE_LOCK, FOREGROUND_SERVICE, AD_ID, ADSERVICES_{AD_ID,ATTRIBUTION,TOPICS}, Play install-referrer, GSF read-gservices, plus one app-private dynamic-receiver permission |
| Custom URI scheme | `playronda` (also `genericidp`, `recaptcha`, `sms`, `https` from SDKs) |
| Signature | `apksigner verify`: **Verifies**, 1 signer, cert DN `CN=Android, OU=Android, O=Google Inc., …`, SHA-256 `103534dd…c310e`; source stamp present |

**Provenance caveat [UNKNOWN]:** the file comes from a third-party mirror. The signature verifies, but that shows internal consistency, not that it equals the Play Store build. If exactness matters, pull the APKs from a device with the official install (`pull_adb_package`).

### 5.3 REA inventory (REA)

Base APK opened and inventoried: **609 file entries, 554 graph nodes, 0 integrity contradictions, coverage complete** (`ev_32613d63…`, parent `ev_16f6939e…`). ZIP central directory only; signing-block bytes are not occurrences. Session closed afterwards.

## 6. Detected technology

### 6.1 Unity — **[VERIFIED]**

| Evidence | Finding |
|---|---|
| `data.unity3d` UnityFS header | engine version string **6000.3.6f1** |
| `libunity.so` strings | `6000.3.6f1 (bbb010bdb8a3)` |
| `libil2cpp.so` (40.3 MB) + `global-metadata.dat` (11.9 MB) | **IL2CPP scripting backend**. Metadata magic `0xFAB11BAF`, metadata version field `0x27` (39) |
| Native libs | `libunity`, `libil2cpp`, `libmain`, `libgame`, `lib_burst_generated` (Burst), `libswappywrapper` (Android Frame Pacing), `libc++_shared`; ELF32 ARM EABI5, **NDK r27c**, Android API 25 |
| Activity | `com.unity3d.player.UnityPlayerGameActivity` (the single gameplay activity; remaining 10 are SDK activities) |
| `boot.config` | `gfx-threading-mode=4`, `androidStartInFullscreen=1`, `android-preferred-data-location=1`, `gc-max-time-slice=3`, `hdr-display-enabled=0` |
| Addressables | `assets/aa/catalog.bin`, `settings.json`, 8 bundles in `aa/Android/` |
| Localization (Unity Localization package) | string tables: **Arabic, Darija (ar-MA), English, French, Spanish** |

### 6.2 Libraries and services — presence only

`ScriptingAssemblies.json` lists 156 managed assemblies. Presence in the build **does not prove runtime use**; many arrive as dependencies of umbrella packages.

| Area | Assemblies / components |
|---|---|
| Unity Gaming Services | `Unity.Services.{Core.*, Authentication, Authentication.PlayerAccounts, CloudCode, CloudSave, Leaderboards, Multiplayer, QoS, Wire.Internal}` and `assets/UnityServicesProjectConfiguration.json` |
| Unity networking | `Unity.Networking.Transport` |
| Firebase | `Firebase.{App, Auth, Database, RemoteConfig, Analytics, Crashlytics}` (C#) plus Firebase C++ native libs (Analytics, App, Auth, Crashlytics, Database, RemoteConfig) |
| Sockets | `websocket-sharp`, `unity-websocket-sharp` |
| Ads / consent | `GoogleMobileAds` (+ UMP), `com.google.android.gms.ads.*` |
| Sign-in (Android side) | Firebase Auth `GenericIdpActivity`, `RecaptchaActivity`, Google `SignInHubActivity`, AndroidX Credentials |
| UI / graphics / tooling | `UnityEngine.UIModule`, `UIElementsModule`, `Unity.TextMeshPro`, `Unity.InputSystem`, `Unity.2D.{Animation,IK,SpriteShape,PixelPerfect}`, `DOTween`, `Newtonsoft.Json`, `Unity.Burst` |

**Hypotheses on the multiplayer stack [HYPOTHESIS, unverified]:**
- H1: Firebase Auth + Realtime Database carry identity and room/game state.
- H2: UGS Authentication / Cloud Save / Leaderboards carry profile and ranking data.
- H3: a `websocket-sharp` channel carries real-time play.
- Any may be a leftover dependency. Only runtime observation, or a device-side network log, can decide. See §8 on scope.

### 6.3 Not yet determined

Rendering pipeline (URP/Built-in), uGUI vs UI Toolkit usage, audio architecture, scene/asset layout (inside `data.unity3d`, which was **not** unpacked), actual network endpoints and protocol.

## 7. REA capability assessment on this machine

REA 6.7.0 (`rea-agents`, served from the npx cache, registered in this Claude Code session). Evidence from `binary_session.tool_availability`.

| Capability | Status | Relevance to this project |
|---|---|---|
| `inspect_artifact`, `extract_artifact` | **available** | Used. ZIP/APK inventory |
| ADB family (`list_adb_devices`, `capture_adb_screen`, `read_adb_logcat`, `start_adb_app`, `pull_adb_package`, …) | **available** (needs a device) | Core of runtime observation: screenshots, logcat, app launch |
| Frida family (`start_frida_session`, …) | tools available; **no frida-server/CLI found** | Instrumentation is not needed for clean-room rules capture and carries scope concerns (§8) |
| `inspect_android_package/class/method`, `search_android_classes`, `trace_android_references` | **unavailable** (`provider_missing`) | Needs the JADX MCP JAR 0.7.1 via `REA_JADX_MCP_JAR`. Low value here: Java layer is mostly SDK code |
| `decode_android_resources` | **unavailable** | Needs apktool. Low value (resources are SDK UI; game UI is in Unity assets) |
| Hopper provider | **available** | Could open ARM32 `libil2cpp.so`; see §8, not recommended for game logic |
| Ghidra / IDA / Kuna / JEB | not configured | n/a |
| Managed `.NET` tools (`rea-dotnet-static`) | available | Not applicable. IL2CPP ships no managed game DLLs |
| macOS-only tools | unsupported host | n/a |

## 8. Limitations, risks and scope notes

1. **No runtime evidence yet.** Gameplay rules, screens, UX, animation and online behavior can only be established by running the game. No device is attached.
2. **Architecture mismatch [UNKNOWN, important].** The only native split is `armeabi-v7a`. The available AVDs are x86_64 on an Android 37 image. Whether that image can execute 32-bit ARM code (via translation) is **unverified**; it may not. A physical Android phone over ADB is the dependable route.
3. **Java-layer analysis is the wrong lens for this game.** Game logic is C# compiled by IL2CPP into `libil2cpp.so`, with names and strings in `global-metadata.dat`. JADX would show only the activity shell and SDKs.
4. **Clean-room boundary.** I recommend we **do not** run IL2CPP dumpers or decompile `libil2cpp.so` to recover game logic or algorithms. Rules come from observed play, UI from screenshots, and design from original work. Static work is limited to identifying technology (as above). This follows your instruction and avoids both copyright and terms-of-service exposure.
5. **Assets not extracted.** `data.unity3d` (21 MB) holds the textures, sprites and audio. I will not unpack or reuse them. Screenshots are the visual reference.
6. **Network capture out of scope by default.** No tshark/proxy is installed, and traffic is TLS. I will not intercept or defeat transport security without your explicit decision. Backend design will rest on public documentation and our own requirements.
7. **Provenance.** Third-party XAPK; version 6.1.1 may differ from the current Play build, and online servers may require the current version.
8. **Unity Android module missing** (§4). Needed only at implementation time.

## 9. Actions taken in this phase

- Read-only probes of the host, SDK and Unity install.
- Extracted the XAPK into a new empty scratchpad directory (original untouched, hash verified).
- Opened and closed one REA session on the base APK. `list_adb_devices` may have started the local ADB server.
- Wrote this file. **Nothing was installed and no configuration was changed.**

## 10. Decisions needed before Phase 2

| # | Decision | Recommendation |
|---|---|---|
| D1 | **Runtime source for gameplay observation.** (a) a physical Android phone with USB debugging; (b) test whether an AVD can run the ARM32 split; (c) you play and send screen recordings and notes | (a). I will test (b) only with your go-ahead, since it launches an emulator and installs the app there |
| D2 | **Install JADX MCP JAR 0.7.1 and apktool?** (downloads, no system changes) | **Skip** for now: low value for a Unity IL2CPP game. Revisit if a concrete Java-layer question arises |
| D3 | **Static-analysis boundary** (§8.4) | Confirm the "identification only, no IL2CPP dumping" policy |
| D4 | **Network observation boundary** (§8.6) | Confirm no traffic interception |
