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
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using Facebook.Unity;
    using Facebook.Unity.Settings;
    using UnityEditor;
    using UnityEngine;

    public class ManifestMod
    {
        public const string AppLinkActivityName = "com.facebook.unity.FBUnityAppLinkActivity";
        public const string DeepLinkingActivityName = "com.facebook.unity.FBUnityDeepLinkingActivity";
        public const string UnityLoginActivityName = "com.facebook.unity.FBUnityLoginActivity";
        public const string UnitySSOActivityName = "com.facebook.unity.FBUnitySSOActivity";
        public const string UnityDialogsActivityName = "com.facebook.unity.FBUnityDialogsActivity";
        public const string UnityGameRequestActivityName = "com.facebook.unity.FBUnityGameRequestActivity";
        public const string UnityGamingServicesFriendFinderActivityName = "com.facebook.unity.FBUnityGamingServicesFriendFinderActivity";
        public const string ApplicationIdMetaDataName = "com.facebook.sdk.ApplicationId";
        public const string ClientTokenMetaDataName = "com.facebook.sdk.ClientToken";
        public const string AutoLogAppEventsEnabled = "com.facebook.sdk.AutoLogAppEventsEnabled";
        public const string AutoLogProductImprovementEnabled = "com.facebook.sdk.AutoLogProductImprovementEnabled";
        public const string AutoLogMetaDataEnabled = "com.facebook.sdk.AutoLogMetaDataEnabled";
        public const string AdvertiserIDCollectionEnabled = "com.facebook.sdk.AdvertiserIDCollectionEnabled";
        public const string FacebookContentProviderName = "com.facebook.FacebookContentProvider";
        public const string FacebookContentProviderAuthFormat = "com.facebook.app.FacebookContentProvider{0}";
        public const string FacebookActivityName = "com.facebook.FacebookActivity";
        public const string AndroidManifestPath = "Plugins/Android/AndroidManifest.xml";
        public const string FacebookDefaultAndroidManifestPath = "FacebookSDK/SDK/Editor/android/DefaultAndroidManifest.xml";

        private const string UnityPlayerActivityName = "com.unity3d.player.UnityPlayerActivity";
        private const string UnityPlayerGameActivityName = "com.unity3d.player.UnityPlayerGameActivity";
        private const string UnityPlayerNativeActivityName = "com.unity3d.player.UnityPlayerNativeActivity";
        private const string UnityActivityTheme = "@style/UnityThemeSelector";
        private const string UnityGameActivityTheme = "@style/BaseUnityGameActivityTheme";
        private const string GameActivityLibNameMetaDataName = "android.app.lib_name";
        private const string GameActivityLibNameMetaDataValue = "game";

        // UnityEditor.AndroidApplicationEntry, which this assembly is too old to reference.
        private const int ActivityEntryFlag = 1;
        private const int GameActivityEntryFlag = 2;

        public static void GenerateManifest()
        {
            var outputFile = Path.Combine(Application.dataPath, ManifestMod.AndroidManifestPath);

            // Create containing directory if it does not exist
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

            // only copy over a fresh copy of the AndroidManifest if one does not exist
            if (!File.Exists(outputFile))
            {
                ManifestMod.CreateDefaultAndroidManifest(outputFile);
            }

            UpdateManifest(outputFile);
        }

        public static bool CheckManifest()
        {
            bool result = true;
            var outputFile = Path.Combine(Application.dataPath, ManifestMod.AndroidManifestPath);
            if (!File.Exists(outputFile))
            {
                Debug.LogError("An android manifest must be generated for the Facebook SDK to work.  Go to Facebook->Edit Settings and press \"Regenerate Android Manifest\"");
                return false;
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(outputFile);

            if (doc == null)
            {
                Debug.LogError("Couldn't load " + outputFile);
                return false;
            }

            XmlNode manNode = FindChildNode(doc, "manifest");
            XmlNode dict = FindChildNode(manNode, "application");

            if (dict == null)
            {
                Debug.LogError("Error parsing " + outputFile);
                return false;
            }

            XmlElement loginElement;
            if (!ManifestMod.TryFindElementWithAndroidName(dict, UnityLoginActivityName, out loginElement))
            {
                Debug.LogError(string.Format("{0} is missing from your android manifest.  Go to Facebook->Edit Settings and press \"Regenerate Android Manifest\"", UnityLoginActivityName));
                result = false;
            }

            var deprecatedMainActivityName = "com.facebook.unity.FBUnityPlayerActivity";
            XmlElement deprecatedElement;
            if (ManifestMod.TryFindElementWithAndroidName(dict, deprecatedMainActivityName, out deprecatedElement))
            {
                Debug.LogWarning(string.Format("{0} is deprecated and no longer needed for the Facebook SDK.  Feel free to use your own main activity or use the default \"com.unity3d.player.UnityPlayerNativeActivity\"", deprecatedMainActivityName));
            }

            // A stale manifest can name an entry point Unity no longer compiles in.
            XmlElement launcherElement;
            if (ManifestMod.TryFindLauncherActivity(dict, out launcherElement))
            {
                string ns = dict.GetNamespaceOfPrefix("android");
                string launcherName = launcherElement.GetAttribute("name", ns);
                string expected = ManifestMod.UseGameActivityEntry()
                    ? UnityPlayerGameActivityName
                    : UnityPlayerActivityName;

                if (!ManifestMod.UsesBothEntryPoints()
                    && ManifestMod.IsUnityEntryPointActivity(launcherName)
                    && !expected.Equals(launcherName))
                {
                    Debug.LogError(string.Format("Your android manifest launches {0}, but the Application Entry Point in Player Settings requires {1}.  Go to Facebook->Edit Settings and press \"Regenerate Android Manifest\"", launcherName, expected));
                    result = false;
                }
            }

            return result;
        }

        public static void UpdateManifest(string fullPath)
        {
            string appId = FacebookSettings.AppId;
            string clientToken = FacebookSettings.ClientToken;

            if (!FacebookSettings.IsValidAppId)
            {
                Debug.LogError("You didn't specify a Facebook app ID.  Please add one using the Facebook menu in the main Unity editor.");
                return;
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(fullPath);

            if (doc == null)
            {
                Debug.LogError("Couldn't load " + fullPath);
                return;
            }

            XmlNode manNode = FindChildNode(doc, "manifest");
            XmlNode dict = FindChildNode(manNode, "application");

            if (dict == null)
            {
                Debug.LogError("Error parsing " + fullPath);
                return;
            }

            string ns = dict.GetNamespaceOfPrefix("android");

            ManifestMod.UpdateLauncherActivity(doc, dict, ns);

            // add the unity login activity
            XmlElement unityLoginElement = CreateUnityOverlayElement(doc, ns, UnityLoginActivityName);
            ManifestMod.SetOrReplaceXmlElement(dict, unityLoginElement);

            // add the unity SSO ("Login with Facebook" app-switch) activity
            XmlElement unitySSOElement = CreateUnityOverlayElement(doc, ns, UnitySSOActivityName);
            ManifestMod.SetOrReplaceXmlElement(dict, unitySSOElement);

            // add the unity dialogs activity
            XmlElement unityDialogsElement = CreateUnityOverlayElement(doc, ns, UnityDialogsActivityName);
            ManifestMod.SetOrReplaceXmlElement(dict, unityDialogsElement);

            XmlElement unityGamingFriendFinderElement = CreateUnityOverlayElement(doc, ns, UnityGamingServicesFriendFinderActivityName);
            ManifestMod.SetOrReplaceXmlElement(dict, unityGamingFriendFinderElement);


            ManifestMod.AddAppLinkingActivity(doc, dict, ns, FacebookSettings.AppLinkSchemes[FacebookSettings.SelectedAppIndex].Schemes);

            ManifestMod.AddSimpleActivity(doc, dict, ns, DeepLinkingActivityName, true);
            ManifestMod.AddSimpleActivity(doc, dict, ns, UnityGameRequestActivityName);

            // add the app id
            // <meta-data android:name="com.facebook.sdk.ApplicationId" android:value="\ fb<APPID>" />
            XmlElement appIdElement = doc.CreateElement("meta-data");
            appIdElement.SetAttribute("name", ns, ApplicationIdMetaDataName);
            appIdElement.SetAttribute("value", ns, "fb" + appId);
            ManifestMod.SetOrReplaceXmlElement(dict, appIdElement);

            // add the client token
            // <meta-data android:name="com.facebook.sdk.ClientToken" android:value="\ <CLIENT_TOKEN>" />
            XmlElement clientTokenElement = doc.CreateElement("meta-data");
            clientTokenElement.SetAttribute("name", ns, ClientTokenMetaDataName);
            clientTokenElement.SetAttribute("value", ns, clientToken);
            ManifestMod.SetOrReplaceXmlElement(dict, clientTokenElement);

            // enable AutoLogAppEventsEnabled by default
            // <meta-data android:name="com.facebook.sdk.AutoLogAppEventsEnabled" android:value="true"/>
            string autoLogAppEventsEnabled = FacebookSettings.AutoLogAppEventsEnabled.ToString().ToLower();
            XmlElement autoLogAppEventsEnabledElement = doc.CreateElement("meta-data");
            autoLogAppEventsEnabledElement.SetAttribute("name", ns, AutoLogAppEventsEnabled);
            autoLogAppEventsEnabledElement.SetAttribute("value", ns, autoLogAppEventsEnabled);
            ManifestMod.SetOrReplaceXmlElement(dict, autoLogAppEventsEnabledElement);

            string autoLogProductImprovementEnabled = FacebookSettings.AutoLogProductImprovementEnabled.ToString().ToLowerInvariant();
            XmlElement autoLogProductImprovementEnabledElement = doc.CreateElement("meta-data");
            autoLogProductImprovementEnabledElement.SetAttribute("name", ns, AutoLogProductImprovementEnabled);
            autoLogProductImprovementEnabledElement.SetAttribute("value", ns, autoLogProductImprovementEnabled);
            ManifestMod.SetOrReplaceXmlElement(dict, autoLogProductImprovementEnabledElement);

            // enable AutoLogMetaDataEnabled by default
            // <meta-data android:name="com.facebook.sdk.AutoLogMetaDataEnabled" android:value="true"/>
            string autoLogMetaDataEnabled = FacebookSettings.AutoLogMetaDataEnabled.ToString().ToLower();
            XmlElement autoLogMetaDataEnabledElement = doc.CreateElement("meta-data");
            autoLogMetaDataEnabledElement.SetAttribute("name", ns, AutoLogMetaDataEnabled);
            autoLogMetaDataEnabledElement.SetAttribute("value", ns, autoLogMetaDataEnabled);
            ManifestMod.SetOrReplaceXmlElement(dict, autoLogMetaDataEnabledElement);

            // enable AdvertiserIDCollectionEnabled by default
            // <meta-data android:name="com.facebook.sdk.AdvertiserIDCollectionEnabled" android:value="true"/>
            string advertiserIDCollectionEnabled = FacebookSettings.AdvertiserIDCollectionEnabled.ToString().ToLower();
            XmlElement advertiserIDCollectionEnabledElement = doc.CreateElement("meta-data");
            advertiserIDCollectionEnabledElement.SetAttribute("name", ns, AdvertiserIDCollectionEnabled);
            advertiserIDCollectionEnabledElement.SetAttribute("value", ns, advertiserIDCollectionEnabled);
            ManifestMod.SetOrReplaceXmlElement(dict, advertiserIDCollectionEnabledElement);

            // Add the facebook content provider
            // <provider
            //   android:name="com.facebook.FacebookContentProvider"
            //   android:authorities="com.facebook.app.FacebookContentProvider<APPID>"
            //   android:exported="true" />
            XmlElement contentProviderElement = CreateContentProviderElement(doc, ns, appId);
            ManifestMod.SetOrReplaceXmlElement(dict, contentProviderElement);

            // Remove the FacebookActivity since we can rely on it in the androidsdk aar as of v4.12
            // (otherwise unity manifest merge likes fail if there's any difference at all)
            XmlElement facebookElement;
            if (TryFindElementWithAndroidName(dict, FacebookActivityName, out facebookElement))
            {
                dict.RemoveChild(facebookElement);
            }

            // Save the document formatted
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };

            using (XmlWriter xmlWriter = XmlWriter.Create(fullPath, settings))
            {
                doc.Save(xmlWriter);
            }
        }

        // Reflective, not #if UNITY_2023_1_OR_NEWER: this DLL is precompiled and a guard would strip the check.
        private static int GetApplicationEntryFlags()
        {
            PropertyInfo property = typeof(PlayerSettings.Android).GetProperty(
                "applicationEntry",
                BindingFlags.Public | BindingFlags.Static);

            // Editors predating the setting offer Activity only.
            return property == null
                ? ActivityEntryFlag
                : System.Convert.ToInt32(property.GetValue(null, null));
        }

        private static bool UseGameActivityEntry()
        {
            int entry = ManifestMod.GetApplicationEntryFlags();
            return (entry & ActivityEntryFlag) == 0 && (entry & GameActivityEntryFlag) != 0;
        }

        // Both flags compile in both entry points, so whichever one the manifest already names launches.
        private static bool UsesBothEntryPoints()
        {
            int entry = ManifestMod.GetApplicationEntryFlags();
            return (entry & ActivityEntryFlag) != 0 && (entry & GameActivityEntryFlag) != 0;
        }

        // A custom launcher belongs to the developer; only Unity's own entry points are ours to rewrite.
        private static bool IsUnityEntryPointActivity(string activityName)
        {
            return UnityPlayerActivityName.Equals(activityName)
                || UnityPlayerGameActivityName.Equals(activityName)
                || UnityPlayerNativeActivityName.Equals(activityName);
        }

        private static void UpdateLauncherActivity(XmlDocument doc, XmlNode dict, string ns)
        {
            if (ManifestMod.UsesBothEntryPoints())
            {
                return;
            }

            List<XmlElement> unityLaunchers = new List<XmlElement>();
            foreach (XmlElement candidate in ManifestMod.FindLauncherActivities(dict))
            {
                if (ManifestMod.IsUnityEntryPointActivity(candidate.GetAttribute("name", ns)))
                {
                    unityLaunchers.Add(candidate);
                }
            }

            if (unityLaunchers.Count == 0)
            {
                return;
            }

            bool useGameActivity = ManifestMod.UseGameActivityEntry();
            string targetName = useGameActivity ? UnityPlayerGameActivityName : UnityPlayerActivityName;

            XmlElement selected = unityLaunchers[0];
            foreach (XmlElement candidate in unityLaunchers)
            {
                if (targetName.Equals(candidate.GetAttribute("name", ns)))
                {
                    selected = candidate;
                    break;
                }
            }

            // Unity's UnityManifest.xml ships both entry-point blocks; keeping the other duplicates the name.
            foreach (XmlElement extra in unityLaunchers)
            {
                if (extra != selected)
                {
                    extra.ParentNode.RemoveChild(extra);
                }
            }

            // The theme moves only with the name, so a theme the developer customized survives.
            if (!targetName.Equals(selected.GetAttribute("name", ns)))
            {
                selected.SetAttribute("name", ns, targetName);
                selected.SetAttribute("theme", ns, useGameActivity ? UnityGameActivityTheme : UnityActivityTheme);
            }

            XmlElement libName;
            bool hasLibName = ManifestMod.TryFindElementWithAndroidName(
                selected, GameActivityLibNameMetaDataName, out libName, "meta-data");

            if (useGameActivity)
            {
                // GameActivity loads the player through this; a stale value starts the app and exits.
                if (!hasLibName)
                {
                    libName = doc.CreateElement("meta-data");
                    libName.SetAttribute("name", ns, GameActivityLibNameMetaDataName);
                    selected.AppendChild(libName);
                }

                libName.SetAttribute("value", ns, GameActivityLibNameMetaDataValue);
            }
            else if (hasLibName)
            {
                selected.RemoveChild(libName);
            }
        }

        private static bool TryFindLauncherActivity(XmlNode parent, out XmlElement element)
        {
            List<XmlElement> launchers = ManifestMod.FindLauncherActivities(parent);
            element = launchers.Count > 0 ? launchers[0] : null;
            return element != null;
        }

        // Matched on the intent filter rather than android:name, which is the attribute being fixed.
        private static List<XmlElement> FindLauncherActivities(XmlNode parent)
        {
            string ns = parent.GetNamespaceOfPrefix("android");
            List<XmlElement> launchers = new List<XmlElement>();
            for (XmlNode curr = parent.FirstChild; curr != null; curr = curr.NextSibling)
            {
                XmlElement activity = curr as XmlElement;
                if (activity == null || !activity.Name.Equals("activity"))
                {
                    continue;
                }

                foreach (XmlNode filter in activity.GetElementsByTagName("intent-filter"))
                {
                    bool isMain = false;
                    bool isLauncher = false;
                    foreach (XmlNode entry in filter.ChildNodes)
                    {
                        XmlElement entryElement = entry as XmlElement;
                        if (entryElement == null)
                        {
                            continue;
                        }

                        string name = entryElement.GetAttribute("name", ns);
                        isMain |= entryElement.Name.Equals("action") && name.Equals("android.intent.action.MAIN");
                        isLauncher |= entryElement.Name.Equals("category") && name.Equals("android.intent.category.LAUNCHER");
                    }

                    if (isMain && isLauncher)
                    {
                        launchers.Add(activity);
                        break;
                    }
                }
            }

            return launchers;
        }

        private static XmlNode FindChildNode(XmlNode parent, string name)
        {
            XmlNode curr = parent.FirstChild;
            while (curr != null)
            {
                if (curr.Name.Equals(name))
                {
                    return curr;
                }

                curr = curr.NextSibling;
            }

            return null;
        }

        private static void SetOrReplaceXmlElement(
            XmlNode parent,
            XmlElement newElement)
        {
            string attrNameValue = newElement.GetAttribute("name");
            string elementType = newElement.Name;

            XmlElement existingElment;
            if (TryFindElementWithAndroidName(parent, attrNameValue, out existingElment, elementType))
            {
                parent.ReplaceChild(newElement, existingElment);
            }
            else
            {
                parent.AppendChild(newElement);
            }
        }

        private static bool TryFindElementWithAndroidName(
            XmlNode parent,
            string attrNameValue,
            out XmlElement element,
            string elementType = "activity")
        {
            string ns = parent.GetNamespaceOfPrefix("android");
            var curr = parent.FirstChild;
            while (curr != null)
            {
                var currXmlElement = curr as XmlElement;
                if (currXmlElement != null &&
                    currXmlElement.Name == elementType &&
                    currXmlElement.GetAttribute("name", ns) == attrNameValue)
                {
                    element = currXmlElement;
                    return true;
                }

                curr = curr.NextSibling;
            }

            element = null;
            return false;
        }

        private static void AddSimpleActivity(XmlDocument doc, XmlNode xmlNode, string ns, string className, bool export = false)
        {
            XmlElement element = CreateActivityElement(doc, ns, className, export);
            ManifestMod.SetOrReplaceXmlElement(xmlNode, element);
        }

        private static XmlElement CreateUnityOverlayElement(XmlDocument doc, string ns, string activityName)
        {
            // <activity android:name="activityName" android:configChanges="all|of|them" android:theme="@android:style/Theme.Translucent.NoTitleBar.Fullscreen">
            // </activity>
            XmlElement activityElement = ManifestMod.CreateActivityElement(doc, ns, activityName);
            activityElement.SetAttribute("configChanges", ns, "fontScale|keyboard|keyboardHidden|locale|mnc|mcc|navigation|orientation|screenLayout|screenSize|smallestScreenSize|uiMode|touchscreen");
            activityElement.SetAttribute("theme", ns, "@android:style/Theme.Translucent.NoTitleBar.Fullscreen");
            return activityElement;
        }

        private static XmlElement CreateContentProviderElement(XmlDocument doc, string ns, string appId)
        {
            XmlElement provierElement = doc.CreateElement("provider");
            provierElement.SetAttribute("name", ns, FacebookContentProviderName);
            string authorities = string.Format(CultureInfo.InvariantCulture, FacebookContentProviderAuthFormat, appId);
            provierElement.SetAttribute("authorities", ns, authorities);
            provierElement.SetAttribute("exported", ns, "true");
            return provierElement;
        }

        private static XmlElement CreateActivityElement(XmlDocument doc, string ns, string activityName, bool exported = false)
        {
            // <activity android:name="activityName" android:exported="true">
            // </activity>
            XmlElement activityElement = doc.CreateElement("activity");
            activityElement.SetAttribute("name", ns, activityName);
            if (exported)
            {
                activityElement.SetAttribute("exported", ns, "true");
            }

            return activityElement;
        }

        private static void AddAppLinkingActivity(XmlDocument doc, XmlNode xmlNode, string ns, List<string> schemes)
        {
            XmlElement element = ManifestMod.CreateActivityElement(doc, ns, AppLinkActivityName, true);
            foreach (var scheme in schemes)
            {
                // We have to create an intent filter for each scheme since an intent filter
                // can have only one data element.
                XmlElement intentFilter = doc.CreateElement("intent-filter");

                var action = doc.CreateElement("action");
                action.SetAttribute("name", ns, "android.intent.action.VIEW");
                intentFilter.AppendChild(action);

                var category = doc.CreateElement("category");
                category.SetAttribute("name", ns, "android.intent.category.DEFAULT");
                intentFilter.AppendChild(category);

                XmlElement dataElement = doc.CreateElement("data");
                dataElement.SetAttribute("scheme", ns, scheme);
                intentFilter.AppendChild(dataElement);
                element.AppendChild(intentFilter);
            }

            ManifestMod.SetOrReplaceXmlElement(xmlNode, element);
        }

        private static void CreateDefaultAndroidManifest(string outputFile)
        {
            string[] possiblePaths =
            {
                // 2019.3+
                "PlaybackEngines/AndroidPlayer/Apk/UnityManifest.xml"
                // 2018.3 - 2019.2
                "PlaybackEngines/AndroidPlayer/Apk/AndroidManifest.xml",
                // 2017.2 - 2018.2
                "PlaybackEngines/androidplayer/AndroidManifest.xml",
            };

            foreach (var relativePath in possiblePaths)
            {
                var fullPath = Path.Combine(EditorApplication.applicationContentsPath, relativePath);
                if (File.Exists(fullPath))
                {
                    File.Copy(fullPath, outputFile);
                    return;
                }
            }

            // On Unity 5.3+ we don't have default manifest so use our own
            // manifest and warn the user that they may need to modify it manually
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream xmlStream = assembly.GetManifestResourceStream("Facebook.Unity.Editor.android.DefaultAndroidManifest.xml");
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(xmlStream);

            Debug.LogWarning(
                string.Format(
                    "No existing android manifest found at '{0}'. Creating a default manifest file",
                    outputFile));
            xmlDocument.Save(outputFile);
        }
    }
}
