#r "../../fake/tools/FakeLib.dll"

open Fake
open Fake.Git
open Fake.NuGet
open System

let sep = EnvironmentHelper.directorySeparator
let builder = (getBuildParamOrDefault "builder" "dev").ToLower()
trace <| sprintf "build environment is %s" builder

let rootDir = FileSystemHelper.currentDirectory +  sep
let buildMode = getBuildParamOrDefault "buildMode" "Release"
let qsDir =  __SOURCE_DIRECTORY__ + sep
let outDir = rootDir + "out" + sep
let optimize = getBuildParamOrDefault "optimize" "true"
let mutable publishRoot = rootDir + "deploy" + sep
let mutable ciPublishRoot = rootDir + "deploy" + sep


let private getGitVersion() = 
    try
        Some(runSimpleGitCommand rootDir "describe --abbrev=0 --tags --exact-match --match v*")
    with
        | _ -> None


let private getNugetVersion (version:string) = 
    if(version.StartsWith("v")) then
        version.Substring(1)
    else
        failwith "version must be in the form of v0.0.1 or v0.0.1-alpha. This follows NuGet versioning prefixed with v."

let mutable version = 
    let buildVersion = getBuildParam "version"
    if buildVersion = "" then
        getGitVersion()
    else
        //maybe check that it's a vn.m.o[-foo] tag.
        Some(buildVersion)
        
let restoreNugetPackagesTo pkgDir = 
    !! "./**/packages.config"
    |> Seq.iter (RestorePackage (fun p -> {p with OutputPath=pkgDir})) 

let buildSolutions slns = 
    let setParams x = 
        {x with 
            Verbosity = Some(Quiet);
            Targets = ["Clean,Build"];
            Properties = 
                [
                    "Configuration", buildMode
                    "DebugSymbols", "True"
                    "Optimize", optimize
                ]
        }

    slns 
    |> List.iter (fun sln ->
        trace <| sprintf "Building %s..." sln   
        sln
        |> build setParams
        |> ignore
    )


let testWithNUnit (csprojGlobs:string list) = 
    let testOutDir = outDir + "TestResults" + sep
    FileSystemHelper.ensureDirectory testOutDir

    csprojGlobs
    |> List.map (fun pattern -> rootDir + pattern.Replace("@buildMode@", buildMode))
    |> List.iter(fun pattern ->
        trace <| sprintf "Testing %s" pattern
        !!pattern
            |> NUnit (fun p ->
                {p with
                    DisableShadowCopy = true
                    WorkingDir = rootDir
                    OutputFile = testOutDir + sep + "NUNitOutput.xml"
                }
        )
    )

let private getTargetDetails proj v = 
    let projFile = (fileInfo proj)
    let fileName = projFile.Name
    let projName = 
        fileName.Substring(0, fileName.Length - projFile.Extension.Length)
    let outProjDir = outDir + projName + sep + v + sep
    (projName, outProjDir)


let private getPublishRoot () = 
    match builder with
    | "ci" -> ciPublishRoot
    | _ -> publishRoot

type QuicksilverWebsite = {
    glob: string
    authors : string list
}

let private defaultWebsite = {glob = ""; authors = []}

let packageQuicksilverWebsites (websites:(QuicksilverWebsite -> QuicksilverWebsite) list) = 
    if version.IsNone then
        trace "Commit not tagged with v* tag. Not packaging quicksilver websites."
    else
        let v = version.Value
        trace <| sprintf "Packaging quicksilver websites for version %A." v

        let setParams (projName, outProjDir) x = 
            {x with 
                Verbosity = Some(Quiet);
                Targets = ["Package"];
                Properties = 
                    [
                        "Configuration", buildMode
                        "DebugSymbols", "True"
                        "Optimize", optimize
                        "PackageLocation", outProjDir + projName + ".zip"
                        "DeployIisAppPath", projName
                    ]
            }

        let copyEnvs (projName, outProjDir) = 
            let target = rootDir + @"env" + sep + projName
            trace <| sprintf "copying tokenization files from %s" target
            if(FileSystemHelper.directoryExists(target)) then
                CopyDir(outProjDir) target (fun _->true)
                
        let copyInstallerScript (projName, outProjDir) = 
            trace <| sprintf "copying installer script" 
            let websiteScript = qsDir + "website" + sep + "boot" + sep + "install_website.bat"
            FileUtils.cp websiteScript (outProjDir + "install.bat")
            FileUtils.cp (qsDir + "website" + sep + "boot" + sep + "fake.deploy.fsx") (outProjDir + "fake.deploy.fsx")
            FileUtils.cp (qsDir + "website" + sep + "boot" + sep + "website_setup.ps1") (outProjDir + "website_setup.ps1")

