/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Facebook.Unity.Tests.BuildPostProcess
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Xml;
    using Facebook.Unity.Editor;
    using Facebook.Unity.Settings;
    using NUnit.Framework;

    [TestFixture]
    public class ProductImprovementSettingsTest
    {
        private object originalSettings;
        private string temporaryFile;
        private FieldInfo instanceField;

        [SetUp]
        public void SetUp()
        {
            this.instanceField = typeof(FacebookSettings).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
            this.originalSettings = this.instanceField.GetValue(null);
            // Mono tests cannot create native Unity objects; isolate the managed settings fields.
            var settings = (FacebookSettings)FormatterServices.GetUninitializedObject(typeof(FacebookSettings));
            var cachedPointer = typeof(FacebookSettings).BaseType.BaseType.GetField(
                "m_CachedPtr", BindingFlags.Instance | BindingFlags.NonPublic);
            if (cachedPointer != null)
            {
                cachedPointer.SetValue(settings, new System.IntPtr(1));
            }
            this.instanceField.SetValue(null, settings);
            SetField(settings, "appIds", new List<string> { "123" });
            SetField(settings, "clientTokens", new List<string> { "token" });
            SetField(settings, "appLinkSchemes", new List<FacebookSettings.UrlSchemes> { new FacebookSettings.UrlSchemes() });
            this.temporaryFile = Path.GetTempFileName();
        }

        [TearDown]
        public void TearDown()
        {
            this.instanceField.SetValue(null, this.originalSettings);
            File.Delete(this.temporaryFile);
        }

        [Test]
        public void SettingNotifiesOnlyOnChangeAndIsIndependent()
        {
            FacebookSettings.AutoLogAppEventsEnabled = true;
            FacebookSettings.AdvertiserIDCollectionEnabled = true;
            int changes = 0;
            FacebookSettings.OnChangeCallback callback = () => changes++;
            FacebookSettings.RegisterChangeEventCallback(callback);
            try
            {
                FacebookSettings.AutoLogProductImprovementEnabled = true;
                FacebookSettings.AutoLogProductImprovementEnabled = true;
                Assert.IsTrue(FacebookSettings.AutoLogProductImprovementEnabled);
                Assert.AreEqual(1, changes);
                FacebookSettings.AutoLogProductImprovementEnabled = false;
                Assert.IsFalse(FacebookSettings.AutoLogProductImprovementEnabled);
                Assert.AreEqual(2, changes);
                Assert.IsTrue(FacebookSettings.AutoLogAppEventsEnabled);
                Assert.IsTrue(FacebookSettings.AdvertiserIDCollectionEnabled);
                FacebookSettings.AutoLogAppEventsEnabled = false;
                Assert.IsFalse(FacebookSettings.AutoLogProductImprovementEnabled);
            }
            finally
            {
                FacebookSettings.UnregisterChangeEventCallback(callback);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ExportsAndReplacesAndroidSetting(bool enabled)
        {
            File.WriteAllText(this.temporaryFile,
                "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\"><application>" +
                "<meta-data android:name=\"com.facebook.sdk.AutoLogProductImprovementEnabled\" android:value=\"stale\"/>" +
                "</application></manifest>");
            FacebookSettings.AutoLogProductImprovementEnabled = enabled;
            ManifestMod.UpdateManifest(this.temporaryFile);
            ManifestMod.UpdateManifest(this.temporaryFile);
            var document = new XmlDocument();
            document.Load(this.temporaryFile);
            var namespaces = new XmlNamespaceManager(document.NameTable);
            namespaces.AddNamespace("android", "http://schemas.android.com/apk/res/android");
            var entries = document.SelectNodes(
                "/manifest/application/meta-data[@android:name='com.facebook.sdk.AutoLogProductImprovementEnabled']", namespaces);
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(enabled.ToString().ToLowerInvariant(),
                ((XmlElement)entries[0]).GetAttribute("value", "http://schemas.android.com/apk/res/android"));
        }

        [Test]
        public void AppLinkIntentFiltersAreBrowsable()
        {
            SetField(
                (FacebookSettings)this.instanceField.GetValue(null),
                "appLinkSchemes",
                new List<FacebookSettings.UrlSchemes> { new FacebookSettings.UrlSchemes(new List<string> { "fb123", "myapp" }) });
            File.WriteAllText(this.temporaryFile,
                "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\"><application/></manifest>");
            ManifestMod.UpdateManifest(this.temporaryFile);
            var document = new XmlDocument();
            document.Load(this.temporaryFile);
            var namespaces = new XmlNamespaceManager(document.NameTable);
            namespaces.AddNamespace("android", "http://schemas.android.com/apk/res/android");
            var filters = document.SelectNodes(
                "/manifest/application/activity[@android:name='" + ManifestMod.AppLinkActivityName + "']/intent-filter", namespaces);
            Assert.AreEqual(2, filters.Count);
            foreach (XmlNode filter in filters)
            {
                Assert.AreEqual(1, filter.SelectNodes("category[@android:name='android.intent.category.DEFAULT']", namespaces).Count);
                Assert.AreEqual(1, filter.SelectNodes("category[@android:name='android.intent.category.BROWSABLE']", namespaces).Count);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ExportsAndReplacesIOSSetting(bool enabled)
        {
            File.WriteAllText(this.temporaryFile,
                "<plist version=\"1.0\"><dict><key>FBSDKAutoLogProductImprovementEnabled</key><string>stale</string></dict></plist>");
            FacebookSettings.AutoLogProductImprovementEnabled = enabled;
            var parser = new PListParser(this.temporaryFile);
            parser.UpdateFBSettings("123", "token", "", new List<string>());
            parser.UpdateFBSettings("123", "token", "", new List<string>());
            parser.WriteToFile();
            var document = new XmlDocument();
            document.XmlResolver = null;
            document.Load(this.temporaryFile);
            var keys = document.SelectNodes("/plist/dict/key[text()='FBSDKAutoLogProductImprovementEnabled']");
            Assert.AreEqual(1, keys.Count);
            Assert.AreEqual(enabled ? "true" : "false", keys[0].NextSibling.Name);
            Assert.AreEqual(0, document.SelectNodes("/plist/dict/key[text()='FacebookAutoLogProductImprovementEnabled']").Count);
        }

        private static void SetField(FacebookSettings settings, string name, object value)
        {
            typeof(FacebookSettings).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(settings, value);
        }
    }
}
