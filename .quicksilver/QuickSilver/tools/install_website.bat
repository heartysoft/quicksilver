@echo OFF

setlocal
cd /d %~dp0

if "%1" == "" (
    echo "usage: install.bat env [parameters to *.deploy.cmd. Note /T for test, /Y for confirm.]"
    exit /b 1
)

set ALL_BUT_FIRST=
for /f "tokens=1,* delims= " %%a in ("%*") do set ALL_BUT_FIRST=%%b

for %%a in (*.deploy.cmd) do (
    set cmdFile=%%a
    goto cmdSet
)

:cmdSet

if "%cmdFile%" == "" (
    echo "FATAL ERROR: There doesn't seem to be an msdeploy *.deploy.cmd file in the directory."
    exit /b 2
)


set appenv=%1
echo %cmdFile%

set _DeploySetParametersFile=%appenv%.xml

echo deploying to appenv using parameters defined in %_DeploySetParametersFile%.
echo calling: %cmdFile% %ALL_BUT_FIRST%

%cmdFile% %ALL_BUT_FIRST%


