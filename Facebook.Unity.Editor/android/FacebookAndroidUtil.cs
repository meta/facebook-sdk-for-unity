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
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Facebook.Unity.Settings;
    using Google;
    using UnityEditor;
    using UnityEngine;
#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
    using global::Unity.Scripting.LifecycleManagement;
#endif

    public partial class FacebookAndroidUtil
    {
        public const string ErrorNoSDK = "no_android_sdk";
        public const string ErrorNoKeystore = "no_android_keystore";
        public const string ErrorNoKeytool = "no_java_keytool";
        public const string ErrorNoOpenSSL = "no_openssl";
        public const string ErrorKeytoolError = "java_keytool_error";

#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
        [NoAutoStaticsCleanup]
#endif
        private static string debugKeyHash;
#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
        [NoAutoStaticsCleanup]
#endif
        private static string setupError;

        public static bool SetupProperly
        {
            get
            {
                return DebugKeyHash != null;
            }
        }

        public static string DebugKeyHash
        {
            get
            {
                if (debugKeyHash == null)
                {
                    if (!HasAndroidSDK())
                    {
                        setupError = ErrorNoSDK;
                        return null;
                    }

                    if (!HasAndroidKeystoreFile())
                    {
                        setupError = ErrorNoKeystore;
                        return null;
                    }

                    if (!DoesCommandExist("echo \"xxx\" | openssl base64"))
                    {
                        setupError = ErrorNoOpenSSL;
                        return null;
                    }

                    if (!DoesCommandExist("keytool"))
                    {
                        setupError = ErrorNoKeytool;
                        return null;
                    }

                    debugKeyHash = GetKeyHash("androiddebugkey", DebugKeyStorePath, "android");
                }

                return debugKeyHash;
            }
        }

        public static string SetupError
        {
            get
            {
                return setupError;
            }
        }

        private static string DebugKeyStorePath
        {
            get
            {
                if (!string.IsNullOrEmpty(FacebookSettings.AndroidKeystorePath))
                {
                    return FacebookSettings.AndroidKeystorePath;
                }
                return (Application.platform == RuntimePlatform.WindowsEditor) ?
                    System.Environment.GetEnvironmentVariable("HOMEDRIVE") + System.Environment.GetEnvironmentVariable("HOMEPATH") + @"\.android\debug.keystore" :
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + @"/.android/debug.keystore";
            }
        }

        public static bool HasAndroidSDK()
        {
            string sdkPath = GetAndroidSdkPath();
            return !string.IsNullOrEmpty(sdkPath) && System.IO.Directory.Exists(sdkPath);
        }

        public static bool HasAndroidKeystoreFile()
        {
            return System.IO.File.Exists(DebugKeyStorePath);
        }

        public static string GetAndroidSdkPath()
        {
            string sdkPath = EditorPrefs.GetString("AndroidSdkRoot");

            if (string.IsNullOrEmpty(sdkPath) || EditorPrefs.GetBool("SdkUseEmbedded"))
            {
                try
                {
                    sdkPath = (string)VersionHandler.InvokeStaticMethod(typeof(BuildPipeline), "GetPlaybackEngineDirectory", new object[] { BuildTarget.Android, BuildOptions.None });
                }
                catch (Exception)
                {
                    // Do not return a stale EditorPrefs value when the embedded
                    // SDK lookup failed.
                    sdkPath = null;
                    foreach (Assembly assembly in GetLoadedAssemblies())
                    {
                        if (assembly.GetName().Name == "UnityEditor.Android.Extensions")
                        {
                            var assemblyPath = GetLoadedAssemblyPath(assembly);
                            if (string.IsNullOrEmpty(assemblyPath))
                            {
                                UnityEngine.Debug.LogWarning(
                                    "UnityEditor.Android.Extensions did not expose a loaded assembly path");
                                continue;
                            }

                            sdkPath = Path.GetDirectoryName(assemblyPath);
                            if (!string.IsNullOrEmpty(sdkPath))
                            {
                                break;
                            }

                            UnityEngine.Debug.LogWarning(
                                "Unable to derive the Android SDK directory from " + assemblyPath);
                        }
                    }
                }
            }

            return sdkPath;
        }

        private static string GetLoadedAssemblyPath(Assembly assembly)
        {
            var assemblyExtension = typeof(UnityEngine.Object).Assembly.GetType(
                "UnityEngine.AssemblyExtension",
                false);
            var getLoadedAssemblyPath = assemblyExtension == null
                ? null
                : assemblyExtension.GetMethod(
                    "GetLoadedAssemblyPath",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(Assembly) },
                    null);
            if (getLoadedAssemblyPath != null)
            {
                try
                {
                    var loadedPath = getLoadedAssemblyPath.Invoke(null, new object[] { assembly }) as string;
                    if (!string.IsNullOrEmpty(loadedPath))
                    {
                        return loadedPath;
                    }
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogWarning(
                        "Unable to read the Unity-managed assembly path: " + exception.Message);
                }
            }

            var location = typeof(Assembly).GetProperty(
                "Location",
                BindingFlags.Public | BindingFlags.Instance);
            if (location == null)
            {
                UnityEngine.Debug.LogWarning(
                    "The runtime does not expose a loaded assembly location API");
                return null;
            }

            try
            {
                return location.GetValue(assembly, null) as string;
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning(
                    "Unable to read the runtime assembly location: " + exception.Message);
                return null;
            }
        }

        private static IEnumerable<Assembly> GetLoadedAssemblies()
        {
            var currentAssembliesType = typeof(UnityEngine.Object).Assembly.GetType(
                "UnityEngine.Assemblies.CurrentAssemblies",
                false);
            var getLoadedAssemblies = currentAssembliesType == null
                ? null
                : currentAssembliesType.GetMethod(
                    "GetLoadedAssemblies",
                    BindingFlags.Public | BindingFlags.Static);
            if (getLoadedAssemblies != null)
            {
                try
                {
                    var loadedAssemblies = getLoadedAssemblies.Invoke(null, null) as IEnumerable<Assembly>;
                    if (loadedAssemblies != null)
                    {
                        return loadedAssemblies;
                    }
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogWarning(
                        "Unable to enumerate loaded assemblies through CurrentAssemblies: " +
                        exception.Message);
                }
            }

            var getAssemblies = typeof(AppDomain).GetMethod(
                "GetAssemblies",
                BindingFlags.Public | BindingFlags.Instance);
            IEnumerable<Assembly> appDomainAssemblies = null;
            if (getAssemblies != null)
            {
                try
                {
                    appDomainAssemblies = getAssemblies.Invoke(
                        AppDomain.CurrentDomain,
                        null) as IEnumerable<Assembly>;
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogWarning(
                        "Unable to enumerate loaded assemblies through AppDomain: " +
                        exception.Message);
                }
            }

            if (appDomainAssemblies == null)
            {
                UnityEngine.Debug.LogWarning("Unable to enumerate loaded assemblies on this Unity runtime");
                return new Assembly[0];
            }

            return appDomainAssemblies;
        }

        private static string GetKeyHash(string alias, string keyStore, string password)
        {
            var proc = new Process();
            var arguments = @"""keytool -storepass {0} -keypass {1} -exportcert -alias {2} -keystore {3} | openssl sha1 -binary | openssl base64""";
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                proc.StartInfo.FileName = "cmd";
                arguments = @"/C " + arguments;
            }
            else
            {
                proc.StartInfo.FileName = "bash";
                arguments = @"-c " + arguments;
            }

            proc.StartInfo.Arguments = string.Format(arguments, password, password, alias, keyStore);
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.Start();
            var keyHash = new StringBuilder();
            while (!proc.HasExited)
            {
                keyHash.Append(proc.StandardOutput.ReadToEnd());
            }

            switch (proc.ExitCode)
            {
                case 255:
                    setupError = ErrorKeytoolError;
                    return null;
            }

            return keyHash.ToString().TrimEnd('\n');
        }

        private static bool DoesCommandExist(string command)
        {
            var proc = new Process();
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                proc.StartInfo.FileName = "cmd";
                proc.StartInfo.Arguments = @"/C" + command;
            }
            else
            {
                proc.StartInfo.FileName = "bash";
                proc.StartInfo.Arguments = @"-c " + command;
            }

            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();
            proc.WaitForExit();
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                return proc.ExitCode == 0;
            }
            else
            {
                return proc.ExitCode != 127;
            }
        }
    }
}
