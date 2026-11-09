using System;
using System.Collections.Generic;
using Facebook.Unity;
using UnityEngine;
#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
using Unity.Scripting.LifecycleManagement;
#endif

public static class FacebookSdkEditorApiContract
{
#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
    [NoAutoStaticsCleanup]
#endif
    private static int assertions;

    public static int Run()
    {
        assertions = 0;
        ValidateSynchronousStateSurface();
        ValidateFbFacade();
        ValidateMobileFacade();
        ValidateGamingServicesFacade();
        ValidateSubscriptionKeyCompatibility();
        Debug.Log($"FACEBOOK_EDITOR_API_CONTRACT_PASS assertions={assertions}");
        return assertions;
    }

    private static void ValidateSynchronousStateSurface()
    {
        Require(FB.IsInitialized, "FB must be initialized before the API contract suite");
        FB.LimitAppEventUsage = true;
        Require(FB.LimitAppEventUsage, "LimitAppEventUsage did not round-trip");
        FB.GraphApiVersion = "v24.0";
        Require(FB.GraphApiVersion == "v24.0", "GraphApiVersion did not round-trip");

        FB.Mobile.ShareDialogMode = ShareDialogMode.AUTOMATIC;
        Require(FB.Mobile.ShareDialogMode == ShareDialogMode.AUTOMATIC, "ShareDialogMode did not round-trip");
        FB.Mobile.UserID = "editor-user";
        Require(FB.Mobile.UserID == "editor-user", "Mobile.UserID did not round-trip");
        Require(FB.Mobile.CurrentAuthenticationToken() == null, "Editor authentication token should be null");
        Require(FB.Mobile.CurrentProfile() == null, "Editor profile should be null before mock login");
        Require(FB.CurrentProfile() == null, "FB profile should be null before mock login");

        // These fire-and-forget Editor APIs satisfy their contract by returning
        // without throwing.
        FB.ActivateApp();
        FB.LogAppEvent("unity7_contract_event", 1.0f, new Dictionary<string, object>());
        FB.LogPurchase(1.25m, "USD", new Dictionary<string, object>());
        FB.LogPurchase(1.25f, "USD", new Dictionary<string, object>());
        FB.Mobile.SetDataProcessingOptions(null);
        FB.Mobile.SetDataProcessingOptions(new[] { "LDU" }, 1, 1000);
        FB.Mobile.EnableProfileUpdatesOnAccessTokenChange(true);
        FB.Mobile.SetAutoLogAppEventsEnabled(true);
        FB.Mobile.SetAdvertiserIDCollectionEnabled(true);
        Require(FB.Mobile.IsImplicitPurchaseLoggingEnabled(), "Editor implicit purchase logging should be enabled");
        Require(FB.Mobile.SetAdvertiserTrackingEnabled(true), "advertiser tracking mock should succeed");
        Require(FB.Mobile.SetDomainErrorEnabled(true), "domain error mock should succeed");
        Require(FB.Mobile.SetFastAppSwitchEnabled(true), "fast app switch mock should succeed");
        FB.Mobile.SetPushNotificationsDeviceTokenString("test-token");
    }

