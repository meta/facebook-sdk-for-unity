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

    internal class ComponentFactory
    {
        public const string GameObjectName = "UnityFacebookSDKPlugin";

#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
        [NoAutoStaticsCleanup]
#endif
        private static GameObject facebookGameObject;

        internal enum IfNotExist
        {
            AddNew,
            ReturnNull
        }

        private static GameObject FacebookGameObject
        {
            get
            {
                if (facebookGameObject == null)
                {
                    facebookGameObject = new GameObject(GameObjectName);
                }

                return facebookGameObject;
            }
        }

        /**
         * Gets one and only one component.  Lazy creates one if it doesn't exist
         */
        public static T GetComponent<T>(IfNotExist ifNotExist = IfNotExist.AddNew) where T : MonoBehaviour
        {
            var facebookGameObject = FacebookGameObject;

            T component = facebookGameObject.GetComponent<T>();
            if (component == null && ifNotExist == IfNotExist.AddNew)
            {
                component = facebookGameObject.AddComponent<T>();
            }

            return component;
        }

        /**
         * Creates a new component on the Facebook object regardless if there is already one
         */
        public static T AddComponent<T>() where T : MonoBehaviour
        {
            return FacebookGameObject.AddComponent<T>();
        }
    }
}
