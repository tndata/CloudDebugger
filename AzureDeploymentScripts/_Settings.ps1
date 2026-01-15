##########################################################################################
# Written by Tore Nestenius
# blog at https://www.nestenius.se
# Training and consulting services at https://www.tn-data.se
##########################################################################################
# General Azure deployment settings

. .\_Helpers.ps1

# resource group name
$rgname = 'rg-CloudDebugger'

# Azure region - alternatives: 'northeurope', 'westeurope', 'eastus', 'westus2', 'germanywestcentral'
$location = 'swedencentral'

# User-assigned managed identity name
$identityName = 'mi-CloudDebugger'

# The name of the App Service Plans
$AppServicePlan_linux = 'asp-CloudDebugger-Linux'
$AppServicePlan_win = 'asp-CloudDebugger-Windows'

# The SKU of the App Service Plans (B1, B2, B3, D1, F1, FREE, P0V3, P1MV3, S1, S2, S3, SHARED, ...)
# Note: Not all SKUs are available in all regions
$AppServicePlanSKU_Linux = 'P0V3'
$AppServicePlanSKU_Windows = 'S1' 

# The name of the App Services
$AppServiceName_linux = 'as-CloudDebugger-Linux'
$AppServiceName_win = 'as-CloudDebugger-Windows'
$AppServiceName_container_linux = 'as-CloudDebugger-Linux-Container'


# Generate a unique and stable Azure Container Registry name for this user/machine
$ACRName = GetUniqueRegistryName('tncontaineregistry')

# CloudDebugger container image name
$imageName = 'clouddebugger'

# Container App 
$containerAppName = "ca-clouddebugger"
$environmentName = "cae-CloudDebuggerAppsEnvironment"

# Container Instances
$containerInstanceName = "clouddebugger"

# Log Analytics workspace
$workspaceName = "log-CloudDebugger"




