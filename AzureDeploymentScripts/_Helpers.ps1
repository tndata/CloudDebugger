
#--------------------------------------------
# Generates a globally unique and stable Azure Container Registry name based on the current Azure user and local machine name.
# Registry names must be globally unique across Azure, 3–50 characters, and contain only lowercase letters and numbers.
# This function ensures that each user gets a unique name that stays the same across runs.
function GetUniqueRegistryName($prefix) {
    # Get Azure username (UPN)
    $azureAccount = az account show | ConvertFrom-Json
    $upn = $azureAccount.user.name

    # Get computer name
    $computerName = $env:COMPUTERNAME

    # Combine to create input for hashing
    $baseString = "$upn-$computerName"

    # Create short hash (8 characters from SHA1)
    $hash = [System.BitConverter]::ToString(
        (New-Object System.Security.Cryptography.SHA1Managed).ComputeHash(
            [System.Text.Encoding]::UTF8.GetBytes($baseString)
        )
    ).Replace("-", "").Substring(0, 8).ToLower()

    # Build final registry name (prefix + hash)
    $registryName = ($prefix + $hash).ToLower()

    # ACR name must be 3–50 characters, alphanumeric only
    if ($registryName.Length -gt 50) {
        $registryName = $registryName.Substring(0, 50)
    }

    return $registryName
}