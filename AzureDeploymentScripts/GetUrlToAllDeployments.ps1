# This script will list all the hostnames

. .\_Settings.ps1



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
$hostName = $containerInstance.ipAddress.fqdn
Write-Host "Container Instance hostname:           http://${hostName}:8080"


$containerApp = az containerapp show --name $containerAppName --resource-group $rgname --output json | ConvertFrom-Json
$hostName = $containerApp.properties.configuration.ingress.fqdn
Write-Host "Container App hostname:                https://${hostName}"