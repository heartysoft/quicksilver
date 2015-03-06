#load "../.quicksilver/QuickSilver/tools/settings.fsx"
open QuickSilver

let settings = 
    settings 
    |> build (fun p -> 
        {p with solutions = ["MyWebAppForDeployment.sln"]; restorePackagesPriorToBuild = true}
    )
    |> nunit (fun p ->
        {p with nunitTests = [@"**\bin\@buildMode@\*Tests.dll"]}
    )
    |> websites (fun p ->
        {p with projFiles = [@"**\MyWebAppForDeployment.csproj"]}
    )
    |> topShelfServices (fun (p) ->
        [
            {
                p with 
                    name="MyWindowsService" 
                    binaryPath= @"**\MyWindowsService\bin\@buildMode@\"
            }
        ]
    )
    |> websitesCIPublishRoot @"D:/woohoo/"
    |> topShelfServicesCIPublishRoot @"D:/woohoo/"


