@echo off
SETLOCAL
::set to packages dir. Must NOT contain trailing \
SET _packages=".\packages"

cls

if not exist "%_packages%\fake" (
".nuget\NuGet.exe" "Install" "FAKE" "-OutputDirectory" "%_packages%" "-ExcludeVersion"
)

if not exist "%_packages%\config-transform" (
".nuget\NuGet.exe" "Install" "config-transform" -pre "-OutputDirectory" "%_packages%" "-ExcludeVersion"
)

if not exist "%_packages%\quicksilver" (
".nuget\NuGet.exe" "Install" "quicksilver" -pre "-OutputDirectory" "%_packages%" "-ExcludeVersion"
)

"%_packages%\FAKE\tools\Fake.exe" build.fsx %*