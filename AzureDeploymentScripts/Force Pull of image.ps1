# This script will delete the specified resource group and all its content.

. .\_Settings.ps1


$imagePath = "${acrname}.azurecr.io/${imagename}:latest"

az webapp config container set --name $AppServiceName_container_linux --resource-group $rgname --container-image-name $imagePath 
