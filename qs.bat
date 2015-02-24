@echo off
cls
if not exist ".quicksilver\fake" (
".nuget\NuGet.exe" "Install" "FAKE" "-OutputDirectory" ".quicksilver" "-ExcludeVersion"
)

if not exist ".quicksilver\config-transform" (
".nuget\NuGet.exe" "Install" "config-transform" -pre "-OutputDirectory" ".quicksilver" "-ExcludeVersion"
)

if not exist ".quicksilver\quicksilver" (
".nuget\NuGet.exe" "Install" "quicksilver" -pre "-OutputDirectory" ".quicksilver" "-ExcludeVersion"
)

".quicksilver\FAKE\tools\Fake.exe" ".\.quicksilver\quicksilver\tools\build.fsx" %*