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
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using Facebook.Unity.Settings;
#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
    using global::Unity.Scripting.LifecycleManagement;
#endif

    internal partial class PListParser
    {
        private const string LSApplicationQueriesSchemesKey = "LSApplicationQueriesSchemes";
        private const string CFBundleURLTypesKey = "CFBundleURLTypes";
        private const string CFBundleURLSchemesKey = "CFBundleURLSchemes";
        private const string CFBundleURLName = "CFBundleURLName";
        private const string FacebookCFBundleURLName = "facebook-unity-sdk";
        private const string FacebookAppIDKey = "FacebookAppID";
        private const string FacebookAppIDPrefix = "fb";
        private const string FacebookClientTokenKey = "FacebookClientToken";
        private const string AutoLogAppEventsEnabled = "FacebookAutoLogAppEventsEnabled";
        private const string AutoLogProductImprovementEnabled = "FBSDKAutoLogProductImprovementEnabled";

        // Carries the `FBSDK` prefix, not the `Facebook` one the keys around it use. That is how
        // the native SDK declares it, so matching the local pattern here would write a key nothing
        // reads.
        private const string AutoLogMetaDataEnabled = "FBSDKAutoLogMetaDataEnabled";
        private const string AdvertiserIDCollectionEnabled = "FacebookAdvertiserIDCollectionEnabled";

#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
        [NoAutoStaticsCleanup]
#endif
        private static readonly IList<object> FacebookLSApplicationQueriesSchemes = new List<object>()
        {
            "fbapi",
            "fb-messenger-api",
            "fbauth2",
            "fbshareextension"
        };

#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
        [NoAutoStaticsCleanup]
#endif
        private static readonly PListDict FacebookUrlSchemes = new PListDict()
        {
            { PListParser.CFBundleURLName, PListParser.FacebookCFBundleURLName },
        };

        private string filePath;

        public PListParser(string fullPath)
        {
            this.filePath = fullPath;
            XmlReaderSettings settings = new XmlReaderSettings();
#if UNITY_7000_0_0 || UNITY_7000_0_OR_NEWER
            settings.DtdProcessing = DtdProcessing.Parse;
#else
            settings.ProhibitDtd = false;
#endif
            XmlReader plistReader = XmlReader.Create(this.filePath, settings);

            XDocument doc = XDocument.Load(plistReader);
            XElement plist = doc.Element("plist");
            XElement dict = plist.Element("dict");
            this.XMLDict = new PListDict(dict);
            plistReader.Close();
        }

        public PListDict XMLDict { get; set; }

        public void UpdateFBSettings(string appID, string clientToken, string urlSuffix, ICollection<string> appLinkSchemes)
        {
            // Set the facbook app ID
            this.XMLDict[PListParser.FacebookAppIDKey] = appID;

            // Set the client token
            this.XMLDict[PListParser.FacebookClientTokenKey] = clientToken;

            // Set the Auto Log AppEvents Enabled
            this.XMLDict[PListParser.AutoLogAppEventsEnabled] = FacebookSettings.AutoLogAppEventsEnabled;
            this.XMLDict[PListParser.AutoLogProductImprovementEnabled] = FacebookSettings.AutoLogProductImprovementEnabled;

            // Set the Auto Log App Metadata Enabled
            this.XMLDict[PListParser.AutoLogMetaDataEnabled] = FacebookSettings.AutoLogMetaDataEnabled;

            // Set the AdvertiserID Collection Enabled
            this.XMLDict[PListParser.AdvertiserIDCollectionEnabled] = FacebookSettings.AdvertiserIDCollectionEnabled;

            // Set the requried schemas for this app
            SetCFBundleURLSchemes(this.XMLDict, appID, urlSuffix, appLinkSchemes);

            // iOS 9+ Support
            WhitelistFacebookApps(this.XMLDict);
        }

        public void WriteToFile()
        {
            // Corrected header of the plist
            string publicId = "-//Apple//DTD PLIST 1.0//EN";
            string stringId = "http://www.apple.com/DTDs/PropertyList-1.0.dtd";
            string internalSubset = null;
            XDeclaration declaration = new XDeclaration("1.0", Encoding.UTF8.EncodingName, null);
            XDocumentType docType = new XDocumentType("plist", publicId, stringId, internalSubset);

            this.XMLDict.Save(this.filePath, declaration, docType);
        }

        private static void WhitelistFacebookApps(PListDict plistDict)
        {
            if (!ContainsKeyWithValueType(plistDict, PListParser.LSApplicationQueriesSchemesKey, typeof(IList<object>)))
            {
                // We don't have a LSApplicationQueriesSchemes entry. We can easily add one
                plistDict[PListParser.LSApplicationQueriesSchemesKey] = PListParser.FacebookLSApplicationQueriesSchemes;
                return;
            }

            var applicationQueriesSchemes = (IList<object>)plistDict[PListParser.LSApplicationQueriesSchemesKey];
            foreach (var scheme in PListParser.FacebookLSApplicationQueriesSchemes)
            {
                if (!applicationQueriesSchemes.Contains(scheme))
                {
                    applicationQueriesSchemes.Add(scheme);
                }
            }
        }

        private static void SetCFBundleURLSchemes(
            PListDict plistDict,
            string appID,
            string urlSuffix,
            ICollection<string> appLinkSchemes)
        {
            IList<object> currentSchemas;
            if (ContainsKeyWithValueType(plistDict, PListParser.CFBundleURLTypesKey, typeof(IList<object>)))
            {
                currentSchemas = (IList<object>)plistDict[PListParser.CFBundleURLTypesKey];
            }
            else
            {
                // Didn't find any CFBundleURLTypes, let's create one
                currentSchemas = new List<object>();
                plistDict[PListParser.CFBundleURLTypesKey] = currentSchemas;
            }

            PListDict facebookBundleUrlSchemes = PListParser.GetFacebookUrlSchemes(currentSchemas);

            // Clear and set the CFBundleURLSchemes for the facebook schemes
            var facebookUrlSchemes = new List<object>();
            facebookBundleUrlSchemes[PListParser.CFBundleURLSchemesKey] = facebookUrlSchemes;
            AddAppID(facebookUrlSchemes, appID, urlSuffix);
            AddAppLinkSchemes(facebookUrlSchemes, appLinkSchemes);
        }

        private static PListDict GetFacebookUrlSchemes(ICollection<object> plistSchemes)
        {
            foreach (var plistScheme in plistSchemes)
            {
                var bundleTypeNode = plistScheme as PListDict;
                if (bundleTypeNode != null)
                {
                    // Check to see if the url scheme name is facebook
                    string bundleURLName;
                    if (bundleTypeNode.TryGetValue<string>(PListParser.CFBundleURLName, out bundleURLName) &&
                        bundleURLName == PListParser.FacebookCFBundleURLName)
                    {
                        return bundleTypeNode;
                    }
                }
            }

            // We didn't find a facebook scheme so lets create one
            PListDict facebookUrlSchemes = new PListDict(PListParser.FacebookUrlSchemes);
            plistSchemes.Add(facebookUrlSchemes);
            return facebookUrlSchemes;
        }

        private static void AddAppID(ICollection<object> schemesCollection, string appID, string urlSuffix)
        {
            string modifiedID = PListParser.FacebookAppIDPrefix + appID;
            if (!string.IsNullOrEmpty(urlSuffix))
            {
                modifiedID += urlSuffix;
            }

            schemesCollection.Add((object)modifiedID);
        }

        private static void AddAppLinkSchemes(ICollection<object> schemesCollection, ICollection<string> appLinkSchemes)
        {
            foreach (var appLinkScheme in appLinkSchemes)
            {
                schemesCollection.Add(appLinkScheme);
            }
        }

        private static bool ContainsKeyWithValueType(IDictionary<string, object> dictionary, string key, Type type)
        {
            if (dictionary.ContainsKey(key) &&
                type.IsAssignableFrom(dictionary[key].GetType()))
            {
                return true;
            }

            return false;
        }
    }
}
