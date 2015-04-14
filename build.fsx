#r "packages/fake/tools/FakeLib.dll"
#load "packages/quicksilver/tools/quicksilver.fsx"


open Fake
open Quicksilver

//adding builder=ci on the command line will use ci publish directory.
ciPublishRoot <- @"D:\Derp\"


//if version=x is passed through command line, that's used.
//otherwise, if it's a git project, a v* tag on the current commit is used.
//specifying version explicitly here will override any other versioning mechanism in place.
//version <- Some("1.5")

//Needed if auto package restore doesn't work. Sometimes the case with BCL builds, etc.
restoreNugetPackagesTo "./packages"

Target "Default" (fun x ->
    //also, CleanDirs [...]
    CleanDir outDir                              
    //must be exact path(s)   
    buildSolutions ["MyWebAppForDeployment.sln"] 
    //list of glob patterns to test with nunit. Requires nunit.runners to be installed in a subdirectory.
    //testWithNUnit [@"**\bin\@buildMode@\*Tests.dll"]
    //Glob patterns for websites to create quicksilver packages for.
    //packageQuicksilverWebsites [@"**\MyWebAppForDeployment.csproj"]
    //Same, but for services
    packageQuicksilverTopshelfServices [
        {name="MyWindowsService"; binaryPath="**/MyWindowsService/bin/@buildMode@/"; authors=["ashic"]}
    ]
)


RunTargetOrDefault "Default"