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
    using UnityEngine;
#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
    using global::Unity.Scripting.LifecycleManagement;
#endif

    internal static class FacebookLogger
    {
#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
        [NoAutoStaticsCleanup]
#endif
        private static IFacebookLogger instance;

        static FacebookLogger()
        {
            FacebookLogger.Instance = new DebugLogger();
        }

        internal static IFacebookLogger Instance
        {
            private get { return instance; }
            set { instance = value; }
        }

        public static void Log(string msg)
        {
            FacebookLogger.Instance.Log(msg);
        }

        public static void Log(string format, params string[] args)
        {
            FacebookLogger.Log(string.Format(format, args));
        }

        public static void Info(string msg)
        {
            FacebookLogger.Instance.Info(msg);
        }

        public static void Info(string format, params string[] args)
        {
            FacebookLogger.Info(string.Format(format, args));
        }

        public static void Warn(string msg)
        {
            FacebookLogger.Instance.Warn(msg);
        }

        public static void Warn(string format, params string[] args)
        {
            FacebookLogger.Warn(string.Format(format, args));
        }

        public static void Error(string msg)
        {
            FacebookLogger.Instance.Error(msg);
        }

        public static void Error(string format, params string[] args)
        {
            FacebookLogger.Error(string.Format(format, args));
        }

        private class DebugLogger : IFacebookLogger
        {
            public DebugLogger()
            {
            }

            public void Log(string msg)
            {
                if (Debug.isDebugBuild)
                {
                    Debug.Log(msg);
                }
            }

            public void Info(string msg)
            {
                Debug.Log(msg);
            }

            public void Warn(string msg)
            {
                Debug.LogWarning(msg);
            }

            public void Error(string msg)
            {
                Debug.LogError(msg);
            }
        }
    }
}
