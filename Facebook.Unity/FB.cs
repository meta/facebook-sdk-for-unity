/**
 * Copyright (c) 2014-present, Facebook, Inc. All rights reserved.
 *
 * You are hereby granted a non-exclusive, worldwide, royalty-free license to use,
 * copy, modify, and distribute this software in source code or binary form for use
 * in connection with the web services and APIs provided by Facebook.
 *
 * As with any software that integrates with the Facebook platform, your use of
 * this software is subject to the Facebook Developer Principles and Policies
 * [http://developers.facebook.com/policy/]. This copyright notice shall be
 * included in all copies or substantial portions of the software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Facebook.Unity
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Facebook.Unity.Windows;
    using Facebook.Unity.Canvas;
    using Facebook.Unity.Editor;
    using Facebook.Unity.Mobile;
    using Facebook.Unity.Mobile.Android;
    using Facebook.Unity.Mobile.IOS;
    using Facebook.Unity.Settings;
    using UnityEngine;

    /// <summary>
    /// Static class for exposing the facebook integration.
    /// </summary>
    public sealed class FB : ScriptableObject
    {
        private const string DefaultJSSDKLocale = "en_US";
        private static IFacebook facebook;
        private static bool isInitCalled = false;
        private static string facebookDomain = "facebook.com";
        private static string gamingDomain = "fb.gg";
        private static string graphApiVersion = Constants.GraphApiVersion;

        private delegate void OnDLLLoaded();

        /// <summary>
        /// Gets the app identifier. AppId might be different from FBSettings.AppId
        /// if using the programmatic version of FB.Init().
        /// </summary>
        /// <value>The app identifier.</value>
        public static string AppId { get; private set; }

        /// <summary>
        /// Gets the app client token. ClientToken might be different from FBSettings.ClientToken
        /// if using the programmatic version of FB.Init().
        /// </summary>
        /// <value>The app client token.</value>
        public static string ClientToken { get; private set; }

        /// <summary>
        /// Gets or sets the graph API version.
        /// The Unity sdk is by default pegged to the latest version of the graph api
        /// at the time of the SDKs release. To override this value to use a different
        /// version set this value.
        /// <remarks>
        /// This value is only valid for direct api calls made through FB.Api and the
        /// graph api version used by the JavaScript SDK when running on the web. The
        /// underlyting Android and iOS SDKs will still be pegged to the graph api
        /// version of this release.
        /// </remarks>
        /// </summary>
        /// <value>The graph API version.</value>
        public static string GraphApiVersion
        {
            get
            {
                return FB.graphApiVersion;
            }

            set
            {
                FB.graphApiVersion = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a user logged in.
        /// </summary>
        /// <value><c>true</c> if is logged in; otherwise, <c>false</c>.</value>
        public static bool IsLoggedIn
        {
            get
            {
                return (facebook != null) && FacebookImpl.LoggedIn;
            }
        }

        /// <summary>
        /// Gets a value indicating whether is the SDK is initialized.
        /// </summary>
        /// <value><c>true</c> if is initialized; otherwise, <c>false</c>.</value>
        public static bool IsInitialized
        {
            get
            {
                return (facebook != null) && facebook.Initialized;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Facebook.Unity.FB"/> limit app event usage.
        /// If the player has set the limitEventUsage flag to YES, your app will continue
        /// to send this data to Facebook, but Facebook will not use the data to serve
        /// targeted ads. Facebook may continue to use the information for other purposes,
        /// including frequency capping, conversion events, estimating the number of unique
        /// users, security and fraud detection, and debugging.
        /// </summary>
        /// <value><c>true</c> if limit app event usage; otherwise, <c>false</c>.</value>
        public static bool LimitAppEventUsage
        {
            get
            {
                return (facebook != null) && facebook.LimitEventUsage;
            }

            set
            {
                if (facebook != null)
                {
                    facebook.LimitEventUsage = value;
                }
            }
        }

        internal static IFacebook FacebookImpl
        {
            get
            {
                if (FB.facebook == null)
                {
                    throw new NullReferenceException("Facebook object is not yet loaded.  Did you call FB.Init()?");
                }

                return FB.facebook;
            }

            set
            {
                FB.facebook = value;
            }
        }

        internal static string FacebookDomain
        {
            get
            {
                if (FB.IsLoggedIn && AccessToken.CurrentAccessToken != null)
                {
                    string graphDomain = AccessToken.CurrentAccessToken.GraphDomain;
                    if (graphDomain == "gaming")
                    {
                        return FB.gamingDomain;
                    }
                }
                return FB.facebookDomain;
            }

            set
            {
                FB.facebookDomain = value;
            }
        }

        private static OnDLLLoaded OnDLLLoadedDelegate { get; set; }

        /// <summary>
        /// This is the preferred way to call FB.Init(). It will take the facebook app id specified in your "Facebook"
        /// => "Edit Settings" menu when it is called.
        /// </summary>
        /// <param name="onInitComplete">
        /// Delegate is called when FB.Init() finished initializing everything. By passing in a delegate you can find
        /// out when you can safely call the other methods.
        /// </param>
        /// <param name="onHideUnity">A delegate to invoke when unity is hidden.</param>
        /// <param name="authResponse">Auth response.</param>
        public static void Init(InitDelegate onInitComplete = null, HideUnityDelegate onHideUnity = null, string authResponse = null)
        {
            Init(
                FacebookSettings.AppId,
                FacebookSettings.ClientToken,
                FacebookSettings.Cookie,
                FacebookSettings.Logging,
                FacebookSettings.Status,
                FacebookSettings.Xfbml,
                FacebookSettings.FrictionlessRequests,
                authResponse,
                FB.DefaultJSSDKLocale,
                onHideUnity,
                onInitComplete);
        }

        /// <summary>
        /// If you need a more programmatic way to set the facebook app id and other setting call this function.
        /// Useful for a build pipeline that requires no human input.
        /// </summary>
        /// <param name="appId">App identifier.</param>
        /// <param name="clientToken">App client token.</param>
        /// <param name="cookie">If set to <c>true</c> cookie.</param>
        /// <param name="logging">If set to <c>true</c> logging.</param>
        /// <param name="status">If set to <c>true</c> status.</param>
        /// <param name="xfbml">If set to <c>true</c> xfbml.</param>
        /// <param name="frictionlessRequests">If set to <c>true</c> frictionless requests.</param>
        /// <param name="authResponse">Auth response.</param>
        /// <param name="javascriptSDKLocale">
        /// The locale of the js sdk used see
        /// https://developers.facebook.com/docs/internationalization#plugins.
        /// </param>
        /// <param name="onHideUnity">
        /// A delegate to invoke when unity is hidden.
        /// </param>
        /// <param name="onInitComplete">
        /// Delegate is called when FB.Init() finished initializing everything. By passing in a delegate you can find
        /// out when you can safely call the other methods.
        /// </param>
        public static void Init(
            string appId,
            string clientToken = null,
            bool cookie = true,
            bool logging = true,
            bool status = true,
            bool xfbml = false,
            bool frictionlessRequests = true,
            string authResponse = null,
            string javascriptSDKLocale = FB.DefaultJSSDKLocale,
            HideUnityDelegate onHideUnity = null,
            InitDelegate onInitComplete = null)
        {
            if (string.IsNullOrEmpty(appId))
            {
                throw new ArgumentException("appId cannot be null or empty!");
            }

            FB.AppId = appId;
            FB.ClientToken = clientToken == null ? FacebookSettings.ClientToken : clientToken;

            if (!isInitCalled)
            {
                isInitCalled = true;

                if (Constants.IsEditor)
                {
                    if (Application.platform == RuntimePlatform.WindowsEditor && (FacebookSettings.EditorBuildTarget == FacebookSettings.BuildTarget.StandaloneWindows || FacebookSettings.EditorBuildTarget == FacebookSettings.BuildTarget.StandaloneWindows64))
                    {
                        FB.OnDLLLoadedDelegate = delegate
                        {
                            ((WindowsFacebook)FB.facebook).Init(appId, FB.ClientToken, onHideUnity, onInitComplete);
                        };
                        ComponentFactory.GetComponent<WindowsFacebookLoader>();
                    }
                    else
                    {
                        FB.OnDLLLoadedDelegate = delegate
                        {
                            ((EditorFacebook)FB.facebook).Init(onInitComplete);
                        };
                        ComponentFactory.GetComponent<EditorFacebookLoader>();
                        ComponentFactory.GetComponent<CodelessCrawler>();
                        ComponentFactory.GetComponent<CodelessUIInteractEvent>();
                    }
                }
                else
                {
                    switch (Constants.CurrentPlatform)
                    {
                        case FacebookUnityPlatform.WebGL:
                            FB.OnDLLLoadedDelegate = delegate
                            {
                                ((CanvasFacebook)FB.facebook).Init(
                                    appId,
                                    cookie,
                                    logging,
                                    status,
                                    xfbml,
                                    FacebookSettings.ChannelUrl,
                                    authResponse,
                                    frictionlessRequests,
                                    javascriptSDKLocale,
                                    Constants.DebugMode,
                                    onHideUnity,
                                    onInitComplete);
                            };
                            ComponentFactory.GetComponent<CanvasFacebookLoader>();
                            break;
                        case FacebookUnityPlatform.IOS:
                            FB.OnDLLLoadedDelegate = delegate
                            {
                                ((IOSFacebook)FB.facebook).Init(
                                    appId,
                                    frictionlessRequests,
                                    FacebookSettings.IosURLSuffix,
                                    onHideUnity,
                                    onInitComplete);
                            };
                            ComponentFactory.GetComponent<IOSFacebookLoader>();
                            ComponentFactory.GetComponent<CodelessCrawler>();
                            ComponentFactory.GetComponent<CodelessUIInteractEvent>();
                            break;
                        case FacebookUnityPlatform.Android:
                            FB.OnDLLLoadedDelegate = delegate
                            {
                                ((AndroidFacebook)FB.facebook).Init(
                                    appId,
                                    clientToken,
                                    onHideUnity,
                                    onInitComplete);
                            };
                            ComponentFactory.GetComponent<AndroidFacebookLoader>();
                            ComponentFactory.GetComponent<CodelessCrawler>();
                            ComponentFactory.GetComponent<CodelessUIInteractEvent>();
                            break;
                        case FacebookUnityPlatform.Windows:
                            FB.OnDLLLoadedDelegate = delegate
                            {
                                ((WindowsFacebook)FB.facebook).Init(appId, clientToken, onHideUnity, onInitComplete);
                            };
                            ComponentFactory.GetComponent<WindowsFacebookLoader>();
                            break;
                        default:
                            throw new NotSupportedException("The facebook sdk does not support this platform");
                    }
                }
            }
            else
            {
                FacebookLogger.Warn("FB.Init() has already been called.  You only need to call this once and only once.");
            }
        }

        /// <summary>
        /// Logs the user in with the requested publish permissions.
        /// </summary>
        /// <param name="permissions">A list of requested permissions.</param>
        /// <param name="callback">Callback to be called when request completes.</param>
        /// <exception cref="System.NotSupportedException">
        /// Thrown when called on a TV device.
        /// </exception>
        public static void LogInWithPublishPermissions(
            IEnumerable<string> permissions = null,
            FacebookDelegate<ILoginResult> callback = null)
        {
            FacebookImpl.LogInWithPublishPermissions(permissions, callback);
        }

        /// <summary>
        /// Logs the user in with the requested read permissions.
        /// </summary>
        /// <param name="permissions">A list of requested permissions.</param>
        /// <param name="callback">Callback to be called when request completes.</param>
        /// <exception cref="System.NotSupportedException">
        /// Thrown when called on a TV device.
        /// </exception>
        public static void LogInWithReadPermissions(
            IEnumerable<string> permissions = null,
            FacebookDelegate<ILoginResult> callback = null)
        {
            FacebookImpl.LogInWithReadPermissions(permissions, callback);
        }

        /// <summary>
        /// Logs out the current user.
        /// </summary>
        public static void LogOut()
        {
            FacebookImpl.LogOut();
        }

        /// <summary>
        /// Apps the request.
        /// </summary>
        /// <param name="message">The request string the recipient will see, maximum length 60 characters.</param>
        /// <param name="actionType">Request action type for structured request.</param>
        /// <param name="objectId">
        /// Open Graph object ID for structured request.
        /// Note the type of object should belong to this app.
        /// </param>
        /// <param name="to">A list of Facebook IDs to which to send the request.</param>
        /// <param name="data">
        /// Additional data stored with the request on Facebook,
        /// and handed back to the app when it reads the request back out.
        /// Maximum length 255 characters.</param>
        /// <param name="title">The title for the platform multi-friend selector dialog. Max length 50 characters..</param>
        /// <param name="callback">A callback for when the request completes.</param>
        public static void AppRequest(
            string message,
            OGActionType actionType,
            string objectId,
            IEnumerable<string> to,
            string data = "",
            string title = "",
            FacebookDelegate<IAppRequestResult> callback = null)
        {
            FacebookImpl.AppRequest(message, actionType, objectId, to, null, null, null, data, title, callback);
        }

        /// <summary>
        /// Apps the request.
        /// </summary>
        /// <param name="message">The request string the recipient will see, maximum length 60 characters.</param>
        /// <param name="actionType">Request action type for structured request.</param>
        /// <param name="objectId">
        /// Open Graph object ID for structured request.
        /// Note the type of object should belong to this app.
        /// </param>
        /// <param name="filters">
        /// The configuration of the platform multi-friend selector.
        /// It should be a List of filter strings.
        /// </param>
        /// <param name="excludeIds">
        /// A list of Facebook IDs to exclude from the platform multi-friend selector dialog.
        /// This list is currently not supported for mobile devices.
        /// </param>
        /// <param name="maxRecipients">
        /// Platform-dependent The maximum number of recipients the sender should be able to
        /// choose in the platform multi-friend selector dialog.
        /// Only guaranteed to work in Unity Web Player app.
        /// </param>
        /// <param name="data">
        /// Additional data stored with the request on Facebook, and handed
        /// back to the app when it reads the request back out.
        /// Maximum length 255 characters.
        /// </param>
        /// <param name="title">
        /// The title for the platform multi-friend selector dialog. Max length 50 characters.
        /// </param>
        /// <param name="callback">A callback for when the request completes.</param>
        public static void AppRequest(
            string message,
            OGActionType actionType,
            string objectId,
            IEnumerable<object> filters = null,
            IEnumerable<string> excludeIds = null,
            int? maxRecipients = null,
            string data = "",
            string title = "",
            FacebookDelegate<IAppRequestResult> callback = null)
        {
            FacebookImpl.AppRequest(message, actionType, objectId, null, filters, excludeIds, maxRecipients, data, title, callback);
        }

        /// <summary>
        /// Apps the request.
        /// </summary>
        /// <param name="message">The request string the recipient will see, maximum length 60 characters.</param>
        /// <param name="to">A list of Facebook IDs to which to send the request.</param>
        /// <param name="filters">
        /// The configuration of the platform multi-friend selector.
        /// It should be a List of filter strings.
        /// </param>
        /// <param name="excludeIds">
        /// A list of Facebook IDs to exclude from the platform multi-friend selector dialog.
        /// This list is currently not supported for mobile devices.
        /// </param>
        /// <param name="maxRecipients">
        /// Platform-dependent The maximum number of recipients the sender should be able to
        /// choose in the platform multi-friend selector dialog.
        /// Only guaranteed to work in Unity Web Player app.
        /// </param>
        /// <param name="data">
        /// Additional data stored with the request on Facebook, and handed
        /// back to the app when it reads the request back out.
        /// Maximum length 255 characters.
        /// </param>
        /// <param name="title">
        /// The title for the platform multi-friend selector dialog. Max length 50 characters.
        /// </param>
        /// <param name="callback">A callback for when the request completes.</param>
        public static void AppRequest(
            string message,
            IEnumerable<string> to = null,
            IEnumerable<object> filters = null,
            IEnumerable<string> excludeIds = null,
            int? maxRecipients = null,
            string data = "",
            string title = "",
            FacebookDelegate<IAppRequestResult> callback = null)
        {
            FacebookImpl.AppRequest(message, null, null, to, filters, excludeIds, maxRecipients, data, title, callback);
        }

        /// <summary>
        /// Opens a share dialog for sharing a link.
        /// </summary>
        /// <param name="contentURL">The URL or the link to share.</param>
        /// <param name="contentTitle">The title to display for this link..</param>
        /// <param name="contentDescription">
        /// The description of the link.  If not specified, this field is automatically populated by
        /// information scraped from the link, typically the title of the page.
        /// </param>
        /// <param name="photoURL">The URL of a picture to attach to this content.</param>
        /// <param name="callback">A callback for when the request completes.</param>
        public static void ShareLink(
            Uri contentURL = null,
            string contentTitle = "",
            string contentDescription = "",
            Uri photoURL = null,
            FacebookDelegate<IShareResult> callback = null)
        {
            FacebookImpl.ShareLink(
                contentURL,
                contentTitle,
                contentDescription,
                photoURL,
                callback);
        }

        /// <summary>
        /// Legacy feed share. Only use this dialog if you need the legacy parameters otherwiese use
        /// <see cref="FB.ShareLink(System.String, System.String, System.String, System.String, Facebook.FacebookDelegate"/>.
        /// </summary>
        /// <param name="toId">
        ///     The ID of the profile that this story will be published to.
        ///     If this is unspecified, it defaults to the value of from.
        ///     The ID must be a friend who also uses your app.
        /// </param>
        /// <param name="link">The link attached to this post.</param>
        /// <param name="linkName">The name of the link attachment.</param>
        /// <param name="linkCaption">
        ///     The caption of the link (appears beneath the link name).
        ///     If not specified, this field is automatically populated
        ///     with the URL of the link.
        /// </param>
        /// <param name="linkDescription">
        ///     The description of the link (appears beneath the link caption).
        ///     If not specified, this field is automatically populated by information
        ///     scraped from the link, typically the title of the page.
        /// </param>
        /// <param name="picture">
        ///     The URL of a picture attached to this post.
        ///     The picture must be at least 200px by 200px.
        ///     See our documentation on sharing best practices for more information on sizes.
        /// </param>
        /// <param name="mediaSource">
        ///     The URL of a media file (either SWF or MP3) attached to this post.
        ///     If SWF, you must also specify picture to provide a thumbnail for the video.
        /// </param>
        /// <param name="callback">The callback to use upon completion.</param>
        public static void FeedShare(
            string toId = "",
            Uri link = null,
            string linkName = "",
            string linkCaption = "",
            string linkDescription = "",
            Uri picture = null,
            string mediaSource = "",
            FacebookDelegate<IShareResult> callback = null)
        {
            FacebookImpl.FeedShare(
                toId,
                link,
                linkName,
                linkCaption,
                linkDescription,
                picture,
                mediaSource,
                callback);
        }

        /// <summary>
        /// Makes a call to the Facebook Graph API.
        /// </summary>
        /// <param name="query">
        /// The Graph API endpoint to call.
        /// You may prefix this with a version string to call a particular version of the API.
        /// </param>
        /// <param name="method">The HTTP method to use in the call.</param>
        /// <param name="callback">The callback to use upon completion.</param>
        /// <param name="formData">The key/value pairs to be passed to the endpoint as arguments.</param>
        public static void API(
            string query,
            HttpMethod method,
            FacebookDelegate<IGraphResult> callback = null,
            IDictionary<string, string> formData = null)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException("query", "The query param cannot be null or empty");
            }

            FacebookImpl.API(query, method, formData, callback);
        }

        /// <summary>
        /// Makes a call to the Facebook Graph API.
        /// </summary>
        /// <param name="query">
        /// The Graph API endpoint to call.
        /// You may prefix this with a version string to call a particular version of the API.
        /// </param>
        /// <param name="method">The HTTP method to use in the call.</param>
        /// <param name="callback">The callback to use upon completion.</param>
        /// <param name="formData">Form data for the request.</param>
        public static void API(
            string query,
            HttpMethod method,
            FacebookDelegate<IGraphResult> callback,
            WWWForm formData)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException("query", "The query param cannot be null or empty");
            }

            FacebookImpl.API(query, method, formData, callback);
        }

        /// <summary>
        /// Sends an app activation event to Facebook when your app is activated.
        ///
        /// On iOS and Android, this event is logged automatically if you turn on
        /// AutoLogAppEventsEnabled flag. You may still need to call this event if
        /// you are running on web.
        ///
        /// On iOS the activate event is fired when the app becomes active
        /// On Android the activate event is fired during FB.Init
        /// </summary>
        public static void ActivateApp()
        {
            FacebookImpl.ActivateApp(AppId);
        }

        /// <summary>
        /// Gets the deep link if available.
        /// </summary>
        /// <param name="callback">The callback to use upon completion.</param>
        public static void GetAppLink(
            FacebookDelegate<IAppLinkResult> callback)
        {
            if (callback == null)
            {
                // No point in fetching the data if there is no callback
                return;
            }

            FacebookImpl.GetAppLink(callback);
        }

        /// <summary>
        /// Clear app link.
        ///
        /// Clear app link when app link has been handled, only works for
        /// Android, this function will do nothing in other platforms.
        /// </summary>
        public static void ClearAppLink()
        {
#if UNITY_ANDROID
          var androidFacebook = FacebookImpl as AndroidFacebook;
          if (androidFacebook != null)
          {
            androidFacebook.ClearAppLink();
          }
#endif
        }

        /// <summary>
        /// Logs an app event.
        /// </summary>
        /// <param name="logEvent">The name of the event to log.</param>
        /// <param name="valueToSum">A number representing some value to be summed when reported.</param>
        /// <param name="parameters">Any parameters needed to describe the event.</param>
        public static void LogAppEvent(
            string logEvent,
            float? valueToSum = null,
            Dictionary<string, object> parameters = null)
        {
            FacebookImpl.AppEventsLogEvent(logEvent, valueToSum, parameters);
        }

        /// <summary>
        /// Logs the purchase.
        /// </summary>
        /// <param name="logPurchase">The amount of currency the user spent.</param>
        /// <param name="currency">The 3-letter ISO currency code.</param>
        /// <param name="parameters">
        /// Any parameters needed to describe the event.
        /// Elements included in this dictionary can't be null.
        /// </param>
        public static void LogPurchase(
            decimal logPurchase,
            string currency = null,
            Dictionary<string, object> parameters = null)
        {
            FB.LogPurchase(float.Parse(logPurchase.ToString()), currency, parameters);
        }

        /// <summary>
        /// Logs the purchase.
        /// </summary>
        /// <param name="logPurchase">The amount of currency the user spent.</param>
        /// <param name="currency">The 3-letter ISO currency code.</param>
        /// <param name="parameters">
        /// Any parameters needed to describe the event.
        /// Elements included in this dictionary can't be null.
        /// </param>
        public static void LogPurchase(
            float logPurchase,
            string currency = null,
            Dictionary<string, object> parameters = null)
        {
            if (string.IsNullOrEmpty(currency))
            {
                currency = "USD";
            }

            FacebookImpl.AppEventsLogPurchase(logPurchase, currency, parameters);
        }

        private static void LogVersion()
        {
            // If we have initlized we can also get the underlying sdk version
            if (facebook != null)
            {
                FacebookLogger.Info(string.Format(
                    "Using Facebook Unity SDK v{0} with {1}",
                    FacebookSdkVersion.Build,
                    FB.FacebookImpl.SDKUserAgent));
            }
            else
            {
                FacebookLogger.Info(string.Format("Using Facebook Unity SDK v{0}", FacebookSdkVersion.Build));
            }
        }

        public static void GetCatalog(FacebookDelegate<ICatalogResult> callback)
        {
            FacebookImpl.GetCatalog(callback);
        }

        public static void GetPurchases(FacebookDelegate<IPurchasesResult> callback)
        {
            FacebookImpl.GetPurchases(callback);
        }

        public static void Purchase(string productID, FacebookDelegate<IPurchaseResult> callback, string developerPayload = "")
        {
            FacebookImpl.Purchase(productID, callback, developerPayload);
        }

        public static void ConsumePurchase(string productToken, FacebookDelegate<IConsumePurchaseResult> callback)
        {
            FacebookImpl.ConsumePurchase(productToken, callback);
        }

        public static void GetSubscribableCatalog(FacebookDelegate<ISubscribableCatalogResult> callback)
        {
            FacebookImpl.GetSubscribableCatalog(callback);
        }

        public static void GetSubscriptions(FacebookDelegate<ISubscriptionsResult> callback)
        {
            FacebookImpl.GetSubscriptions(callback);
        }

        public static void PurchaseSubscription(string productID, FacebookDelegate<ISubscriptionResult> callback)
        {
            FacebookImpl.PurchaseSubscription(productID, callback);
        }

        public static void CancelSubscription(string purchaseToken, FacebookDelegate<ICancelSubscriptionResult> callback)
        {
            FacebookImpl.CancelSubscription(purchaseToken, callback);
        }

        public static Profile CurrentProfile()
        {
            return FacebookImpl.CurrentProfile();
        }

        public static void CurrentProfile(FacebookDelegate<IProfileResult> callback)
        {
            FacebookImpl.CurrentProfile(callback);
        }

        public static void LoadInterstitialAd(string placementID, FacebookDelegate<IInterstitialAdResult> callback)
        {
            FacebookImpl.LoadInterstitialAd(placementID, callback);
        }

        public static void ShowInterstitialAd(string placementID, FacebookDelegate<IInterstitialAdResult> callback)
        {
            FacebookImpl.ShowInterstitialAd(placementID, callback);
        }

        public static void LoadRewardedVideo(string placementID, FacebookDelegate<IRewardedVideoResult> callback)
        {
            FacebookImpl.LoadRewardedVideo(placementID, callback);
        }

        public static void ShowRewardedVideo(string placementID, FacebookDelegate<IRewardedVideoResult> callback)
        {
            FacebookImpl.ShowRewardedVideo(placementID, callback);
        }

        public static void OpenFriendFinderDialog(FacebookDelegate<IGamingServicesFriendFinderResult> callback)
        {
            FacebookImpl.OpenFriendFinderDialog(callback);
        }

        public static void GetFriendFinderInvitations(FacebookDelegate<IFriendFinderInvitationResult> callback)
        {
            FacebookImpl.GetFriendFinderInvitations(callback);
        }

        public static void DeleteFriendFinderInvitation(string invitationId, FacebookDelegate<IFriendFinderInvitationResult> callback)
        {
            FacebookImpl.DeleteFriendFinderInvitation(invitationId, callback);
        }

        public static void ScheduleAppToUserNotification(string title, string body, Uri media, int timeInterval, string payload, FacebookDelegate<IScheduleAppToUserNotificationResult> callback)
        {
            FacebookImpl.ScheduleAppToUserNotification(title, body, media, timeInterval, payload, callback);
        }

        public static void PostSessionScore(int score, FacebookDelegate<ISessionScoreResult> callback)
        {
            FacebookImpl.PostSessionScore(score, callback);
        }

        public static void PostTournamentScore(int score, FacebookDelegate<ITournamentScoreResult> callback)
        {
            FacebookImpl.PostTournamentScore(score, callback);
        }

        public static void GetTournament(FacebookDelegate<ITournamentResult> callback)
        {
            FacebookImpl.GetTournament(callback);
        }

        public static void ShareTournament(int score, Dictionary<string, string> data, FacebookDelegate<ITournamentScoreResult> callback)
        {
            FacebookImpl.ShareTournament(score, data, callback);
        }

        public static void CreateTournament(
            int initialScore,
            string title,
            string imageBase64DataUrl,
            string sortOrder,
            string scoreFormat,
            Dictionary<string, string> data,
            FacebookDelegate<ITournamentResult> callback)
        {
            FacebookImpl.CreateTournament(initialScore, title, imageBase64DataUrl, sortOrder, scoreFormat, data, callback);
        }


        public static void UploadImageToMediaLibrary(string caption, Uri imageUri, bool shouldLaunchMediaDialog, FacebookDelegate<IMediaUploadResult> callback)
        {
            FacebookImpl.UploadImageToMediaLibrary(caption, imageUri, shouldLaunchMediaDialog, callback);
        }

        public static void UploadVideoToMediaLibrary(string caption, Uri videoUri, bool shouldLaunchMediaDialog, FacebookDelegate<IMediaUploadResult> callback)
        {
            FacebookImpl.UploadVideoToMediaLibrary(caption, videoUri, shouldLaunchMediaDialog, callback);
        }

        public static void UploadImageToMediaLibrary(string caption, Uri imageUri, bool shouldLaunchMediaDialog, string travelId, FacebookDelegate<IMediaUploadResult> callback)
        {
            FacebookImpl.UploadImageToMediaLibrary(caption, imageUri, shouldLaunchMediaDialog, travelId, callback);
        }

        public static void UploadVideoToMediaLibrary(string caption, Uri videoUri, bool shouldLaunchMediaDialog, string travelId, FacebookDelegate<IMediaUploadResult> callback)
        {
            FacebookImpl.UploadVideoToMediaLibrary(caption, videoUri, shouldLaunchMediaDialog, travelId, callback);
        }

        public static void GetUserLocale(FacebookDelegate<ILocaleResult> callback)
        {
            FacebookImpl.GetUserLocale(callback);
        }

        /// <summary>
        /// Contains methods specific to the Facebook Games Canvas platform.
        /// </summary>
        public sealed class Canvas
        {
            private static IPayFacebook FacebookPayImpl
            {
                get
                {
                    IPayFacebook impl = FacebookImpl as IPayFacebook;

                    if (impl == null)
                    {
                        throw new InvalidOperationException("Attempt to call Facebook pay interface on unsupported platform");
                    }

                    return impl;
                }
            }

            /// <summary>
            /// Pay the specified product, action, quantity, quantityMin, quantityMax, requestId, pricepointId,
            /// testCurrency and callback.
            /// </summary>
            /// <param name="product">The URL of your og:product object that the user is looking to purchase.</param>
            /// <param name="action">Should always be purchaseitem.</param>
            /// <param name="quantity">
            /// The amount of this item the user is looking to purchase - typically used when implementing a virtual currency purchase.
            /// </param>
            /// <param name="quantityMin">
            /// The minimum quantity of the item the user is able to purchase.
            /// This parameter is important when handling price jumping to maximize the efficiency of the transaction.
            /// </param>
            /// <param name="quantityMax">
            /// The maximum quantity of the item the user is able to purchase.
            /// This parameter is important when handling price jumping to maximize the efficiency of the transaction.
            /// </param>
            /// <param name="requestId">
            /// The developer defined unique identifier for this transaction, which becomes
            /// attached to the payment within the Graph API.
            /// </param>
            /// <param name="pricepointId">
            /// Used to shortcut a mobile payer directly to the
            /// mobile purchase flow at a given price point.
            /// </param>
            /// <param name="testCurrency">
            /// This parameter can be used during debugging and testing your implementation to force the dialog to
            /// use a specific currency rather than the current user's preferred currency. This allows you to
            /// rapidly prototype your payment experience for different currencies without having to repeatedly
            /// change your personal currency preference settings. This parameter is only available for admins,
            /// developers and testers associated with the app, in order to minimize the security risk of a
            /// malicious JavaScript injection. Provide the 3 letter currency code of the intended forced currency.
            /// </param>
            /// <param name="callback">The callback to use upon completion.</param>
            public static void Pay(
                string product,
                string action = "purchaseitem",
                int quantity = 1,
                int? quantityMin = null,
                int? quantityMax = null,
                string requestId = null,
                string pricepointId = null,
                string testCurrency = null,
                FacebookDelegate<IPayResult> callback = null)
            {
                FacebookPayImpl.Pay(
                    product,
                    action,
                    quantity,
                    quantityMin,
                    quantityMax,
                    requestId,
                    pricepointId,
                    testCurrency,
                    callback);
            }

            /// <summary>
            /// Pay the specified productId, action, quantity, quantityMin, quantityMax, requestId, pricepointId,
            /// testCurrency and callback.
            /// </summary>
            /// <param name="productId">The product Id referencing the product managed by Facebook.</param>
            /// <param name="action">Should always be purchaseiap.</param>
            /// <param name="quantity">
            /// The amount of this item the user is looking to purchase - typically used when implementing a virtual currency purchase.
            /// </param>
            /// <param name="quantityMin">
            /// The minimum quantity of the item the user is able to purchase.
            /// This parameter is important when handling price jumping to maximize the efficiency of the transaction.
            /// </param>
            /// <param name="quantityMax">
            /// The maximum quantity of the item the user is able to purchase.
            /// This parameter is important when handling price jumping to maximize the efficiency of the transaction.
            /// </param>
            /// <param name="requestId">
            /// The developer defined unique identifier for this transaction, which becomes
            /// attached to the payment within the Graph API.
            /// </param>
            /// <param name="pricepointId">
            /// Used to shortcut a mobile payer directly to the
            /// mobile purchase flow at a given price point.
            /// </param>
            /// <param name="testCurrency">
            /// This parameter can be used during debugging and testing your implementation to force the dialog to
            /// use a specific currency rather than the current user's preferred currency. This allows you to
            /// rapidly prototype your payment experience for different currencies without having to repeatedly
            /// change your personal currency preference settings. This parameter is only available for admins,
            /// developers and testers associated with the app, in order to minimize the security risk of a
            /// malicious JavaScript injection. Provide the 3 letter currency code of the intended forced currency.
            /// </param>
            /// <param name="callback">The callback to use upon completion.</param>
            public static void PayWithProductId(
                string productId,
                string action = "purchaseiap",
                int quantity = 1,
                int? quantityMin = null,
                int? quantityMax = null,
                string requestId = null,
                string pricepointId = null,
                string testCurrency = null,
                FacebookDelegate<IPayResult> callback = null)
            {
                FacebookPayImpl.PayWithProductId(
                    productId,
                    action,
                    quantity,
                    quantityMin,
                    quantityMax,
                    requestId,
                    pricepointId,
                    testCurrency,
                    callback);
            }

            /// <summary>
            /// Pay the specified productId, action, developerPayload, testCurrency and callback.
            /// </summary>
            /// <param name="productId">The product Id referencing the product managed by Facebook.</param>
            /// <param name="action">Should always be purchaseiap.</param>
            /// <param name="developerPayload">
            /// A string that can be used to provide supplemental information about an order. It can be
            /// used to uniquely identify the purchase request.
            /// </param>
            /// <param name="testCurrency">
            /// This parameter can be used during debugging and testing your implementation to force the dialog to
            /// use a specific currency rather than the current user's preferred currency. This allows you to
            /// rapidly prototype your payment experience for different currencies without having to repeatedly
            /// change your personal currency preference settings. This parameter is only available for admins,
            /// developers and testers associated with the app, in order to minimize the security risk of a
            /// malicious JavaScript injection. Provide the 3 letter currency code of the intended forced currency.
            /// </param>
            /// <param name="callback">The callback to use upon completion.</param>
            public static void PayWithProductId(
                string productId,
                string action = "purchaseiap",
                string developerPayload = null,
                string testCurrency = null,
                FacebookDelegate<IPayResult> callback = null)
            {
                FacebookPayImpl.PayWithProductId(
                    productId,
                    action,
                    developerPayload,
                    testCurrency,
                    callback);
            }
        }

        /// <summary>
        /// A class containing the settings specific to the supported mobile platforms.
        /// </summary>
        public sealed class Mobile
        {
            /// <summary>
            /// Gets or sets the share dialog mode.
            /// </summary>
            /// <value>The share dialog mode.</value>
            public static ShareDialogMode ShareDialogMode
            {
                get
                {
                    return Mobile.MobileFacebookImpl.ShareDialogMode;
                }

                set
                {
                    Mobile.MobileFacebookImpl.ShareDialogMode = value;
                }
            }

            /// <summary>
            /// Gets or sets the user ID.
            /// </summary>
            /// <value>The user ID.</value>
            public static string UserID
            {
                get
                {
                    return Mobile.MobileFacebookImpl.UserID;
                }

                set
                {
                    Mobile.MobileFacebookImpl.UserID = value;
                }
            }

            public static void SetDataProcessingOptions(IEnumerable<string> options)
            {
                if (options == null)
                {
                    options = new string[] { };
                }
                Mobile.MobileFacebookImpl.SetDataProcessingOptions(options, 0, 0);
            }

            public static void SetDataProcessingOptions(IEnumerable<string> options, int country, int state)
            {
                if (options == null)
                {
                    options = new string[] { };
                }
                Mobile.MobileFacebookImpl.SetDataProcessingOptions(options, country, state);
            }

            private static IMobileFacebook MobileFacebookImpl
            {
                get
                {
                    IMobileFacebook impl = FacebookImpl as IMobileFacebook;
                    if (impl == null)
                    {
                        throw new InvalidOperationException("Attempt to call Mobile interface on non mobile platform");
                    }

                    return impl;
                }
            }

            /// <summary>
            /// Call this function so that Profile will be automatically updated based on the changes to the access token.
            /// </summary>
            public static void EnableProfileUpdatesOnAccessTokenChange(bool enable)
            {
                Mobile.MobileFacebookImpl.EnableProfileUpdatesOnAccessTokenChange(enable);
            }

            /// <summary>
            /// Login with tracking experience.
            /// </summary>
            /// <param name="loginTracking">The option for login tracking preference, "enabled" or "limited".</param>
            /// <param name="permissions">A list of permissions.</param>
            /// <param name="nonce">An optional nonce to use for the login attempt.</param>
            /// <param name="callback">A callback for when the call is complete.</param>
            public static void LoginWithTrackingPreference(
                LoginTracking loginTracking,
                IEnumerable<string> permissions = null,
                string nonce = null,
                FacebookDelegate<ILoginResult> callback = null)
            {
                if (loginTracking == LoginTracking.ENABLED)
                {
                    Mobile.MobileFacebookImpl.LoginWithTrackingPreference("enabled", permissions, nonce, callback);
                }
                else
                {
                    Mobile.MobileFacebookImpl.LoginWithTrackingPreference("limited", permissions, nonce, callback);
                }
            }

            /// <summary>
            /// Current Authentication Token.
            /// </summary>
            public static AuthenticationToken CurrentAuthenticationToken()
            {
                return Mobile.MobileFacebookImpl.CurrentAuthenticationToken();
            }

            /// <summary>
            /// Current Profile.
            /// </summary>
            public static Profile CurrentProfile()
            {
                return Mobile.MobileFacebookImpl.CurrentProfile();
            }

            /// <summary>
            /// Current Profile via vallback.
            /// </summary>
            public static void CurrentProfile(FacebookDelegate<IProfileResult> callback)
            {
                Profile currentProfile = Mobile.MobileFacebookImpl.CurrentProfile();
                Dictionary<string, object> result = new Dictionary<string, object>() { { ProfileResult.ProfileKey, currentProfile } };
                if (currentProfile == null)
                {
                    result[Constants.ErrorKey] = "ERROR: No profile data";
                }
                callback.Invoke(new ProfileResult(new ResultContainer(result)));
            }

            /// <summary>
            /// Fetchs the deferred app link data.
            /// </summary>
            /// <param name="callback">A callback for when the call is complete.</param>
            public static void FetchDeferredAppLinkData(
                FacebookDelegate<IAppLinkResult> callback = null)
            {
                if (callback == null)
                {
                    // No point in fetching the data if there is no callback
                    return;
                }

                Mobile.MobileFacebookImpl.FetchDeferredAppLink(callback);
            }

            /// <summary>
            /// Refreshs the current access to get a new access token if possible.
            /// </summary>
            /// <param name="callback">A on refresh access token compelte callback.</param>
            public static void RefreshCurrentAccessToken(
                FacebookDelegate<IAccessTokenRefreshResult> callback = null)
            {
                Mobile.MobileFacebookImpl.RefreshCurrentAccessToken(callback);
            }

            /// <summary>
            /// Refreshes the profile data for a Limited Login user (iOS only).
            /// On success the refreshed data is available via <see cref="CurrentProfile()"/>
            /// and the callback receives the new authentication token. On other
            /// platforms this is a no-op.
            /// </summary>
            /// <param name="callback">A callback for when the refresh is complete.</param>
            /// <param name="fallbackPolicy">How the refresh should be attempted (direct/silent/explicit cascade).</param>
            public static void RefreshLimitedLogin(
                FacebookDelegate<ILoginResult> callback = null,
                RefreshFallbackPolicy fallbackPolicy = RefreshFallbackPolicy.Automatic)
            {
                Mobile.MobileFacebookImpl.RefreshLimitedLogin(fallbackPolicy, callback);
            }

            /// <summary>
            /// Logs the user in using Facebook SSO ("Login with Facebook") via the installed
            /// Facebook app, falling back to a "Continue with Facebook" dialog / web login when
            /// the Facebook app is missing or out of date (Android only). On other platforms
            /// this is a no-op. On success the callback receives the login result.
            /// </summary>
            /// <param name="permissions">A list of permissions to request.</param>
            /// <param name="callback">A callback for when the login is complete.</param>
            public static void LoginWithSSO(
                IEnumerable<string> permissions = null,
                FacebookDelegate<ILoginResult> callback = null)
            {
                Mobile.MobileFacebookImpl.LoginWithSSO(permissions, callback);
            }

            /// <summary>
            /// Returns the setting for Automatic Purchase Logging
            /// </summary>
            public static bool IsImplicitPurchaseLoggingEnabled()
            {
                return Mobile.MobileFacebookImpl.IsImplicitPurchaseLoggingEnabled();
            }

            /// <summary>
            /// Sets the setting for Automatic App Events Logging.
            /// </summary>
            /// <param name="autoLogAppEventsEnabled">The setting for Automatic App Events Logging</param>
            public static void SetAutoLogAppEventsEnabled(bool autoLogAppEventsEnabled)
            {
                Mobile.MobileFacebookImpl.SetAutoLogAppEventsEnabled(autoLogAppEventsEnabled);
            }

            /// <summary>
            /// Sets whether diagnostic product improvement data about the app and its onscreen content may be automatically logged.
            /// Defaults to true. Runtime assignments persist across launches and are independent of
            /// metadata collection and automatic app event logging. Requires native SDK 19 or later.
            /// This control does not itself collect or send product improvement data.
            /// </summary>
            /// <param name="autoLogProductImprovementEnabled">Whether automatic product improvement data logging is enabled.</param>
            public static void SetAutoLogProductImprovementEnabled(bool autoLogProductImprovementEnabled)
            {
                Mobile.MobileFacebookImpl.SetAutoLogProductImprovementEnabled(autoLogProductImprovementEnabled);
            }

            /// <summary>
            /// Sets the setting for automatic app metadata collection, such as screen titles
            /// and AppLink URLs, which is included with logged app events.
            ///
            /// Metadata is collected only when this setting and the Events Manager setting are
            /// both enabled. Setting this to false discards metadata already collected.
            /// </summary>
            /// <param name="autoLogMetaDataEnabled">The setting for automatic app metadata collection</param>
            public static void SetAutoLogMetaDataEnabled(bool autoLogMetaDataEnabled)
            {
                Mobile.MobileFacebookImpl.SetAutoLogMetaDataEnabled(autoLogMetaDataEnabled);
            }

            /// <summary>
            /// Returns the setting for automatic app metadata collection.
            ///
            /// This reports the setting only. Collection additionally requires the Events Manager
            /// setting to be enabled, so a true result does not on its own mean metadata is being
            /// collected.
            /// </summary>
            public static bool IsAutoLogMetaDataEnabled()
            {
                return Mobile.MobileFacebookImpl.IsAutoLogMetaDataEnabled();
            }

            /// <summary>
            /// Sets the setting for Advertiser ID collection.
            /// </summary>
            /// <param name="advertiserIDCollectionEnabled">The setting for Advertiser ID collection</param>
            public static void SetAdvertiserIDCollectionEnabled(bool advertiserIDCollectionEnabled)
            {
                Mobile.MobileFacebookImpl.SetAdvertiserIDCollectionEnabled(advertiserIDCollectionEnabled);
            }

            /// <summary>
            /// Sets the setting for Advertiser Tracking Enabled.
            /// </summary>
            /// <param name="advertiserTrackingEnabled">The setting for Advertiser Tracking Enabled</param>
            public static bool SetAdvertiserTrackingEnabled(bool advertiserTrackingEnabled)
            {
                return Mobile.MobileFacebookImpl.SetAdvertiserTrackingEnabled(advertiserTrackingEnabled);
            }

            /// <summary>
            /// Sets the setting to enable/disable domain errors.
            /// </summary>
            /// <param name="domainErrorEnabled">The setting for enabling/disabling domain errors</param>
            public static bool SetDomainErrorEnabled(bool domainErrorEnabled)
            {
                return Mobile.MobileFacebookImpl.SetDomainErrorEnabled(domainErrorEnabled);
            }

            /// <summary>
            /// Sets the setting to enable/disable fast app switch for login.
            /// When enabled, users can switch to the Facebook app for authentication if installed.
            /// On iOS this is enabled by default, matching the native iOS SDK; pass false to opt out.
            /// </summary>
            /// <param name="fastAppSwitchEnabled">The setting for enabling/disabling fast app switch</param>
            public static bool SetFastAppSwitchEnabled(bool fastAppSwitchEnabled)
            {
                return Mobile.MobileFacebookImpl.SetFastAppSwitchEnabled(fastAppSwitchEnabled);
            }

            /// <summary>
            /// Sets device token in the purpose of uninstall tracking.
            /// </summary>
            /// <param name="token">The device token from APNs</param>
            public static void SetPushNotificationsDeviceTokenString(string token)
            {
                Mobile.MobileFacebookImpl.SetPushNotificationsDeviceTokenString(token);
            }

            public static void GetCatalog(FacebookDelegate<ICatalogResult> callback)
            {
                Mobile.MobileFacebookImpl.GetCatalog(callback);
            }

            public static void GetPurchases(FacebookDelegate<IPurchasesResult> callback)
            {
                Mobile.MobileFacebookImpl.GetPurchases(callback);
            }

            public static void Purchase(string productID, FacebookDelegate<IPurchaseResult> callback, string developerPayload = "")
            {
                Mobile.MobileFacebookImpl.Purchase(productID, callback, developerPayload);
            }
            public static void ConsumePurchase(string productToken, FacebookDelegate<IConsumePurchaseResult> callback)
            {
                Mobile.MobileFacebookImpl.ConsumePurchase(productToken, callback);
            }

            public static void LoadInterstitialAd(string placementID, FacebookDelegate<IInterstitialAdResult> callback)
            {
                Mobile.MobileFacebookImpl.LoadInterstitialAd(placementID, callback);
            }

            public static void ShowInterstitialAd(string placementID, FacebookDelegate<IInterstitialAdResult> callback)
            {
                Mobile.MobileFacebookImpl.ShowInterstitialAd(placementID, callback);
            }

            public static void LoadRewardedVideo(string placementID, FacebookDelegate<IRewardedVideoResult> callback)
            {
                Mobile.MobileFacebookImpl.LoadRewardedVideo(placementID, callback);
            }

            public static void ShowRewardedVideo(string placementID, FacebookDelegate<IRewardedVideoResult> callback)
            {
                Mobile.MobileFacebookImpl.ShowRewardedVideo(placementID, callback);
            }

            public static void OpenFriendFinderDialog(FacebookDelegate<IGamingServicesFriendFinderResult> callback)
            {
                Mobile.MobileFacebookImpl.OpenFriendFinderDialog(callback);
            }

            public static void ScheduleAppToUserNotification(string title, string body, Uri media, int timeInterval, string payload, FacebookDelegate<IScheduleAppToUserNotificationResult> callback)
            {
                Mobile.MobileFacebookImpl.ScheduleAppToUserNotification(title, body, media, timeInterval, payload, callback);
            }

            public static void PostSessionScore(int score, FacebookDelegate<ISessionScoreResult> callback)
            {
                Mobile.MobileFacebookImpl.PostSessionScore(score, callback);
            }

            public static void PostTournamentScore(int score, FacebookDelegate<ITournamentScoreResult> callback)
            {
                Mobile.MobileFacebookImpl.PostTournamentScore(score, callback);
            }

            public static void GetTournament(FacebookDelegate<ITournamentResult> callback)
            {
                Mobile.MobileFacebookImpl.GetTournament(callback);
            }

            public static void ShareTournament(int score, Dictionary<string, string> data, FacebookDelegate<ITournamentScoreResult> callback)
            {
                Mobile.MobileFacebookImpl.ShareTournament(score, data, callback);
            }

            public static void GetTournaments(FacebookDelegate<IGetTournamentsResult> callback)
            {
                Mobile.MobileFacebookImpl.GetTournaments(callback);
            }

            public static void UpdateTournament(string tournamentID, int score, FacebookDelegate<ITournamentScoreResult> callback)
            {
                Mobile.MobileFacebookImpl.UpdateTournament(tournamentID, score, callback);
            }

            public static void UpdateAndShareTournament(string tournamentID, int score, FacebookDelegate<IDialogResult> callback)
            {
                Mobile.MobileFacebookImpl.UpdateAndShareTournament(tournamentID, score, callback);
            }

            public static void CreateAndShareTournament(
                int initialScore,
                string title,
                TournamentSortOrder sortOrder,
                TournamentScoreFormat scoreFormat,
                DateTime endTime,
                string payload,
                FacebookDelegate<IDialogResult> callback)
            {
                Mobile.MobileFacebookImpl.CreateAndShareTournament(
                    initialScore,
                    title,
                    sortOrder,
                    scoreFormat,
                    (long)endTime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                    payload,
                    callback);
            }

            public static void UploadImageToMediaLibrary(string caption, Uri imageUri, bool shouldLaunchMediaDialog, FacebookDelegate<IMediaUploadResult> callback)
            {
                Mobile.MobileFacebookImpl.UploadImageToMediaLibrary(caption, imageUri, shouldLaunchMediaDialog, callback);
            }

            public static void UploadVideoToMediaLibrary(string caption, Uri videoUri, bool shouldLaunchMediaDialog, FacebookDelegate<IMediaUploadResult> callback)
            {
                Mobile.MobileFacebookImpl.UploadVideoToMediaLibrary(caption, videoUri, shouldLaunchMediaDialog, callback);
            }

        }

        /// <summary>
        /// Contains code specific to the Android Platform.
        /// </summary>
        public sealed class Android
        {
            /// <summary>
            /// Gets the key hash.
            /// </summary>
            /// <value>The key hash.</value>
            public static string KeyHash
            {
                get
                {
                    var androidFacebook = FacebookImpl as AndroidFacebook;
                    return (androidFacebook != null) ? androidFacebook.KeyHash : string.Empty;
                }
            }

            /// <summary>
            /// Retrieves the login status for the user. This will return an access token for the app if a user
            /// is logged into the Facebook for Android app on the same device and that user had previously
            /// logged into the app.If an access token was retrieved then a toast will be shown telling the
            /// user that they have been logged in.
            /// </summary>
            /// <param name="callback">The callback to be called when the request completes</param>
            public static void RetrieveLoginStatus(FacebookDelegate<ILoginStatusResult> callback)
            {
                var androidFacebook = FacebookImpl as AndroidFacebook;
                if (androidFacebook != null)
                {
                    androidFacebook.RetrieveLoginStatus(callback);
                }
            }
        }

        public sealed class Windows
        {
            private static IWindowsFacebook WindowsFacebookImpl
            {
                get
                {
                    IWindowsFacebook impl = FacebookImpl as IWindowsFacebook;
                    if (impl == null)
                    {
                        throw new InvalidOperationException("Attempt to call Windows interface on non Windows platform");
                    }

                    return impl;
                }
            }

            /// <summary>
            /// Sets the Virtual Gamepad Layout to use.
            /// </summary>
            /// <param name="layout">Name of the layout to use.</param>
            /// <param name="callback">Callback to be called when request completes.</param>
            public static void SetVirtualGamepadLayout(string layout, FacebookDelegate<IVirtualGamepadLayoutResult> callback)
            {
                Windows.WindowsFacebookImpl.SetVirtualGamepadLayout(layout, callback);
            }

            /// <summary>
            /// Open Virtual Keyboard in mobile devices.
            /// </summary>
            /// <param name="open"> true if you want to open the keyboard</param>
            /// <param name="callback">Callback to be called when request completes.</param>
            public static void SetSoftKeyboardOpen(bool open, FacebookDelegate<ISoftKeyboardOpenResult> callback)
            {
                Windows.WindowsFacebookImpl.SetSoftKeyboardOpen(open, callback);
            }

            /// <summary>
            /// Create a referral link
            /// </summary>
            /// <param name="payload">Custom payload to get by the game</param>
            public static void CreateReferral(string payload, FacebookDelegate<IReferralsCreateResult> callback)
            {
                Windows.WindowsFacebookImpl.CreateReferral(payload, callback);
            }

            /// <summary>
            /// Get Data from referral link
            /// </summary>
            public static void GetDataReferral(FacebookDelegate<IReferralsGetDataResult> callback)
            {
                Windows.WindowsFacebookImpl.GetDataReferral(callback);
            }
        }

        internal abstract class CompiledFacebookLoader : MonoBehaviour
        {
            protected abstract FacebookGameObject FBGameObject { get; }

            public void Start()
            {
                FB.facebook = this.FBGameObject.Facebook;
                FB.OnDLLLoadedDelegate();
                FB.LogVersion();
                MonoBehaviour.Destroy(this);
            }
        }
    }
}

public enum TournamentSortOrder
{
    HigherIsBetter,
    LowerIsBetter
}

public enum TournamentScoreFormat
{
    Numeric,
    Time
}
