 // include Fake lib
#r @"../../FAKE/tools/FakeLib.dll"
#load @"../../../build/config.fsx"

open Fake
open Fake.Git
open System

let root = FileSystemHelper.currentDirectory +  @"/"
let buildMode = getBuildParamOrDefault "buildMode" "Release"
let qsDir = root + @".quicksilver/quicksilver/tools/"

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

//only GitTag for now.
let version = 
    try
        Some(runSimpleGitCommand root "describe --abbrev=0 --tags --exact-match --match v*")
    with
        | _ -> None

let getTargetDetails proj v = 
    let projFile = (fileInfo proj)
    let fileName = projFile.Name
    let projName = 
        fileName.Substring(0, fileName.Length - projFile.Extension.Length)
    let outProjDir = outDir + projName + @"/" + v + @"/"
    (projName, outProjDir)

Target "Package" (fun _ ->
    if version.IsNone then
        trace "Commit not tagged with v* tag. Not packaging."
    else
        let v = version.Value

        let setParams (projName, outProjDir) x = 
            {x with 
                Verbosity = Some(Quiet);
                Targets = ["Package"];
                Properties = 
                    [
                        "Configuration", buildMode
                        "DebugSymbols", "True"
                        "Optimize", buildSettings.optimize.ToString()
                        "PackageLocation", outProjDir + projName + ".zip"
                        "DeployIisAppPath", projName
                    ]
            }

        let copyMSDeployEnvs (projName, outProjDir) = 
            let target = root + @"env/" + projName
            trace target
            if(FileSystemHelper.directoryExists(target)) then
                CopyDir(outProjDir) target (fun _->true)
                FileUtils.cp (qsDir + "install_website.bat") (outProjDir + "install.bat")
    
        settings.WebsitePackages.projFiles
        |> List.iter (fun pattern ->
            !!pattern
            |> Seq.iter (fun proj ->
                let targetDetails = getTargetDetails proj v
                
                proj
                |> build (setParams targetDetails)
                |> ignore 

                copyMSDeployEnvs targetDetails
            )
        )
)

Target "Publish" (fun _ ->
    if(version.IsSome) then 
        settings.WebsitePackages.projFiles
        |> List.iter (fun pattern ->
            !!pattern
            |> Seq.iter (fun proj ->
                let (projName, outProjDir) = getTargetDetails proj version.Value

                if settings.PublishSettings.WebsitesRoot.IsNone then
                    failwithf "Attempting to publish website msdeploy package, but settings.PublishSettings.WebsitesRoot is not set. Please use websitePublishRoot in config.fsx."
                
                let targetDir = settings.PublishSettings.WebsitesRoot.Value + projName + @"/" 
                ensureDirectory targetDir
                //outProjDir = outDir + projName + @"/" + v + @"/"
                !! (outProjDir + "**/*.*")
                |> Zip outProjDir (targetDir + version.Value + ".zip") 
            )
        )
    else
        ()
)

"Clean"
    ==> "Build"
    ==> "NUnit"
    ==> "Package"
    ==> "Publish"

// start build
RunTargetOrDefault "Publish"