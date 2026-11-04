using System;
using System.IO;
using System.Linq;
using System.Text;
using Unity.ProjectAuditor.Editor;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

public static class FacebookSdkUnity7ProjectAuditorCI
{
    public static void AuditAndExit()
    {
        var edmPackages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
            .Where(package => package.name == "com.unity.external-dependency-manager")
            .ToArray();
        if (edmPackages.Length != 1 || edmPackages[0].version != "2.1.1")
        {
            var observed = edmPackages.Length == 0
                ? "<none>"
                : string.Join(",", edmPackages.Select(package => package.name + "@" + package.version).ToArray());
            Debug.LogError(
                $"FACEBOOK_SDK_EDM_CHECK_FAIL expected=com.unity.external-dependency-manager@2.1.1 count={edmPackages.Length} observed={observed}");
            EditorApplication.Exit(1);
            return;
        }

        var edm = edmPackages[0];
        Debug.Log($"FACEBOOK_SDK_EDM_CHECK_PASS package={edm.name}@{edm.version}");
        var report = new ProjectAuditor().Audit(new AnalysisParams
        {
            Platform = BuildTarget.StandaloneLinux64,
            CodeOptimization = CodeOptimization.Debug,
        });

        ReportItem[] coreClrIssues;
        try
        {
            coreClrIssues = report.GetAllIssues()
                .Where(issue => IsSdkSourcePath(issue.RelativePath))
                .Where(issue => GetAreas(issue).HasFlag(Areas.MigrationToCoreCLR))
                .OrderBy(issue => issue.RelativePath)
                .ThenBy(issue => issue.Line)
                .ToArray();
        }
        catch (Exception exception) when (exception is InvalidOperationException || exception is NullReferenceException)
        {
            Debug.LogError($"FACEBOOK_SDK_UNITY7_AUDIT_FAIL descriptor_resolution_error={exception}");
            EditorApplication.Exit(1);
            return;
        }

        var outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../facebook-sdk-unity7-coreclr.tsv"));
        WriteSummary(outputPath, coreClrIssues);
        if (coreClrIssues.Length != 0)
        {
            Debug.LogError($"FACEBOOK_SDK_UNITY7_AUDIT_FAIL coreclr_issues={coreClrIssues.Length} report={outputPath}");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"FACEBOOK_SDK_UNITY7_AUDIT_PASS coreclr_issues=0 report={outputPath}");
        EditorApplication.Exit(0);
    }

    private static bool IsSdkSourcePath(string relativePath)
    {
        relativePath = relativePath ?? string.Empty;
        return relativePath.StartsWith("Assets/FacebookSDKSource/", StringComparison.Ordinal)
            || relativePath.StartsWith("Assets/Editor/FacebookSDKEditorSource/", StringComparison.Ordinal);
    }

    private static Areas GetAreas(ReportItem issue)
    {
        try
        {
            return issue.Id.GetDescriptor().Areas;
        }
        catch (Exception exception) when (exception is InvalidOperationException || exception is NullReferenceException)
        {
            Debug.LogError(
                $"FACEBOOK_SDK_UNITY7_DESCRIPTOR_FAIL path={issue.RelativePath} line={issue.Line} severity={issue.Severity} description={issue.Description}");
            throw;
        }
    }

    private static void WriteSummary(string path, ReportItem[] issues)
    {
        var summary = new StringBuilder();
        summary.AppendLine("descriptor\tseverity\tareas\tpath\tline\tdescription");
        foreach (var issue in issues)
        {
            summary.Append(GetId(issue)).Append('\t')
                .Append(issue.Severity).Append('\t')
                .Append(GetAreas(issue)).Append('\t')
                .Append(issue.RelativePath).Append('\t')
                .Append(issue.Line).Append('\t')
                .Append((issue.Description ?? string.Empty).Replace("\t", " ").Replace("\n", " "))
                .AppendLine();
        }

        File.WriteAllText(path, summary.ToString());
    }

    private static string GetId(ReportItem issue)
    {
        return issue.Id.AsString() ?? string.Empty;
    }
}
