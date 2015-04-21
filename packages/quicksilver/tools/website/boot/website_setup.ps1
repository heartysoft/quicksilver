param ([string]$psonFile)
$web = gc $psonFile | Out-String | iex

import-module WebAdministration

write-host "------------------------------------------"
write-host "processing application pool $($web.AppPool.Name)"
write-host "------------------------------------------"
#App Pool

if(test-path "IIS:\AppPools\$($web.AppPool.Name)")
{
  write-host "app pool exists."
} else {
  write-host "app pool doesn't exist. creating..."
  New-WebAppPool $web.AppPool.Name -Force
}

$appPool = gi "IIS:\AppPools\$($web.AppPool.Name)" -ErrorAction SilentlyContinue

$changed = $false

###################
##Managed Runtime
###################
if($appPool.managedRuntimeVersion -ne $web.AppPool.ManagedRuntimeVersion) {
    write-host "will update managed runtime version to $($web.AppPool.ManagedRuntimeVersion)."
    $appPool.managedRuntimeVersion = $web.AppPool.ManagedRuntimeVersion
    $changed = $true
} else {
    write-host "managed runtime version is already $($web.AppPool.ManagedRuntimeVersion)."
}
###################
##32 bit
###################
if($appPool.enable32BitAppOnWin64 -ne $web.AppPool.Enable32Bit) {
    write-host "will set enable32BitAppOnWin64 to $($web.AppPool.Enable32Bit)."
    $appPool.enable32BitAppOnWin64 = $web.AppPool.Enable32Bit
    $changed = $true
} else {
    write-host "enable32BitAppOnWin64 is already $($web.AppPool.Enable32Bit)."
}

###################
##Process User
###################
write-host "processing app pool user..."
if ((($appPool.processModel.identityType -eq "SpecificUser") -and 
        ($appPool.processModel.userName -ne $web.AppPool.UserName)) -or 
    (($appPool.processModel.identityType -ne "SpecificUser") -and ($appPool.processModel.identityType -ne $web.AppPool.UserName))){
    write-host "updating app pool user..."
    $changed = $true
    $u = $web.AppPool.UserName
    if(($u -eq "ApplicationPoolIdentity") -or ($u -eq "LocalSystem") -or ($u -eq "LocalService") -or ($u -eq "NetworkService")) {
        $appPool.processModel.identityType = $u
        write-host "will set app pool user to $($u)."
    } else {
        write-host "setting app pool user to specific account $($u)."
        $appPool.processModel.identityType = 3
        $appPool.processModel.userName = $u
        $appPool.processModel.password = $web.AppPool.Password
    }
} else {
    write-host "app pool using the correct user. no steps taken."
}

###################
##Pipeline
###################
if($appPool.managedPipelineMode -ne $web.AppPool.Pipeline) {
    write-host "will set pipeline mode to $($web.AppPool.Pipeline)."
    $appPool.managedPipelineMode = $web.AppPool.Pipeline
    $changed = $true
} else {
    write-host "pipeline mode is already $($web.AppPool.Pipeline)."
}



if($changed){
    write-host "changes required...applying..."
    $appPool | set-item
    write-host "changes applied."
} else {
    write-host "no change required."
}


write-host "finished processing app pool."
write-host "##########################################"
write-host "processing website $($web.Website.Name)"
write-host "##########################################"

$ws = gi "IIS:\Sites\$($web.Website.Name)" -ErrorAction SilentlyContinue

if($ws){
   write-host "website exists. removing..."
   $ws | Remove-Website
}


write-host "creating website..."
   
if("$($web.Website.IpAddress)".trim() -eq ""){
    $web.Website.IpAddress = "*"
}

New-Website $web.WebSite.Name -Force -PhysicalPath $web.Website.PhysicalPath -Port $web.Website.Port `
-Ssl:$web.Website.SSL -HostHeader $web.Website.HostHeader -IPAddress $web.Website.IpAddress -ApplicationPool $web.Website.AppPool

write-host "website created"


write-host "all done. bye bye :)"
