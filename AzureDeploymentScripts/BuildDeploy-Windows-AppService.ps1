# Deploy files to App Service
# https://learn.microsoft.com/en-us/azure/app-service/deploy-zip

. .\_Settings.ps1

# Step 1: Build and publish the project for Windows
Write-Host "`n`nBuilding and publishing the project for Windows."
dotnet publish ../CloudDebugger.sln -r win-x64

if ($LASTEXITCODE -eq 0) {
    Write-Host "Publish succeeded."
} else {
    Write-Host "Publish failed with exit code $LASTEXITCODE. .NET 10 SDK not installed?" -ForegroundColor Red
    exit 1
}

# Step 2: Compress the published files
Write-Host "`nCompressing the published files."
$zipFolder = '../publish'
if (-not (Test-Path $zipFolder)) {
    New-Item -ItemType Directory -Path $zipFolder
}
Compress-Archive -Path ..\CloudDebugger\bin\Release\net10.0\win-x64\publish\* -Force  -DestinationPath "${zipFolder}/publish-windows.zip"
Write-Host "Zip file created at ${zipFolder}/publish-windows.zip"

# Step 3: Create the resource group
Write-Host "`nCreating resource group '${rgname}'."
$resGroup = az group create --name $rgname --location $location | ConvertFrom-Json
$resId = $resGroup.id
Write-Host "Resource group created with id: ${resId}"

# Step 4: Create a user-assigned managed identity
Write-Host "`nCreating a user-assigned managed identity '${identityName}'."
$identity = az identity create --name $identityName `
                   --resource-group $rgname `
                   --output json | ConvertFrom-Json
$identityId = $identity.id
$principalId = $identity.principalId
$clientId = $identity.clientId
Write-Host "User-assigned managed identity created"
Write-Host "id: ${identityId}"
Write-Host "PrincipalId: ${principalId}"
Write-Host "ClientId: ${clientId}"

# Step 5: Create the App Service Plan
Write-Host "`nCreating the Windows App Service Plan '${AppServicePlan_win}'."
$servicePlan = az appservice plan create --name $AppServicePlan_win `
                                           --resource-group $rgname `
                                           --sku $AppServicePlanSKU_Windows `
                                           --output json | ConvertFrom-Json
$planId = $servicePlan.id
Write-Host "Windows App Service Plan created with id: ${planId}"
                  
# Step 6: Create the App Service
Write-Host "`nCreating the Windows App Service '${AppServiceName_win}'."

$AppService = az webapp create --name $AppServiceName_win `
                               --plan $AppServicePlan_win `
                               -g $rgname `
                               --runtime 'dotnet:10' `
                               --assign-identity $identityId `
                               | ConvertFrom-Json
$appServiceID = $AppService.id
$hostName = $AppService.defaultHostName
Write-Host "App Service created, id: ${appServiceID}"

# Step 7: Set the AZURE_CLIENT_ID Environment variable (To get managed Identity to work inside the app)
Write-Host "`nSet the AZURE_CLIENT_ID environment variable/configuration."
$tmp = az webapp config appsettings set --name $AppServiceName_win `
	--resource-group $rgname `
	--settings AZURE_CLIENT_ID=$clientId `
	--output json | ConvertFrom-Json

# Step 8: Enable Application Logging (Filesystem)
Write-Host "`nEnabling application logging for the App Service."
$res2 = az webapp log config --name $AppServiceName_win `
                             --resource-group $rgname `
                             --application-logging filesystem `
                             --level verbose `
                             | ConvertFrom-Json

write-host "Set platform settings to 64 bits for Windows App Service (The App Service is by default 32 bit!)"
$tmp = az webapp config set --resource-group $rgname `
                     --name $AppServiceName_win `
                     --use-32bit-worker-process false

# Step 9: Deploy the ZIP file to the App Service
Write-Host "`nUploading the CloudDebugger ZIP file to the App Service"
$deployResult = az webapp deploy --resource-group $rgname `
                            --name $AppServiceName_win `
                            --type zip `
                            --clean true `
                            --restart true `
                            --src-path $zipFolder/publish-windows.zip `
                            | ConvertFrom-Json
                            

$deploymentId = $deployResult.id
$deploymentProvisioningState = $deployResult.provisioningState
Write-Host "App Service deployment completed with id: ${deploymentId}, provisioningState: ${deploymentProvisioningState}"

# Step 10: Assign the User-Assigned Identity to the Web App
Write-Host "`nAssigning the User-Assigned Identity to the Web App."
$tmp = az webapp identity assign --name $AppServiceName_win --resource-group $rgname --identities $identityId


# Step 11: Restart the App Service to apply the latest deployment
Write-Host "`nRestarting the App Service to apply the latest deployment."
az webapp restart `
    --name $AppServiceName_win `
    --resource-group $rgname

# Final Output
Write-Host "`n`nWindows App Service hostname: https://${hostName}`n`n" -ForegroundColor Green