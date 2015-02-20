namespace QuickSilver
[<AutoOpen>]
module Settings = 
    type BuildSettingsRecord = 
        { solutions:string list; optimize:bool }

    let defaultBuildSettings = {solutions = []; optimize=true}

    type TestSettingsRecord = 
        {
            nunitTests : string list
        }

    let defaultTestSettings = {nunitTests = []}

    type WebsitePackageRecord = 
        {
            projFiles : string list
        }

    let defaultWebsitePackages = {WebsitePackageRecord.projFiles = []}
    
    type SettingsRecord = 
        { 
            BuildSettings : BuildSettingsRecord
            TestSettings : TestSettingsRecord 
            WebsitePackages : WebsitePackageRecord
        }
        member this.Build f = 
            {this with BuildSettings = f(this.BuildSettings)}

        member this.NUnit f = 
            {this with TestSettings = f(this.TestSettings)}
        member this.Websites f = 
            {this with WebsitePackages = f(this.WebsitePackages)}

    
    let settings = {
            BuildSettings = defaultBuildSettings
            TestSettings = defaultTestSettings
            WebsitePackages = defaultWebsitePackages
        }

    let build f (settings:SettingsRecord) = settings.Build(f)
    let nunit f (settings:SettingsRecord) = settings.NUnit(f)
    let websites f (settings:SettingsRecord) = settings.Websites f