    private static void ValidateFbFacade()
    {
        Expect<IAppLinkResult>("FB.GetAppLink", callback => FB.GetAppLink(callback), result =>
            Require(result.Url == "mockurl://testing.url", "unexpected app-link URL"));
        ExpectNotSupported(
            "FB.CurrentProfile",
            () => FB.CurrentProfile(result =>
            {
                throw new InvalidOperationException(
                    "FB.CurrentProfile unexpectedly invoked its callback");
            }));

        ExpectCatalog("FB.GetCatalog", callback => FB.GetCatalog(callback));
        ExpectPurchases("FB.GetPurchases", callback => FB.GetPurchases(callback));
        ExpectPurchase("FB.Purchase", callback => FB.Purchase("123", callback));
        Expect<IConsumePurchaseResult>("FB.ConsumePurchase", callback => FB.ConsumePurchase("token", callback));
        ExpectSubscribableCatalog("FB.GetSubscribableCatalog", callback => FB.GetSubscribableCatalog(callback));
        ExpectSubscriptions("FB.GetSubscriptions", callback => FB.GetSubscriptions(callback));
        ExpectSubscription("FB.PurchaseSubscription", callback => FB.PurchaseSubscription("123", callback));
        Expect<ICancelSubscriptionResult>("FB.CancelSubscription", callback => FB.CancelSubscription("token", callback));

        Expect<IInterstitialAdResult>("FB.LoadInterstitialAd", callback => FB.LoadInterstitialAd("placement", callback));
        Expect<IInterstitialAdResult>("FB.ShowInterstitialAd", callback => FB.ShowInterstitialAd("placement", callback));
        Expect<IRewardedVideoResult>("FB.LoadRewardedVideo", callback => FB.LoadRewardedVideo("placement", callback));
        Expect<IRewardedVideoResult>("FB.ShowRewardedVideo", callback => FB.ShowRewardedVideo("placement", callback));
        Expect<IFriendFinderInvitationResult>("FB.GetFriendFinderInvitations", callback => FB.GetFriendFinderInvitations(callback), result =>
            Require(result.Invitations != null && result.Invitations.Count == 0, "friend invitations should be an empty list"));
        Expect<IFriendFinderInvitationResult>("FB.DeleteFriendFinderInvitation", callback => FB.DeleteFriendFinderInvitation("invitation", callback));
        Expect<IScheduleAppToUserNotificationResult>("FB.ScheduleAppToUserNotification", callback =>
            FB.ScheduleAppToUserNotification("title", "body", null, 60, "payload", callback));
        Expect<ISessionScoreResult>("FB.PostSessionScore", callback => FB.PostSessionScore(10, callback));
        Expect<ITournamentScoreResult>("FB.PostTournamentScore", callback => FB.PostTournamentScore(10, callback));
        ExpectTournament("FB.GetTournament", callback => FB.GetTournament(callback));
        Expect<ITournamentScoreResult>("FB.ShareTournament", callback => FB.ShareTournament(10, new Dictionary<string, string>(), callback));
        ExpectTournament("FB.CreateTournament", callback => FB.CreateTournament(10, "title", "data:image/png;base64,AA==", "HIGHER_IS_BETTER", "NUMERIC", new Dictionary<string, string>(), callback));

        var mediaUri = new Uri("file:///tmp/facebook-sdk-contract.png");
        ExpectMedia("FB.UploadImageToMediaLibrary", callback => FB.UploadImageToMediaLibrary("caption", mediaUri, false, callback));
        ExpectMedia("FB.UploadVideoToMediaLibrary", callback => FB.UploadVideoToMediaLibrary("caption", mediaUri, false, callback));
        ExpectMedia("FB.UploadImageToMediaLibrary(travelId)", callback => FB.UploadImageToMediaLibrary("caption", mediaUri, false, "travel", callback));
        ExpectMedia("FB.UploadVideoToMediaLibrary(travelId)", callback => FB.UploadVideoToMediaLibrary("caption", mediaUri, false, "travel", callback));
        Expect<ILocaleResult>("FB.GetUserLocale", callback => FB.GetUserLocale(callback), result =>
            Require(result.Locale == "en_US", "unexpected editor locale"));
    }

