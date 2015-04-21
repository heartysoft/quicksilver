open System
open System.Diagnostics
open System.IO

let envOpt = 
    fsi.CommandLineArgs 
    |> Seq.tryFind (fun arg -> arg.StartsWith("env="))

let env = 
    Option.map (fun (e:string) -> e.Substring(e.IndexOf("=") + 1)) envOpt

printfn "Step: iis initialisation"
printfn "########################################################"

let iisFile = 
    match env with
    | Some(e) -> sprintf "iis.%s.pson" e
    | None -> "iis.pson"

let iisFilePath = Path.Combine(__SOURCE_DIRECTORY__,  iisFile)

if File.Exists iisFilePath then
    printfn "IIS file %s found. Running..." iisFile
    let p = new ProcessStartInfo(@"C:\Windows\SysNative\WindowsPowerShell\v1.0\powershell.exe")
    p.UseShellExecute <- false
    p.RedirectStandardOutput <- true
    p.RedirectStandardError <- true
    p.WorkingDirectory <- __SOURCE_DIRECTORY__
    p.Arguments <- sprintf "-ExecutionPolicy RemoteSigned -File website_setup.ps1 %s" iisFilePath

    printfn "ensuring iis setup matches %s" iisFile

    let pr = Process.Start(p)
    pr.OutputDataReceived.Add(fun args -> printfn "%A" args.Data)
    pr.ErrorDataReceived.Add(fun args -> System.Console.Error.WriteLine(args.Data))
    pr.BeginOutputReadLine();
    pr.BeginErrorReadLine();

    pr.WaitForExit()

    let exitCode = pr.ExitCode

    printfn "iis initialisation script finished. Exit code: %A" exitCode

    if exitCode <> 0 then
        failwith "iis initialisation failed"
    else
        printfn "iis initialisation complete"
else
    printfn "IIS file %s not found. Skipping iis initialisation." iisFile

printfn "########################################################"
printfn "step: website installation"
printfn "########################################################"

printfn "starting installer via install.bat."

let p = new ProcessStartInfo("cmd.exe")
p.UseShellExecute <- false
p.RedirectStandardOutput <- true
p.RedirectStandardError <- true
p.WorkingDirectory <- __SOURCE_DIRECTORY__

match envOpt with
| Some(e) -> 
    p.Arguments <- sprintf "/c install.bat %s /Y" env.Value
    printfn "environment: %s." env.Value
| None -> 
    p.Arguments <- "/c install.bat /Y"
    printfn "environment not specified."

let pr = Process.Start(p)
pr.OutputDataReceived.Add(fun args -> printfn "%A" args.Data)
pr.ErrorDataReceived.Add(fun args -> System.Console.Error.WriteLine(args.Data))
pr.BeginOutputReadLine();
pr.BeginErrorReadLine();

pr.WaitForExit()

let exitCode = pr.ExitCode

printfn "installation completed. Exit code: %A" exitCode

if exitCode <> 0 then
    failwith "Installation failed"
