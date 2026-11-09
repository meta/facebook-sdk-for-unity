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
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    internal class FacebookBuild
    {
        private const string FacebookPath = "Assets/FacebookSDK/";
        private const string SDKPath = "Assets/FacebookSDK/SDK/";
        private const string ExamplesPath = "Assets/FacebookSDK/Examples/";
        private const string PluginsPath = "Assets/FacebookSDK/Plugins/";
        private const string StreamingAssetsPath = "Assets/StreamingAssets/";
        private const string ExternalDependencyManagerPath = "Assets/ExternalDependencyManager/";
        private const string EditorPath = "Assets/Editor/";
        private const string ScriptsPath = "Assets/Scripts/";
        private const string IAPiOSPath = "Assets/IAPiOS/";

        public enum Target
        {
            DEBUG,
            RELEASE
        }

        private static string PackageName
        {
            get
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "facebook-unity-sdk-{0}.unitypackage",
                    FacebookSdkVersion.Build);
            }
        }

        private static string OutputPath
        {
            get
            {
                DirectoryInfo projectRoot = Directory.GetParent(Directory.GetCurrentDirectory());
                var outputDirectory = new DirectoryInfo(Path.Combine(projectRoot.FullName, "out"));

                // Create the directory if it doesn't exist
                outputDirectory.Create();
                return Path.Combine(outputDirectory.FullName, FacebookBuild.PackageName);
            }
        }

        // Exporting the *.unityPackage for Asset store
        public static string ExportPackage()
        {
            Debug.Log("Exporting Facebook Unity Package...");
            string path = OutputPath;
            try
            {
                if (!File.Exists(Path.Combine(Application.dataPath, "Temp")))
                {
                    AssetDatabase.CreateFolder("Assets", "Temp");
                }

                AssetDatabase.MoveAsset(SDKPath + "Resources/FacebookSettings.asset", "Assets/Temp/FacebookSettings.asset");
                AssetDatabase.DeleteAsset(PluginsPath + "Android/AndroidManifest.xml");
                AssetDatabase.DeleteAsset(PluginsPath + "Android/AndroidManifest.xml.meta");

                var fileList = new System.Collections.Generic.List<string>();
                fileList.AddRange(Directory.GetFiles(FacebookPath, "*.*", SearchOption.TopDirectoryOnly));
                fileList.AddRange(Directory.GetFiles(SDKPath, "*.*", SearchOption.AllDirectories));
                fileList.AddRange(Directory.GetFiles(ExamplesPath, "*.*", SearchOption.AllDirectories));
                fileList.AddRange(Directory.GetFiles(PluginsPath, "*.*", SearchOption.AllDirectories));
                fileList.AddRange(Directory.GetFiles(StreamingAssetsPath, "*.*", SearchOption.AllDirectories));
                fileList.AddRange(Directory.GetFiles(ExternalDependencyManagerPath, "*.*", SearchOption.AllDirectories));
                fileList.AddRange(Directory.GetFiles(EditorPath, "*.*", SearchOption.AllDirectories));
                fileList.AddRange(Directory.GetFiles(ScriptsPath, "*.*", SearchOption.AllDirectories));
                fileList.AddRange(Directory.GetFiles(IAPiOSPath, "*.*", SearchOption.AllDirectories));

                // No IncludeDependencies: the list is already recursive, and the walk adds uGUI.
                ExportPackageForCurrentUnity(fileList.ToArray(), path);
            }
            finally
            {
                // Move files back no matter what
                AssetDatabase.MoveAsset("Assets/Temp/FacebookSettings.asset", SDKPath + "Resources/FacebookSettings.asset");
                AssetDatabase.DeleteAsset("Assets/Temp");

                // regenerate the manifest
                ManifestMod.GenerateManifest();
            }

            Debug.Log("Finished exporting!");

            return path;
        }

        private static void ExportPackageForCurrentUnity(string[] assetPaths, string outputPath)
        {
            var editorAssembly = typeof(AssetDatabase).Assembly;
            var parametersType = editorAssembly.GetType(
                "UnityEditor.AssetPackage.ExportPackageParameters",
                false);
            var packageType = editorAssembly.GetType("UnityEditor.AssetPackage.Package", false);
            if (parametersType != null && packageType != null)
            {
                var constructor = parametersType.GetConstructor(
                    new[]
                    {
                        typeof(string[]),
                        typeof(string),
                        typeof(string),
                        typeof(ExportPackageOptions),
                    });
                var export = packageType.GetMethod(
                    "Export",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { parametersType },
                    null);
                if (constructor == null || export == null)
                {
                    throw new MissingMethodException(
                        "Unity's AssetPackage export API is present but has an unsupported signature");
                }

                var parameters = constructor.Invoke(
                    new object[]
                    {
                        assetPaths,
                        outputPath,
                        null,
                        ExportPackageOptions.Recurse,
                    });
                export.Invoke(null, new[] { parameters });
                return;
            }

            var legacyExport = typeof(AssetDatabase).GetMethod(
                "ExportPackage",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[]
                {
                    typeof(string[]),
                    typeof(string),
                    typeof(ExportPackageOptions),
                },
                null);
            if (legacyExport == null)
            {
                throw new MissingMethodException("Unity does not expose a supported package export API");
            }

            legacyExport.Invoke(
                null,
                new object[]
                {
                    assetPaths,
                    outputPath,
                    ExportPackageOptions.Recurse,
                });
        }
    }
}
