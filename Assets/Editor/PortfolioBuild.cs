using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class PortfolioBuild
{
    public static void BuildWindows()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new InvalidOperationException("No enabled scenes were found in EditorBuildSettings.");

        string outputDirectory = Path.GetFullPath("Builds/Windows-Portfolio");
        Directory.CreateDirectory(outputDirectory);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(outputDirectory, "PushAndHack.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        Console.WriteLine($"Portfolio build result: {summary.result}");
        Console.WriteLine($"Enabled scene count: {scenes.Length}");
        Console.WriteLine($"Build size: {summary.totalSize}");

        if (summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"Windows build failed: {summary.result}");
    }
}
