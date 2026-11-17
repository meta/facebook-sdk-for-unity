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

using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Facebook.Unity.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace Facebook.Unity.PostProcess
{
    /// <summary>
    /// Configures the exported Xcode project. Source-shipped to use the importing editor's APIs.
    /// </summary>
    public static class FBXcodePostProcess
    {
        // The Facebook iOS SDK is added as a Swift package rather than through CocoaPods.
        private const string FacebookSDKPackageURL = "https://github.com/meta/facebook-ios-sdk";

        // Up to the next major version: any 19.x release, the range the CocoaPods dependency allowed.
        private const string FacebookSDKMinimumVersion = "19.0.0";

        // The plugin imports FBSDKCoreKit, FBSDKLoginKit, FBSDKShareKit and FBSDKGamingServicesKit.
        // FacebookCore brings in FacebookBasics and FacebookAEM.
        private static readonly string[] FacebookSDKProducts =
        {
            "FacebookCore",
            "FacebookLogin",
            "FacebookShare",
            "FacebookGamingServices",
        };

        // When no plugin declares pods, the External Dependency Manager still writes an empty Podfile
        // and runs pod install by default, leaving Pods/ and a workspace the project doesn't need.
        // Projects with pods from other plugins are unaffected. Reflection, because the plugin
        // doesn't reference the resolver's assemblies and they may be absent or an older version.
        internal static void DisableEmptyPodfileGeneration()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type resolver = assembly.GetType("Google.IOSResolver");
                if (resolver == null)
                {
                    continue;
                }

                PropertyInfo setting = resolver.GetProperty(
                    "AllowEmptyPodfileGeneration",
                    BindingFlags.Public | BindingFlags.Static);
                if (setting == null || !setting.CanWrite || setting.PropertyType != typeof(bool))
                {
                    return;
                }

                try
                {
                    if ((bool)setting.GetValue(null, null))
                    {
                        setting.SetValue(null, false, null);
                        Debug.Log(
                            "Facebook SDK: turned off the External Dependency Manager's " +
                            "\"Allow empty Podfile generation\" setting. The Facebook SDK is added as a " +
                            "Swift package, so an iOS build with no pods from any plugin needs no Podfile.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning(
                        "Facebook SDK: could not turn off empty Podfile generation in the External " +
                        "Dependency Manager. The export may contain an unneeded empty Podfile. " + e.Message);
                }

                return;
            }
        }

        // Runs after the External Dependency Manager writes the Podfile and before it installs it.
        [PostProcessBuildAttribute(45)]
        public static void WarnAboutFacebookPods(BuildTarget buildTarget, string pathToBuiltProject)
        {
#if UNITY_IOS
            if (buildTarget != BuildTarget.iOS)
            {
                return;
            }

            // Other plugins may still use pods. Only Facebook SDK pods would duplicate the package.
            string podfilePath = Path.Combine(pathToBuiltProject, "Podfile");
            if (File.Exists(podfilePath) && Regex.IsMatch(File.ReadAllText(podfilePath), @"pod\s+['""]FBSDK"))
            {
                Debug.LogWarning(
                    "The Podfile at " + podfilePath + " adds Facebook SDK pods, but the Facebook SDK is " +
                    "now added as a Swift package. Remove those pods, or the app links two copies of the SDK.");
            }
#endif
        }

        [PostProcessBuildAttribute(100)]
        public static void ConfigureXcodeProject(BuildTarget buildTarget, string pathToBuiltProject)
        {
#if UNITY_IOS
            if (buildTarget != BuildTarget.iOS)
            {
                return;
            }

            string plistPath = ResolveInfoPlistPath(pathToBuiltProject);
            if (plistPath == null)
            {
                Debug.LogError(
                    "Could not find Info.plist under " + pathToBuiltProject +
                    ". The Facebook app ID, client token and URL schemes were not written.");
            }
            else
            {
                XCodePostProcess.UpdatePlistAtPath(plistPath);
            }

            string projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            PBXProject project = new PBXProject();
            project.ReadFromFile(projectPath);

            // The native plugin compiles into UnityFramework, so it needs configuring too.
            string mainTargetGuid = project.GetUnityMainTargetGuid();
            string frameworkTargetGuid = project.GetUnityFrameworkTargetGuid();
            ConfigureTarget(project, mainTargetGuid);
            ConfigureTarget(project, frameworkTargetGuid);
            AddFacebookSDKPackage(project, mainTargetGuid, frameworkTargetGuid);

            project.WriteToFile(projectPath);
#endif
        }

#if UNITY_IOS
        private static void ConfigureTarget(PBXProject project, string targetGuid)
        {
            project.AddFrameworkToProject(targetGuid, "Accelerate.framework", true);

            // Setting this unconditionally produced the invalid "5 5.0" (T110370082).
            if (string.IsNullOrEmpty(project.GetBuildPropertyForAnyConfig(targetGuid, "SWIFT_VERSION")))
            {
                project.SetBuildProperty(targetGuid, "SWIFT_VERSION", "5.0");
            }
        }

        private static void AddFacebookSDKPackage(PBXProject project, string mainTargetGuid, string frameworkTargetGuid)
        {
            // PBXProject adds a new reference on every call, and an Append build runs this again on
            // the exported project.
            if (project.WriteToString().Contains(FacebookSDKPackageURL))
            {
                return;
            }

            string packageGuid = project.AddRemotePackageReferenceAtVersionUpToNextMajor(
                FacebookSDKPackageURL,
                FacebookSDKMinimumVersion);

            // UnityFramework compiles the plugin. Xcode only embeds the SDK's dynamic frameworks in
            // the app when the app target uses the products too.
            foreach (string targetGuid in new[] { frameworkTargetGuid, mainTargetGuid })
            {
                foreach (string product in FacebookSDKProducts)
                {
                    project.AddRemotePackageFrameworkToProject(targetGuid, product, packageGuid, false);
                }
            }
        }

        // The .xcodeproj name, and the directory Swift nests Info.plist in. Not the target name.
        private static string GetXcodeProjectName(string pathToBuiltProject)
        {
            string xcodeProjectDirectory = Path.GetDirectoryName(
                PBXProject.GetPBXProjectPath(pathToBuiltProject));

            return Path.GetFileNameWithoutExtension(xcodeProjectDirectory);
        }

        // Objective-C emits Info.plist at the build root; Swift nests it under the project dir.
        private static string ResolveInfoPlistPath(string pathToBuiltProject)
        {
            string atBuildRoot = Path.Combine(pathToBuiltProject, "Info.plist");
            if (File.Exists(atBuildRoot))
            {
                return atBuildRoot;
            }

            string underProjectDirectory = Path.Combine(
                Path.Combine(pathToBuiltProject, GetXcodeProjectName(pathToBuiltProject)),
                "Info.plist");

            return File.Exists(underProjectDirectory) ? underProjectDirectory : null;
        }
#endif
    }

    /// <summary>
    /// Applies External Dependency Manager settings before an iOS build, ahead of its Podfile step.
    /// </summary>
    public class FBPodfilePreprocess : IPreprocessBuildWithReport
    {
        public int callbackOrder
        {
            get { return 0; }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.iOS)
            {
                FBXcodePostProcess.DisableEmptyPodfileGeneration();
            }
        }
    }
}
