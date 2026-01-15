# This script will delete the specified resource group and all its content.

. .\_Settings.ps1


Write-Host "Deleting the user-assigned managed identity '${identityName}'."

az identity delete --name $identityName --resource-group $rgname

Write-Host "Deleting the resource group '${rgname}' and all its content. This might take a while."

az group delete --name $rgname



Write-Host "Resource group '${rgname}' deleted."
