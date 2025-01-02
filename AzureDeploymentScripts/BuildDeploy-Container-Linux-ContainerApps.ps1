# Deploy CloudDebugger as an Azure Container Apps

. .\_Settings.ps1


# Step 1: Create the resource group
Write-Host "`nCreating resource group '${rgname}'."
$resGroup = az group create --name $rgname --location $location | ConvertFrom-Json
$resId = $resGroup.id
Write-Host "Resource group created with id: ${resId}"

# Step 2: Create a user-assigned managed identity
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

# Step 3: Get the Azure Container Registry ID
Write-Host "`n`nRetrieving the Azure Container Registry ID"
$containerRegistry = az acr show --name $acrname --output json | ConvertFrom-Json
$acrid = $containerRegistry.id
Write-Host "Azure Container Registry ID: ${acrid}"

# Step 4: Assign the AcrPull role to the managed identity on the ACR
Write-Host "`n`nAssigning AcrPull role to the managed identity on the container registry."
$roleAssignment = az role assignment create --assignee $principalId --role "AcrPull" --scope $acrid --output json | ConvertFrom-Json

# Step 5: Create a Log Analytics workspace
Write-Host "`n`nCreating a Log Analytics workspace named '${workspaceName}'"
$logAnalyticsWorkspace = az monitor log-analytics workspace create `
                            --resource-group $rgname `
                            --workspace-name $workspaceName `
                            --location $location `
                            --output json | ConvertFrom-Json
$workspaceId = $logAnalyticsWorkspace.customerId
Write-Host "Log Analytics workspace created with id ${workspaceId}"

# Step 6: Get Log Analytics workspace keys
Write-Host "`nRetrieving Log Analytics workspace keys"
$workspaceKeys = az monitor log-analytics workspace get-shared-keys `
                         --resource-group $rgname `
                         --workspace-name $workspaceName `
                         --output json | ConvertFrom-Json
$workspaceKey = $workspaceKeys.primarySharedKey

# Step 7: Create a Container Apps environment
Write-Host "`nCreating a Container Apps environment named '${environmentName}'"
$containerEnv = az containerapp env create `
                         --name $environmentName  `
                         --resource-group $rgname `
                         --location $location `
                         --logs-workspace-id $workspaceId `
                         --logs-workspace-key $workspaceKey `
                         --output json | ConvertFrom-Json
Write-Host "Container Apps Environment created"

# Step 8: Create Azure Container App for CloudDebugger
# Set DEPLOY_TRIGGER to a random value to force a redeployment
Write-Host "`nCreating Azure Container App for CloudDebugger"
$randomValue = [guid]::NewGuid().ToString()
$container = az containerapp create `
    --name $containerAppName `
    --environment $environmentName `
    --resource-group $rgname `
    --user-assigned $identityId `
    --registry-identity $identityId `
    --registry-server "${acrname}.azurecr.io" `
    --image "${acrname}.azurecr.io/${imagename}:latest" `
    --target-port 8080 `
    --ingress external `
    --cpu 0.25 `
    --memory 0.5 `
    --min-replicas 1 `
    --max-replicas 1 `
    --env-vars AZURE_CLIENT_ID=$clientId DEPLOY_TRIGGER=$randomValue `
    --output json | ConvertFrom-Json
$containerId = $container.id

# Step 9: Get the container details to obtain the FQDN
Write-Host "`nRetrieving Azure Container App host name"
$containerDetails = az containerapp show --name $containerAppName --resource-group $rgname --output json | ConvertFrom-Json
$hostName = $containerDetails.properties.configuration.ingress.fqdn

Write-Host "`n$containerDetails.properties.configuration.ingress"
$containerDetails.properties.configuration.ingress

# Final Output
write-host "`n`nCloudDebugger container image deployed to Azure Container Apps.`n" -ForegroundColor Green
write-host "Hostname: https://${hostName}" -ForegroundColor Green
