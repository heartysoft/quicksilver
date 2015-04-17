open System
open System.Diagnostics

let envOpt = 
    fsi.CommandLineArgs 
    |> Seq.tryFind (fun arg -> arg.StartsWith("env="))


printfn "starting installer via install.bat."

let p = new ProcessStartInfo("cmd.exe")
p.UseShellExecute <- false
p.RedirectStandardOutput <- true
p.RedirectStandardError <- true
p.WorkingDirectory <- __SOURCE_DIRECTORY__

match envOpt with
| Some(e) -> 
    let env = e.Substring(e.IndexOf("=") + 1)
    p.Arguments <- sprintf "/c install.bat %s" env
    printfn "environment: %s." env
| None -> 
    p.Arguments <- "/c install.bat"
    printfn "environment not specified."

let pr = Process.Start(p)
pr.OutputDataReceived.Add(fun args -> printfn "Info: %A" args.Data)
pr.ErrorDataReceived.Add(fun args -> System.Console.Error.WriteLine("Error: {0}", args.Data))
pr.BeginOutputReadLine();
pr.BeginErrorReadLine();

pr.WaitForExit()

let exitCode = pr.ExitCode

printfn "installation completed. Exit code: %A" exitCode

if exitCode <> 0 then
    failwith "Installation failed"

