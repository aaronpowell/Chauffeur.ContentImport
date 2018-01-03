#I @"tools/FAKE.Core/tools/"
#r @"FakeLib.dll"

open Fake
open Fake.AssemblyInfoFile

let authors = ["Aaron Powell"]

let chauffeurContentImporterDir = "./Chauffeur.ContentImport/bin/"
let packagingRoot = "./packaging/"
let packagingDir = packagingRoot @@ "chauffeur.ContentImport"

let buildMode = getBuildParamOrDefault "buildMode" "Release"

let isAppVeyorBuild = not (isNull (environVar "APPVEYOR"))

let projectName = "Chauffeur.ContentImport"
let summary = "Chauffeur.ContentImport is a plugin for Chauffeur that uses the Umbraco Packaging API to import and publish content."
let description = summary

let releaseNotes =
    ReadFile "ReleaseNotes.md"
        |> ReleaseNotesHelper.parseReleaseNotes

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

Target "Default" DoNothing

Target "AssemblyInfo" (fun _ ->
    CreateCSharpAssemblyInfo "SolutionInfo.cs"
      [ Attribute.Product projectName
        Attribute.Version releaseNotes.AssemblyVersion
        Attribute.FileVersion releaseNotes.AssemblyVersion
        Attribute.ComVisible false ]
)

Target "Clean" (fun _ ->
    CleanDirs [chauffeurContentImporterDir]
)

Target "RestorePackages" (fun _ ->
    RestorePackage (id) "./Chauffeur.ContentImport/packages.config"
)

Target "Build" (fun _ ->
    MSBuild null "Build" ["Configuration", buildMode] ["Chauffeur.ContentImport.sln"]
    |> Log "AppBuild-Output: "
)

Target "UnitTests" (fun _ ->
    !! (sprintf "./Chauffeur.ContentImport.Tests/bin/%s/**/Chauffeur.ContentImport.Tests*.dll" buildMode)
    |> NUnitParallel (fun p ->
            {p with
                DisableShadowCopy = true;
                OutputFile = (sprintf "./Chauffeur.ContentImport.Tests/bin/%s/TestResults.xml" buildMode) })
)

Target "Package" (fun _ ->
    let libDir = packagingDir @@ "lib/net45/"
    CleanDirs [libDir]

    CopyFile libDir (chauffeurContentImporterDir @@ "Release/Chauffeur.ContentImport.dll")
    CopyFiles packagingDir ["LICENSE.md"; "README.md"]


    NuGet (fun p ->
        {p with
            Authors = authors
            Project = projectName
            Description = description
            OutputPath = packagingRoot
            Summary = summary
            WorkingDir = packagingDir
            Version = nugetVersion
            ReleaseNotes = toLines releaseNotes.Notes
            SymbolPackage = NugetSymbolPackage.Nuspec
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey" }) "Chauffeur.ContentImport/Chauffeur.ContentImport.nuspec"
)

Target "BuildVersion" (fun _ ->
    Shell.Exec("appveyor", sprintf "UpdateBuild -Version \"%s\"" nugetVersion) |> ignore
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

RunTargetOrDefault "Default"