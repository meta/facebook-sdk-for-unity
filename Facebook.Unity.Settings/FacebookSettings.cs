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

namespace Facebook.Unity.Settings
{
    using System.Collections.Generic;
    using UnityEngine;
#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
    using global::Unity.Scripting.LifecycleManagement;
#endif

    /// <summary>
    /// Facebook settings.
    /// </summary>
    public partial class FacebookSettings : ScriptableObject
    {


        public const string FacebookSettingsAssetName = "FacebookSettings";
        public const string FacebookSettingsPath = "FacebookSDK/SDK/Resources";
        public const string FacebookSettingsAssetExtension = ".asset";

        public enum BuildTarget
        {
            StandaloneOSX,
            StandaloneWindows,
            iOS,
            Android,
            StandaloneWindows64,
            WebGL,
            WSAPlayer,
            StandaloneLinux64,
            PS4,
            XboxOne,
            tvOS,
            Switch,
            Stadia,
            CloudRendering,
            PS5,
            none,
        }

#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
        [NoAutoStaticsCleanup]
#endif
        private static List<OnChangeCallback> onChangeCallbacks = new List<OnChangeCallback>();
#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
        [NoAutoStaticsCleanup]
#endif
        private static FacebookSettings instance;

        private static List<OnChangeCallback> OnChangeCallbacks
        {
            get
            {
                if (onChangeCallbacks == null)
                {
                    onChangeCallbacks = new List<OnChangeCallback>();
                }

                return onChangeCallbacks;
            }
        }

        [SerializeField]
        private int selectedAppIndex = 0;
        [SerializeField]
        private List<string> clientTokens = new List<string> { string.Empty };
        [SerializeField]
        private List<string> appIds = new List<string> { "0" };
        [SerializeField]
        private List<string> appLabels = new List<string> { "App Name" };
        [SerializeField]
        private bool cookie = true;
        [SerializeField]
        private bool logging = true;
        [SerializeField]
        private bool status = true;
        [SerializeField]
        private bool xfbml = false;
        [SerializeField]
        private bool frictionlessRequests = true;
#if UNITY_EDITOR
        [SerializeField]
        private string androidKeystorePath = string.Empty;
#endif
        [SerializeField]
        private string iosURLSuffix = string.Empty;
        [SerializeField]
        private List<UrlSchemes> appLinkSchemes = new List<UrlSchemes>() { new UrlSchemes() };
#if UNITY_EDITOR
        [SerializeField]
        private string uploadAccessToken = string.Empty;
#endif
        // App Events Settings
        [SerializeField]
        private bool autoLogAppEventsEnabled = true;
        [SerializeField]
        private bool autoLogProductImprovementEnabled = true;
        [SerializeField]
        private bool autoLogMetaDataEnabled = true;
        [SerializeField]
        private bool advertiserIDCollectionEnabled = true;

        public delegate void OnChangeCallback();

        private BuildTarget editorBuildTargetName = BuildTarget.none;

        /// <summary>
        /// Gets or sets the current editor build target
        /// </summary>
        /// <value>Build target name</value>
        public static BuildTarget EditorBuildTarget
        {
            get
            {
                return Instance.editorBuildTargetName;
            }

            set
            {
                Instance.editorBuildTargetName = value;
            }
        }

