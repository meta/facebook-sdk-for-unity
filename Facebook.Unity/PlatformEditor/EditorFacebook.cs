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

namespace Facebook.Unity.Editor
{
    using System;
    using System.Collections.Generic;
    using Facebook.Unity.Canvas;
    using Facebook.Unity.Editor.Dialogs;
    using Facebook.Unity.Mobile;

    internal class EditorFacebook : FacebookBase, IMobileFacebookImplementation, ICanvasFacebookImplementation
    {
        private const string WarningMessage = "You are using the facebook SDK in the Unity Editor. " +
            "Behavior may not be the same as when used on iOS, Android, or Web.";

        private const string AccessTokenKey = "com.facebook.unity.editor.accesstoken";

        private IEditorWrapper editorWrapper;

        public EditorFacebook(IEditorWrapper wrapper, CallbackManager callbackManager) : base(callbackManager)
        {
            this.editorWrapper = wrapper;
        }

        public EditorFacebook() : this(new EditorWrapper(EditorFacebook.EditorGameObject), new CallbackManager())
        {
        }

        public override bool LimitEventUsage { get; set; }

        public ShareDialogMode ShareDialogMode { get; set; }

        public override string SDKName
        {
            get
            {
                return "FBUnityEditorSDK";
            }
        }

        public override string SDKVersion
        {
            get
            {
                return Facebook.Unity.FacebookSdkVersion.Build;
            }
        }

        public string UserID { get; set; }

        private static IFacebookCallbackHandler EditorGameObject
        {
            get
            {
                return ComponentFactory.GetComponent<EditorFacebookGameObject>();
            }
        }

        public override void Init(InitDelegate onInitComplete)
        {
            // Warn that editor behavior will not match supported platforms
            FacebookLogger.Warn(WarningMessage);

            base.Init(onInitComplete);
            this.editorWrapper.Init();
        }

        public override void LogInWithReadPermissions(
            IEnumerable<string> permissions,
            FacebookDelegate<ILoginResult> callback)
        {
            // For the editor don't worry about the difference between
            // LogInWithReadPermissions and LogInWithPublishPermissions
            this.LogInWithPublishPermissions(permissions, callback);
        }

        public override void LogInWithPublishPermissions(
            IEnumerable<string> permissions,
            FacebookDelegate<ILoginResult> callback)
        {
            this.editorWrapper.ShowLoginMockDialog(
                this.OnLoginComplete,
                this.CallbackManager.AddFacebookDelegate(callback),
                permissions.ToCommaSeparateList());
        }

        public void EnableProfileUpdatesOnAccessTokenChange(bool enable)
        {
            FacebookLogger.Log("Pew! Pretending to enable Profile updates on access token change.  Doesn't actually work in the editor");
        }

        public void LoginWithTrackingPreference(
            string tracking,
            IEnumerable<string> permissions,
            string nonce,
            FacebookDelegate<ILoginResult> callback)
        {
            this.editorWrapper.ShowLoginMockDialog(
                this.OnLoginComplete,
                this.CallbackManager.AddFacebookDelegate(callback),
                permissions.ToCommaSeparateList());
        }

        public void RefreshLimitedLogin(
            RefreshFallbackPolicy fallbackPolicy,
            FacebookDelegate<ILoginResult> callback)
        {
            this.editorWrapper.ShowLoginMockDialog(
                this.OnRefreshLimitedLoginComplete,
                this.CallbackManager.AddFacebookDelegate(callback),
                "public_profile");
        }

        public void LoginWithSSO(
            IEnumerable<string> permissions,
            FacebookDelegate<ILoginResult> callback)
        {
            this.editorWrapper.ShowLoginMockDialog(
                this.OnLoginComplete,
                this.CallbackManager.AddFacebookDelegate(callback),
                permissions == null ? "public_profile" : permissions.ToCommaSeparateList());
        }

        public override void AppRequest(
            string message,
            OGActionType? actionType,
            string objectId,
            IEnumerable<string> to,
            IEnumerable<object> filters,
            IEnumerable<string> excludeIds,
            int? maxRecipients,
            string data,
            string title,
            FacebookDelegate<IAppRequestResult> callback)
        {
            this.editorWrapper.ShowAppRequestMockDialog(
                this.OnAppRequestsComplete,
                this.CallbackManager.AddFacebookDelegate(callback));
        }

