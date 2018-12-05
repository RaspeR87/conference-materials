function InstallProject {
    param(
        [Parameter(Mandatory=$true)]
        [string]$webpartName,
        [Parameter(Mandatory=$false)]
        [boolean]$fromLockFile = $true,
        [Parameter(Mandatory=$false)]
        [boolean]$noSaveLockFile = $true
    )

    $SPFxRoot = Get-Location
    $webParts = Get-ChildItem -Directory
    $found = $false
    foreach ($webPart in $webParts) {
        if ($webPart.Name -eq $webpartName) {
            $found = $true

            Set-Location -Path $SPFxRoot"/"$webPart
            Write-Host "SPFx Web Part '$SPFxRoot'"

            npm install -g rimraf
            rimraf ./node_modules

            npm cache clean --force

            $arguments = New-Object System.Collections.ArrayList
            if ($fromLockFile) {
                $arguments.Add("--from-lock-file")
            }
            if ($noSaveLockFile) {
                $arguments.Add("--no-save")
            }
            
            npm install $arguments
        }
    }

    if (!$found) {
        Write-Error "Web Part with name '$webpartName' not found"
    }
}

function StartProvision {
    param(
        [Parameter(Mandatory=$true)]
        [string]$targetCDN
    )

    Write-Host "TargetCDN '$targetCDN'"
    
    $SPFxRoot = Get-Location
    $webParts = Get-ChildItem -Directory
    foreach ($webPart in $webParts) {
        Set-Location -Path $SPFxRoot"/"$webPart
        Write-Host "SPFx Web Part '$SPFxRoot'"

        gulp modifyconfigfile --target-cdn $targetCDN
        gulp bundle --ship --copytowsp --target-cdn $targetCDN
        gulp copyassetstowsp
        gulp package-solution --ship
        gulp copypackagetowsp
        gulp cleanconfigfile
    }
}

function StartProvisionExact {
    param(
        [Parameter(Mandatory=$true)]
        [string]$targetCDN,
        [Parameter(Mandatory=$true)]
        [string]$webpartName
    )

    Write-Host "TargetCDN '$targetCDN'"

    $SPFxRoot = Get-Location
    $webParts = Get-ChildItem -Directory
    $found = $false
    foreach ($webPart in $webParts) {
        if ($webPart.Name -eq $webpartName) {
            $found = $true

            Set-Location -Path $SPFxRoot"/"$webPart
            Write-Host "SPFx Web Part '$SPFxRoot'"

            gulp modifyconfigfile --target-cdn $targetCDN
            gulp bundle --ship --copytowsp --target-cdn $targetCDN
            gulp copyassetstowsp
            gulp package-solution --ship
            gulp copypackagetowsp
            gulp cleanconfigfile
        }
    }

    if (!$found) {
        Write-Error "Web Part with name '$webpartName' not found"
    }
}

function CopyAssetsToServer {
    param(
        [Parameter(Mandatory=$true)]
        [string]$targetCDN,
        [Parameter(Mandatory=$true)]
        [string]$webpartName,
        [Parameter(Mandatory=$true)]
        [string]$server
    )

    Write-Host "TargetCDN '$targetCDN'"
    Write-Host "Server '$server'"

    $SPFxRoot = Get-Location
    $webParts = Get-ChildItem -Directory
    $found = $false
    foreach ($webPart in $webParts) {
        if ($webPart.Name -eq $webpartName) {
            $found = $true

            Set-Location -Path $SPFxRoot"/"$webPart
            Write-Host "SPFx Web Part '$SPFxRoot'"

            gulp bundle --ship --copytowsp --target-cdn $targetCDN
            gulp copyassetstowsp --server $server
        }
    }

    if (!$found) {
        Write-Error "Web Part with name '$webpartName' not found"
    }
}