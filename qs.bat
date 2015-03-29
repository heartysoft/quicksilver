@echo off
SETLOCAL
::set to packages dir. Must NOT contain trailing \
SET _packages=".\packages"
SET _nuget=".nuget\NuGet.exe"

cls

if not exist "%_packages%\fake" (
%_nuget% "Install" "FAKE" "-OutputDirectory" "%_packages%" "-ExcludeVersion"
)

if not exist "%_packages%\config-transform" (
%_nuget% "Install" "config-transform" -pre "-OutputDirectory" "%_packages%" "-ExcludeVersion"
)

if not exist "%_packages%\quicksilver" (
%_nuget% "Install" "quicksilver" -pre "-OutputDirectory" "%_packages%" "-ExcludeVersion"
)

"%_packages%\FAKE\tools\Fake.exe" build.fsx %*