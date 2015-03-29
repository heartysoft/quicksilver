@echo off
setlocal
cd /d %~dp0

powershell.exe -NoProfile -ExecutionPolicy unrestricted -Command "& { .\scripts\topshelf.ps1 %*; if ($lastexitcode -ne 0) {write-host "ERROR: $lastexitcode" -fore RED; exit $lastexitcode} }" 
