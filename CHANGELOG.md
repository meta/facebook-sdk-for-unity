# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [19.0.0]

### Added
- Starting on 10/31/2026, Screen Title and AppLink URL will be included with events logged with Facebook SDK. You can disable additional metadata by setting AutoLogAppMetadataEnabled false or by turning off "Automatically include more app event details" in Events Manager.

- FB SDK will automatically send sampled information about your app, such as the layout of elements on app screens. This information helps Meta improve ads systems and build new features. It is not used for ads delivery. You can disable the feature by setting the LogProductImprovementEnabled false or by turning off "Automatically send more app information" in Events Manager.

### Changed
- Bumped SDK to 19.0.0
- Bumped iOS SDK version to 19.0.0
- Bumped Android SDK version to 19.0.0
- **Breaking:** the minimum supported iOS deployment target is now 15.0, raised
  from 12.0 by Facebook iOS SDK `19.0.0`. Set **Target minimum iOS Version** to
  15.0 or higher under Player Settings before building for iOS; a lower value
  fails CocoaPods dependency resolution with a "higher minimum deployment
  target" error. The Android minimum is unchanged.
- If the `AutoLogAppEventsEnabled` flag is set as false, Automatic App Event
  Logging will be disabled even if it is enabled in Events Manager.
- iOS: `LoginWithTrackingPreference` now defaults to fast app switch enabled,
  matching the native iOS SDK and the classic `LogInWithReadPermissions` path.
  Apps can still opt out via `FB.Mobile.SetFastAppSwitchEnabled(false)`. Behavior
  change: eligible logins app-switch to the Facebook app instead of the web flow.

## [18.1.1]

### Added
- Unity 6 support on iOS for the Swift Xcode project type, including Login
  redirects, deep links, and app links.
- Unity 6 support on Android.

### Changed
- Bumped the Android External Dependency Manager to `1.2.188` — required for
  the Unity 6 Android support above.
    - Upgrading an existing project requires deleting
      `Assets/ExternalDependencyManager/Editor/1.2.166/` and the old
      `gpsr`-labeled `.aar`/`.jar` files under `Assets/Plugins/Android/`, then
      restarting the editor.

## [18.1.0]

### Added
- `FB.Mobile.RefreshLimitedLogin(callback, fallbackPolicy)` to refresh the profile
  data of a Limited Login user without a full re-login (iOS only). Added the
  `RefreshFallbackPolicy` enum (`Automatic`/`SilentOnly`/`ExplicitOnly`/`DirectOnly`)
  mirroring the native iOS `FBSDKRefreshFallbackPolicy`. On success the refreshed
  data is available via `FB.Mobile.CurrentProfile()`. No-op on Android.
- `FB.Mobile.LoginWithSSO(permissions, callback)` to start "Login with Facebook"
  SSO via an app-switch to the Facebook app, with a fallback dialog when no
  compatible Facebook app is installed (Android only). No-op on iOS.

### Changed
- Bumped the iOS dependency to Facebook iOS SDK `~> 18.1.0` — the first release
  containing `LoginManager.refreshLimitedLogin` (required by the iOS API above).
- Bumped the Android dependency to Facebook Android SDK `18.3.0` — the first
  release containing `FBLoginSSOLauncher` (required by the Android API above).

## [18.0.2]

### Changed
- Bumped SDK to 18.0.1
- Bumped iOS SDK version to 18.0.2

## [18.0.0]

### Added
- Native iOS SDK
    - Improved support for in-app purchase events for Original StoreKit APIs and StoreKit 2 APIs
- Native Android SDK
    - Improved support for in-app purchase events for Google Play Billing Libraries 5 through 7
    - Fixed potential Ad-Services namespace conflict
    - Upgraded a variety of dependencies including Kotlin, Gradle, and the Android Gradle Plugin
    - Modified how we pass the content id of purchases in custom events

### Changed
- Bumped SDK to 18.0.0
- Bumped iOS SDK version to 18.0.0
- Bumped Android SDK version to 18.0.3

## [17.0.2]

### Updated
The release has been deprecated due to Unity support issues caused by removing “fb” prefix from previous version, please use other releases.

### Changed
- Bumped SDK to 17.0.2
    - Removed "fb" prefix of ApplicationId in Android Manifest file
    - Fixed Mobile Install Referrer issue

## [17.0.1]

### Changed
- Bumped SDK to 17.0.1
- Bumped iOS SDK version to 17.0.1
    - Updated privacy manifests

## [17.0.0]

### Added
- Native iOS SDK
    - Added privacy manifests.
- Native Android SDK
    - Supported more integrity use cases.

### Changed
- Bumped SDK to 17.0.0
- Bumped iOS SDK version to 17.0.0
- Bumped Android SDK version to 17.0.0
- Bumped Graph API version to 17.0.0

## [16.0.2]

### Fixed
- Fixed a bug where user did not remain logged in on subsequent sessions

### Changed
- Bumped SDK to 16.0.2
- Bumped Android SDK version to 16.0.1
- Bumped iOS SDK version to 16.0.1

