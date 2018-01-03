#I @"tools/FAKE.Core/tools/"
#r @"FakeLib.dll"

open Fake.Core
open Fake.Core.Environment
open Fake.Core.TargetOperators
open Fake.DotNet.AssemblyInfoFile
open Fake.DotNet.MsBuild
open Fake.DotNet.NuGet.Restore
open Fake.DotNet.NuGet.NuGet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.Tools


let authors = ["Aaron Powell"]

let chauffeurContentImporterDir = "./Chauffeur.ContentImport/bin/"
let packagingRoot = "./.packaging/"
let packagingDir = packagingRoot @@ "chauffeur.ContentImport"

let buildMode = environVarOrDefault "buildMode" "Release"

let isAppVeyorBuild = not (isNull (environVar "APPVEYOR"))

let projectName = "Chauffeur.ContentImport"
let summary = "Chauffeur.ContentImport is a plugin for Chauffeur that uses the Umbraco Packaging API to import and publish content."
let description = summary

let releaseNotes =
    File.read "ReleaseNotes.md"
        |> Fake.ReleaseNotesHelper.parseReleaseNotes

let trimBranchName (branch: string) =
    let trimmed = match branch.Length > 10 with
                    | true -> branch.Substring(0, 10)
                    | _ -> branch

    trimmed.Replace(".", "")

let prv = match environVar "APPVEYOR_REPO_BRANCH" with
            | null -> ""
            | "master" -> ""
            | branch -> sprintf "-%s%s" (trimBranchName branch) (
                            match environVar "APPVEYOR_BUILD_NUMBER" with
                            | null -> ""
                            | _ -> sprintf "-%s" (environVar "APPVEYOR_BUILD_NUMBER")
                            )
let nugetVersion = sprintf "%d.%d.%d%s" releaseNotes.SemVer.Major releaseNotes.SemVer.Minor releaseNotes.SemVer.Patch prv

Target.Create "Default" Target.DoNothing

Target.Create "AssemblyInfo" (fun _ ->
    let commitHash = Git.Information.getCurrentHash()

    let attributes =
        [ Fake.DotNet.AssemblyInfo.Product projectName
          Fake.DotNet.AssemblyInfo.Title "Chauffeur Content Import tools"
          Fake.DotNet.AssemblyInfo.Version releaseNotes.AssemblyVersion
          Fake.DotNet.AssemblyInfo.FileVersion releaseNotes.AssemblyVersion
          Fake.DotNet.AssemblyInfo.InformationalVersion releaseNotes.AssemblyVersion
          Fake.DotNet.AssemblyInfo.ComVisible false
          Fake.DotNet.AssemblyInfo.Metadata("githash", commitHash) ]

    CreateCSharp "AssemblyInfo.cs" attributes
)

Target.Create "Clean" (fun _ ->
    Shell.CleanDirs [chauffeurContentImporterDir]
)

Target.Create "RestorePackages" (fun _ ->
    RestorePackage id "./Chauffeur.ContentImport/packages.config"
)

Target.Create "Build" (fun _ ->
    let setParams (defaults: MSBuildParams) =
        let p = { defaults with
                    Verbosity = Some(Quiet)
                    Targets = ["Build"]
                    Properties =
                    [
                        "Configuration", buildMode
                        "Optimize", "True"
                        "DebugSymbols", "True"
                    ] }
        if isAppVeyorBuild then p
        else { p with ToolPath = "C:\Program Files (x86)\Microsoft Visual Studio\Preview\Enterprise\MSBuild\15.0\Bin\msbuild.exe" }

    build setParams "./Chauffeur.ContentImport.sln"
)

// Target.Create "UnitTests" (fun _ ->
//     !! (sprintf "./Chauffeur.ContentImport.Tests/bin/%s/**/Chauffeur.ContentImport.Tests*.dll" buildMode)
//     |> NUnitParallel (fun p ->
//             {p with
//                 DisableShadowCopy = true;
//                 OutputFile = (sprintf "./Chauffeur.ContentImport.Tests/bin/%s/TestResults.xml" buildMode) })
// )

Target.Create "Package" (fun _ ->
    let libDir = packagingDir @@ "lib/net45/"
    Shell.CleanDirs [libDir]

    Shell.CopyFile libDir (chauffeurContentImporterDir @@ "Release/Chauffeur.ContentImport.dll")
    Shell.CopyFiles packagingDir ["LICENSE.md"; "README.md"]


    NuGet (fun p ->
        {p with
            Authors = authors
            Project = projectName
            Description = description
            OutputPath = packagingRoot
            Summary = summary
            WorkingDir = packagingDir
            Version = nugetVersion
            ReleaseNotes = String.toLines releaseNotes.Notes
            SymbolPackage = NugetSymbolPackage.Nuspec
            AccessKey = environVarOrDefault "nugetkey" ""
            Publish = hasEnvironVar "nugetkey" }) "Chauffeur.ContentImport/Chauffeur.ContentImport.nuspec"
)

Target.Create "BuildVersion" (fun _ ->
    Process.Shell.Exec("appveyor", sprintf "UpdateBuild -Version \"%s\"" nugetVersion) |> ignore
)

"Clean"
    =?> ("BuildVersion", isAppVeyorBuild)
    ==> "RestorePackages"
    ==> "Build"
    ==> "Default"

//"Build"
    // ==> "UnitTests"

"Build"
    ==> "Package"

Target.RunOrDefault "Default"