        public override void ShareLink(
            Uri contentURL,
            string contentTitle,
            string contentDescription,
            Uri photoURL,
            FacebookDelegate<IShareResult> callback)
        {
            this.editorWrapper.ShowMockShareDialog(
                this.OnShareLinkComplete,
                "ShareLink",
                this.CallbackManager.AddFacebookDelegate(callback));
        }

        public override void FeedShare(
            string toId,
            Uri link,
            string linkName,
            string linkCaption,
            string linkDescription,
            Uri picture,
            string mediaSource,
            FacebookDelegate<IShareResult> callback)
        {
            this.editorWrapper.ShowMockShareDialog(
                this.OnShareLinkComplete,
                "FeedShare",
                this.CallbackManager.AddFacebookDelegate(callback));
        }

        public override void ActivateApp(string appId)
        {
            FacebookLogger.Log("Pew! Pretending to send this off.  Doesn't actually work in the editor");
        }

        public override void GetAppLink(FacebookDelegate<IAppLinkResult> callback)
        {
            var result = new Dictionary<string, object>();
            result[Constants.UrlKey] = "mockurl://testing.url";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnGetAppLinkComplete(new ResultContainer(result));
        }

        public override void AppEventsLogEvent(
            string logEvent,
            float? valueToSum,
            Dictionary<string, object> parameters)
        {
            FacebookLogger.Log("Pew! Pretending to send this off.  Doesn't actually work in the editor");
        }

        public override void AppEventsLogPurchase(
            float logPurchase,
            string currency,
            Dictionary<string, object> parameters)
        {
            FacebookLogger.Log("Pew! Pretending to send this off.  Doesn't actually work in the editor");
        }

        public bool IsImplicitPurchaseLoggingEnabled()
        {
            return true;
        }

        public void SetAutoLogAppEventsEnabled(bool autoLogAppEventsEnabled)
        {
            return;
        }

        public void SetAutoLogProductImprovementEnabled(bool autoLogProductImprovementEnabled)
        {
            return;
        }

        public void SetAutoLogMetaDataEnabled(bool autoLogMetaDataEnabled)
        {
            return;
        }

        public bool IsAutoLogMetaDataEnabled()
        {
            return true;
        }

        public void SetAdvertiserIDCollectionEnabled(bool advertiserIDCollectionEnabled)
        {
            return;
        }

        public bool SetAdvertiserTrackingEnabled(bool advertiserTrackingEnabled)
        {
            return true;
        }
        public bool SetDomainErrorEnabled(bool domainErrorEnabled)
        {
            return true;
        }
        public bool SetFastAppSwitchEnabled(bool fastAppSwitchEnabled)
        {
            return true;
        }

        public void SetPushNotificationsDeviceTokenString(string token)
        {
            return;
        }

        public void SetDataProcessingOptions(IEnumerable<string> options, int country, int state)
        {
            return;
        }

        public AuthenticationToken CurrentAuthenticationToken()
        {
            return null;
        }

        public override Profile CurrentProfile()
        {
            return null;
        }

        public override void CurrentProfile(FacebookDelegate<IProfileResult> callback)
        {
            var result = new Dictionary<string, object>()
            {
                { Constants.ErrorKey, "No current profile is available in the Unity Editor" },
                { Constants.CallbackIdKey, this.CallbackManager.AddFacebookDelegate(callback) },
            };
            this.OnCurrentProfileComplete(new ResultContainer(result));
        }

        public void FetchDeferredAppLink(
            FacebookDelegate<IAppLinkResult> callback)
        {
            var result = new Dictionary<string, object>();
            result[Constants.UrlKey] = "mockurl://testing.url";
            result[Constants.RefKey] = "mock ref";
            result[Constants.ExtrasKey] = new Dictionary<string, object>()
            {
                {
                    "mock extra key", "mock extra value"
                }
            };

            result[Constants.TargetUrlKey] = "mocktargeturl://mocktarget.url";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnFetchDeferredAppLinkComplete(new ResultContainer(result));
        }

        public void Pay(
            string product,
            string action,
            int quantity,
            int? quantityMin,
            int? quantityMax,
            string requestId,
            string pricepointId,
            string testCurrency,
            FacebookDelegate<IPayResult> callback)
        {
            this.editorWrapper.ShowPayMockDialog(
                this.OnPayComplete,
                this.CallbackManager.AddFacebookDelegate(callback));
        }

