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
    using UnityEditor;
    using UnityEngine;
#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
    using global::Unity.Scripting.LifecycleManagement;
#endif

    public partial class FacebookImporter : AssetPostprocessor
    {
        private enum WindowsArchitecture
        {
            x86,
            x86_64,
            both
        }

#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
        [NoAutoStaticsCleanup]
#endif
        private static string currentAsset = "";

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string str in importedAssets)
            {
                currentAsset = str;

                SetCanvasDllConfiguration("/Facebook.Unity.Canvas.dll");
                SetCanvasDllConfiguration("/CanvasJSSDKBindings.jslib");

                SetAndroidDllConfiguration("/Facebook.Unity.Android.dll");
                SetAndroidDllConfiguration("/libs/facebook-android-wrapper-15.0.0.aar");

                SetiOSDllConfiguration("/Facebook.Unity.IOS.dll");

                SetiOSEditorSwiftConfiguration("/FBSDKShareTournamentDialog.swift");
                SetiOSEditorSwiftConfiguration("/FBSDKTournament.swift");
                SetiOSEditorSwiftConfiguration("/FBSDKTournamentFetcher.swift");
                SetiOSEditorSwiftConfiguration("/FBSDKTournamentUpdater.swift");

                SetWindowsDllConfiguration("/Facebook.Unity.Windows.dll", WindowsArchitecture.both);

                // Windows SDK Dlls x84 and x64
                SetWindowsDllConfiguration("/LibFBGManaged.dll", WindowsArchitecture.both);
                SetWindowsDllConfiguration("/XInputDotNetPure.dll", WindowsArchitecture.both);

                // Windows SDK Dlls only x84
                SetWindowsDllConfiguration("/x86/LibFBGPlatform.dll", WindowsArchitecture.x86);
                SetWindowsDllConfiguration("/x86/cpprest_2_10.dll", WindowsArchitecture.x86);
                SetWindowsDllConfiguration("/x86/libcrypto-3.dll", WindowsArchitecture.x86);
                SetWindowsDllConfiguration("/x86/libcurl.dll", WindowsArchitecture.x86);
                SetWindowsDllConfiguration("/x86/LibFBGUI.dll", WindowsArchitecture.x86);
                SetWindowsDllConfiguration("/x86/libssl-3.dll", WindowsArchitecture.x86);
                SetWindowsDllConfiguration("/x86/tinyxml2.dll", WindowsArchitecture.x86);
                SetWindowsDllConfiguration("/x86/WebView2Loader.dll", WindowsArchitecture.x86);
                SetWindowsDllConfiguration("/x86/XInputInterface.dll", WindowsArchitecture.x86);
                SetWindowsDllConfiguration("/x86/zlib1.dll", WindowsArchitecture.x86);

                // Windows SDK Dlls only x64
                SetWindowsDllConfiguration("/x64/LibFBGPlatform.dll", WindowsArchitecture.x86_64);
                SetWindowsDllConfiguration("/x64/cpprest_2_10.dll", WindowsArchitecture.x86_64);
                SetWindowsDllConfiguration("/x64/libcrypto-3-x64.dll", WindowsArchitecture.x86_64);
                SetWindowsDllConfiguration("/x64/libcurl.dll", WindowsArchitecture.x86_64);
                SetWindowsDllConfiguration("/x64/LibFBGUI.dll", WindowsArchitecture.x86_64);
                SetWindowsDllConfiguration("/x64/libssl-3-x64.dll", WindowsArchitecture.x86_64);
                SetWindowsDllConfiguration("/x64/tinyxml2.dll", WindowsArchitecture.x86_64);
                SetWindowsDllConfiguration("/x64/WebView2Loader.dll", WindowsArchitecture.x86_64);
                SetWindowsDllConfiguration("/x64/XInputInterface.dll", WindowsArchitecture.x86_64);
                SetWindowsDllConfiguration("/x64/zlib1.dll", WindowsArchitecture.x86_64);
            }
        }

        private static PluginImporter GetPluginImporter(string path)
        {
            string sdkAsset = "Assets" + path;
            if (currentAsset == sdkAsset)
            {
                return ((PluginImporter)PluginImporter.GetAtPath(sdkAsset));
            }
            return null;
        }

        private static void SetCanvasDllConfiguration(string path)
        {
            PluginImporter canvasDLL = GetPluginImporter("/FacebookSDK/Plugins/Canvas" + path);
            if (canvasDLL)
            {
                canvasDLL.ClearSettings();
                canvasDLL.SetCompatibleWithAnyPlatform(false);
                canvasDLL.SetCompatibleWithEditor(true);
                canvasDLL.SetCompatibleWithPlatform(BuildTarget.WebGL, true);
            }
        }

        private static void SetAndroidDllConfiguration(string path)
        {
            PluginImporter androidDLL = GetPluginImporter("/FacebookSDK/Plugins/Android" + path);
            if (androidDLL)
            {
                androidDLL.ClearSettings();
                androidDLL.SetCompatibleWithAnyPlatform(false);
                androidDLL.SetCompatibleWithEditor(true);
                androidDLL.SetCompatibleWithPlatform(BuildTarget.Android, true);
            }
        }

        private static void SetiOSDllConfiguration(string path)
        {
            PluginImporter iOSDLL = GetPluginImporter("/FacebookSDK/Plugins/iOS" + path);
            if (iOSDLL)
            {
                iOSDLL.ClearSettings();
                iOSDLL.SetCompatibleWithAnyPlatform(false);
                iOSDLL.SetCompatibleWithPlatform(BuildTarget.iOS, true);
            }
        }

        private static void SetiOSEditorSwiftConfiguration(string path)
        {
            PluginImporter iOSDLL = GetPluginImporter("/FacebookSDK/SDK/Editor/iOS/Swift" + path);
            if (iOSDLL)
            {
                iOSDLL.ClearSettings();
                iOSDLL.SetCompatibleWithAnyPlatform(false);
                iOSDLL.SetCompatibleWithEditor(true);
                iOSDLL.SetCompatibleWithPlatform(BuildTarget.iOS, true);
            }
        }

        private static void SetWindowsDllConfiguration(string path, WindowsArchitecture architecture)
        {
            PluginImporter windowsDLL = GetPluginImporter("/FacebookSDK/Plugins/Windows" + path);
            if (windowsDLL)
            {
                windowsDLL.ClearSettings();

                bool isEditor = architecture == WindowsArchitecture.x86_64 || architecture == WindowsArchitecture.both;

                windowsDLL.SetCompatibleWithAnyPlatform(false);
                windowsDLL.SetCompatibleWithEditor(isEditor);
                windowsDLL.SetCompatibleWithPlatform(BuildTarget.WebGL, false);
                windowsDLL.SetCompatibleWithPlatform(BuildTarget.Android, false);
                windowsDLL.SetCompatibleWithPlatform(BuildTarget.iOS, false);

                switch (architecture)
                {
                    case WindowsArchitecture.x86:
                        windowsDLL.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, true);
                        windowsDLL.SetPlatformData(BuildTarget.StandaloneWindows, "CPU", "AnyCPU");
                        windowsDLL.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, false);
                        windowsDLL.SetPlatformData(BuildTarget.StandaloneWindows64, "CPU", "none");
                        break;
                    case WindowsArchitecture.x86_64:
                        windowsDLL.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, false);
                        windowsDLL.SetPlatformData(BuildTarget.StandaloneWindows, "CPU", "none");
                        windowsDLL.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, true);
                        windowsDLL.SetPlatformData(BuildTarget.StandaloneWindows64, "CPU", "AnyCPU");
                        break;
                    case WindowsArchitecture.both:
                        windowsDLL.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, true);
                        windowsDLL.SetPlatformData(BuildTarget.StandaloneWindows, "CPU", "AnyCPU");
                        windowsDLL.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, true);
                        windowsDLL.SetPlatformData(BuildTarget.StandaloneWindows64, "CPU", "AnyCPU");
                        break;
                }
            }
        }
    }
}
