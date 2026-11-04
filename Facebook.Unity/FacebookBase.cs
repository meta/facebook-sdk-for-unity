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
    using System.Linq;
    using UnityEngine;

    internal abstract class FacebookBase : IFacebookImplementation
    {
        private InitDelegate onInitCompleteDelegate;

        protected FacebookBase(CallbackManager callbackManager)
        {
            this.CallbackManager = callbackManager;
        }

        public abstract bool LimitEventUsage { get; set; }

        public abstract string SDKName { get; }

        public abstract string SDKVersion { get; }

        public virtual string SDKUserAgent
        {
            get
            {
                return Utilities.GetUserAgent(this.SDKName, this.SDKVersion);
            }
        }

        public virtual bool LoggedIn
        {
            get
            {
                AccessToken token = AccessToken.CurrentAccessToken;
                return token != null && token.ExpirationTime > DateTime.UtcNow;
            }
        }

        public bool Initialized { get; set; }

        protected CallbackManager CallbackManager { get; private set; }

        public virtual void Init(InitDelegate onInitComplete)
        {
            this.onInitCompleteDelegate = onInitComplete;
        }

        public abstract void LogInWithPublishPermissions(
            IEnumerable<string> scope,
            FacebookDelegate<ILoginResult> callback);

        public abstract void LogInWithReadPermissions(
            IEnumerable<string> scope,
            FacebookDelegate<ILoginResult> callback);

        public virtual void LogOut()
        {
            AccessToken.CurrentAccessToken = null;
        }

        public void AppRequest(
            string message,
            IEnumerable<string> to = null,
            IEnumerable<object> filters = null,
            IEnumerable<string> excludeIds = null,
            int? maxRecipients = null,
            string data = "",
            string title = "",
            FacebookDelegate<IAppRequestResult> callback = null)
        {
            this.AppRequest(message, null, null, to, filters, excludeIds, maxRecipients, data, title, callback);
        }

        public abstract void AppRequest(
            string message,
            OGActionType? actionType,
            string objectId,
            IEnumerable<string> to,
            IEnumerable<object> filters,
            IEnumerable<string> excludeIds,
            int? maxRecipients,
            string data,
            string title,
            FacebookDelegate<IAppRequestResult> callback);

        public abstract void ShareLink(
            Uri contentURL,
            string contentTitle,
            string contentDescription,
            Uri photoURL,
            FacebookDelegate<IShareResult> callback);

        public abstract void FeedShare(
            string toId,
            Uri link,
            string linkName,
            string linkCaption,
            string linkDescription,
            Uri picture,
            string mediaSource,
            FacebookDelegate<IShareResult> callback);

        public void API(
            string query,
            HttpMethod method,
            IDictionary<string, string> formData,
            FacebookDelegate<IGraphResult> callback)
        {
            IDictionary<string, string> inputFormData;

            // Copy the formData by value so it's not vulnerable to scene changes and object deletions
            inputFormData = (formData != null) ? this.CopyByValue(formData) : new Dictionary<string, string>();
            if (!inputFormData.ContainsKey(Constants.AccessTokenKey) && !query.Contains("access_token="))
            {
                inputFormData[Constants.AccessTokenKey] =
                    FB.IsLoggedIn ? AccessToken.CurrentAccessToken.TokenString : string.Empty;
            }

            FBUnityUtility.AsyncRequestStringWrapper.Request(this.GetGraphUrl(query), method, inputFormData, callback);
        }

        public void API(
            string query,
            HttpMethod method,
            WWWForm formData,
            FacebookDelegate<IGraphResult> callback)
        {
            if (formData == null)
            {
                formData = new WWWForm();
            }

            string tokenString = (AccessToken.CurrentAccessToken != null) ?
                AccessToken.CurrentAccessToken.TokenString : string.Empty;
            formData.AddField(
                Constants.AccessTokenKey,
                tokenString);

            FBUnityUtility.AsyncRequestStringWrapper.Request(this.GetGraphUrl(query), method, formData, callback);
        }

        public abstract void ActivateApp(string appId = null);

        public abstract void GetAppLink(FacebookDelegate<IAppLinkResult> callback);

        public abstract void AppEventsLogEvent(
            string logEvent,
            float? valueToSum,
            Dictionary<string, object> parameters);

        public abstract void AppEventsLogPurchase(
            float logPurchase,
            string currency,
            Dictionary<string, object> parameters);

        public virtual void OnInitComplete(ResultContainer resultContainer)
        {
            this.Initialized = true;

            // Wait for the parsing of login to complete since we may need to pull
            // in more info about the access token returned
            FacebookDelegate<ILoginResult> loginCallback = (ILoginResult result) =>
            {
                if (this.onInitCompleteDelegate != null)
                {
                    this.onInitCompleteDelegate();
                }
            };

            resultContainer.ResultDictionary[Constants.CallbackIdKey]
                = this.CallbackManager.AddFacebookDelegate(loginCallback);
            this.OnLoginComplete(resultContainer);
        }

        public abstract void OnLoginComplete(ResultContainer resultContainer);

        public void OnLogoutComplete(ResultContainer resultContainer)
        {
            AccessToken.CurrentAccessToken = null;
        }

        public abstract void OnGetAppLinkComplete(ResultContainer resultContainer);

        public abstract void OnAppRequestsComplete(ResultContainer resultContainer);

        public abstract void OnShareLinkComplete(ResultContainer resultContainer);

        protected void ValidateAppRequestArgs(
            string message,
            OGActionType? actionType,
            string objectId,
            IEnumerable<string> to = null,
            IEnumerable<object> filters = null,
            IEnumerable<string> excludeIds = null,
            int? maxRecipients = null,
            string data = "",
            string title = "",
            FacebookDelegate<IAppRequestResult> callback = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException("message", "message cannot be null or empty!");
            }

            if (!string.IsNullOrEmpty(objectId) && !(actionType == OGActionType.ASKFOR || actionType == OGActionType.SEND))
            {
                throw new ArgumentNullException("objectId", "objectId must be set if and only if action type is SEND or ASKFOR");
            }

            if (actionType == null && !string.IsNullOrEmpty(objectId))
            {
                throw new ArgumentNullException("actionType", "actionType must be specified if objectId is provided");
            }

            if (to != null && to.Any(toWhom => string.IsNullOrEmpty(toWhom)))
            {
                throw new ArgumentNullException("to", "'to' cannot contain any null or empty strings");
            }
        }

        protected virtual void OnAuthResponse(LoginResult result)
        {
            // If the login is cancelled we won't have a access token.
            // Don't overwrite a valid token
            if (result.AccessToken != null)
            {
                AccessToken.CurrentAccessToken = result.AccessToken;
            }

            this.CallbackManager.OnFacebookResponse(result);
        }

        private IDictionary<string, string> CopyByValue(IDictionary<string, string> data)
        {
            var newData = new Dictionary<string, string>(data.Count);
            foreach (KeyValuePair<string, string> kvp in data)
            {
                newData[kvp.Key] = kvp.Value != null ? new string(kvp.Value.ToCharArray()) : null;
            }

            return newData;
        }

        private Uri GetGraphUrl(string query)
        {
            if (!string.IsNullOrEmpty(query) && query.StartsWith("/"))
            {
                query = query.Substring(1);
            }

            return new Uri(Constants.GraphUrl, query);
        }

        public abstract void GetCatalog(FacebookDelegate<ICatalogResult> callback);

        public abstract void GetPurchases(FacebookDelegate<IPurchasesResult> callback);

        public abstract void Purchase(string productID, FacebookDelegate<IPurchaseResult> callback, string developerPayload = "");

        public abstract void ConsumePurchase(string productToken, FacebookDelegate<IConsumePurchaseResult> callback);

        public abstract void GetSubscribableCatalog(FacebookDelegate<ISubscribableCatalogResult> callback);

        public abstract void GetSubscriptions(FacebookDelegate<ISubscriptionsResult> callback);

        public abstract void PurchaseSubscription(string productToken, FacebookDelegate<ISubscriptionResult> callback);

        public abstract void CancelSubscription(string purchaseToken, FacebookDelegate<ICancelSubscriptionResult> callback);

        public abstract Profile CurrentProfile();

        public abstract void CurrentProfile(FacebookDelegate<IProfileResult> callback);

        public abstract void LoadInterstitialAd(string placementID, FacebookDelegate<IInterstitialAdResult> callback);

        public abstract void ShowInterstitialAd(string placementID, FacebookDelegate<IInterstitialAdResult> callback);

        public abstract void LoadRewardedVideo(string placementID, FacebookDelegate<IRewardedVideoResult> callback);

        public abstract void ShowRewardedVideo(string placementID, FacebookDelegate<IRewardedVideoResult> callback);

        public abstract void OpenFriendFinderDialog(FacebookDelegate<IGamingServicesFriendFinderResult> callback);

        public abstract void GetFriendFinderInvitations(FacebookDelegate<IFriendFinderInvitationResult> callback);

        public abstract void DeleteFriendFinderInvitation(string invitationId, FacebookDelegate<IFriendFinderInvitationResult> callback);

        public abstract void ScheduleAppToUserNotification(string title, string body, Uri media, int timeInterval, string payload, FacebookDelegate<IScheduleAppToUserNotificationResult> callback);

        public abstract void PostSessionScore(int score, FacebookDelegate<ISessionScoreResult> callback);

        public abstract void PostTournamentScore(int score, FacebookDelegate<ITournamentScoreResult> callback);

        public abstract void GetTournament(FacebookDelegate<ITournamentResult> callback);

        public abstract void ShareTournament(int score, Dictionary<string, string> data, FacebookDelegate<ITournamentScoreResult> callback);

        public abstract void CreateTournament(int initialScore, string title, string imageBase64DataUrl, string sortOrder, string scoreFormat, Dictionary<string, string> data, FacebookDelegate<ITournamentResult> callback);

        public abstract void UploadImageToMediaLibrary(string caption, Uri imageUri, bool shouldLaunchMediaDialog, FacebookDelegate<IMediaUploadResult> callback);

        public abstract void UploadVideoToMediaLibrary(string caption, Uri videoUri, bool shouldLaunchMediaDialog, FacebookDelegate<IMediaUploadResult> callback);

        // travelId is a Windows-only extension. Other implementations retain
        // their normal upload behavior when callers use this overload.
        public virtual void UploadImageToMediaLibrary(string caption, Uri imageUri, bool shouldLaunchMediaDialog, string travelId, FacebookDelegate<IMediaUploadResult> callback)
        {
            this.UploadImageToMediaLibrary(caption, imageUri, shouldLaunchMediaDialog, callback);
        }

        public virtual void UploadVideoToMediaLibrary(string caption, Uri videoUri, bool shouldLaunchMediaDialog, string travelId, FacebookDelegate<IMediaUploadResult> callback)
        {
            this.UploadVideoToMediaLibrary(caption, videoUri, shouldLaunchMediaDialog, callback);
        }

        public abstract void GetUserLocale(FacebookDelegate<ILocaleResult> callback);
    }
}
