 // include Fake lib
#r @"../../FAKE/tools/FakeLib.dll"
#load @"../../../build/config.fsx"

open Fake
open Fake.Git
open System

let root = FileSystemHelper.currentDirectory +  @"/"
let buildMode = getBuildParamOrDefault "buildMode" "Release"

let outDir = root + "out/"

let settings = Config.settings
let buildSettings = settings.BuildSettings


let solutions = 
    buildSettings.solutions
    |> List.map (fun x -> root + x)


Target "Clean" (fun _ ->
    CleanDirs [outDir]
)

// Default target
Target "Build" (fun _ ->
    
    let setParams x = 
        {x with 
            Verbosity = Some(Quiet);
            Targets = ["Clean,Build"];
            Properties = 
                [
                    "Configuration", buildMode
                    "DebugSymbols", "True"
                    "Optimize", buildSettings.optimize.ToString()
                ]
        }
    
    solutions
    |> List.iter (fun sln ->
        trace <| sprintf "Building %s..." sln   
        sln
        |> build setParams
        |> ignore
    )
    
)

Target "NUnit" (fun _ ->
    let testOutDir = outDir + @"TestResults"
    FileSystemHelper.ensureDirectory testOutDir

    settings.TestSettings.nunitTests
    |> List.map (fun pattern -> root + pattern.Replace("@buildMode@", buildMode))
    |> List.iter(fun pattern ->
        trace <| sprintf "%s" pattern
        !!pattern
            |> NUnit (fun p ->
                trace p.WorkingDir
                {p with
                    DisableShadowCopy = true
                    WorkingDir = root
                    OutputFile = testOutDir + @"/NUNitOutput.xml"
                }
        )
    )
)

Target "Package" (fun _ ->
    let version = describe root
    trace version
//    let version = 
//        match settings.PackageConvention.Type with
//        | QuickSilver.Settings.PackageConventionType.GitTag ->
            

//    let setParams proj x = 
//        let projFile = (fileInfo proj)
//        let fileName = projFile.Name
//        let projName = 
//            fileName.Substring(0, fileName.Length - projFile.Extension.Length)
//        let outProjDir = outDir + projName + @"/"
//        {x with 
//            Verbosity = Some(Quiet);
//            Targets = ["Package"];
//            Properties = 
//                [
//                    "Configuration", buildMode
//                    "DebugSymbols", "True"
//                    "Optimize", buildSettings.optimize.ToString()
//                    "PackageLocation", outProjDir + projName + ".zip"
//                    "DeployIisAppPath", projName
//                    "DesktopBuildPackageLocation", projName
//                ]
//        }
//
//    
//    settings.WebsitePackages.projFiles
//    |> List.iter (fun pattern ->
//        !!pattern
//        |> Seq.iter (fun proj ->
//            proj
//            |> build (setParams proj)
//            |> ignore 
//        )
//    )
)

"Clean"
    ==> "Build"
    ==> "NUnit"
    ==> "Package"

// start build
RunTargetOrDefault "NUnit"