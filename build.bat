
@echo off
cls
".nuget\NuGet.exe" "Install" "FAKE" "-OutputDirectory" ".quicksilver" "-ExcludeVersion"
".nuget\NuGet.exe" "Install" "config-transform" -pre "-OutputDirectory" ".quicksilver" "-ExcludeVersion"
".quicksilver\FAKE\tools\Fake.exe" ".\.quicksilver\quicksilver\tools\build.fsx" %*
