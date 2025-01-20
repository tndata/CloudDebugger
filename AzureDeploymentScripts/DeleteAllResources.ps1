# This script will delete the specified resource group and all its content.

. .\_Settings.ps1


write-host "Deleting the user-assigned managed identity '${identityName}'."

az identity delete --name $identityName --resource-group $rgname

write-host "Deleting the resource group '${rgname}' and all its content. This might take a while."

az group delete --name $rgname



write-host "Resource group '${rgname}' deleted."
