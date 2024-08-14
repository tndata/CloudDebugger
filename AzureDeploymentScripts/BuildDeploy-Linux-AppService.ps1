# Deploy files to App Service
# https://learn.microsoft.com/en-us/azure/app-service/deploy-zip

. .\_Settings.ps1

# Step 1: Build and publish the project for Linux
Write-Host "`n`nBuilding and publishing the project for Linux."
dotnet publish ../CloudDebugger.sln -r linux-x64

# Step 2: Compress the published files
Write-Host "`nCompressing the published files."
$zipFolder = '../publish'
if (-not (Test-Path $zipFolder)) {
    New-Item -ItemType Directory -Path $zipFolder
}
Compress-Archive -Path ..\CloudDebugger\bin\Release\net8.0\linux-x64\publish\* -Force  -DestinationPath "${zipFolder}/publish-linux.zip"
Write-Host "Zip file created at ${zipFolder}/publish-linux.zip"

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
Write-Host "User-assigned managed identity created"
Write-Host "id: ${identityId}"
Write-Host "PrincipalId: ${principalId}"

# Step 5: Create the Linux App Service Plan
Write-Host "`nCreating the Linux App Service Plan '${AppServicePlan_linux}'."
$servicePlan = az appservice plan create --name $AppServicePlan_linux `
                                         --resource-group $rgname `
                                         --is-linux `
                                         --sku $AppServicePlanSKU_Linux `
                                         --output json | ConvertFrom-Json
$planId = $servicePlan.id
Write-Host "App Service Plan created with id: ${planId}"

#Step 6: Create the App Service
Write-Host "`nCreating the Linux App Service '${AppServiceName_linux}'."
$AppService = az webapp create --name $AppServiceName_linux `
                                          --plan $AppServicePlan_linux `
                                          --resource-group $rgname `
                                          --runtime 'DOTNETCORE:8.0' `
                                          --assign-identity $identityId `
                                          --output json | ConvertFrom-Json
# $appServiceID = $AppService.id
# $hostName = $AppService.defaultHostName
# Write-Host "App Service created, id: ${appServiceID}"

# # We use the New-AzWebApp cmdlet to create the App Service instead of 'az webapp create'.
# # We do this to avoid odd timeouts during the .ZIP deployment that we have experienced with 'az webapp create'.
# $AppService = New-AzWebApp -ResourceGroupName $rgName `
             # -Name $AppServiceName_linux `
             # -Location $location `
             # -AppServicePlan $AppServicePlan_linux
             
# Write-Host "Set the stack to .NET Core 8.0" 
# $tmp1 = az webapp config set --resource-group $rgname `
                     # --name $AppServiceName_linux `
                     # --use-32bit-worker-process false `
                     # --linux-fx-version 'DOTNETCORE"|"8.0'
                     
Write-Host "Get App Service details"                    
$AppService = Get-AzWebApp -ResourceGroupName $rgname -Name $AppServiceName_linux 
$appServiceID = $AppService.id
$hostName = $AppService.DefaultHostName
Write-Host "App Service created, id: ${appServiceID}"

# Step 7: Enable Application Logging (Filesystem)
Write-Host "`nEnabling application logging for the App Service."
$res1 = az webapp log config --name $AppServiceName_linux `
                             --resource-group $rgname `
                             --application-logging filesystem `
                             --level verbose `
                             --output json | ConvertFrom-Json


# Step 8: Deploy the ZIP file to the App Service
Write-Host "`nUpload and deploy CloudDebugger App Service (Zip deployment)"
$deployResult = az webapp deploy --resource-group $rgname `
                                 --name $AppServiceName_linux `
                                 --type zip `
                                 --src-path "${zipFolder}/publish-linux.zip" `
                                 --output json | ConvertFrom-Json

Write-Host "Application uploaded, waiting for deployment to complete, status=${deploymentStatus}"

# Step 9: Assign the User-Assigned Identity to the Web App
Write-Host "`nAssigning the User-Assigned Identity to the Web App."
$tmp1 = az webapp identity assign --name $AppServiceName_linux --resource-group $rgname --identities $identityId

# Final Output
Write-Host "`n`nLinux App Service hostname: https://${hostName}`n`n" -ForegroundColor Green