    private static void ValidateMobileFacade()
    {
        ExpectExpectedError<IProfileResult>("FB.Mobile.CurrentProfile", callback => FB.Mobile.CurrentProfile(callback));
        Expect<IAppLinkResult>("FB.Mobile.FetchDeferredAppLinkData", callback => FB.Mobile.FetchDeferredAppLinkData(callback));
        ExpectExpectedError<IAccessTokenRefreshResult>("FB.Mobile.RefreshCurrentAccessToken", callback => FB.Mobile.RefreshCurrentAccessToken(callback));

        ExpectCatalog("FB.Mobile.GetCatalog", callback => FB.Mobile.GetCatalog(callback));
        ExpectPurchases("FB.Mobile.GetPurchases", callback => FB.Mobile.GetPurchases(callback));
        ExpectPurchase("FB.Mobile.Purchase", callback => FB.Mobile.Purchase("123", callback));
        Expect<IConsumePurchaseResult>("FB.Mobile.ConsumePurchase", callback => FB.Mobile.ConsumePurchase("token", callback));
        Expect<IInterstitialAdResult>("FB.Mobile.LoadInterstitialAd", callback => FB.Mobile.LoadInterstitialAd("placement", callback));
        Expect<IInterstitialAdResult>("FB.Mobile.ShowInterstitialAd", callback => FB.Mobile.ShowInterstitialAd("placement", callback));
        Expect<IRewardedVideoResult>("FB.Mobile.LoadRewardedVideo", callback => FB.Mobile.LoadRewardedVideo("placement", callback));
        Expect<IRewardedVideoResult>("FB.Mobile.ShowRewardedVideo", callback => FB.Mobile.ShowRewardedVideo("placement", callback));
        Expect<IScheduleAppToUserNotificationResult>("FB.Mobile.ScheduleAppToUserNotification", callback =>
            FB.Mobile.ScheduleAppToUserNotification("title", "body", null, 60, "payload", callback));
        Expect<ISessionScoreResult>("FB.Mobile.PostSessionScore", callback => FB.Mobile.PostSessionScore(10, callback));
        Expect<ITournamentScoreResult>("FB.Mobile.PostTournamentScore", callback => FB.Mobile.PostTournamentScore(10, callback));
        ExpectTournament("FB.Mobile.GetTournament", callback => FB.Mobile.GetTournament(callback));
        Expect<ITournamentScoreResult>("FB.Mobile.ShareTournament", callback => FB.Mobile.ShareTournament(10, new Dictionary<string, string>(), callback));
        Expect<IGetTournamentsResult>("FB.Mobile.GetTournaments", callback => FB.Mobile.GetTournaments(callback), result =>
            Require(result.ResultDictionary.ContainsKey("0"), "tournament list did not contain its mock entry"));
        Expect<ITournamentScoreResult>("FB.Mobile.UpdateTournament", callback => FB.Mobile.UpdateTournament("123", 10, callback));
        Expect<IDialogResult>("FB.Mobile.UpdateAndShareTournament", callback => FB.Mobile.UpdateAndShareTournament("123", 10, callback));
        Expect<IDialogResult>("FB.Mobile.CreateAndShareTournament", callback => FB.Mobile.CreateAndShareTournament(10, "title", TournamentSortOrder.HigherIsBetter, TournamentScoreFormat.Numeric, DateTime.UtcNow.AddHours(1), "payload", callback));

        var mediaUri = new Uri("file:///tmp/facebook-sdk-contract.png");
        ExpectMedia("FB.Mobile.UploadImageToMediaLibrary", callback => FB.Mobile.UploadImageToMediaLibrary("caption", mediaUri, false, callback));
        ExpectMedia("FB.Mobile.UploadVideoToMediaLibrary", callback => FB.Mobile.UploadVideoToMediaLibrary("caption", mediaUri, false, callback));
    }