        public void PayWithProductId(
            string productId,
            string action,
            int quantity,
            int? quantityMin,
            int? quantityMax,
            string requestId,
            string pricepointId,
            string testCurrency,
            FacebookDelegate<IPayResult> callback)
        {
            this.editorWrapper.ShowPayMockDialog(
                this.OnPayComplete,
                this.CallbackManager.AddFacebookDelegate(callback));
        }

        public void PayWithProductId(
            string productId,
            string action,
            string developerPayload,
            string testCurrency,
            FacebookDelegate<IPayResult> callback)
        {
            this.editorWrapper.ShowPayMockDialog(
                this.OnPayComplete,
                this.CallbackManager.AddFacebookDelegate(callback));
        }

        public void RefreshCurrentAccessToken(
            FacebookDelegate<IAccessTokenRefreshResult> callback)
        {
            if (callback == null)
            {
                return;
            }

            var result = new Dictionary<string, object>()
            {
                { Constants.CallbackIdKey, this.CallbackManager.AddFacebookDelegate(callback) }
            };

            if (AccessToken.CurrentAccessToken == null)
            {
                result[Constants.ErrorKey] = "No current access token";
            }
            else
            {
                var accessTokenDic = (IDictionary<string, object>)MiniJSON.Json.Deserialize(
                    AccessToken.CurrentAccessToken.ToJson());

                result.AddAllKVPFrom(accessTokenDic);
            }

            this.OnRefreshCurrentAccessTokenComplete(new ResultContainer(result));
        }

