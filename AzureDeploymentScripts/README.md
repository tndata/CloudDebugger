# CloudDebugger Deployment Scripts

This folder contains various PowerShell scripts for deploying CloudDebugger across different Azure services.

## Getting Started

### Prerequisites

1. Ensure you have the Azure CLI installed and logged in.
2. Review and configure the settings in the **_Settings.ps1** script.

### How to Deploy

Choose one of the following scripts based on your deployment target:

#### 1. **Azure App Service - Linux**
Deploy CloudDebugger to a Linux App Service on a Linux App Service Plan.  
 
1. Run **BuildDeploy-Container-Linux-AppService.ps1**.

    This script will:
    - Build and publish the application locally, creating a .ZIP file.
    - Create an Azure Resource Group.
    - Create a user-assigned managed identity.
    - Create a Linux App Service Plan.
    - Create a Linux App Service.
    - Deploy the ZIP file to the App Service.


#### 2. **Azure App Service - Windows**

Deploy CloudDebugger to a Windows App Service on a Windows App Service Plan.

1. Run **BuildDeploy-Windows-AppService.ps1**.

    This script will:
    - Build and publish the application locally, creating a .ZIP file.
    - Create an Azure Resource Group.
    - Create a user-assigned managed identity.
    - Create a Windows App Service Plan.
    - Create a Windows App Service.
    - Deploy the ZIP file to the App Service.


#### 3. **Container Deployments**

To deploy containers, first run the following create an Azure Container Registry (ACR) and push your Docker image to it.

1. Run **BuildAndPushDockerImage.ps1**.

    This script will:
    - Create an Azure Resource Group.
    - Create an Azure Container Registry.
    - Log in to the Azure Container Registry.
    - Build the local Docker image.
    - Tag the Docker image with the ACR registry server name.
    - Push the Docker image to Azure Container Registry.

### How to Deploy Containers

After pushing the Docker image, use the following scripts to deploy the container:



### 4. **Azure App Service - Linux Container**

Deploy CloudDebugger to a Linux Container on a Linux App Service Plan.

1. Run **BuildDeploy-Container-Linux-AppService.ps1**.

    This script will:
    - Create a user-assigned managed identity.
    - Create a Linux App Service Plan.
    - Assign the AcrPull role to the managed identity on the container registry.
    - Create an Azure App Service and deploy the container image.

#### 5. **Azure Container Instances**

Deploy CloudDebugger to a Linux Container on Azure Container Instances.

1. Run **BuildDeploy-Container-Linux-ContainerInstance.ps1**.

    This script will:
    - Create a user-assigned managed identity.
    - Assign the AcrPull role to the managed identity on the container registry.
    - Create a Log Analytics Workspace
    - Create an Azure Container Instance with the CloudDebugger image.

    **Important:** Access is available on port 8080 using `http://` (not `https://`).

#### 6. **Azure Container Apps**

Deploy CloudDebugger to a Linux Container on Azure Container Apps.

1. Run **BuildDeploy-Container-Linux-ContainerApps.ps1**.

    This script will:
    - Create a user-assigned managed identity.
    - Assign the AcrPull role to the managed identity on the container registry.
    - Create a Log Analytics workspace.
    - Create a Container Apps environment.
    - Create an Azure Container App and deploy the CloudDebugger image.


### How to Tear Down

To delete all resources, run **DeleteAllResources.ps1**. This script will delete the resource group and all associated resources.

## Notes

- Each script can be executed multiple times without adverse side effects.
- All resources created by the deployment scripts are contained within the same resource group specified in **_Settings.ps1**.
