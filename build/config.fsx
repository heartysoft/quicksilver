#load "settings.fsx"
open QuickSilver

let settings = 
    settings 
    |> build (fun p -> 
        {p with solutions = ["MyWebAppForDeployment.sln"]}
    )
    |> nunit (fun p ->
        {p with nunitTests = [@"**\bin\@buildMode@\*Tests.dll"]}
    )
    |> websites (fun p ->
        {p with projFiles = [@"**\MyWebAppForDeployment.csproj"]}
    )