//        let zipPackage (projName, outProjDir) = 
//            let targetDir = getPublishRoot() + projName + @"/" 
//            trace <| sprintf "Publishing website msdeploy package from %A  to %A" outProjDir targetDir
//            ensureDirectory targetDir
//            !! (outProjDir + "**/*.*")
//            |> Zip outProjDir (targetDir + version.Value + ".zip") 

        let nuGetPackage (projName, outProjDir) website = 
            let targetDir = getPublishRoot() + projName + @"/" 
            trace <| sprintf "Publishing website msdeploy package from %A  to %A" outProjDir targetDir
            ensureDirectory targetDir

            let nuspecPath = qsDir + "nuget" + sep + "fake.deploy.nuspec"
            if fileExists nuspecPath = false then
                FileHelper.CopyFile nuspecPath (qsDir + "nuget" + sep + "fake.deploy.nuspectemplate")
            
            NuGet (fun p ->
                {p with
                    Authors = website.authors
                    Files = [outProjDir, None, None]
                    Description = projName
                    Project = projName
                    Version = getNugetVersion v
                    OutputPath = targetDir
                }
                ) nuspecPath

        websites
        |> List.iter (fun definer ->
            let website = definer defaultWebsite
            !!website.glob
            |> Seq.head
            |> fun proj ->
                let targetDetails = getTargetDetails proj v
                
                proj
                |> build (setParams targetDetails)
                |> ignore 
                
                copyEnvs targetDetails
                copyInstallerScript targetDetails
                nuGetPackage targetDetails website
        )

type QuicksilverTopshelfService = {
    name: string
    binaryPath : string
    authors : string list
}

let private defaultTopshelfService = {name=""; binaryPath=""; authors=[]}

let packageQuicksilverTopshelfServices (services:(QuicksilverTopshelfService -> QuicksilverTopshelfService) list) = 
    if version.IsNone then
        trace "Commit not tagged with v* tag. Not packaging quicksilver topshelf services."
    else
        let v = version.Value
        trace <| sprintf "Packaging quicksilver services for version %A." v
        services
        |> List.iter (fun tssFunc ->
            let tss = tssFunc defaultTopshelfService
            let pattern =  tss.binaryPath.Replace("@buildMode@", buildMode)
            let binaryPath = !!pattern |> Seq.head
            let outProjDir = outDir + tss.name + sep + version.Value + sep
            CopyDir (outProjDir + "binaries" + sep) (binaryPath + sep) (fun _ -> true)
            CopyDir (outProjDir + "tools" + sep + "config-transform") (qsDir + sep + ".." + sep + ".." + sep + "config-transform" + sep + "tools" + sep) (fun _ -> true)
            CopyDir (outProjDir + "scripts" + sep) (qsDir + "topshelf" + sep + "scripts" + sep) (fun _ -> true)
            FileUtils.cp (qsDir + "topshelf" + sep + "boot" + sep + "install_topshelf.bat") (outProjDir + "install.bat")
            FileUtils.cp (qsDir + "topshelf" + sep + "boot" + sep + "fake.deploy.fsx") (outProjDir + "fake.deploy.fsx")
            
            let envDirForProject = (rootDir + sep + "env" + sep + tss.name + sep)

            if (directoryExists envDirForProject) then 
                CopyDir (outProjDir + "env" + sep) envDirForProject (fun _ -> true)
            else
                failwith <| sprintf "Top shelf services must have servicekey.deploy.envname.pson in env directory. Please look at sample templates in %s/topshelf/templates/ for formats." qsDir


            let serviceInfo = 
                "@{" + Environment.NewLine +
                    "    serviceKey='" + tss.name + "';" + Environment.NewLine +
                    "    version='" + v + "';" + Environment.NewLine +
                    "    packageUtcTime='" + DateTime.UtcNow.ToString() + "'" + Environment.NewLine +
                    "}" + Environment.NewLine

            WriteStringToFile false (outProjDir + "serviceInfo.pson") serviceInfo
            
            //publish
            let source = outDir + tss.name + sep + v + sep
            let targetDir = getPublishRoot() + tss.name + sep
            
            trace <| sprintf "Publishing top shelf service from %A  to %A" source targetDir
            
            ensureDirectory targetDir

            //!!(source + "**/*.*")
            //|> Zip source (targetDir + version.Value + ".zip")

            let nuspecPath = qsDir + "nuget" + sep + "fake.deploy.nuspec"
            if fileExists nuspecPath = false then
                FileHelper.CopyFile nuspecPath (qsDir + "nuget" + sep + "fake.deploy.nuspectemplate")

            NuGet (fun p ->
                {p with
                    Authors = tss.authors
                    Files = [source, None, None]
                    Description = tss.name
                    Project = tss.name
                    Version = getNugetVersion v
                    OutputPath = targetDir
                }
                ) nuspecPath
      )