        /// <summary>
        /// Gets or sets the index of the selected app.
        /// </summary>
        /// <value>The index of the selected app.</value>
        public static int SelectedAppIndex
        {
            get
            {
                return Instance.selectedAppIndex;
            }

            set
            {
                if (Instance.selectedAppIndex != value)
                {
                    Instance.selectedAppIndex = value;
                    SettingsChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the app identifiers.
        /// </summary>
        /// <value>The app identifiers.</value>
        public static List<string> AppIds
        {
            get
            {
                return Instance.appIds;
            }

            set
            {
                if (Instance.appIds != value)
                {
                    Instance.appIds = value;
                    SettingsChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the app labels.
        /// </summary>
        /// <value>The app labels.</value>
        public static List<string> AppLabels
        {
            get
            {
                return Instance.appLabels;
            }

            set
            {
                if (Instance.appLabels != value)
                {
                    Instance.appLabels = value;
                    SettingsChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the app client token.
        /// </summary>
        /// <value>The app client token.</value>
        public static List<string> ClientTokens
        {
            get
            {
                return Instance.clientTokens;
            }

            set
            {
                if (Instance.clientTokens != value)
                {
                    Instance.clientTokens = value;
                    SettingsChanged();
                }
            }
        }

        /// <summary>
        /// Gets the app identifier.
        /// </summary>
        /// <value>The app identifier.</value>
        public static string AppId
        {
            get
            {
                return AppIds[SelectedAppIndex].Trim();
            }
        }

        /// <summary>
        /// Gets the app client token.
        /// </summary>
        /// <value>The app identifier.</value>
        public static string ClientToken
        {
            get
            {
                return ClientTokens[SelectedAppIndex].Trim();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the app id is valid app identifier.
        /// </summary>
        /// <value><c>true</c> if is valid app identifier; otherwise, <c>false</c>.</value>
        public static bool IsValidAppId
        {
            get
            {
                return FacebookSettings.AppId != null
                    && FacebookSettings.AppId.Length > 0
                    && !FacebookSettings.AppId.Equals("0");
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Facebook.Unity.FacebookSettings"/> is cookie.
        /// </summary>
        /// <value><c>true</c> if cookie; otherwise, <c>false</c>.</value>
        public static bool Cookie
        {
            get
            {
                return Instance.cookie;
            }

            set
            {
                if (Instance.cookie != value)
                {
                    Instance.cookie = value;
                    SettingsChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Facebook.Unity.FacebookSettings"/> is logging.
        /// </summary>
        /// <value><c>true</c> if logging; otherwise, <c>false</c>.</value>
        public static bool Logging
        {
            get
            {
                return Instance.logging;
            }

            set
            {
                if (Instance.logging != value)
                {
                    Instance.logging = value;
                    SettingsChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Facebook.Unity.FacebookSettings"/> is status.
        /// </summary>
        /// <value><c>true</c> if status; otherwise, <c>false</c>.</value>
        public static bool Status
        {
            get
            {
                return Instance.status;
            }

            set
            {
                if (Instance.status != value)
                {
                    Instance.status = value;
                    SettingsChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Facebook.Unity.FacebookSettings"/> is xfbml.
        /// </summary>
        /// <value><c>true</c> if xfbml; otherwise, <c>false</c>.</value>
        public static bool Xfbml
        {
            get
            {
                return Instance.xfbml;
            }

            set
            {
                if (Instance.xfbml != value)
                {
                    Instance.xfbml = value;
                    SettingsChanged();
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Gets or sets the android keystore path.
        /// </summary>
        /// <value>The android keystore path.</value>
        public static string AndroidKeystorePath
        {
            get
            {
                return Instance.androidKeystorePath;
            }

            set
            {
                if (Instance.androidKeystorePath != value)
                {
                    Instance.androidKeystorePath = value;
                    SettingsChanged();
                }
            }
        }
#endif

        /// <summary>
        /// Gets or sets the ios URL suffix.
        /// </summary>
        /// <value>The ios URL suffix.</value>
        public static string IosURLSuffix
        {
            get
            {
                return Instance.iosURLSuffix;
            }

            set
            {
                if (Instance.iosURLSuffix != value)
                {
                    Instance.iosURLSuffix = value;
                    SettingsChanged();
                }
            }
        }

        /// <summary>
        /// Gets the channel URL.
        /// </summary>
        /// <value>The channel URL.</value>
        public static string ChannelUrl
        {
            get { return "/channel.html"; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Facebook.Unity.FacebookSettings"/> frictionless requests.
        /// </summary>
        /// <value><c>true</c> if frictionless requests; otherwise, <c>false</c>.</value>
        public static bool FrictionlessRequests
        {
            get
            {
                return Instance.frictionlessRequests;
            }

            set
            {
                if (Instance.frictionlessRequests != value)
                {
                    Instance.frictionlessRequests = value;
                    SettingsChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the app link schemes.
        /// </summary>
        /// <value>A list of app link schemese for each app.</value>
        public static List<UrlSchemes> AppLinkSchemes
        {
            get
            {
                return Instance.appLinkSchemes;
            }

            set
            {
                if (Instance.appLinkSchemes != value)
                {
                    Instance.appLinkSchemes = value;
                    SettingsChanged();
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Gets or sets the upload access token.
        /// </summary>
        /// <value>The access token to upload build to Facebook hosting.</value>
        public static string UploadAccessToken
        {
            get
            {
                return Instance.uploadAccessToken;
            }


            set
            {
                if (Instance.uploadAccessToken != value)
                {
                    Instance.uploadAccessToken = value;
                    SettingsChanged();
                }
            }
        }
#endif

        /// <summary>
        /// Gets or sets a value indicating whether App Events can be automatically logged.
        /// </summary>
        /// <value><c>true</c> if auto logging; otherwise, <c>false</c>.</value>
        public static bool AutoLogAppEventsEnabled
        {
            get
            {
                return Instance.autoLogAppEventsEnabled;
            }

            set
            {
                if (Instance.autoLogAppEventsEnabled != value)
                {
                    Instance.autoLogAppEventsEnabled = value;
                    SettingsChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the build-time default for automatic diagnostic product improvement data logging.
        /// Native persisted runtime assignments take precedence over this setting.
        /// </summary>
        /// <value><c>true</c> by default; <c>false</c> to opt out.</value>
        public static bool AutoLogProductImprovementEnabled
        {
            get
            {
                return Instance.autoLogProductImprovementEnabled;
            }

            set
            {
                if (Instance.autoLogProductImprovementEnabled != value)
                {
                    Instance.autoLogProductImprovementEnabled = value;
                    SettingsChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether app metadata, such as screen titles and
        /// AppLink URLs, can be automatically collected and included with logged app events.
        /// </summary>
        /// <value><c>true</c> if collecting metadata; otherwise, <c>false</c>.</value>
        public static bool AutoLogMetaDataEnabled
        {
            get
            {
                return Instance.autoLogMetaDataEnabled;
            }

            set
            {
                if (Instance.autoLogMetaDataEnabled != value)
                {
                    Instance.autoLogMetaDataEnabled = value;
                    SettingsChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether advertiserID can be collected.
        /// </summary>
        /// <value><c>true</c> if advertiserID can be collected; otherwise, <c>false</c>.</value>
        public static bool AdvertiserIDCollectionEnabled
        {
            get
            {
                return Instance.advertiserIDCollectionEnabled;
            }

            set
            {
                if (Instance.advertiserIDCollectionEnabled != value)
                {
                    Instance.advertiserIDCollectionEnabled = value;
                    SettingsChanged();
                }
            }
        }


        public static FacebookSettings Instance
        {
            get
            {
                instance = NullableInstance;

                if (instance == null)
                {
                    // If not found, autocreate the asset object.
                    instance = ScriptableObject.CreateInstance<FacebookSettings>();
                }

                return instance;
            }
        }

        public static FacebookSettings NullableInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load(FacebookSettingsAssetName) as FacebookSettings;
                }

                return instance;
            }
        }

        public static void RegisterChangeEventCallback(OnChangeCallback callback)
        {
            OnChangeCallbacks.Add(callback);
        }

        public static void UnregisterChangeEventCallback(OnChangeCallback callback)
        {
            OnChangeCallbacks.Remove(callback);
        }

        private static void SettingsChanged()
        {
            OnChangeCallbacks.ForEach(callback => callback());
        }

        /// <summary>
        /// Unity doesn't seralize lists of lists so create a serializable type to wrapp the list for use.
        /// </summary>
        [System.Serializable]
        public class UrlSchemes
        {
            [SerializeField]
            private List<string> list;

            /// <summary>
            /// Initializes a new instance of the <see cref="UrlSchemes"/> class.
            /// </summary>
            /// <param name="schemes">Url schemes.</param>
            public UrlSchemes(List<string> schemes = null)
            {
                this.list = schemes == null ? new List<string>() : schemes;
            }

            /// <summary>
            /// Gets or sets the schemes.
            /// </summary>
            /// <value>The schemes.</value>
            public List<string> Schemes
            {
                get
                {
                    return this.list;
                }

                set
                {
                    this.list = value;
                }
            }
        }
    }
}
