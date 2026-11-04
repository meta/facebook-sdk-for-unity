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
    using System.Linq;
    using System.Reflection;
    using UnityEditor;

    public static class Utility
    {
        private const string BundleIdentifier = "bundleIdentifier";
        private const string ApplicationIdentifier = "applicationIdentifier";

        public static T Pop<T>(this IList<T> list)
        {
            if (!list.Any())
            {
                throw new InvalidOperationException("Attempting to pop item on empty list.");
            }

            int index = list.Count - 1;
            T value = list[index];
            list.RemoveAt(index);
            return value;
        }

        public static bool TryGetValue<T>(
            this IDictionary<string, object> dictionary,
            string key,
            out T value)
        {
            object resultObj;
            if (dictionary.TryGetValue(key, out resultObj) && resultObj is T)
            {
                value = (T)resultObj;
                return true;
            }

            value = default(T);
            return false;
        }

        public static string GetApplicationIdentifier()
        {
            Type playerSettingType = typeof(PlayerSettings);
            PropertyInfo info = playerSettingType.GetProperty(ApplicationIdentifier)
                                                ?? playerSettingType.GetProperty(BundleIdentifier);
            if (info != null)
            {
                string applicationIdentifier = (string)info.GetValue(playerSettingType, null);
                if (applicationIdentifier is string)
                {
                    return applicationIdentifier;
                }
            }

            return null;
        }
    }
}
