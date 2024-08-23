# Update containers in Azure Container Instances
# https://learn.microsoft.com/en-us/azure/container-instances/container-instances-update
# Deploy to Azure Container Instances from Azure Container Registry using a managed identity
# https://learn.microsoft.com/en-us/azure/container-instances/using-azure-container-registry-mi
#
# Important:
# * Windows containers don't support system-assigned managed identity-authenticated image pulls with ACR, only user-assigned.

. .\_Settings.ps1

# Step 1: Create a user-assigned managed identity
Write-Host "`nCreating a user-assigned managed identity '${identityName}'."
$identity = az identity create --name $identityName `
                   --resource-group $rgname `
                   --output json | ConvertFrom-Json
$identityId = $identity.id
$principalId = $identity.principalId
Write-Host "User-assigned managed identity created"
Write-Host "id: ${identityId}"
Write-Host "PrincipalId: ${principalId}"

# Step 2: Get the Azure Container Registry ID
Write-Host "`n`nRetrieving the Azure Container Registry ID"
$containerRegistry = az acr show --name $acrname --output json | ConvertFrom-Json
$acrid = $containerRegistry.id
Write-Host "Azure Container Registry ID: ${acrid}"

# Step 3: Assign the AcrPull role to the managed identity on the ACR
Write-Host "`n`nAssigning AcrPull role to the managed identity on the container registry."
$role = az role assignment create --assignee $principalId --role "AcrPull" --scope $acrid --output json | ConvertFrom-Json


# Step 4: Create a Log Analytics workspace
Write-Host "`n`nCreating a Log Analytics workspace named '${workspaceName}'"
$logAnalyticsWorkspace = az monitor log-analytics workspace create `
                            --resource-group $rgname `
                            --workspace-name $workspaceName `
                            --location $location `
                            --output json | ConvertFrom-Json
$workspaceId = $logAnalyticsWorkspace.customerId
Write-Host "Log Analytics workspace created with id ${workspaceId}"

# Step 5: Get Log Analytics workspace keys
Write-Host "`n`nGet log analytics workspace keys"
$workspaceKeys = az monitor log-analytics workspace get-shared-keys `
                         --resource-group $rgname `
                         --workspace-name $workspaceName `
                         --output json | ConvertFrom-Json
$workspaceKey = $workspaceKeys.primarySharedKey

# Step 6: Create Azure Container Instance
Write-Host "`n`nCreating Azure Container Instance"
$container = az container create --resource-group $rgname `
                                --name $containerInstanceName `
                                --image "${acrname}.azurecr.io/${imagename}:latest" `
                                --cpu 0.5 `
                                --memory 0.5 `
                                --ports 8080 `
                                --dns-name-label $containerInstanceName `
                                --assign-identity $identityId `
                                --acr-identity $identityId `
                                --log-analytics-workspace $workspaceId `
                                --log-analytics-workspace-key $workspaceKey `
                                --output json | ConvertFrom-Json
$containerId = $container.id
$hostName = $container.ipAddress.fqdn

# Final Output
Write-Host "`n`nCloudDebugger container image deployed to Azure Container Instance." -ForegroundColor Green
Write-Host "Hostname: http://${hostName}:8080" -ForegroundColor Green
