asnp *sharepoint*

Restart-Service sptimerv4
Update-SPSolution -Identity Thrive2018Demo.wsp -LiteralPath C:\Temp\Thrive2018Demo.wsp -GACDeployment

Uninstall-SPSolution -Identity Thrive2018Demo.wsp
Remove-SPSolution -Identity Thrive2018Demo.wsp -force
Add-SPSolution C:\Temp\Thrive2018Demo.wsp
Restart-Service sptimerv4
Install-SPSolution -Identity Thrive2018Demo.wsp -GACDeployment -Verbose -force

iisreset