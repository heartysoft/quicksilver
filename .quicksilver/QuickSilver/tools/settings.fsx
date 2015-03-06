namespace QuickSilver
[<AutoOpen>]
module Settings = 
    type BuildSettingsRecord = 
        { solutions:string list; optimize:bool; additionalPackageSources : string list; restorePackagesPriorToBuild : bool }

    let defaultBuildSettings = {solutions = []; optimize=true; additionalPackageSources = []; restorePackagesPriorToBuild = false }

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

    type TopShelfServicePackageRecord = 
        {
            name: string
            binaryPath : string
        }

    let defaultTopShelfServicePackage = {TopShelfServicePackageRecord.name = ""; binaryPath = ""}

    type PackageConventionType = 
        | GitTag

    type PackageConventionRecord = 
        {
            Type : PackageConventionType
        }

    let defaultPackageConventionRecord = {PackageConventionRecord.Type = GitTag}

    type PublishSettingsRecord = 
        {
            WebsitesDevRoot : string
            WebsitesCIRoot : string
            TopShelfServicesDevRoot : string
            TopShelfServicesCIRoot : string
        }

        member this.GetWebPublishRoot (builder:string) = 
            match builder with
            | "dev" -> this.WebsitesDevRoot
            | "ci" -> this.WebsitesCIRoot
            | _ -> failwith <| sprintf "Builder %s not supported. Please specify ci, or leave empty for dev." builder
        
        member this.GetTopShelfServicesPublishRoot (builder:string) = 
            match builder with
            | "dev" -> this.TopShelfServicesDevRoot
            | "ci" -> this.TopShelfServicesCIRoot
            | _ -> failwith <| sprintf "Builder %s not supported. Please specify ci, or leave empty for dev." builder

    let defaultPublishSettingsRecord = 
        {
            WebsitesDevRoot = "deploy/"
            WebsitesCIRoot = "deploy/"
            TopShelfServicesDevRoot = "deploy/"
            TopShelfServicesCIRoot = "deploy/"
        }

    type SettingsRecord = 
        { 
            BuildSettings : BuildSettingsRecord
            TestSettings : TestSettingsRecord 
            WebsitePackages : WebsitePackageRecord
            TopShelfServicePackages : TopShelfServicePackageRecord list
            PackageConvention : PackageConventionRecord
            PublishSettings : PublishSettingsRecord
        }
        member this.Build f = 
            {this with BuildSettings = f(this.BuildSettings)}

        member this.NUnit f = 
            {this with TestSettings = f(this.TestSettings)}
        member this.Websites f = 
            {this with WebsitePackages = f(this.WebsitePackages)}
        member this.TopShelfServices f = 
            {this with TopShelfServicePackages = f(defaultTopShelfServicePackage)}
        member this.WebsitesDevPublishRoot path = 
            {this with PublishSettings = {this.PublishSettings with WebsitesDevRoot = path}}
        member this.WebsitesCIPublishRoot path = 
            {this with PublishSettings = {this.PublishSettings with WebsitesCIRoot = path}}
        member this.TopShelfServicesDevPublishRoot path = 
            {this with PublishSettings = {this.PublishSettings with TopShelfServicesDevRoot = path}}
        member this.TopShelfServicesCIPublishRoot path = 
            {this with PublishSettings = {this.PublishSettings with TopShelfServicesCIRoot = path}}

    let settings = {
            BuildSettings = defaultBuildSettings
            TestSettings = defaultTestSettings
            WebsitePackages = defaultWebsitePackages
            TopShelfServicePackages = []
            PackageConvention = defaultPackageConventionRecord
            PublishSettings = defaultPublishSettingsRecord
        }

    let build f (settings:SettingsRecord) = settings.Build(f)
    let nunit f (settings:SettingsRecord) = settings.NUnit(f)
    let websites f (settings:SettingsRecord) = settings.Websites f
    let topShelfServices f (settings:SettingsRecord) = settings.TopShelfServices f
    let websitesDevPublishRoot path (settings:SettingsRecord) = settings.WebsitesDevPublishRoot path
    let websitesCIPublishRoot path (settings:SettingsRecord) = settings.WebsitesCIPublishRoot path
    let topShelfServicesDevPublishRoot path (settings:SettingsRecord) = settings.TopShelfServicesDevPublishRoot path
    let topShelfServicesCIPublishRoot path (settings:SettingsRecord) = settings.TopShelfServicesCIPublishRoot path
