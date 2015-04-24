param ([string]$psonFile)
$web = gc $psonFile | Out-String | iex

import-module WebAdministration

$ErrorActionPreference = "stop"

write-host "pson file is $psonFile"

$web.AppPools | foreach {
    $pool = $_
    $poolName = $pool.Name
    write-host "------------------------------------------"
    write-host "processing application pool $poolName)"
    write-host "------------------------------------------"
    #App Pool

    if(test-path "IIS:\AppPools\$poolName")
    {
      write-host "app pool exists."
    } else {
      write-host "app pool doesn't exist. creating..."
      New-WebAppPool $poolName -Force
    }

    $appPool = gi "IIS:\AppPools\$poolName" -ErrorAction SilentlyContinue

    $changed = $false

    ###################
    ##Managed Runtime
    ###################
    if($appPool.managedRuntimeVersion -ne $pool.ManagedRuntimeVersion) {
        write-host "will update managed runtime version to $($pool.ManagedRuntimeVersion)."
        $appPool.managedRuntimeVersion = $pool.ManagedRuntimeVersion
        $changed = $true
    } else {
        write-host "managed runtime version is already $($pool.ManagedRuntimeVersion)."
    }
    ###################
    ##32 bit
    ###################
    if($appPool.enable32BitAppOnWin64 -ne $pool.Enable32Bit) {
        write-host "will set enable32BitAppOnWin64 to $($pool.Enable32Bit)."
        $appPool.enable32BitAppOnWin64 = $pool.Enable32Bit
        $changed = $true
    } else {
        write-host "enable32BitAppOnWin64 is already $($pool.Enable32Bit)."
    }

    ###################
    ##Process User
    ###################
    write-host "processing app pool user..."
    if ((($appPool.processModel.identityType -eq "SpecificUser") -and 
            ($appPool.processModel.userName -ne $pool.UserName)) -or 
        (($appPool.processModel.identityType -ne "SpecificUser") -and ($appPool.processModel.identityType -ne $pool.UserName))){
        write-host "updating app pool user..."
        $changed = $true
        $u = $pool.UserName
        if(($u -eq "ApplicationPoolIdentity") -or ($u -eq "LocalSystem") -or ($u -eq "LocalService") -or ($u -eq "NetworkService")) {
            $appPool.processModel.identityType = $u
            write-host "will set app pool user to $($u)."
        } else {
            write-host "setting app pool user to specific account $($u)."
            $appPool.processModel.identityType = 3
            $appPool.processModel.userName = $u
            $appPool.processModel.password = $pool.Password
        }
    } else {
        write-host "app pool using the correct user. no steps taken."
    }

    ###################
    ##Pipeline
    ###################
    if($appPool.managedPipelineMode -ne $pool.Pipeline) {
        write-host "will set pipeline mode to $($pool.Pipeline)."
        $appPool.managedPipelineMode = $pool.Pipeline
        $changed = $true
    } else {
        write-host "pipeline mode is already $($pool.Pipeline)."
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
}

$web.Websites | foreach {
    $website = $_
    write-host "processing website $($website.Name)"
    write-host "##########################################"

    $ws = gi "IIS:\Sites\$($website.Name)" -ErrorAction SilentlyContinue

    if($ws){
       write-host "website exists. removing..."
       $ws | Remove-Website
    }


    write-host "creating website..."
   
    if("$($website.IpAddress)".trim() -eq ""){
        $website.IpAddress = "*"
    }

    New-Website $website.Name -Force -PhysicalPath $website.PhysicalPath -Port $website.Port `
    -Ssl:$website.SSL -HostHeader $website.HostHeader -IPAddress $website.IpAddress -ApplicationPool $website.AppPool

    write-host "website created"

    if(-not $website.SSL){write-host "Not using SSL. Proceeding"} else {
        if(-not $website.CertificatePath) { write-host "No certificates specified. Skipping attaching to certificate" } else {       
    
            $ip = $website.IpAddress
            if($ip -eq "*"){$ip = "0.0.0.0"}
            $binding = "IIS:\SSLBindings\$ip!$($website.Port)"

            if(test-path $binding) {
                write-host "removing IIS SSL binding."
                remove-item $binding
            }

            $cert = dir $website.CertificatePath
            write-host "attaching ssl binding to certificate..."
            gi "$($website.CertificatePath)\$($cert.ThumbPrint)" | new-item $binding
            write-host "SSL certificate attached."
        }

    }

    write-host "processing additional application for website..."
    write-host "removing existing applications..."
    
    Get-WebApplication -site $website.Name | Remove-WebApplication -site $website.Name
    write-host "removed existing applications..."

    if($website.AdditionalApplications){
        $website.AdditionalApplications | foreach {
            $app = $_
        
            write-host "------------------------------"
            write-host "creating application $($app.Name)"

            if(-not(test-path $app.PhysicalPath)) {
                write-host "path doesn't exist. ensuring: $($app.PhysicalPath)"
                New-Item -ItemType Directory -Force -Path $app.PhysicalPath
            }

            New-WebApplication -Site $website.Name -Name $app.Name -ApplicationPool $app.AppPool -PhysicalPath $app.PhysicalPath
        
            write-host "application created."        
            write-host "------------------------------"
        }

        write-host "additional applications processed."
    }
}

write-host "all done. bye bye :)"
