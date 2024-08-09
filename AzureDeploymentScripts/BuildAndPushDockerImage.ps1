##########################################################################################
# Written by Tore Nestenius
# blog at https://www.nestenius.se
# Training and consulting services at https://www.tn-data.se
##########################################################################################

# This script will:
# 1. Create the resource group (if needed)
# 2. Create an Azure Container Registry (if needed)
# 3. Build the local CloudDebugger container image
# 4. Tag the container image
# 5. Push it to the container registry

. .\_Settings.ps1

# Step 1: Create the resource group
Write-Host "`n`nCreating resource group '${rgname}'."
$resgroup = az group create --name $rgname --location $location | ConvertFrom-Json
$resId = $resgroup.id
Write-Host "Resource group created, id: ${resId}"

# Step 2: Create Azure Container Registry
Write-Host "`n`nCreating Azure Container Registry named '${ACRName}'."
$acr = az acr create --resource-group $rgname --name $ACRName --sku Basic --admin-enabled true | ConvertFrom-Json
$acrid = $acr.id
Write-Host "Azure Container Registry created, id: ${acrid}"

# Step 3: Login to the Azure Container Registry
Write-Host "`n`nLogging into Azure Container Registry '${ACRName}'."
Write-Host "If this steps hangs, ensure Docker is running locally."
az acr login --name $ACRName

# Step 4: Build the local Docker image
$dockerfilePath = "../Dockerfile"
$contextPath = "../."
Write-Host "`n`nBuilding the local Docker image '${imageName}'."
docker build -t $imageName -f $dockerfilePath $contextPath --progress=plain

# Step 5: Tag the local Docker image with the registry server name of your ACR
$taggedImage = "${ACRName}.azurecr.io/${imageName}:latest"
Write-Host "`n`nTagging the Docker image '${imageName}' with '${taggedImage}'."
docker tag $imageName $taggedImage

# Step 6: Push the local image to Azure Container Registry
Write-Host "`n`nPushing the Docker image '${taggedImage}' to Azure Container Registry."
docker push $taggedImage

# Final Output
Write-Host "`n`nYou can now use this image and push it to your container hosting service."
Write-Host "${taggedImage}`n`n" -ForegroundColor Green
