@echo off
setlocal
cd /d %~dp0

set publish=
if "%1"=="push" (
    set publish="1"
)


echo "deleting existing nupkgs"
del .\nuget\*.nupkg

echo "creating nuget package"
.\.nuget\nuget.exe pack .\nuget\Package.nuspec -NoDefaultExcludes -OutputDirectory .\nuget

echo %1
echo %publish%


if %publish%=="1" (
    echo "publishing..."
    for %%f in (.\nuget\*.nupkg) do (
        set filename=%%f
        goto gotFile
    )

    echo %filename%
    :gotFile
    .\.nuget\nuget.exe push %filename%
)
