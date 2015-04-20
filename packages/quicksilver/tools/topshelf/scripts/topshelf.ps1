 param (
    [string]$environ
 )

$ErrorActionPreference = "Stop"


function Exec([scriptblock]$cmd, [string]$errorMessage = "Error executing command: " + $cmd) { 
  & $cmd 
  if ($LastExitCode -ne 0) {
    throw $errorMessage 
  } 
}


$scriptPath = split-path -parent $MyInvocation.MyCommand.Definition
$r = (get-item $scriptPath).Parent.FullName



write-host "starting processing of topshelf service..."

if($environ-eq "") {
    write-error "Environment not specified. Usage: topshelf_install.bat [env]"   
}

$environ = $environ.ToLowerInvariant()

write-host "Using environment $environ..."


if (-not (Test-Path ("$r\serviceInfo.pson"))){
   write-error "Expected serviceInfo.pson at root."   
}

write-host "Found serviceInfo.pson in root. Proceeding..."

$serviceInfo = gc "$r\serviceInfo.pson" | Out-String | iex
$serviceKey =  $serviceInfo["servicekey"]

write-host "Service key is $serviceKey..."

#service key loaded.

$configFile = "$r\env\$serviceKey.deploy.$environ.pson"
if(-not (Test-Path ($configFile))){
    write-error "Service configuration not found at $configFile."
}

write-host "Loading service configuration from $configFile..."

$config = gc "$configFile" | Out-String | iex

$runAsSystem = $config["RunAsSystem"]
$runAsLocalService = $config["RunAsLocalService"]
$runAsNetworkService = $config["RunAsNetworkService"]
$user = $config["User"]
$password = $config["Password"]
$interactive = $config["Interactive"]
$installRoot = $config["InstallationLocationRoot"]
$displayName = $config["DisplayName"]
$serviceDescription = $config["ServiceDescription"]
$reinstall = $config["Reinstall"]
$executable = $config["Executable"]
$serviceName = $config["ServiceName"]

#config loaded.

$identity = ""

if($runAsSystem){
    $identity = '--localsystem'
} elseif($runAsLocalService) {
        $identity = '--localservice'
} elseif($runAsNetworkService){
    $identity = '--networkservice'
} elseif($interactive){
    if("$user".trim() -ne ""){
      $identity = "-username `"$user`" --interactive"  
    } else {
      $identity = "--interactive"  
    }
} else {
    if("$user".trim() -eq "" -or "$password".trim() -eq ""){
        write-error "Installation is not for system, local service, or network service, and interactive was not requested. In this case, username and password are required."
    }

    $identity = "-username `"$user`" -password `"$password`""
}

write-host "installation identity selected..."
#identity set

$existing = Get-Service $serviceName -ErrorAction SilentlyContinue


if($existing){
    write-host "Existing service named $serviceName found. Stopping..."
    Stop-Service $serviceName -ErrorAction SilentlyContinue
    write-host "Existing service has been stopped if it was running..."
    

    if($reinstall){
        write-host "Reinstall requested. Removing service $serviceName..."
        exec {
            sc.exe delete `"$serviceName`" | echo
        }

        write-host "service $servicename uninstalled..."
        write-host "waiting 5 seconds for teardown. if deployment fails, consider waiting a bit for process to die and retrying..."
        start-sleep -s 5
        write-host "commencing."
    }
}
else {
    write-host "A service with name $serviceName was not found. Stopping/uninstalling not required. Proceeding..."
}

write-host "Ensuring $installRoot$serviceName exists..."
New-Item -ItemType directory -Path "$installRoot$serviceName" -ErrorAction SilentlyContinue | Out-Null

write-host "Deleting contents of $installRoot$serviceName..."
remove-item "$installRoot$serviceName\*" -Recurse -Force
write-host "Deleted..."

$binaryPath = "$r\binaries\*"
write-host "Copying binaries from $binaryPath to target location $installRoot$serviceName\"
copy-item $binaryPath "$installRoot$serviceName" -Recurse -Force
write-host "binaries copied..."

#files are in target. Tokenize!!

write-host "Tokenizing via config transforms for environment: $environ"
gi "$r\env\*.$environ*.config" | foreach {
    $fileName = $_.Name.ToLowerInvariant()
    $fileFullName = $_.FullName.ToLowerInvariant()
    $targetFileName = $fileName.Replace(".$environ", "")
    $targetFile = "$installRoot$serviceName\$targetFileName".ToLowerInvariant()

    write-host "Tokenizing $targetFile with $fileFullName"
    exec {
        & "$r\tools\config-transform\config-transform.exe" $targetFile $fileFullName
    }
    write-host "Tokenization $targetFile with $fileFullName successful."
}


#tokenization done. 

$exePath = "$installRoot$serviceName\$executable"
echo "Target executable: $exePath"

if(-not($existing) -or $reinstall){
    write-host "Installing service $serviceName using $exePath"
    
    $installArgs = "install $identity -servicename `"$serviceName`" -description `"$serviceDescription`" -displayname `"$displayName`""
    #$wrappedInstallCommand = "$installCommand'"
    #write-host $wrappedInstallCommand

    exec {
      cmd /C "$exePath $installArgs" | echo
    }
    
    write-host "Service installed."
}

write-host "Starting service $serviceName"
start-service $serviceName


write-host "copying service info file..."
copy-item "$r\serviceInfo.pson" -destination "$installRoot$serviceName"

write-host "all done...bye bye."

exit 0