## [16.0.1]

### Fixed
- Removed fetching app configuration data before calling FB.Init on iOS/Android platforms.
- Updated copyright in some files.

### Changed
- Bumped SDK to 16.0.1

## [16.0.0]

### Added
- Windows platform methods
    - FB.Windows.CreateReferral
    - FB.Windows.GetDataReferral
- Android Cloud
    - Android Cloud Unity example
    - Subscriptions payments methods

### Fixed
- Payments price conversion to double
- Improved mobile orientation enum for Unity example
- Improved mobile Tournaments example
- Changed folder structure for External Dependency Manager dlls

### Changed
- Bumped SDK to 16.0.0
- Bumped Android SDK versions to 16.0.0
- Bumped iOS SDK versions to 16.0.0
- Bumped Graph API version to 16.0
- Updated Windows SDK Dlls

## [15.1.0]

### Changed
- Bumped iOS SDK version to 15.1
- Bumped android SDK versions to 15.1

## [15.0.0]

### Fixed
- Android SDK path detection on latest Unity versions
- Duplicate Event System in Unity SDK example
- Fixed DLLs configuration in asset importer
- Fixed ConsoleBase.cs script for Unity 2021 and above.

### Changed
- Bumped windows SDK versions to 1.0.15
- Bumped Graph API version to 15.0
- Bumped iOS SDK version to 15.0.0
- Bumped android SDK versions to 15.0.1

## [14.1.0]

### Added
- Windows platform methods
    - FB.Windows.SetSoftKeyboardOpen

### Fixed
- Issue with new Unity Input System and UnitySDK Example.
- Issue with misconfiguration of each DLL when Unity import the package.
- Fixed some example scenes background color
- Fixed Windows platform loggin issue

### Changed
- Bumped native SDK versions to 14.1.0
- Updated Windows platform DLLs

## [14.0.0]

### Added
- Windows platform methods
    - FB.Windows.SetVirtualGamepadLayout
    - FB.GetUserLocale
- Windows example
    - Virtual Gamepad Layout Test
    - Physical Gamepad Test

### Changed
- Bumped windows SDK versions to 1.0.14
- Bumped native SDK versions to 14.0
- Bumped Graph API version to 14.0
- Facebook Settings
  - Client Token is automatically added to Podfile and AndroidManifest.xml
  - Client Token is no longer marked as optional in the editor

### Fixed
- GraphAPI version
- IAP empty description field
- Podfile creation for iOS platform

## [13.2.0]

### Added
- Added tournament APIs for the native mobile SDKs

### Changed
- Bumped native SDK versions to 13.2

## [12.0.0] - 2021-12-09

### Added
- OIDC support for login
- Added APIs for Instant Games context dialogs
- Added Windows platform (Unity standalone build)
    - Windows platform methods:
        FB.Init
        FB.LogInWithReadPermissions
        FB.LogInWithPublishPermissions
        FB.LogOut
        FB.API
        FB.GetCatalog
        FB.Purchase
        FB.GetPurchases
        FB.ConsumePurchase
        FB.CurrentProfile
        FB.LoadRewardedVideo
        FB.ShowRewardedVideo
        FB.LoadInterstitialAd
        FB.ShowInterstitialAd
        FB.OpenFriendDialog
        FB.GetFriendFinderInvitations
        FB.DeleteFriendInvitation
        FB.ScheduleAppToUserNotification
        FB.PostSessionScoreAsync
        FB.CreateTournamentAsync
        FB.GetTournament
        FB.ShareTournament
        FB.PostTournamentScore
        FB.UploadImageToMediaLibrary
        FB.UploadVideoToMediaLibrary

- Current public session properties:
    AccessToken
    FB.IsInistialized
    FB.IsLoggedIn
    FB.GraphAPIVersion

- App events:
    FB.ActiveApp
    FB.LogAppEvent
    FB.LogPurchase

### Changed
- Bumped native SDK versions to 12.1.0
- Bumped Graph API version to 12.0

### Removed
- Removed methods relating to user properties as they have been removed from the native mobile SDK versions
- Removed gameroom support

## [11.0.0] - 2021-06-05

### Added

- Added tournaments APIs for cloud game
- Added Limited Login support for `user_hometown`, `user_location`, `user_gender` and `user_link` permissions under public beta.

### Changed

- Bumped native SDK version to 11.0.0
- Bumped Graph API version to 11.0

### Deprecated

- Deprecated UpdateUserProperties API

## [9.2.0] - 2021-04-25

### Added

- Added AEM (Aggregated Events Measurement) support under public beta.

### Fixed

- Fix `IOSFBEnableProfileUpdatesOnAccessTokenChange` build issue

## [9.1.0] - 2021-04-06

### Added

- Added Limited Login support for `user_friends`, `user_birthday` and `user_age_range` permissions under public beta.
- Shared `Profile` instance will be populated with `FriendIDs`, `Birthday` and `AgeRange` fields using the claims from the AuthenticationToken. (NOTE: `FriendIDs`, `Birthday` and `AgeRange` fields are in public beta mode)
- Added `EnableProfileUpdatesOnAccessTokenChange` as part of fixing a bug where upgrading from limited to regular login would fail to fetch the profile using the newly available access token.

