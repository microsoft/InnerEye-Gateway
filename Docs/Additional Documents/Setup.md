# Getting started 
Here is a page with intros to DICOM and subject domain: https://dev.azure.com/msdsip/AshInnerEye/_wiki/wikis/AshInnerEye.wiki/24/Start-Here

# Environment

**Stack Hub environment portal**: https://portal.ppe2.stackpoc.com/#@avanadestackpoc.onmicrosoft.com
Resource group used for demo: **rgcnabgroup**

# Chapter 1: Resource deployment
When it comes to resources deployment on Azure Environment we mostly think about using ARM templates, which is the good thing which stack hub also supports. We can deploy our resources using ARM template. In this setup we are taking help of [CNAB](https://cnab.io/) to bundle our infrastructure and deploy it on Azure Stack Hub.

# Prerequisites

- Azure Stack Hub subscription

- Docker (Here is a link if you need to install [Docker Installation Instructions](https://docs.docker.com/get-docker/) )

- Porter (Here is a link if you need to install: Porter Installation Instructions [[Porter Installation Instructions]](https://porter.sh/install/)) 

  > **NOTE:** be sure to add porter to your PATH

- Service Principal that has been granted contributor access to your Azure Stack Hub subscription

  - You will need the following information for the service principal
    - Client ID
    - Client secret
    - Object ID (this is different than the object id and can be found on the enterprise application area of your Azure Active Directory)
    - Tenant ID

- Your user account needs to have owner access to the subscription. (This is needed to assign access to the service principal for deployment)

# Step 1: Prepare for Installation

### Create CNAB Parameter File

Locate the file named `azure-stack-profile.template.txt` and open it for editing. You will need to provide some values so the CNAB package can register your Azure Stack environment and deploy into it. Save the file as `azure-stack-profile.txt` after you have assigned the required values.

```
azure_stack_tenant_arm="Your Azure Stack Tenant Endpoint"
azure_stack_storage_suffix="Your Azure Stack Storage Suffix"
azure_stack_keyvault_suffix="Your Azure Stack KeyVault Suffix"
azure_stack_location="Your Azure Stack’s location identifier here."
azure_stack_resource_group="Your desired Azure Stack resource group name to create"
slicer_ip="IP address for your clinical endpoint for receiving DICOM RT"
```

### Generate Credentials

Open a new shell window and make sure you are in the root directory of this repo. Run the command below to generate credentials required for deployment. Follow the prompts to assign values for the credentials needed. Select "specific value" from the interactive menu for each of the required credential fields. A description of each credential is provided below.

```
porter generate credentials
```

| Item                        | Description                                                  |
| :-------------------------- | :----------------------------------------------------------- |
| AZURE_STACK_SP_CLIENT_ID    | The client id for the service principal that is registered with your Azure Stack Hub Subscription |
| AZURE_STACK_SP_PASSWORD     | The secret associated with the service principal that is registered with your Azure Stack Hub Subscription |
| AZURE_STACK_SP_TENANT_DNS   | The dns for the Azure Active Directory that is tied to your Azure Stack Hub (e.g. [mycomany.onmicrosoft.com](http://mycomany.onmicrosoft.com/) ) |
| AZURE_STACK_SUBSCRIPTION_ID | The subscription id for the subscription on your Azure Stack Hub that you want to deploy into |
| VM_PASSWORD                 | The password you would like to use for the login to the VM that is deployed as part of this CNAB package |

# Step 2: Build CNAB

Run the command below to build the Porter CNAB package.

```
porter build
```

# Step 3: Install CNAB

### Install CNAB Package

Run the below command to install the CNAB package. This will create a new resource group on you Azure Stack subscription and will deploy the solution into it.

```
porter install InnerEye --cred InnerEye --param-file "azure-stack-profile.txt"
```

### (Optional) Uninstall CNAB Package

If you wish to remove the solution from your Azure Stack Hub, run the below command. Please note that this will delete the entire resource group that the solution was deployed into. If you have created any other custom resources in this resource group, they will also be deleted.

```
porter uninstall InnerEye --cred InnerEye --param-file "azure-stack-profile.txt"
```

# Step 4: Start Inferencing Container(s)

- Get the IP of the Inferencing Container VM from the Azure Stack Hub Portal
- Connect to the VM via ssh
- Navigate to the app directory
- Make any necessary modifications to the model_inference_config.json file
- Start the containers by running the below commands

```
python setup-inferencing.py model_inference_config.json
```

# Step 5: Start the Gateway

- Get the IP of the Inferencing Container VM from the Azure Stack Hub Portal
- Connect to the VM via Remote Desktop Protocol (RDP)
- Open the gateway.msi file on the desktop

## Summary of Deployment Components

- KeyVault and grants read access to Service Principal
- Storage Account
- GPU Linux VM to host inferencing containers
- App service plan to host Inferencing API and Inferencing Engine
- Inferencing API app service
- Inferencing Engine app service
- Gateway VM

# Chapter 2: Building and deploying the code
We can always use Azure DevOps CICD pipelines to build and deploy the code on Infrastructure created. In this chapter we will talk about how to build and deploy code using local environment.

# Prerequisites

- Cloned git repositories.
- Visual Studio 2017
- Azure Stack Hub Storage Account Details
- Gateway VM Details 
- Inferencing container Details

### Clone the repos

To clone the code to local environment please follow below steps:

1. First you need to clone the InnerEye Cloud Solution.

```
git clone https://msdsip@dev.azure.com/msdsip/AshInnerEye/_git/AshInnerEye
```

2. After cloning the InnerEye Cloud Solution, second repository to clone is Gateway.

```
git clone https://msdsip@dev.azure.com/msdsip/AshInnerEye/_git/Gateway
```

- > **NOTE:** Make sure you clone both the repos at root folder of any directory, this is to avoid max-length path issue.

### Building the solutions

#### Building InnerEye Cloud Solution

1. Open Visual Studio 2017

2. Open InnerEye Cloud.sln to open InnerEye Cloud Solution from the repo cloned.

3. Once opened, set the solution configuration to x64.

4. Open Web.config for Microsoft.InnerEye.Azure.Segmentation.API.AppService

5. Update the following app settings

   ```
   <appSettings>
       <add key="webpages:Version" value="3.0.0.0" />
       <add key="webpages:Enabled" value="false" />
       <add key="ClientValidationEnabled" value="true" />
       <add key="UnobtrusiveJavaScriptEnabled" value="true" />
       <add key="AccountName" value="" />
       <add key="StorageConnectionString" value="" />
    </appSettings>
   ```

   Here:

   1. AccoutName is storage Account Name of Azure Stack Hub
   2. StorageConnectionString is Connection string of Azure Stack Hub storage account.

6. Once this is updated. once the Web.Config for Microsoft.InnerEye.Azure.Segmentation.Worker.AppService

   ```
   <appSettings>
       <add key="webpages:Version" value="3.0.0.0" />
       <add key="webpages:Enabled" value="false" />
       <add key="ClientValidationEnabled" value="true" />
       <add key="UnobtrusiveJavaScriptEnabled" value="true" />
       <add key="InferencingContainerEndpoint" value="" />
       <add key="InferencingContainerEndpointPort" value="" />
       <add key="AccountName" value="" />
       <add key="StorageConnectionString" value="" />
     </appSettings>
   ```

   Here:

   	1. AccoutName is storage Account Name of Azure Stack Hub
   	2. StorageConnectionString is Connection string of Azure Stack Hub storage account.
    	3. InferencingContainerEndpoint is IP address of Inferencing container VM
    	4. InferencingContainerEndpointPort is port number where Inferencing container is hosted.

7. When both Web.config files are ready build the solution from the build menu of Visual Studio.

#### Building Gateway Solution

1. Open the new instance of Visual Studio 2017
2. Open Microsoft.Gateway.sln from the Gateway repo cloned.
3. Modify the InnerEyeSegementationClient.cs to add inferencing API endpoint.
4. Set the project configuration to x64
5. Build the solution from the build menu of Visual Studio.

### Deploying the solutions

#### Deploying InnerEye Cloud Solution

1. Either you can download Publish Profile of deployed Inferencing API or you can also create new one using Visual Studio Publish option.
2. To download publish profile go to Azure Stack Hub portal and open Inferencing API resource.
3. Click on Get Publish profile button from overview page.
4. Once downloaded switch to Visual Studio window which has InnerEye Cloud solution opened.
5. Right click on Microsoft.InnerEye.Azure.Segmentation.API.AppService and select Publish.
6. Import the downloaded publish profile.
7. Set the release configuration to x64 and click publish.
8. This will deploy the Microsoft.InnerEye.Azure.Segmentation.API.AppService to hub.
9. Now switch to browser window and open Inferencing Engine App Service.
10. Once open go to overview page and download the publish profile by clicking on Get Publish Profile button.
11. Once downloaded right click on Microsoft.InnerEye.Azure.Segmentation.Worker.AppService and click publish.
12. Import the downloaded publish profile.
13. Set the release configuration to x64 and click Publish.
14. This will publish Microsoft.InnerEye.Azure.Segmentation.Worker.AppService

#### Deploying Gateway Solution

1. Gateway needs to run as Windows Service but you can also run executables of Gateway solution independently.
2. To run gateway from installer copy the build contents from Gateway solution to the Virtual Machine dedicated for Gateway.
3. Search and open Microsoft.InnerEye.Gateway.msi
4. This will install Gateway as Windows Service in virtual machine.
5. If you do not want to run Gateway Services as Windows Service then
6. From the build contents go to Microsoft.InnerEye.Listener.Processor and look for executable.
7. Open the executable file.
8. Go to Microsoft.InnerEye.Listener.Receiver and look for executable.
9. Open the executable.

# Chapter 3: Testing and Configuring Deployed Environment

Below are the instructions to debug and monitored resources after the solution has been deployed below.

## VM: Inferencing container (Linux)

Once the machine is running, ensure that the _headandneckmodel_ container is running by running `docker ps` and ensuring you have the container "_headandneckmodel_" up. If everything is up you should be able to navigate to http://38.102.181.60:8081/ and see output there. 

If the container is not running or needs to be restarted, run `docker kill` and then do the following: 

1. Run `docker run -it --entrypoint=/bin/bash -p 8081:5000 -e AZURE_STORAGE_ACCOUNT_NAME=<ACCOUNT_NAME> -e AZURE_STORAGE_KEY=<ACCOUNT_KEY> -e AZURE_STORAGE_ENDPOINT=ppe2.stackpoc.com --gpus all headandneckmodel:latest`
2. Run `conda activate nnenv`
3. Run `python web-api.py` - this should launch the web server that is wrapping the inferencing container

If the container is already running:
* Use `docker logs <container ID>` to retrieve logs from the container shell
* Use `docker attach <container ID>` to connect to the interactive shell of the inferencing container

## Inferencing Engine AppService
Runs model invoker. Makes calls to the model server. Converts from DICOM to NIFTI.

In sample environment runs on: app-inferapi

## Inferencing API AppService
Runs API, kicks off model inferencing via communication with the inferencing engine. 

In sample environment runs on: infereng

## Gateway VM (Windows)

Gateway should be launched like so:
1. **GatewayProcessor** (C:\Users\azureuser\source\repos\Gateway\Source\Microsoft.Gateway\Microsoft.InnerEye.Listener.Processor\bin\x64\Debug\Microsoft.InnerEye.Listener.Processor.exe)
2. **GatewayReceiver** (C:\Users\azureuser\source\repos\Gateway\Source\Microsoft.Gateway\Microsoft.InnerEye.Listener.Receiver\bin\x64\Debug\Microsoft.InnerEye.Listener.Receiver.exe)

Before launching make sure that the API App Service is running as the Gateway checks its health first and wouldn't start if it can't find the app service.

Watch startup logs to make sure there are no errors. Note that the inferencing container should have its server running before the segmentation processor and API are launched.

### Gateway Configuration 

Gateway configuration files are located in: C:\Users\azureuser\source\repos\Gateway\Source\Microsoft.Gateway\SampleConfigurations
**TODO** Server URL needs to be configured. For now hardcoded in InnerEyeSegmentationClient.cs, EnvironmentBaseAddress

## Storage account
**Account name**: devstoreaccount1

### Containers
Used to store segmentations coming in (converted to NIFTI) and out (converted to NIFTI). One container is created per segmentation request

#### Tables

| Name              | Purpose                                                      |
| ----------------- | ------------------------------------------------------------ |
| gatewayversions   | not in use                                                   |
| imagesindexes     | not in use                                                   |
| models            | not in use (will be used after model configuration is read from the AML package) |
| progresstable     | contains progress per segmentation, updated by cloud code. PartitionKey - model ID; RowKey - segmentation ID |
| rtfeedbackindexes | not in use                                                   |
| rtindexes         | ??                                                           |
| TimingsTable      | Used to compute progress by storing average time of multiple model runs |

## InnerEye app 

If using InnerEye app, below are some tips on configuration.

App launched from shortcut on the desktop (Microsoft InnerEye Dev). App configuration is stored in _%APPDATA%\Microsoft\InnerEye\MicrosoftInnerEyeWPFDevexeSettings.json_. 

Relevant configuration parameters are:
* DcmReceiveFolder - where the received rtstruct or images are stored
* UserDefinedPushLocations - configured send destinations (also can be configured through the app)
* DcmAeTitle - AET of the DICOM server that app is running
* DcmPort - port for the app's DICOM server

You can check that the app can talk to the gateway by going to app setting (gear icon in the bottom left) and pressing "Echo" next to the endpoint corresponding to the gateway. If successful, the gateway should show success message in the notification bar and the gateway receiver console log should show informational log messages as well.

Test H&N volume is located in _C:\Users\azureuser\Test Images\headandneck_

# Chapter 4. Running the Demo

In order to run the demo, ensure that all services are running by launching them in the following order: 

1. Inferencing container and API
2. Gateway

Then do the following if using the InnerEye app:
1. Launch the InnerEye app. You can use the echo to test if there is connectivity with the gateway. 
2. Then, open the image (test H&N volume is located in _C:\Users\azureuser\Test Images\headandneck_) and ensure you can see it in the app
3. Select '...' button that is shown when hovering over image in the explorer panel, select "Export", choose destination, check "Send Series", make sure that "Send Structure Set" is unchecked, and click "Export".
4. Observe the console output from solution components to ensure that the process is underway. Once process is complete you should see a yellow "new image received" icon in the left sidebar of the app.
5. Go into the Incoming tab on the app and press "Open" next to the latest segmentation received.