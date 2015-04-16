open System
open System.Diagnostics

let envOpt = 
    fsi.CommandLineArgs 
    |> Seq.tryFind (fun arg -> arg.StartsWith("env="))


printfn "starting installer via install.bat."

let p = new ProcessStartInfo("install.bat")
p.WorkingDirectory <- __SOURCE_DIRECTORY__
p.UseShellExecute <- false

match envOpt with
| Some(e) -> 
    let env = e.Substring(e.IndexOf("=") + 1)
    p.Arguments <- env
    printfn "environment: %s." env
| None -> 
    printfn "environment not specified."

let pr = Process.Start(p)

pr.WaitForExit()

let exitCode = pr.ExitCode

printfn "installation completed. Exit code: %A" exitCode

if exitCode <> 0 then
    failwith "Installation failed"

