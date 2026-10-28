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

using UnityEditor;
using UnityEditor.Callbacks;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
#endif

namespace Facebook.Unity.PostProcess
{
	/// <summary>
	/// Post-build processor that ensures required Facebook iOS frameworks are embedded into the Xcode project.
	/// This version fixes app crash at startup by correctly embedding and signing .xcframeworks,
	/// and setting search paths for runtime resolution.
	/// </summary>
	public static class AddFramework
	{
		[PostProcessBuild(999)]
		public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToBuiltProject)
		{
#if UNITY_IOS
			string[] frameworks =
			{
				"FBAEMKit",
				"FBSDKCoreKit",
				"FBSDKCoreKit_Basics",
				"FBSDKGamingServicesKit",
				"FBSDKLoginKit",
				"FBSDKShareKit"
			};

			string projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
			PBXProject proj = new PBXProject();
			proj.ReadFromFile(projPath);
			string unityMainTarget = proj.GetUnityMainTargetGuid();

			foreach (var framework in frameworks)
			{
				var frameworkName = $"{framework}.xcframework";
				var src = Path.Combine("Pods", framework, "XCFrameworks", frameworkName);
				var frameworkPath = proj.AddFile(src, src);
				proj.AddFileToBuild(unityMainTarget, frameworkPath);
				proj.AddFileToEmbedFrameworks(unityMainTarget, frameworkPath);
			}

			proj.WriteToFile(projPath);
#endif
		}
	}
}
