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

    let restoreNuget sln = 
        if buildSettings.restorePackagesPriorToBuild then
            trace <| "restoring packages prior to build..."
            sln
            |> RestoreMSSolutionPackages(fun p ->
                { p with
                    Retries = 3
                    Sources = buildSettings.additionalPackageSources @ p.Sources
                    OutputPath = System.IO.Path.Combine(directory sln, "packages")
                }
            )
        else
            trace <| "pre-build package restore not requested. skipping..."
        sln
    
    solutions
    |> List.iter (fun sln ->
        trace <| sprintf "Building %s..." sln   
        sln
        |> restoreNuget
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
        trace <| sprintf "Commit not tagged with v* tag %A. packaging." v

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
            trace <| sprintf "copying tokenization files from %s" target
            if(FileSystemHelper.directoryExists(target)) then
                CopyDir(outProjDir) target (fun _->true)
                
        let copyInstallerScript (projName, outProjDir) = 
            trace <| sprintf "copying installer script" 
            FileUtils.cp (qsDir + "website/boot/install_website.bat") (outProjDir + "install.bat")

        settings.WebsitePackages.projFiles
        |> List.iter (fun pattern ->
            !!pattern
            |> Seq.iter (fun proj ->
                let targetDetails = getTargetDetails proj v
                
                proj
                |> build (setParams targetDetails)
                |> ignore 

                copyMSDeployEnvs targetDetails
                copyInstallerScript targetDetails
            )
        )

        settings.TopShelfServicePackages
        |> List.iter (fun tss ->
            let pattern =  tss.binaryPath.Replace("@buildMode@", buildMode)
            let binaryPath = !!pattern |> Seq.head
            let outProjDir = outDir + tss.name + @"/" + version.Value + @"/"
            CopyDir (outProjDir + @"binaries/") (binaryPath + @"/") (fun _ -> true)
            CopyDir (outProjDir + @"tools/config-transform") (root + @".quicksilver/config-transform/tools/") (fun _ -> true)
            CopyDir (outProjDir + @"scripts/") (root + @".quicksilver/quicksilver/tools/topshelf/scripts/") (fun _ -> true)
            FileUtils.cp (qsDir + "topshelf/boot/install_topshelf.bat") (outProjDir + "install.bat")
            
            let envDirForProject = (root + @"/env/" + tss.name + @"/")

            if (directoryExists envDirForProject) then 
                CopyDir (outProjDir + @"env/") envDirForProject (fun _ -> true)
            else
                failwith "Top shelf services must have servicekey.deploy.envname.pson in env directory. Please look at sample templates in .\quicksilver\quicksilver\tools\topshelf\templates\ for formats."


            let serviceInfo = 
                "@{" + Environment.NewLine +
                    "    serviceKey='" + tss.name + "';" + Environment.NewLine +
                    "    version='" + v + "';" + Environment.NewLine +
                    "    packageUtcTime='" + DateTime.UtcNow.ToString() + "'" + Environment.NewLine +
                    "}" + Environment.NewLine

            WriteStringToFile false (outProjDir + "serviceInfo.pson") serviceInfo
          )
        
)

Target "Publish" (fun _ ->
    if(version.IsSome) then 
        settings.WebsitePackages.projFiles
        |> List.iter (fun pattern ->
            !!pattern
            |> Seq.iter (fun proj ->
                let (projName, outProjDir) = getTargetDetails proj version.Value

                let targetDir = settings.PublishSettings.WebsitesRoot + projName + @"/" 
                ensureDirectory targetDir
                //outProjDir = outDir + projName + @"/" + v + @"/"

                trace outProjDir
                trace targetDir
                !! (outProjDir + "**/*.*")
                |> Zip outProjDir (targetDir + version.Value + ".zip") 
            )
        )

        settings.TopShelfServicePackages
        |> List.iter (fun tss ->
            let source = outDir + tss.name + @"/" + version.Value + @"/"
            let targetDir = settings.PublishSettings.TopShelfServicesRoot + tss.name + @"/"
            trace source
            trace targetDir
            
            ensureDirectory targetDir

            !!(source + "**/*.*")
            |> Zip source (targetDir + version.Value + ".zip")
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