    private static void ValidateGamingServicesFacade()
    {
        var mediaUri = new Uri("file:///tmp/facebook-sdk-contract.png");
        ExpectMedia("FBGamingServices.UploadImageToMediaLibrary", callback => FBGamingServices.UploadImageToMediaLibrary("caption", mediaUri, false, callback));
        ExpectMedia("FBGamingServices.UploadVideoToMediaLibrary", callback => FBGamingServices.UploadVideoToMediaLibrary("caption", mediaUri, false, callback));
        Expect<IIAPReadyResult>("FBGamingServices.OnIAPReady", callback => FBGamingServices.OnIAPReady(callback));
        ExpectCatalog("FBGamingServices.GetCatalog", callback => FBGamingServices.GetCatalog(callback));
        ExpectPurchases("FBGamingServices.GetPurchases", callback => FBGamingServices.GetPurchases(callback));
        ExpectPurchase("FBGamingServices.Purchase", callback => FBGamingServices.Purchase("123", callback));
        Expect<IConsumePurchaseResult>("FBGamingServices.ConsumePurchase", callback => FBGamingServices.ConsumePurchase("token", callback));
        ExpectSubscribableCatalog("FBGamingServices.GetSubscribableCatalog", callback => FBGamingServices.GetSubscribableCatalog(callback));
        ExpectSubscriptions("FBGamingServices.GetSubscriptions", callback => FBGamingServices.GetSubscriptions(callback));
        ExpectSubscription("FBGamingServices.PurchaseSubscription", callback => FBGamingServices.PurchaseSubscription("123", callback));
        Expect<ICancelSubscriptionResult>("FBGamingServices.CancelSubscription", callback => FBGamingServices.CancelSubscription("token", callback));
        Expect<IInitCloudGameResult>("FBGamingServices.InitCloudGame", callback => FBGamingServices.InitCloudGame(callback));
        Expect<IGameLoadCompleteResult>("FBGamingServices.GameLoadComplete", callback => FBGamingServices.GameLoadComplete(callback));
        Expect<IScheduleAppToUserNotificationResult>("FBGamingServices.ScheduleAppToUserNotification", callback =>
            FBGamingServices.ScheduleAppToUserNotification("title", "body", null, 60, "payload", callback));
        Expect<IInterstitialAdResult>("FBGamingServices.LoadInterstitialAd", callback => FBGamingServices.LoadInterstitialAd("placement", callback));
        Expect<IInterstitialAdResult>("FBGamingServices.ShowInterstitialAd", callback => FBGamingServices.ShowInterstitialAd("placement", callback));
        Expect<IRewardedVideoResult>("FBGamingServices.LoadRewardedVideo", callback => FBGamingServices.LoadRewardedVideo("placement", callback));
        Expect<IRewardedVideoResult>("FBGamingServices.ShowRewardedVideo", callback => FBGamingServices.ShowRewardedVideo("placement", callback));
        Expect<IPayloadResult>("FBGamingServices.GetPayload", callback => FBGamingServices.GetPayload(callback), result =>
            Require(result.Payload != null && result.Payload["key"] == "test", "payload did not parse"));
        Expect<ISessionScoreResult>("FBGamingServices.PostSessionScore", callback => FBGamingServices.PostSessionScore(10, callback));
        ExpectTournament("FBGamingServices.GetTournament", callback => FBGamingServices.GetTournament(callback));
        Expect<ITournamentScoreResult>("FBGamingServices.ShareTournament", callback => FBGamingServices.ShareTournament(10, new Dictionary<string, string>(), callback));
        ExpectTournament("FBGamingServices.CreateTournament", callback => FBGamingServices.CreateTournament(10, "title", "data:image/png;base64,AA==", "HIGHER_IS_BETTER", "NUMERIC", new Dictionary<string, string>(), callback));
        Expect<ITournamentScoreResult>("FBGamingServices.PostTournamentScore", callback => FBGamingServices.PostTournamentScore(10, callback));
        Expect<IOpenAppStoreResult>("FBGamingServices.OpenAppStore", callback => FBGamingServices.OpenAppStore(callback));
        Expect<ICreateGamingContextResult>("FBGamingServices.CreateGamingContext", callback => FBGamingServices.CreateGamingContext("player", callback), result =>
            Require((string)result.ResultDictionary["contextId"] == "1234", "create context ID did not parse"));
        Expect<ISwitchGamingContextResult>("FBGamingServices.SwitchGamingContext", callback => FBGamingServices.SwitchGamingContext("context", callback), result =>
            Require((string)result.ResultDictionary["contextId"] == "1234", "switch context ID did not parse"));
        Expect<IChooseGamingContextResult>("FBGamingServices.ChooseGamingContext", callback => FBGamingServices.ChooseGamingContext(new List<string>(), 1, 10, callback), result =>
            Require((string)result.ResultDictionary["contextId"] == "1234", "choose context ID did not parse"));
        Expect<IGetCurrentGamingContextResult>("FBGamingServices.GetCurrentGamingContext", callback => FBGamingServices.GetCurrentGamingContext(callback), result =>
            Require(result.ContextId == "1234", "current context ID did not parse"));
    }

    private static void ExpectCatalog(string name, Action<FacebookDelegate<ICatalogResult>> invoke)
    {
        Expect(name, invoke, result => Require(result.Products != null && result.Products.Count == 1 && result.Products[0].ProductID == "123", "catalog did not parse"));
    }

    private static void ExpectPurchases(string name, Action<FacebookDelegate<IPurchasesResult>> invoke)
    {
        Expect(name, invoke, result => Require(result.Purchases != null && result.Purchases.Count == 1 && result.Purchases[0].IsConsumed, "purchases did not parse consumed state"));
    }

    private static void ExpectPurchase(string name, Action<FacebookDelegate<IPurchaseResult>> invoke)
    {
        Expect(name, invoke, result => Require(result.Purchase != null && result.Purchase.IsConsumed, "purchase did not parse consumed state"));
    }