        public override void OnAppRequestsComplete(ResultContainer resultContainer)
        {
            var result = new AppRequestResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public override void OnGetAppLinkComplete(ResultContainer resultContainer)
        {
            var result = new AppLinkResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public override void OnLoginComplete(ResultContainer resultContainer)
        {
            var result = new LoginResult(resultContainer);
            this.OnAuthResponse(result);
        }

        public override void OnShareLinkComplete(ResultContainer resultContainer)
        {
            var result = new ShareResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnFetchDeferredAppLinkComplete(ResultContainer resultContainer)
        {
            var result = new AppLinkResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnPayComplete(ResultContainer resultContainer)
        {
            var result = new PayResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnRefreshCurrentAccessTokenComplete(ResultContainer resultContainer)
        {
            var result = new AccessTokenRefreshResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnRefreshLimitedLoginComplete(ResultContainer resultContainer)
        {
            var result = new LoginResult(resultContainer);
            this.OnAuthResponse(result);
        }

        public void OnCurrentProfileComplete(ResultContainer resultContainer)
        {
            var result = new ProfileResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnFriendFinderComplete(ResultContainer resultContainer)
        {
            var result = new GamingServicesFriendFinderResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnFriendFinderInvitationsComplete(ResultContainer resultContainer)
        {
            var result = new FriendFinderInvitationResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnGetUserLocaleComplete(ResultContainer resultContainer)
        {
            var result = new LocaleResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnUploadImageToMediaLibraryComplete(ResultContainer resultContainer)
        {
            var result = new MediaUploadResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnUploadVideoToMediaLibraryComplete(ResultContainer resultContainer)
        {
            var result = new MediaUploadResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnOnIAPReadyComplete(ResultContainer resultContainer)
        {
            var result = new IAPReadyResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnGetCatalogComplete(ResultContainer resultContainer)
        {
            var result = new CatalogResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnGetPurchasesComplete(ResultContainer resultContainer)
        {
            var result = new PurchasesResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnPurchaseComplete(ResultContainer resultContainer)
        {
            var result = new PurchaseResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnConsumePurchaseComplete(ResultContainer resultContainer)
        {
            var result = new ConsumePurchaseResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnGetSubscribableCatalogComplete(ResultContainer resultContainer)
        {
            var result = new SubscribableCatalogResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnGetSubscriptionsComplete(ResultContainer resultContainer)
        {
            var result = new SubscriptionsResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);

        }

        public void OnPurchaseSubscriptionComplete(ResultContainer resultContainer)
        {
            var result = new SubscriptionResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnCancelSubscriptionComplete(ResultContainer resultContainer)
        {
            var result = new CancelSubscriptionResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnInitCloudGameComplete(ResultContainer resultContainer)
        {
            var result = new InitCloudGameResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnGameLoadCompleteComplete(ResultContainer resultContainer)
        {
            var result = new GameLoadCompleteResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnScheduleAppToUserNotificationComplete(ResultContainer resultContainer)
        {
            var result = new ScheduleAppToUserNotificationResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnLoadInterstitialAdComplete(ResultContainer resultContainer)
        {
            var result = new InterstitialAdResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnShowInterstitialAdComplete(ResultContainer resultContainer)
        {
            var result = new InterstitialAdResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnLoadRewardedVideoComplete(ResultContainer resultContainer)
        {
            var result = new RewardedVideoResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnShowRewardedVideoComplete(ResultContainer resultContainer)
        {
            var result = new RewardedVideoResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnGetPayloadComplete(ResultContainer resultContainer)
        {
            var result = new PayloadResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnPostSessionScoreComplete(ResultContainer resultContainer)
        {
            var result = new SessionScoreResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnPostTournamentScoreComplete(ResultContainer resultContainer)
        {
            var result = new TournamentScoreResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnGetTournamentComplete(ResultContainer resultContainer)
        {
            var result = new TournamentResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnShareTournamentComplete(ResultContainer resultContainer)
        {
            var result = new TournamentScoreResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnCreateTournamentComplete(ResultContainer resultContainer)
        {
            var result = new TournamentResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnGetTournamentsComplete(ResultContainer resultContainer)
        {
            var result = new GetTournamentsResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnUpdateTournamentComplete(ResultContainer resultContainer)
        {
            var result = new TournamentScoreResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnTournamentDialogSuccess(ResultContainer resultContainer)
        {
            var result = new TournamentResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnTournamentDialogCancel(ResultContainer resultContainer)
        {
            var result = new AbortDialogResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnTournamentDialogError(ResultContainer resultContainer)
        {
            var result = new AbortDialogResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnOpenAppStoreComplete(ResultContainer resultContainer)
        {
            var result = new OpenAppStoreResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnCreateGamingContextComplete(ResultContainer resultContainer)
        {
            var result = new CreateGamingContextResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnSwitchGamingContextComplete(ResultContainer resultContainer)
        {
            var result = new SwitchGamingContextResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnChooseGamingContextComplete(ResultContainer resultContainer)
        {
            var result = new ChooseGamingContextResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public void OnGetCurrentGamingContextComplete(ResultContainer resultContainer)
        {
            var result = new GetCurrentGamingContextResult(resultContainer);
            CallbackManager.OnFacebookResponse(result);
        }

        public override void OpenFriendFinderDialog(FacebookDelegate<IGamingServicesFriendFinderResult> callback)
        {
            this.editorWrapper.ShowMockFriendFinderDialog(
                this.OnFriendFinderComplete,
                "Friend Finder Dialog",
                this.CallbackManager.AddFacebookDelegate(callback));
        }

        public override void GetFriendFinderInvitations(FacebookDelegate<IFriendFinderInvitationResult> callback)
        {
            var result = new Dictionary<string, object>();
            result[FriendFinderInvitationResult.InvitationsKey] = new List<FriendFinderInviation>();
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnFriendFinderInvitationsComplete(new ResultContainer(result));
        }

        public override void DeleteFriendFinderInvitation(string invitationId, FacebookDelegate<IFriendFinderInvitationResult> callback)
        {
            var result = new Dictionary<string, object>();
            result[FriendFinderInvitationResult.InvitationsKey] = new List<FriendFinderInviation>();
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnFriendFinderInvitationsComplete(new ResultContainer(result));
        }

        public override void UploadImageToMediaLibrary(
            string caption,
            Uri imageUri,
            bool shouldLaunchMediaDialog,
            FacebookDelegate<IMediaUploadResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["id"] = "1232453";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnUploadImageToMediaLibraryComplete(new ResultContainer(result));
        }

        public override void UploadVideoToMediaLibrary(
            string caption,
            Uri imageUri,
            bool shouldLaunchMediaDialog,
            FacebookDelegate<IMediaUploadResult> callback)
        {
            // construct a dummy ResultContainer
            // to pretend we actually did an upload
            var result = new Dictionary<string, object>();
            result["video_id"] = "456789";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnUploadVideoToMediaLibraryComplete(new ResultContainer(result));
        }

        public override void GetUserLocale(FacebookDelegate<ILocaleResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["locale"] = "en_US";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnGetUserLocaleComplete(new ResultContainer(result));
        }

        public void OnIAPReady(FacebookDelegate<IIAPReadyResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnOnIAPReadyComplete(new ResultContainer(result));
        }

        public override void GetCatalog(FacebookDelegate<ICatalogResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "[{\"productID\":\"123\",\"title\":\"item\",\"price\":\"$0.99\",\"priceAmount\":\"0.99\",\"priceCurrencyCode\":\"USD\"}]";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnGetCatalogComplete(new ResultContainer(result));
        }

        public override void GetPurchases(FacebookDelegate<IPurchasesResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "[{\"isConsumed\":true,\"paymentID\":\"2607915835989565\",\"productID\":\"123\",\"purchasePlatform\":\"FB\",\"purchaseTime\":1583797821,\"purchaseToken\":\"1655700687901784\",\"signedRequest\":\"abc123ZYZ\"}]";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnGetPurchasesComplete(new ResultContainer(result));
        }

        public override void Purchase(string productID, FacebookDelegate<IPurchaseResult> callback, string developerPayload = "")
        {
            var result = new Dictionary<string, object>();
            result["success"] = "{\"isConsumed\":true,\"paymentID\":\"2607915835989565\",\"productID\":\"123\",\"purchasePlatform\":\"FB\",\"purchaseTime\":1583797821,\"purchaseToken\":\"1655700687901784\",\"signedRequest\":\"XZZ9xQDHOGulfhZMRVQ8UC-TadAqFrueYveAAqxock.eyJhbGdvcm10aG0iOiJITUFDLVNIQTI1Nilslm...\"}";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnPurchaseComplete(new ResultContainer(result));
        }

        public override void ConsumePurchase(string purchaseToken, FacebookDelegate<IConsumePurchaseResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnConsumePurchaseComplete(new ResultContainer(result));
        }

        public override void GetSubscribableCatalog(FacebookDelegate<ISubscribableCatalogResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "[{\"productID\":\"123\",\"title\":\"item\",\"price\":\"$0.99\",\"priceAmount\":0.99,\"priceCurrencyCode\":\"USD\",\"subscriptionTerm\":\"MONTHLY\"}]";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnGetSubscribableCatalogComplete(new ResultContainer(result));
        }

        public override void GetSubscriptions(FacebookDelegate<ISubscriptionsResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "[{\"deactivationTime\":1588785783,\"isEntitlementActive\":true,\"periodStartTime\":1583797821,\"periodEndTime\":1584516124,\"productID\":\"123\",\"purchasePlatform\":\"FB\",\"purchasePrice\":{\"amount\":\"0.99\",\"currency\":\"USD\"},\"purchaseTime\":1583797821,\"purchaseToken\":\"1655700687901784\",\"status\":\"ACTIVE\",\"signedRequest\":\"abc123ZYZ\",\"subscriptionTerm\":\"MONTHLY\"}]";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnGetSubscriptionsComplete(new ResultContainer(result));
        }

        public override void PurchaseSubscription(string productID, FacebookDelegate<ISubscriptionResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "{\"deactivationTime\":1588785783,\"isEntitlementActive\":true,\"periodStartTime\":1583797821,\"periodEndTime\":1584516124,\"productID\":\"123\",\"purchasePlatform\":\"FB\",\"purchasePrice\":{\"amount\":\"0.99\",\"currency\":\"USD\"},\"purchaseTime\":1583797821,\"purchaseToken\":\"1655700687901784\",\"status\":\"ACTIVE\",\"signedRequest\":\"abc123ZYZ\",\"subscriptionTerm\":\"MONTHLY\"}";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnPurchaseSubscriptionComplete(new ResultContainer(result));
        }

        public override void CancelSubscription(string purchaseToken, FacebookDelegate<ICancelSubscriptionResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnCancelSubscriptionComplete(new ResultContainer(result));
        }

        public void InitCloudGame(FacebookDelegate<IInitCloudGameResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnInitCloudGameComplete(new ResultContainer(result));
        }

        public void GameLoadComplete(FacebookDelegate<IGameLoadCompleteResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnGameLoadCompleteComplete(new ResultContainer(result));
        }

        public override void ScheduleAppToUserNotification(
            string title,
            string body,
            Uri media,
            int timeInterval,
            string payload,
            FacebookDelegate<IScheduleAppToUserNotificationResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnScheduleAppToUserNotificationComplete(new ResultContainer(result));
        }

        public override void LoadInterstitialAd(string placementID, FacebookDelegate<IInterstitialAdResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnLoadInterstitialAdComplete(new ResultContainer(result));
        }

        public override void ShowInterstitialAd(string placementID, FacebookDelegate<IInterstitialAdResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnShowInterstitialAdComplete(new ResultContainer(result));
        }

        public override void LoadRewardedVideo(string placementID, FacebookDelegate<IRewardedVideoResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnLoadRewardedVideoComplete(new ResultContainer(result));
        }

        public override void ShowRewardedVideo(string placementID, FacebookDelegate<IRewardedVideoResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnShowRewardedVideoComplete(new ResultContainer(result));
        }

        public void GetPayload(FacebookDelegate<IPayloadResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "{\"key\":\"test\",\"value\":\"123\"}";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnGetPayloadComplete(new ResultContainer(result));
        }

        public override void PostSessionScore(int score, FacebookDelegate<ISessionScoreResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnPostSessionScoreComplete(new ResultContainer(result));
        }

        public override void PostTournamentScore(int score, FacebookDelegate<ITournamentScoreResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnPostTournamentScoreComplete(new ResultContainer(result));
        }

        public override void GetTournament(FacebookDelegate<ITournamentResult> callback)
        {
            var result = CreateMockTournamentResult();
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnGetTournamentComplete(new ResultContainer(result));
        }

        public override void ShareTournament(int score, Dictionary<string, string> data, FacebookDelegate<ITournamentScoreResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnShareTournamentComplete(new ResultContainer(result));
        }

        public override void CreateTournament(
            int initialScore,
            string title,
            string imageBase64DataUrl,
            string sortOrder,
            string scoreFormat,
            Dictionary<string, string> data,
            FacebookDelegate<ITournamentResult> callback)
        {
            var result = CreateMockTournamentResult();
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnCreateTournamentComplete(new ResultContainer(result));
        }

        public void GetTournaments(FacebookDelegate<IGetTournamentsResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["0"] = CreateMockTournamentResult();
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnGetTournamentsComplete(new ResultContainer(result));
        }

        public void UpdateTournament(string tournamentID, int score, FacebookDelegate<ITournamentScoreResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnUpdateTournamentComplete(new ResultContainer(result));
        }

        public void UpdateAndShareTournament(string tournamentID, int score, FacebookDelegate<IDialogResult> callback)
        {
            var result = CreateMockTournamentResult();
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnTournamentDialogSuccess(new ResultContainer(result));
        }

        public void CreateAndShareTournament(
            int initialScore,
            string title,
            TournamentSortOrder sortOrder,
            TournamentScoreFormat scoreFormat,
            long endTime,
            string payload,
            FacebookDelegate<IDialogResult> callback)
        {
            var result = CreateMockTournamentResult();
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnTournamentDialogSuccess(new ResultContainer(result));
        }

        public void OpenAppStore(FacebookDelegate<IOpenAppStoreResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["success"] = "";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnOpenAppStoreComplete(new ResultContainer(result));
        }

        public void CreateGamingContext(string playerID, FacebookDelegate<ICreateGamingContextResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["contextId"] = "1234";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnCreateGamingContextComplete(new ResultContainer(result));
        }

        public void SwitchGamingContext(string gamingContextID, FacebookDelegate<ISwitchGamingContextResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["contextId"] = "1234";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnSwitchGamingContextComplete(new ResultContainer(result));
        }

        public void ChooseGamingContext(List<string> filters, int minSize, int maxSize, FacebookDelegate<IChooseGamingContextResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["contextId"] = "1234";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnChooseGamingContextComplete(new ResultContainer(result));
        }

        public void GetCurrentGamingContext(FacebookDelegate<IGetCurrentGamingContextResult> callback)
        {
            var result = new Dictionary<string, object>();
            result["contextId"] = "1234";
            result[Constants.CallbackIdKey] = this.CallbackManager.AddFacebookDelegate(callback);
            this.OnGetCurrentGamingContextComplete(new ResultContainer(result));
        }

        private static Dictionary<string, object> CreateMockTournamentResult()
        {
            return new Dictionary<string, object>()
            {
                { "tournament_id", "123" },
                { "context_id", "456" },
                { "end_time", 456 },
                { "payload", new Dictionary<string, string>() },
            };
        }

        #region Canvas Dummy Methods

        public void OnFacebookAuthResponseChange(ResultContainer resultContainer)
        {
            throw new NotSupportedException();
        }

        public void OnUrlResponse(string message)
        {
            throw new NotSupportedException();
        }

        public void OnHideUnity(bool hidden)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
