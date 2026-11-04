# Facebook SDK Unity 7 compatibility app

This project stages the current Facebook Unity SDK source into a clean Unity
7000.0.0a7 project and runs concrete import and static compatibility checks. It
is both a small sample integration and a headless regression suite.

Run it from `xplat/unity-sdk`:

```bash
UNITY_PATH=/path/to/7000.0.0a7/Editor/Unity \
  ./Unity7Compatibility/run_unity7_compatibility.sh
```

`Packages/manifest.json` uses Unity's default package registry. Environments
that mirror Unity packages can set `UNITY_PACKAGE_REGISTRY_URL`; the runner
adds that scoped registry only to the temporary staged project.

Set `KEEP_PROJECT=1` to retain the fully staged sample project printed by the
runner. Open that directory in Unity Editor and press Play to see the integrated
Facebook SDK initialize through the Editor backend.

The run passes only when all of these checks pass:

- official `com.unity.external-dependency-manager@2.1.1` resolves and the
  Facebook `Dependencies.xml` is present;
- the SDK runtime, settings, and Editor source assemblies import without
  compiler errors;
- SDK-owned Unity 7/CoreCLR Project Auditor findings are zero.

## Backward-compatibility boundary

Unity 7-only APIs and lifecycle annotations are compiled only behind
`UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER`. Earlier Unity versions retain the
existing `AssetDatabase.ExportPackage`, `Assembly.Location`, and
`XmlReaderSettings.ProhibitDtd` paths. Object lookup keeps separate branches
for Unity versions before 2023.1, Unity 2023.1 through Unity 6, and Unity 7.
The runner compiles the retained BCL calls against Unity's bundled .NET 2.0
reference profile, then executes the smoke binary on bundled Mono before
starting the Unity 7 checks.

That smoke test verifies the legacy BCL surface rather than an older Unity
Editor runtime. It does not replace the full supported-version release matrix,
which remains required. The EDM 2.1.1 dependency is isolated to this
compatibility sample and does not change the SDK's production dependency graph.

The results are written to
`/tmp/facebook-sdk-unity7-compatibility-results` by default.

## What this does not claim

Unity's Editor backend is a mock and explicitly does not match production
platform behavior. This app therefore does not claim that native or live
Facebook operations work. The following need separate lanes:

- Android: Unity Android module, Gradle/EDM resolution, APK, device or emulator,
  configured Meta test app, login redirects, Graph, sharing, app links, app
  events, purchases, lifecycle, and permissions;
- iOS: macOS, Unity iOS module, Xcode/CocoaPods, device or simulator, URL and
  native `__Internal` callbacks;
- WebGL: Unity WebGL module, served browser origin, and JavaScript bridge;
- Windows cloud gaming: Windows runner and native DLLs;
- live Graph, login, app request, share, pay, SSO, and custom-update flows:
  credentials, test users, backend products/tournaments/placements, and user UI.

This runner validates raw source. A release gate must additionally rebuild and
import the distributed `Facebook.Unity.dll`/package so the actual compiled
artifact—not only Unity-recompiled source—runs the same lifecycle matrix.
