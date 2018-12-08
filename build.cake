//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=GitVersion.CommandLine&version=4.0.0-beta0011"
#tool "nuget:?package=ILRepack&version=2.0.13"
#addin "nuget:?package=SharpCompress&version=0.12.4"
#addin "nuget:?package=Cake.Npm&version=0.14.0"
#addin nuget:?package=Feedz.Client
#addin nuget:?package=Octodiff

using SharpCompress;
using SharpCompress.Common;
using SharpCompress.Writer;

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var artifactsDir = "./artifacts/";
var publishDir = "./publish/";
GitVersion gitVersionInfo;
string nugetVersion;

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup(context =>
{
    gitVersionInfo = GitVersion(new GitVersionSettings {
        OutputType = GitVersionOutput.Json
    });

  
    nugetVersion = gitVersionInfo.NuGetVersion;

    if(BuildSystem.IsRunningOnAppVeyor)
        BuildSystem.AppVeyor.UpdateBuildVersion(nugetVersion);

    Information("Building Feedz.Console v{0}", nugetVersion);
    Information("Informational Version {0}", gitVersionInfo.InformationalVersion);
});

Teardown(context =>
{
    Information("Finished running tasks.");
});

//////////////////////////////////////////////////////////////////////
//  PRIVATE TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() => {
		CleanDirectory(artifactsDir);
		CleanDirectory("./artifacts");
		CleanDirectory("./publish");
		CleanDirectories("./src/**/bin");
		CleanDirectories("./src/**/obj");
	});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() => {
		DotNetCoreRestore("./src");
    });

Task("Build")
    .IsDependentOn("Restore")
    .IsDependentOn("Clean")
    .Does(() => {
		DotNetCoreBuild("./src", new DotNetCoreBuildSettings
		{
			Configuration = configuration,
			ArgumentCustomization = args => args.Append($"/p:Version={nugetVersion}")
		});
	});

Task("Publish")
    .IsDependentOn("Build")
    .Does(() => {
    
        DotNetCorePublish("./src/Console", new DotNetCorePublishSettings
        {
            Configuration = configuration,
            NoBuild = true,
            OutputDirectory = $"{publishDir}\\netfx",
            Framework = "net461",
            ArgumentCustomization = args => args.Append($"/p:Version={nugetVersion}")
        });
        
        DotNetCorePublish("./src/Console", new DotNetCorePublishSettings
        {
            Configuration = configuration,
            OutputDirectory = $"{publishDir}\\linux",
            Framework = "netcoreapp2.1",
            Runtime = "linux-x64",
            ArgumentCustomization = args => args.Append($"/p:Version={nugetVersion}")
        });
    });
    
Task("MergeExe")
    .IsDependentOn("Publish")
    .Does(() => {
        var inputFolder = $"{publishDir}/netfx";
        var outputFolder = $"{publishDir}/netfx-merged";
        CreateDirectory(outputFolder);
        
        ILRepack(
            $"{outputFolder}/Feedz.exe",
            $"{inputFolder}/Feedz.exe",
            System.IO.Directory.EnumerateFiles(inputFolder, "*.dll").Select(f => (FilePath) f),
            new ILRepackSettings {
                Internalize = true,
                Parallel = true,
                Libs = new List<DirectoryPath>() { inputFolder }
            }
        );
    });

    
Task("Zip")
    .IsDependentOn("MergeExe")
    .Does(() => {

        Zip(System.IO.Path.GetFullPath($"{publishDir}/netfx-merged"), $"{artifactsDir}/Feedz.Console.{nugetVersion}.zip");
        TarGzip($"{publishDir}/linux",  $"{artifactsDir}/Feedz.Console.linux.{nugetVersion}");
    });

Task("Push")
    .IsDependentOn("Zip")
   // .WithCriteria(BuildSystem.IsRunningOnAppVeyor)
    .Does(async () => {
        var repo = Feedz.Client.FeedzClient.Create(EnvironmentVariable("FeedzApiKey"))
            .ScopeToRepository("feedz-io", "public");

        await repo.Packages.Upload($"{artifactsDir}/Feedz.Console.{nugetVersion}.zip");
        await repo.Packages.Upload($"{artifactsDir}/Feedz.Console.linux.{nugetVersion}.tar.gz");
     });

Task("Default")
        .IsDependentOn("Push");
        
        
private void TarGzip(string path, string outputFile)
{
    var outFile = $"{outputFile}.tar.gz";
    Information("Creating TGZ file {0} from {1}", outFile, path);
    using (var tarMemStream = new MemoryStream())
    {
        using (var tar = WriterFactory.Open(tarMemStream, ArchiveType.Tar, CompressionType.None, true))
        {
            tar.WriteAll(path, "*", SearchOption.AllDirectories);
        }

        tarMemStream.Seek(0, SeekOrigin.Begin);

        using (Stream stream = System.IO.File.Open(outFile, FileMode.Create))
        using (var zip = WriterFactory.Open(stream, ArchiveType.GZip, CompressionType.GZip))
            zip.Write($"{outputFile}.tar", tarMemStream);
    }
    Information("Successfully created TGZ file: {0}", outFile);
}


//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);
