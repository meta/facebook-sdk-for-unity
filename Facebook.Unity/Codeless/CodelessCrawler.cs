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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using System.Text;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;
#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
using Unity.Scripting.LifecycleManagement;
#endif

namespace Facebook.Unity
{
    public partial class CodelessCrawler : MonoBehaviour
    {
#if UNITY_IOS
        [DllImport ("__Internal")]
        private static extern void IOSFBSendViewHierarchy (string tree);
#endif

#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
        [NoAutoStaticsCleanup]
#endif
        private static bool isGeneratingSnapshot = false;

#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
        [NoAutoStaticsCleanup]
#endif
        private static Camera mainCamera = null;

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        private static void ResetStaticState()
        {
            isGeneratingSnapshot = false;
            mainCamera = null;
        }

        public void OnEnable()
        {
            SceneManager.activeSceneChanged -= onActiveSceneChanged;
            SceneManager.activeSceneChanged += onActiveSceneChanged;
        }

        public void OnDisable()
        {
            SceneManager.activeSceneChanged -= onActiveSceneChanged;
        }

        public void CaptureViewHierarchy(string message)
        {
            if (null == mainCamera || !mainCamera.isActiveAndEnabled)
            {
                updateMainCamera();
            }
            StartCoroutine(GenSnapshot());
        }

        private IEnumerator GenSnapshot()
        {
            yield return (new WaitForEndOfFrame());

            GenerateSnapshot(GenerateAndSendSnapshot);
        }

        internal static void GenerateSnapshot(Action generateSnapshot)
        {
            if (isGeneratingSnapshot)
            {
                return;
            }
            isGeneratingSnapshot = true;
            try
            {
                generateSnapshot();
            }
            finally
            {
                isGeneratingSnapshot = false;
            }
        }

        private static void GenerateAndSendSnapshot()
        {
            StringBuilder masterBuilder = new StringBuilder();
            masterBuilder.AppendFormat(
                @"{{""screenshot"":""{0}"",",
                GenBase64Screenshot()
            );
            masterBuilder.AppendFormat(
                @"""screenname"":""{0}"",",
                SceneManager.GetActiveScene().name
            );
            masterBuilder.AppendFormat(
                @"""view"":[{0}]}}",
                GenViewJson()
            );
            string json = masterBuilder.ToString();
            switch (Constants.CurrentPlatform)
            {
                case FacebookUnityPlatform.Android:
                    SendAndroid(json);
                    break;
                case FacebookUnityPlatform.IOS:
                    SendIos(json);
                    break;
                default:
                    break;
            }
        }

        private static void SendAndroid(string json)
        {
            using (AndroidJavaObject viewIndexer = new AndroidJavaClass("com.facebook.appevents.codeless.ViewIndexer"))
            {
                viewIndexer.CallStatic("sendToServerUnityInstance", json);
            }
        }

        private static void SendIos(string json)
        {
#if UNITY_IOS
            CodelessCrawler.IOSFBSendViewHierarchy (json);
#endif
        }

        private static string GenBase64Screenshot()
        {
            Texture2D tex = new Texture2D(Screen.width, Screen.height);
            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            tex.Apply();
            string screenshot64 = System.Convert.ToBase64String(tex.EncodeToJPG());
            UnityEngine.Object.Destroy(tex);
            return screenshot64;
        }

        private static string GenViewJson()
        {
            GameObject[] rootGameObjs = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(
                @"{{""classname"":""{0}"",""childviews"":[",
                SceneManager.GetActiveScene().name
            );
            foreach (GameObject curObj in rootGameObjs)
            {
                GenChild(curObj, builder);
                builder.Append(",");
            }
            if (builder[builder.Length - 1] == ',')
            {
                builder.Length--;
            }
            builder.AppendFormat(
                @"],""classtypebitmask"":""{0}"",""tag"":""0"",""dimension"":{{""height"":{1},""width"":{2},""scrolly"":{3},""left"":{4},""top"":{5},""scrollx"":{6},""visibility"":{7}}}}}",
                "0",
                (int)Screen.height,
                (int)Screen.width,
                "0",
                "0",
                "0",
                "0",
                "0"
            );
            return builder.ToString();
        }

        private static void GenChild(GameObject curObj, StringBuilder builder)
        {
            builder.AppendFormat(
                @"{{""classname"":""{0}"",""childviews"":[",
                curObj.name
            );
            int childCount = curObj.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                if (null == curObj.GetComponent<Button>())
                {
                    GenChild(curObj.transform.GetChild(i).gameObject, builder);
                    builder.Append(",");
                }
            }

            if (builder[builder.Length - 1] == ',')
            {
                builder.Length--;
            }

            UnityEngine.Canvas canvasParent = curObj.GetComponentInParent<UnityEngine.Canvas>();
            string btntext = "";
            if (null != curObj.GetComponent<Button>() && null != canvasParent)
            {
                Rect rect = curObj.GetComponent<RectTransform>().rect;
                Vector2 position = getScreenCoordinate(curObj.transform.position, canvasParent.renderMode);
                Text textComponent = curObj.GetComponent<Button>().GetComponentInChildren<Text>();
                if (null != textComponent)
                {
                    btntext = "\"text\":\"" + textComponent.text + "\",";
                }

                builder.AppendFormat(
                    @"],{8}""classtypebitmask"":""{0}"",""tag"":""0"",""dimension"":{{""height"":{1},""width"":{2},""scrolly"":{3},""left"":{4},""top"":{5},""scrollx"":{6},""visibility"":{7}}}}}",
                    getClasstypeBitmaskButton(),
                    (int)rect.height,
                    (int)rect.width,
                    0,
                    (int)Math.Ceiling(position.x - (rect.width / 2)),//left
                    (int)Math.Ceiling((Screen.height - position.y - (rect.height / 2))),
                    0,
                    getVisibility(curObj),
                    btntext
                );
            }
            else
            {
                builder.AppendFormat(
                    @"],{8}""classtypebitmask"":""{0}"",""tag"":""0"",""dimension"":{{""height"":{1},""width"":{2},""scrolly"":{3},""left"":{4},""top"":{5},""scrollx"":{6},""visibility"":{7}}}}}",
                    getClasstypeBitmaskButton(),
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    getVisibility(curObj),
                    btntext
                );
            }
        }

        private void onActiveSceneChanged(Scene arg0, Scene arg1)
        {
            updateMainCamera();
        }

        private static void updateMainCamera()
        {
            mainCamera = Camera.main;
        }

        private static Vector2 getScreenCoordinate(Vector3 position, RenderMode renderMode)
        {
            if (RenderMode.ScreenSpaceOverlay == renderMode || null == mainCamera)
            {
                return (position);
            }
            else
            {
                return mainCamera.WorldToScreenPoint(position);
            }
        }

        private static string getClasstypeBitmaskButton()
        {
            switch (Constants.CurrentPlatform)
            {
                case FacebookUnityPlatform.Android:
                    return "4";
                case FacebookUnityPlatform.IOS:
                    return "16";
                default:
                    return "0";
            }
        }

        private static string getVisibility(GameObject gameObj)
        {
            if (gameObj.activeInHierarchy)
            {
                return "0";
            }
            else
            {
                return "8";
            }
        }
    }
}
