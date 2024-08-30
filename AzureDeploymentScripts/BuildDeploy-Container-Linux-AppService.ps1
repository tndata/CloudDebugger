##########################################################################################
# Written by Tore Nestenius
# blog at https://www.nestenius.se
# Training and consulting services at https://www.tn-data.se
##########################################################################################
# Deploy CloudDebugger to Azure App Services as a Linux Container
# see https://learn.microsoft.com/en-us/azure/app-service/tutorial-custom-container
# https://github.com/Azure/app-service-windows-containers/blob/master/HowTo/use_user-assigned_managed_identities.md 
# https://raw.githubusercontent.com/mkmibrahim/AZ204/adc4e007a93784dc5ecda7b827bfaf0e18b9716d/Demo/backend/scripts/Azure_DeployContainerCityImages.ps1
# https://learn.microsoft.com/en-us/azure/app-service/configure-custom-container
#
# See my App Service deployment blog posts on https://www.nestenius.se 

. .\_Settings.ps1

# Step 1: Create managed identity
Write-Host "`nCreating a managed identity."
$identity = az identity create `
        --name $identityName `
        --resource-group $rgname `
        --output json | ConvertFrom-Json
$identityId = $identity.id
$principalId = $identity.principalId
$clientId = $identity.clientId
Write-Host "User-assigned managed identity created."
Write-Host "name: ${identityName}"
Write-Host "id: ${identityId}"
Write-Host "PrincipalId: ${principalId}"
Write-Host "ClientId: ${clientId}"

# Step 3: Create the Linux App Service Plan
Write-Host "`nCreating a Linux App Service Plan named '${AppServicePlan_linux}'."
$servicePlan = az appservice plan create `
    --name $AppServicePlan_linux `
    --resource-group $rgname `
    --is-linux `
    --sku $AppServicePlanSKU_Linux `
    --output json | ConvertFrom-Json
$planId = $servicePlan.id
Write-Host "App Service Plan created with id: ${planId}"


Write-Host "`nWaiting 15s to ensure the identity is fully registered and propagated in Azure AD..."
# The AcrPull assignment might otherwise fail.
Start-Sleep -Seconds 15


# We do this after creating the App Service Plan to give the identity some time to propagate in the system.
# Step 3: Query for the Azure Container Registry ID
Write-Host "`n`nQuerying for the container registry ID"
$containerRegistry = az acr show `
    --name $acrName `
    --resource-group $rgname `
    --output json | ConvertFrom-Json
$acrId = $containerRegistry.id
Write-Host "Azure Container Registry ID: ${acrid}"


# Step 4: Assign the AcrPull role to the managed identity on the ACR
Write-Host "`n`nAssigning AcrPull role to the managed identity on the container registry."
$role = az role assignment create `
        --assignee $principalId `
        --role "AcrPull" `
        --scope $acrid `
        --output json `
        --output json | ConvertFrom-Json

# Step 5: Create App Service and deploy the default .NET 8 runtime, we deploy the container at the end of the script.
Write-Host "`n`nCreating the App Service with the default runtime 'DOTNETCORE:8.0'."
$AppService = az webapp create `
    --name $AppServiceName_container_linux `
    --acr-use-identity `
    --plan $AppServicePlan_linux `
    --resource-group $rgname `
    --runtime 'DOTNETCORE:8.0' `
    --assign-identity $identityId `
    --output json | ConvertFrom-Json
$hostName = $AppService.defaultHostName
$appServiceID = $AppService.id
Write-Host "App Service created, id: ${appServiceID}"

# Step 6: Set the AZURE_CLIENT_ID Environment variable (To get managed Identity to work inside the app)
Write-Host "`nSet the AZURE_CLIENT_ID environment variable/configuration."
$tmp = az webapp config appsettings set --name $AppServiceName_container_linux `
	--resource-group $rgname `
	--settings AZURE_CLIENT_ID=$clientId `
	--output json | ConvertFrom-Json


# Step 7: Set the AcrUserManagedIdentityID using  
Write-Host "`nSet the identity in the App Service for accessing the ACR."
$data="{\""acrUserManagedIdentityID\"": \""${clientId}\""}"
$tmp = az webapp config set `
    --resource-group $rgname `
    --name $AppServiceName_container_linux `
    --generic-configurations $data `
    --output json | ConvertFrom-Json


# Step 8: Verify the ACR access settings  
$settings = az webapp config show `
    --resource-group $rgname `
    --name $AppServiceName_container_linux `
    --output json | ConvertFrom-Json
Write-Host "`nThese two settings must be set for successful ACR pull:"
Write-Host "acrUseManagedIdentityCreds='$($settings.acrUseManagedIdentityCreds)'"
Write-Host "acrUserManagedIdentityID='$($settings.acrUserManagedIdentityId)'"


# Step 9: Enable Application Logging (Filesystem)
Write-Host "`nEnabling application logging for the Container App Service."
$tmp = az webapp log config `
    --name $AppServiceName_container_linux `
    --resource-group $rgname `
    --application-logging filesystem `
    --docker-container-logging filesystem `
    --level verbose `
    --output json | ConvertFrom-Json
                             

#  Step 10: Now, when everything is setup correctly, we can switch to use the container image instead.
$imagePath = "${acrname}.azurecr.io/${imagename}:latest"
Write-Host "`n`nChange the service to use the container ${imagePath}."
az webapp config container set `
  --name $AppServiceName_container_linux `
  --resource-group $rgname `
  --container-image-name $imagePath `
  --output json | ConvertFrom-Json


# Final Output
Write-Host "`n`nContainer image deployed to Azure App Service Linux Container." -ForegroundColor Green
Write-Host "Container hostname: https://${hostName}" -ForegroundColor Green

Write-Host "`n`nFirst deployment might take up to 5 minutes..."

# # enable log stream for debugging
# az webapp log tail --name $AppServiceName_container_linux --resource-group $rgname