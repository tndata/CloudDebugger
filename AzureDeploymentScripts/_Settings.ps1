##########################################################################################
# Written by Tore Nestenius
# blog at https://www.nestenius.se
# Training and consulting services at https://www.tn-data.se
##########################################################################################
# General Azure deployment settings

# resource group name
$rgname = 'rg-CloudDebugger'

# location
$location = 'swedencentral'

# User-assigned managed identity name
$identityName = 'CloudDebugger'

# The name of the App Service Plans
$AppServicePlan_linux = 'asp-CloudDebugger-Linux'
$AppServicePlan_win = 'asp-CloudDebugger-Windows'

# The SKU of the App Service Plans
$AppServicePlanSKU_Linux = 'P0V3'     # S1 is not available in the region swedencentral?
$AppServicePlanSKU_Windows = 'S1'   # Standard plan 

# The name of the App Services
$AppServiceName_linux = 'as-CloudDebugger-Linux'
$AppServiceName_win = 'as-CloudDebugger-Windows'
$AppServiceName_container_linux = 'as-CloudDebugger-Linux-Container'


# Azure Container Registry name
$ACRName = 'tncontaineregistry'

# CloudDebugger container image name
$imageName = 'clouddebugger'

# Container App 
$containerAppName = "ca-clouddebugger"
$environmentName = "cae-CloudDebuggerAppsEnvironment"

# Container Instances
$containerInstanceName = "clouddebugger"

# Log Analytics workspace
$workspaceName = "log-CloudDebugger"