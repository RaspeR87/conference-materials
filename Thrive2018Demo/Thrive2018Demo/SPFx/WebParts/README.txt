### PROJECT INSTALLATION ###
powershell.exe ". .\ProvisionToWSP.ps1; InstallProject -webpartName 'helloworld-webpart'"
powershell.exe ". .\ProvisionToWSP.ps1; InstallProject -webpartName 'helloworld2-webpart'"

### DEV ###
powershell.exe ". .\ProvisionToWSP.ps1; StartProvision -targetCDN 'http://sp2016/sites/appcatalog/SPFxAssets'"

powershell.exe ". .\ProvisionToWSP.ps1; StartProvisionExact -targetCDN 'http://sp2016/sites/appcatalog/SPFxAssets'" -webpartName "helloworld-webpart"
powershell.exe ". .\ProvisionToWSP.ps1; StartProvisionExact -targetCDN 'http://sp2016/sites/appcatalog/SPFxAssets'" -webpartName "helloworld2-webpart"

// Copy Assets files directly to your SharePoint Server App Catalog (no need for WSP deployment)
powershell.exe ". .\ProvisionToWSP.ps1; CopyAssetsToServer -targetCDN 'http://sp2016/sites/appcatalog/SPFxAssets'" -webpartName "helloworld-webpart" -server 'http://sp2016/sites/appcatalog'
powershell.exe ". .\ProvisionToWSP.ps1; CopyAssetsToServer -targetCDN 'http://sp2016/sites/appcatalog/SPFxAssets'" -webpartName "helloworld2-webpart" -server 'http://sp2016/sites/appcatalog'

### PRODUCTION ###
powershell.exe ". .\ProvisionToWSP.ps1; StartProvision -targetCDN 'https://partner.company.com/sites/appcatalog/SPFxAssets'"

powershell.exe ". .\ProvisionToWSP.ps1; StartProvisionExact -targetCDN 'https://partner.company.com/sites/appcatalog/SPFxAssets'" -webpartName "helloworld-webpart"
powershell.exe ". .\ProvisionToWSP.ps1; StartProvisionExact -targetCDN 'https://partner.company.com/sites/appcatalog/SPFxAssets'" -webpartName "helloworld2-webpart"
