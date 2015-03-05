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
            WebsitesRoot : string
            TopShelfServicesRoot : string
        }
    let defaultPublishSettingsRecord = 
        {
            WebsitesRoot = "deploy/"
            TopShelfServicesRoot = "deploy/"
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
        member this.WebsitesPublishRoot path = 
            {this with PublishSettings = {this.PublishSettings with WebsitesRoot = path}}
        member this.TopShelfServicesPublishRoot path = 
            {this with PublishSettings = {this.PublishSettings with TopShelfServicesRoot = path}}

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
    let websitesPublishRoot path (settings:SettingsRecord) = settings.WebsitesPublishRoot path
    let topShelfServicesPublishRoot path (settings:SettingsRecord) = settings.TopShelfServicesPublishRoot path
