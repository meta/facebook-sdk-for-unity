using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

internal static class LegacyBclSmoke
{
    private static int Main()
    {
        var settings = new XmlReaderSettings();
#pragma warning disable 0618
        settings.ProhibitDtd = false;
#pragma warning restore 0618
        using (var reader = XmlReader.Create(
            new StringReader("<!DOCTYPE root [<!ENTITY value 'parsed'>]><root>&value;</root>"),
            settings))
        {
            reader.MoveToContent();
            if (reader.ReadElementContentAsString() != "parsed")
            {
                return 1;
            }
        }

        var getAssemblies = typeof(AppDomain).GetMethod(
            "GetAssemblies",
            BindingFlags.Public | BindingFlags.Instance);
        if (getAssemblies == null)
        {
            return 2;
        }

        var assemblies = getAssemblies.Invoke(AppDomain.CurrentDomain, null) as IEnumerable<Assembly>;
        if (assemblies == null)
        {
            return 3;
        }

        foreach (var assembly in assemblies)
        {
            var location = assembly.Location;
            if (location != null)
            {
                Console.WriteLine(
                    "FACEBOOK_LEGACY_BCL_SMOKE_PASS reference_profile=.NET-2.0 runtime="
                    + Environment.Version);
                return 0;
            }
        }

        return 4;
    }
}
