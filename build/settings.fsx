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


    type SettingsRecord = 
        { BuildSettings : BuildSettingsRecord; TestSettings : TestSettingsRecord }
        member this.Build(f:BuildSettingsRecord -> BuildSettingsRecord) = 
            {this with BuildSettings = f(this.BuildSettings)}

        member this.NUnit(f:TestSettingsRecord -> TestSettingsRecord) = 
            {this with TestSettings = f(this.TestSettings)}

    
    let settings = {BuildSettings = defaultBuildSettings; TestSettings = defaultTestSettings}

    let build f (settings:SettingsRecord) = settings.Build(f)
    let nunit f (settings:SettingsRecord) = settings.NUnit(f)

