using System;
using System.Collections.Generic;

/// <summary>
/// Represents the build configuration and targets for the project.
/// </summary>
class Build : NukeBuild
{
    /// <summary>
    /// Entry point for the build script.
    /// </summary>
    /// <returns>The exit code of the build process.</returns>
    public static int Main() => Execute<Build>(x => x.Compile);

    /// <summary>
    /// The solution file to be built.
    /// </summary>
    [Solution]
    readonly Solution Solution;

    /// <summary>
    /// The Git repository associated with the build.
    /// </summary>
    [GitRepository]
    readonly GitRepository GitRepository;

    /// <summary>
    /// The build configuration (e.g., Debug or Release).
    /// </summary>
    [Parameter("configuration")]
    public string Configuration { get; set; }

    /// <summary>
    /// The version suffix to be applied to the build.
    /// </summary>
    [Parameter("version-suffix")]
    public string VersionSuffix { get; set; }

    /// <summary>
    /// The target framework for publishing.
    /// </summary>
    [Parameter("publish-framework")]
    public string PublishFramework { get; set; }

    /// <summary>
    /// The target runtime for publishing.
    /// </summary>
    [Parameter("publish-runtime")]
    public string PublishRuntime { get; set; }

    /// <summary>
    /// The project to be published.
    /// </summary>
    [Parameter("publish-project")]
    public string PublishProject { get; set; }

    /// <summary>
    /// Indicates whether the publish should be self-contained.
    /// </summary>
    [Parameter("publish-self-contained")]
    public bool PublishSelfContained { get; set; } = true;

    /// <summary>
    /// The source directory path.
    /// </summary>
    AbsolutePath SourceDirectory => RootDirectory / "src";

    /// <summary>
    /// The tests directory path.
    /// </summary>
    AbsolutePath TestsDirectory => RootDirectory / "tests";

    /// <summary>
    /// The artifacts directory path.
    /// </summary>
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    /// <summary>
    /// Initializes the build configuration.
    /// </summary>
    protected override void OnBuildInitialized()
    {
        Configuration = Configuration ?? "Release";
        VersionSuffix = VersionSuffix ?? "";

        InitializeLinuxBuild();
    }

    /// <summary>
    /// Initializes the build configuration for Linux.
    /// </summary>
    private void InitializeLinuxBuild()
    {
        if (OperatingSystem.IsLinux())
        {
            var iosProjects = Solution.GetProjects("*.iOS");
            foreach (var project in iosProjects)
            {
                Console.WriteLine($"Removed project: {project.Name}");
                Solution.RemoveProject(project);
            }
            Solution.Save();
        }
    }

    /// <summary>
    /// Deletes the specified directories.
    /// </summary>
    /// <param name="directories">The directories to delete.</param>
    private void DeleteDirectories(IReadOnlyCollection<string> directories)
    {
        foreach (var directory in directories)
        {
            DeleteDirectory(directory);
        }
    }

    /// <summary>
    /// Target to clean the build directories.
    /// </summary>
    Target Clean => _ => _
        .Executes(() =>
        {
            DeleteDirectories(GlobDirectories(SourceDirectory, "**/bin", "**/obj"));
            DeleteDirectories(GlobDirectories(TestsDirectory, "**/bin", "**/obj"));
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    /// <summary>
    /// Target to restore the project dependencies.
    /// </summary>
    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    /// <summary>
    /// Target to compile the project.
    /// </summary>
    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetVersionSuffix(VersionSuffix)
                .EnableNoRestore());
        });

    /// <summary>
    /// Target to run the tests.
    /// </summary>
    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetLoggers("trx")
                .SetResultsDirectory(ArtifactsDirectory / "TestResults")
                .EnableNoBuild()
                .EnableNoRestore());
        });

    /// <summary>
    /// Target to pack the project into a NuGet package.
    /// </summary>
    Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetPack(s => s
                .SetProject(Solution)
                .SetConfiguration(Configuration)
                .SetVersionSuffix(VersionSuffix)
                .SetOutputDirectory(ArtifactsDirectory / "NuGet")
                .EnableNoBuild()
                .EnableNoRestore());
        });

    /// <summary>
    /// Target to publish the project.
    /// </summary>
    Target Publish => _ => _
        .DependsOn(Restore)
        .Requires(() => PublishRuntime)
        .Requires(() => PublishFramework)
        .Requires(() => PublishProject)
        .Executes(() =>
        {
            DotNetPublish(s => s
                .SetProject(Solution.GetProject(PublishProject))
                .SetConfiguration(Configuration)
                .SetVersionSuffix(VersionSuffix)
                .SetFramework(PublishFramework)
                .SetRuntime(PublishRuntime)
                .SetSelfContained(PublishSelfContained)
                .SetOutput(ArtifactsDirectory / "Publish" / PublishProject + "-" + PublishFramework + "-" + PublishRuntime));
        });
}
