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

namespace Facebook.Unity.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    internal sealed class WindowsFacebook : FacebookBase,
    IWindowsFacebookImplementation
    {
        private string appId;
        private IWindowsWrapper windowsWrapper;

        public WindowsFacebook() : this(GetWindowsWrapper(), new CallbackManager())
        {
        }

        public WindowsFacebook(IWindowsWrapper windowsWrapper, CallbackManager callbackManager) : base(callbackManager)
        {
            this.windowsWrapper = windowsWrapper;
        }

        public delegate void OnComplete(ResultContainer resultContainer);

        public override bool LimitEventUsage { get; set; }

        public override string SDKName
        {
            get
            {
                return "FBWindowsSDK";
            }
        }

        public override string SDKVersion
        {
            get
            {
                return "1.0.15";
            }
        }
        public void Init(
            string appId,
            string clientToken,
            HideUnityDelegate hideUnityDelegate,
            InitDelegate onInitComplete)
        {
            this.appId = appId;
            Initialized = this.windowsWrapper.Init(appId, clientToken);
            onInitComplete.Invoke();
        }

        public override void LogInWithPublishPermissions(IEnumerable<string> scope, FacebookDelegate<ILoginResult> callback)
        {
            this.windowsWrapper.LogInWithScopes(scope, this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void LogInWithReadPermissions(IEnumerable<string> scope, FacebookDelegate<ILoginResult> callback)
        {
            this.windowsWrapper.LogInWithScopes(scope, this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void LogOut()
        {
            this.windowsWrapper.LogOut();
        }

        public override bool LoggedIn
        {
            get
            {
                return this.windowsWrapper.IsLoggedIn();
            }
        }

        public override void ActivateApp(string appId = null)
        {
            this.AppEventsLogEvent(
                AppEventName.ActivatedApp,
                null,
                new Dictionary<string, object>());
        }

        public override void AppEventsLogEvent(string logEvent, float? valueToSum, Dictionary<string, object> parameters)
        {
            var appEventParameters = parameters != null ? parameters : new Dictionary<string, object>();

            appEventParameters.Add("_eventName", logEvent);
            if (valueToSum.HasValue)
            {
                appEventParameters.Add("_valueToSum", valueToSum.Value);
            }

            var formData = new Dictionary<string, string>
            {
                { "event", "CUSTOM_APP_EVENTS" },
                { "application_tracking_enabled", "0" },
                { "advertiser_tracking_enabled", "0" },
                { "custom_events", string.Format("[{0}]", MiniJSON.Json.Serialize(appEventParameters)) },
            };

            FB.API(
                string.Format("{0}/activities", this.appId),
                HttpMethod.POST,
                null,
                formData);
        }

        public override void AppEventsLogPurchase(
            float logPurchase,
            string currency,
            Dictionary<string, object> parameters)
        {
            if (parameters == null)
            {
                parameters = new Dictionary<string, object>();
            }

            parameters.Add("currency", currency);
            this.AppEventsLogEvent(
                AppEventName.Purchased,
                logPurchase,
                parameters);
        }

        public override void GetCatalog(FacebookDelegate<ICatalogResult> callback)
        {
            this.windowsWrapper.GetCatalog(this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void GetPurchases(FacebookDelegate<IPurchasesResult> callback)
        {
            this.windowsWrapper.GetPurchases(this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void Purchase(string productID, FacebookDelegate<IPurchaseResult> callback, string developerPayload = "")
        {
            this.windowsWrapper.Purchase(productID, developerPayload, this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void ConsumePurchase(string productToken, FacebookDelegate<IConsumePurchaseResult> callback)
        {
            this.windowsWrapper.ConsumePurchase(productToken, this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void GetSubscribableCatalog(FacebookDelegate<ISubscribableCatalogResult> callback)
        {
            throw new NotImplementedException();
        }

        public override void GetSubscriptions(FacebookDelegate<ISubscriptionsResult> callback)
        {
            throw new NotImplementedException();
        }

        public override void PurchaseSubscription(string productID, FacebookDelegate<ISubscriptionResult> callback)
        {
            throw new NotImplementedException();
        }

        public override void CancelSubscription(string purchaseToken, FacebookDelegate<ICancelSubscriptionResult> callback)
        {
            throw new NotImplementedException();
        }

        public override void CurrentProfile(FacebookDelegate<IProfileResult> callback)
        {
            this.windowsWrapper.CurrentProfile(this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void LoadInterstitialAd(string placementID, FacebookDelegate<IInterstitialAdResult> callback)
        {
            this.windowsWrapper.LoadInterstitialAd(placementID, this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void ShowInterstitialAd(string placementID, FacebookDelegate<IInterstitialAdResult> callback)
        {
            this.windowsWrapper.ShowInterstitialAd(placementID, this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void LoadRewardedVideo(string placementID, FacebookDelegate<IRewardedVideoResult> callback)
        {
            this.windowsWrapper.LoadRewardedVideo(placementID, this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void ShowRewardedVideo(string placementID, FacebookDelegate<IRewardedVideoResult> callback)
        {
            this.windowsWrapper.ShowRewardedVideo(placementID, this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void OpenFriendFinderDialog(FacebookDelegate<IGamingServicesFriendFinderResult> callback)
        {
            this.windowsWrapper.OpenFriendFinderDialog(this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void GetFriendFinderInvitations(FacebookDelegate<IFriendFinderInvitationResult> callback)
        {
            this.windowsWrapper.GetFriendFinderInvitations(this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void DeleteFriendFinderInvitation(string invitationId, FacebookDelegate<IFriendFinderInvitationResult> callback)
        {
            this.windowsWrapper.DeleteFriendFinderInvitation(invitationId, this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void ScheduleAppToUserNotification(string title, string body, Uri media, int timeInterval, string payload, FacebookDelegate<IScheduleAppToUserNotificationResult> callback)
        {
            this.windowsWrapper.ScheduleAppToUserNotification(title, body, media, timeInterval, payload, this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void PostSessionScore(int score, FacebookDelegate<ISessionScoreResult> callback)
        {
            this.windowsWrapper.PostSessionScore(score, this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void PostTournamentScore(int score, FacebookDelegate<ITournamentScoreResult> callback)
        {
            this.windowsWrapper.PostTournamentScore(score, this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void GetTournament(FacebookDelegate<ITournamentResult> callback)
        {
            this.windowsWrapper.GetTournament(this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void ShareTournament(int score, Dictionary<string, string> data, FacebookDelegate<ITournamentScoreResult> callback)
        {
            this.windowsWrapper.ShareTournament(score, data, this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void CreateTournament(int initialScore, string title, string imageBase64DataUrl, string sortOrder, string scoreFormat, Dictionary<string, string> data, FacebookDelegate<ITournamentResult> callback)
        {
            this.windowsWrapper.CreateTournament(initialScore, title, imageBase64DataUrl, sortOrder, scoreFormat, data, this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void UploadImageToMediaLibrary(string caption, Uri imageUri, bool shouldLaunchMediaDialog, string travelId, FacebookDelegate<IMediaUploadResult> callback)
        {
            this.windowsWrapper.UploadImageToMediaLibrary(caption, imageUri, shouldLaunchMediaDialog, travelId, this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void UploadVideoToMediaLibrary(string caption, Uri videoUri, bool shouldLaunchMediaDialog, string travelId, FacebookDelegate<IMediaUploadResult> callback)
        {
            this.windowsWrapper.UploadVideoToMediaLibrary(caption, videoUri, shouldLaunchMediaDialog, travelId, this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void UploadImageToMediaLibrary(string caption, Uri imageUri, bool shouldLaunchMediaDialog, FacebookDelegate<IMediaUploadResult> callback)
        {
            this.windowsWrapper.UploadImageToMediaLibrary(caption, imageUri, shouldLaunchMediaDialog, "", this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);

        }

        public override void UploadVideoToMediaLibrary(string caption, Uri videoUri, bool shouldLaunchMediaDialog, FacebookDelegate<IMediaUploadResult> callback)
        {
            this.windowsWrapper.UploadVideoToMediaLibrary(caption, videoUri, shouldLaunchMediaDialog, "", this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public override void GetUserLocale(FacebookDelegate<ILocaleResult> callback)
        {
            this.windowsWrapper.GetUserLocale(this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        private static IWindowsWrapper GetWindowsWrapper()
        {
            Assembly assembly = Assembly.Load("Facebook.Unity.Windows");
            Type type = assembly.GetType("Facebook.Unity.Windows.WindowsWrapper");
            IWindowsWrapper windowsWrapper = (IWindowsWrapper)Activator.CreateInstance(type);
            return windowsWrapper;
        }

        // Only Windows SDK
        public void Tick()
        {
            this.windowsWrapper.Tick();
        }

        public void Deinit()
        {
            this.windowsWrapper.Deinit();
        }

        public void SetVirtualGamepadLayout(string layout, FacebookDelegate<IVirtualGamepadLayoutResult> callback)
        {
            this.windowsWrapper.SetVirtualGamepadLayout(layout, this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public void SetSoftKeyboardOpen(bool open, FacebookDelegate<ISoftKeyboardOpenResult> callback)
        {
            this.windowsWrapper.SetSoftKeyboardOpen(open, this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public void CreateReferral(string payload, FacebookDelegate<IReferralsCreateResult> callback)
        {
            this.windowsWrapper.CreateReferral(payload, this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        public void GetDataReferral(FacebookDelegate<IReferralsGetDataResult> callback)
        {
            this.windowsWrapper.GetDataReferral(this.CallbackManager.AddFacebookDelegate(callback), this.CallbackManager);
        }

        // Not supported by Windows SDK
        public override void AppRequest(string message, OGActionType? actionType, string objectId, IEnumerable<string> to, IEnumerable<object> filters, IEnumerable<string> excludeIds, int? maxRecipients, string data, string title, FacebookDelegate<IAppRequestResult> callback)
        {
            throw new NotSupportedException();
        }
        public override void OnAppRequestsComplete(ResultContainer resultContainer)
        {
            throw new NotSupportedException();
        }
        public override void OnLoginComplete(ResultContainer resultContainer)
        {
            throw new NotSupportedException();
        }
        public override void FeedShare(string toId, Uri link, string linkName, string linkCaption, string linkDescription, Uri picture, string mediaSource, FacebookDelegate<IShareResult> callback)
        {
            throw new NotSupportedException();
        }
        public override void ShareLink(Uri contentURL, string contentTitle, string contentDescription, Uri photoURL, FacebookDelegate<IShareResult> callback)
        {
            throw new NotSupportedException();
        }
        public override void GetAppLink(FacebookDelegate<IAppLinkResult> callback)
        {
            throw new NotSupportedException();
        }
        public override void OnShareLinkComplete(ResultContainer resultContainer)
        {
            throw new NotSupportedException();
        }
        public override void OnGetAppLinkComplete(ResultContainer resultContainer)
        {
            throw new NotSupportedException();
        }
        public void Pay(string product, string action, int quantity, int? quantityMin, int? quantityMax, string requestId, string pricepointId, string testCurrency, FacebookDelegate<IPayResult> callback)
        {
            throw new NotSupportedException();
        }
        public void PayWithProductId(string productId, string action, int quantity, int? quantityMin, int? quantityMax, string requestId, string pricepointId, string testCurrency, FacebookDelegate<IPayResult> callback)
        {
            throw new NotSupportedException();
        }
        public void PayWithProductId(string productId, string action, string developerPayload, string testCurrency, FacebookDelegate<IPayResult> callback)
        {
            throw new NotSupportedException();
        }
        public override Profile CurrentProfile()
        {
            throw new NotSupportedException();
        }
    }
}