## [9.0.0] - 2021-01-10

### Added

- Limited Login. See the [docs](https://developers.facebook.com/docs/facebook-login/ios/limited-login/) for a general overview and implementation details.

## [8.1.1] - 2020-10-25

### Fixed

- Add use_frameworks! to Podfile
- Avoid adding swift standard libs

## [8.1.0] - 2020-10-14

### Fixed

- Fixed AndroidSdk path detection in Unity 2019 and above

## [8.0.0] - 2020-09-27

### Added

- Added method `SetAdvertiserTrackingEnabled` to overwrite the `advertiser_tracking_enabled` flag

### Changed

- Bumped native SDK version to 8.0.0
- Bumped Graph API version to 8.0

### Fixed

- Fixed lock up issue in Facebook Login

## [7.21.2] - 2020-08-14

### Added

- Support customized Android Keystore path

### Fixed

- Fix iOS build issue for swift libraries
- Deprecate old GUI.ModalWindow API

## [7.21.1] - 2020-06-25

### Changed

- Bump native SDK version to 7.1.1

## [7.21.0] - 2020-06-20

### Added

- Introduce DataProcessingOptions

### Deprecated

- Remove UserProperties API

## [7.20.0] - 2020-06-15

### Changed

- Bump native SDK version to 7.0.1

### Added

- Add Express Login

## [7.19.2] - 2020-04-08

### Fixed

- Fix FeedShare issue: https://github.com/facebook/facebook-sdk-for-unity/issues/284
- Fix crash when AccessToken has no graph domain

## [7.19.1] - 2020-03-22

### Fixed

- Fix Login issue in Editor mode

## [7.19.0] - 2020-03-11

### Added

- Support for Gaming Video Uploads
- Support for Gaming Image Uploads
- Support for Gaming Friend Finder

## [7.18.1] - 2020-02-03

### Fixed

- Fix exported iOS project build issue
- Fix Android install referrer issue
- Fix GetUserID issue
- Remove AndroidJNIHelper log

### Changed

- Bump Google Playservices Resolver to v1.2.135

## [7.18.0] - 2019-10-11

### Fixed

- Fix Android logout issue
- Fix the issue of multiple AppRequest instances when changing screen orientation
- Fix GetUserID issue

### Changed

- Remove Bolts framework

## [7.17.2] - 2019-07-18

### Fixed

- Various bug fixes

## [7.17.1] - 2019-07-05

### Other

- Facebook Developer Docs: [Changelog v7.x](https://developers.facebook.com/docs/unity/change-log)

<!-- Links -->
[19.0.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-18.1.1...HEAD
[18.1.1]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-18.1.0...sdk-version-18.1.1
[18.1.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-18.0.0...sdk-version-18.1.0
[18.0.2]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-18.0.0...sdk-version-18.0.2
[18.0.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-17.0.2...sdk-version-18.0.0
[17.0.2]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-17.0.1...sdk-version-17.0.2
[17.0.1]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-17.0.0...sdk-version-17.0.1
[17.0.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-16.0.2...sdk-version-17.0.0
[16.0.2]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-16.0.1...sdk-version-16.0.2
[16.0.1]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-16.0.0...sdk-version-16.0.1
[16.0.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-15.1.0...sdk-version-16.0.0
[15.1.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-15.0.0...sdk-version-15.1.0
[15.0.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-14.1.0...sdk-version-15.0.0
[14.1.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-14.0.0...sdk-version-14.1.0
[14.0.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-13.2.0...sdk-version-14.0.0
[13.2.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-12.0.0...sdk-version-13.2.0
[12.0.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-11.0.0...sdk-version-12.0.0
[11.0.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-9.2.0...sdk-version-11.0.0
[9.2.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-9.1.0...sdk-version-9.2.0
[9.1.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-9.0.0...sdk-version-9.1.0
[9.0.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-8.1.1...sdk-version-9.0.0
[8.1.1]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-8.1.0...sdk-version-8.1.1
[8.1.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-8.0.0...sdk-version-8.1.0
[8.0.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-7.21.2...sdk-version-8.0.0
[7.21.2]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-7.21.1...sdk-version-7.21.2
[7.21.1]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-7.21.0...sdk-version-7.21.1
[7.21.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-7.20.0...sdk-version-7.21.0
[7.20.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-7.19.2...sdk-version-7.20.0
[7.19.2]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-7.19.1...sdk-version-7.19.2
[7.19.1]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-7.19.0...sdk-version-7.19.1
[7.19.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-7.18.1...sdk-version-7.19.0
[7.18.1]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-7.18.0...sdk-version-7.18.1
[7.18.0]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-7.17.2...sdk-version-7.18.0
[7.17.2]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-7.17.1...sdk-version-7.17.2
[7.17.1]: https://github.com/facebook/facebook-sdk-for-unity/compare/sdk-version-7.1.0...sdk-version-7.17.1
