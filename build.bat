
@echo off
cls
".nuget\NuGet.exe" "Install" "FAKE" "-OutputDirectory" ".quicksilver" "-ExcludeVersion"
".quicksilver\FAKE\tools\Fake.exe" ".\.quicksilver\quicksilver\tools\build.fsx" %*
