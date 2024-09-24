# This script will list all the hostnames

. .\_Settings.ps1


# Delete the .URL files if they exist
Remove-Item -Path ".\Link-WindowsAppService.url" -ErrorAction SilentlyContinue
Remove-Item -Path ".\Link-LinuxAppService.url" -ErrorAction SilentlyContinue
Remove-Item -Path ".\Link-LinuxContainerAppService.url" -ErrorAction SilentlyContinue
Remove-Item -Path ".\Link-ContainerInstance.url" -ErrorAction SilentlyContinue
Remove-Item -Path ".\Link-ContainerApp.url" -ErrorAction SilentlyContinue


Write-Host "`n`n";

$windowsAppService = az webapp show --resource-group $rgname --name $AppServiceName_win --output json | ConvertFrom-Json  
$windowsAppServiceUrl = $windowsAppService.defaultHostName
Write-Host "Windows App Service hostname:          https://${windowsAppServiceUrl}"


$linuxAppService = az webapp show --resource-group $rgname --name $AppServiceName_linux --output json | ConvertFrom-Json  
$linuxAppServiceUrl = $linuxAppService.defaultHostName
Write-Host "Linux App Service hostname:            https://${linuxAppServiceUrl}"


$linuxContainerAppService = az webapp show --resource-group $rgname --name $AppServiceName_container_linux --output json | ConvertFrom-Json  
$linuxContaiinerAppServiceUrl = $linuxContainerAppService.defaultHostName
Write-Host "Linux Container hostname:              https://${linuxContaiinerAppServiceUrl}"


$containerInstance = az container show --resource-group $rgname --name $containerInstanceName  --output json | ConvertFrom-Json
$containerInstanceHostName = $containerInstance.ipAddress.fqdn
Write-Host "Container Instance hostname:           http://${containerInstanceHostName}:8080"


$containerApp = az containerapp show --name $containerAppName --resource-group $rgname --output json | ConvertFrom-Json
$containerAppHostName = $containerApp.properties.configuration.ingress.fqdn
Write-Host "Container App hostname:                https://${containerAppHostName}"


# Generate .URL files to each service
"[InternetShortcut]`nURL=https://${windowsAppServiceUrl}" | Out-File -FilePath ".\Link-WindowsAppService.url"
"[InternetShortcut]`nURL=https://${linuxAppServiceUrl}" | Out-File -FilePath ".\Link-LinuxAppService.url"
"[InternetShortcut]`nURL=https://${linuxContaiinerAppServiceUrl}" | Out-File -FilePath ".\Link-LinuxContainerAppService.url"
"[InternetShortcut]`nURL=http://${containerInstanceHostName}:8080" | Out-File -FilePath ".\Link-ContainerInstance.url"
"[InternetShortcut]`nURL=https://${containerAppHostName}" | Out-File -FilePath ".\Link-ContainerApp.url"
Write-Host "`n`n";



