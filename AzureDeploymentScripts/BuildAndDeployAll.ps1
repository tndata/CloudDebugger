# Run all the deployment scripts

Write-Host "Deploying CloudDebugger to Azure App Services (Linux, Windows, Linux container) , Container Instanes and Container Apps"

.\BuildDeploy-Linux-AppService.ps1

.\BuildDeploy-Windows-AppService.ps1

.\BuildAndPushDockerImage.ps1

.\BuildDeploy-Container-Linux-AppService.ps1

.\BuildDeploy-Container-Linux-ContainerInstance.ps1

.\BuildDeploy-Container-Linux-ContainerApps.ps1