    private static void ExpectSubscribableCatalog(string name, Action<FacebookDelegate<ISubscribableCatalogResult>> invoke)
    {
        Expect(name, invoke, result => Require(result.SubscribableProducts != null && result.SubscribableProducts.Count == 1, "subscribable catalog did not parse"));
    }

    private static void ExpectSubscriptions(string name, Action<FacebookDelegate<ISubscriptionsResult>> invoke)
    {
        Expect(name, invoke, result =>
        {
            Require(result.Subscriptions != null && result.Subscriptions.Count == 1, "subscriptions did not parse");
            ValidateSubscription(result.Subscriptions[0]);
        });
    }

    private static void ExpectSubscription(string name, Action<FacebookDelegate<ISubscriptionResult>> invoke)
    {
        Expect(name, invoke, result =>
        {
            Require(result.Subscription != null, "subscription did not parse");
            ValidateSubscription(result.Subscription);
        });
    }

    private static void ValidateSubscription(Subscription subscription)
    {
        var expectedPeriodEndTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddSeconds(1584516124);
        Require(subscription.IsEntitlementActive, "subscription entitlement did not parse");
        Require(subscription.PeriodEndTime == expectedPeriodEndTime, "subscription period end time did not parse");
    }

    private static void ValidateSubscriptionKeyCompatibility()
    {
        var payload = CreateSubscriptionPayload();
        ValidateSubscription(Utilities.ParseSubscriptionFromDictionary(payload));
        assertions++;

        var legacyPayload = CreateSubscriptionPayload();
        legacyPayload.Remove("periodEndTime");
        legacyPayload["period_end_time"] = 1584516124L;
        ValidateSubscription(Utilities.ParseSubscriptionFromDictionary(legacyPayload));
        assertions++;
    }

    private static Dictionary<string, object> CreateSubscriptionPayload()
    {
        return new Dictionary<string, object>()
        {
            { "deactivationTime", 1588785783L },
            { "isEntitlementActive", true },
            { "periodStartTime", 1583797821L },
            { "periodEndTime", 1584516124L },
            { "productID", "123" },
            { "purchasePlatform", "FB" },
            {
                "purchasePrice",
                new Dictionary<string, object>()
                {
                    { "amount", "0.99" },
                    { "currency", "USD" },
                }
            },
            { "purchaseTime", 1583797821L },
            { "purchaseToken", "1655700687901784" },
            { "status", "ACTIVE" },
            { "signedRequest", "abc123ZYZ" },
            { "subscriptionTerm", "MONTHLY" },
        };
    }

    private static void ExpectTournament(string name, Action<FacebookDelegate<ITournamentResult>> invoke)
    {
        Expect(name, invoke, result => Require(result.ResultDictionary.ContainsKey("tournament_id"), "tournament payload did not parse"));
    }

    private static void ExpectMedia(string name, Action<FacebookDelegate<IMediaUploadResult>> invoke)
    {
        Expect(name, invoke, result => Require(!string.IsNullOrEmpty(result.MediaId), "media ID was empty"));
    }

    private static void ExpectExpectedError<T>(string name, Action<FacebookDelegate<T>> invoke) where T : IResult
    {
        Expect(name, invoke, result => Require(!string.IsNullOrEmpty(result.Error), "expected an Editor mock error"), false);
    }

    private static void ExpectNotSupported(string name, Action invoke)
    {
        try
        {
            invoke();
        }
        catch (NotSupportedException)
        {
            assertions++;
            return;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                name + " threw " + exception.GetType().Name + " instead of NotSupportedException",
                exception);
        }

        throw new InvalidOperationException(name + " did not throw NotSupportedException");
    }

    private static void Expect<T>(string name, Action<FacebookDelegate<T>> invoke, Action<T> validate = null, bool expectSuccess = true) where T : IResult
    {
        var callbackCount = 0;
        invoke(result =>
        {
            callbackCount++;
            Require(result != null, name + " returned a null result");
            if (expectSuccess)
            {
                Require(string.IsNullOrEmpty(result.Error), name + " returned error: " + result.Error);
            }
            validate?.Invoke(result);
        });
        // Editor mocks intentionally complete callbacks synchronously. A future
        // asynchronous implementation must update this contract and its runner.
        Require(callbackCount == 1, name + " callback count was " + callbackCount);
        assertions++;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
