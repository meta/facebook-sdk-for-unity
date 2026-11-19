/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Facebook.Unity.Tests.BuildPostProcess
{
    using Facebook.Unity.Editor;
    using Facebook.Unity.Settings;
    using NUnit.Framework;

    [TestFixture]
    public class PlayModeBuildTargetTest
    {
        [TestCase(UnityEditor.BuildTarget.Android, FacebookSettings.BuildTarget.Android)]
        [TestCase(UnityEditor.BuildTarget.iOS, FacebookSettings.BuildTarget.iOS)]
        [TestCase(UnityEditor.BuildTarget.StandaloneWindows64, FacebookSettings.BuildTarget.StandaloneWindows64)]
        [TestCase(UnityEditor.BuildTarget.XboxOne, FacebookSettings.BuildTarget.XboxOne)]
        public void MapsKnownTargets(UnityEditor.BuildTarget target, FacebookSettings.BuildTarget expected)
        {
            Assert.AreEqual(expected, PlayModeStateChanged.ToFacebookBuildTarget(target));
        }

        [TestCase(UnityEditor.BuildTarget.GameCoreXboxSeries)]
        [TestCase(UnityEditor.BuildTarget.EmbeddedLinux)]
        [TestCase((UnityEditor.BuildTarget)12345)]
        public void MapsUnknownTargetsToNone(UnityEditor.BuildTarget target)
        {
            Assert.AreEqual(FacebookSettings.BuildTarget.none, PlayModeStateChanged.ToFacebookBuildTarget(target));
        }
    }
}
