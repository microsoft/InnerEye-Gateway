# InnerEye-Gateway

## Overview

This document has instructions specially for InnerEye-Gateway [https://github.com/microsoft/InnerEye-Gateway](https://github.com/microsoft/InnerEye-Gateway).

*For InnerEye-Inference repo please visit* [*this*](https://github.com/microsoft/InnerEye-Inference) *documentation.*

The InnerEye-Gateway comprises Windows services that act as a DICOM Service Class Provider. After an Association Request to C-STORE a set of DICOM image files, these will be anonymised by removing a user-defined set of identifiers and passed to a web service running [InnerEye-Inference](https://github.com/microsoft/InnerEye-Inference). Inference will then pass them to an instance of [InnerEye-Deeplearning](https://github.com/microsoft/InnerEye-DeepLearning) running on Azure to execute InnerEye-DeepLearning models. The result is downloaded, deanonymised and passed to a configurable DICOM destination. All DICOM image files, and the model output, are automatically deleted immediately after use.

This OSS code is provided under an MIT License and you are responsible for the performance, the necessary testing, and if needed any regulatory clearance for use of this toolbox.

The gateway should be installed on a machine within your DICOM network that is able to access a running instance of [InnerEye-Inference](https://github.com/microsoft/InnerEye-Inference).

![environment](./Docs/environment.png)

## Contents

<!-- TOC depthFrom:2 -->

- [Overview](#overview)
- [Contents](#contents)
- [Getting Started](#getting-started)
- [License Keys](#license-keys)
- [To Build The Gateway](#to-build-the-gateway)
- [To Run The Tests](#to-run-the-tests)
- [To Run The Gateway](#to-run-the-gateway)
- [Architecture](#architecture)
    - [Receiver Application](#receiver-application)
    - [Processor Application](#processor-application)
        - [Upload Service](#upload-service)
        - [Download Service](#download-service)
        - [Push Service](#push-service)
        - [Delete Service](#delete-service)
- [Anonymisation](#anonymisation)
- [Configuration](#configuration)
    - [Common Configuration](#common-configuration)
    - [Processor Configuration](#processor-configuration)
    - [Receiver Configuration](#receiver-configuration)
    - [Model Configuration](#model-configuration)
        - [ModelsConfig](#modelsconfig)
        - [ChannelConstraint](#channelconstraint)
    - [DicomConstraint](#dicomconstraint)
        - [GroupConstraint](#groupconstraint)
        - [RequiredTagConstraint](#requiredtagconstraint)
    - [DicomTagConstraint](#dicomtagconstraint)
        - [GroupTagConstraint](#grouptagconstraint)
        - [StringContainsConstraint](#stringcontainsconstraint)
        - [RegexConstraint](#regexconstraint)
        - [OrderedDateTimeConstraint, OrderedDoubleConstraint, OrderedIntConstraint, OrderedStringConstraint](#ordereddatetimeconstraint-ordereddoubleconstraint-orderedintconstraint-orderedstringconstraint)
        - [UIDStringOrderConstraint](#uidstringorderconstraint)
        - [TimeOrderConstraint](#timeorderconstraint)
- [Licensing](#licensing)
- [Contributing](#contributing)
- [Microsoft Open Source Code of Conduct](#microsoft-open-source-code-of-conduct)

<!-- /TOC -->

## Getting Started

To get started with setting up this project you will need the following pre-requisites:

1. A machine running Windows 10, because the Gateway runs as Windows Services. It requires a network card to act as a DICOM SCP and for access to the InnerEye-Inference web service. A large hard disk is not required because the DICOM image files are not kept for very long.

1. Visual Studio 2019 Community Edition [(Download Visual Studio Community Edition)](https://visualstudio.microsoft.com/thank-you-downloading-visual-studio/?sku=Community&rel=16)

1. .Net Framework 4.6.2 [Download .Net Framework](https://dotnet.microsoft.com/download/dotnet-framework/thank-you/net462-developer-pack-offline-installer)

1. To clone the repository a git client with [large file support](https://git-lfs.github.com/), e.g. [git for windows](https://gitforwindows.org/)

1. Wix Toolset for building the installer [Download WixToolset](https://wixtoolset.org/releases/)

1. Run the PowerShell script to download two DICOM tools required for testing the gateway: [./Source/Microsoft.Gateway/download_dcmtk.ps1](./Source/Microsoft.Gateway/download_dcmtk.ps1). Note that these tools are only required for testing but the test projects and therefore the gateway will not build without them. Note also that the default PowerShell execution policy will not allow running scripts, more information is available here: [about execution policies](https:/go.microsoft.com/fwlink/?LinkID=135170). In brief:

    a. Start PowerShell with the `Run as administrator` option.

    b. Run the PowerShell command:

    ```shell
    Set-ExecutionPolicy -ExecutionPolicy Unrestricted
    ```

    and agree to the change.

1. InnerEye-Inference service from [https://github.com/microsoft/InnerEye-Inference](https://github.com/microsoft/InnerEye-Inference) running as a web service, using, for example [Azure Web Services](https://azure.microsoft.com/en-gb/services/app-service/web/). Note the URI that the service has been deployed to and the license key stored in the environment variable `CUSTOMCONNSTR_API_AUTH_SECRET` on the InnerEye-Inference deployment, they are needed as explained below.

## License Keys

For security in InnerEye-Gateway and InnerEye-Inference the license keys are stored in environment variables and never stored in JSON or other configuration files. InnerEye-Inference uses the environment variable `CUSTOMCONNSTR_API_AUTH_SECRET` whereas InnerEye-Gateway allows the name of the environment variable to be configured in the JSON file in the `LicenseKeyEnvVar` property. This is so that the tests may be configured to run against a different InnerEye-Inference web service. Note also that because the applications run as windows services the environment variables should be system variables, not user variables, so they can be accessed by the services.

Alongside `LicenseKeyEnvVar` the property `InferenceUri` holds the URI of a running instance of InnerEye-Inference and the environment variable identified by `LicenseKeyEnvVar` should hold the license key for that instance.

For example if `InferenceUri` is "https://myinnereyeinference.azurewebsites.net", `LicenseKeyEnvVar` is "MY_GATEWAY_API_AUTH_SECRET", and the contents of the environment variable `CUSTOMCONNSTR_API_AUTH_SECRET` used for "https://myinnereyeinference.azurewebsites.net" is `MYINFERENCELICENSEKEY` then set this environment variable with the PowerShell command, running as administrator:

```cmd
setx MY_GATEWAY_API_AUTH_SECRET MYINFERENCELICENSEKEY /M
```

## To Build The Gateway

1. Clone the repository.

1. Download the DICOM tools using the PowerShell script in [Getting Started](#getting-started).

1. Open the project solution file [./Source/Microsoft.Gateway/Microsoft.Gateway.sln](./Source/Microsoft.Gateway/Microsoft.Gateway.sln) in Visual Studio 2019.

1. Set the project platform to **x64**.

1. Build the Solution.

## To Run The Tests

1. [Build the gateway](#to-build-the-gateway).

1. For end to end tests:

    - check the configuration settings for `Microsoft.InnerEye.Listener.Processor` in [./Source/Microsoft.Gateway/Microsoft.InnerEye.Listener.Tests/TestConfigurations/GatewayProcessorConfig.json](./Source/Microsoft.Gateway/Microsoft.InnerEye.Listener.Tests/TestConfigurations/GatewayProcessorConfig.json). Check the following settings in `ProcessorSettings`:

        - `InferenceUri` is the Uri for the InnerEye-Inference web service from [Getting Started](#getting-started). See [License Keys](#license-keys) for more details.

        - `LicenseKeyEnvVar` is the name of the environment variable which contains the license key for the InnerEye-Inference web service at `InferenceUri`. See [License Keys](#license-keys) for more details.

    - check the configuration settings for the test application entity models in [./Source/Microsoft.GatewayMicrosoft.InnerEye.Listener.Tests/TestConfigurations/GatewayModelRulesConfig/GatewayModelRulesConfig.json"](./Source/Microsoft.GatewayMicrosoft.InnerEye.Listener.Tests/TestConfigurations/GatewayModelRulesConfig/GatewayModelRulesConfig.json"). The tests will use the first found application entity model so check that `ModelId` in the first `ModelsConfig` is a valid PassThrough model id for the configured InnerEye-Inference service. Note that the PassThrough model is a special model intended for testing that simply returns a hard-coded list of structures for any input DICOM image series; it will always return the structures `["SpinalCord", "Lung_R", "Lung_L", "Heart", "Esophagus"]`. If this is not present in the instance of [InnerEye-Deeplearning](https://github.com/microsoft/InnerEye-DeepLearning) build the model by running the command:

    ```cmd
    python InnerEye/ML/runner.py --azureml=True --model=PassThroughModel
    ```

1. Make sure the testing **Default Processor Architecture** is set to **x64**. This can be checked by navigating to **Test > Test Settings > Default Process Architecture**

1. The tests can be executed from Visual Studio 2019 by navigating to **Test > Windows > Test Explorer**.

1. All the available tests will be visible in the Test Explorer.

1. A test can be executed by right clicking on the test and selecting the Run Selected Tests option.

1. To execute all the available tests, click on the Run All link in the Test Explorer.

1. The log for the test execution can be found in the Output window.

## To Run The Gateway

1. The gateway uses multiple startup projects: `Microsoft.InnerEye.Listener.Processor` and `Microsoft.InnerEye.Listener.Receiver`. Configure this in Visual Studio by right clicking on the Solution in Solution Explorer and selecting "Set Startup Projects...". Click the radio button "Multiple startup projects" and for the projects above select "Start" in the "Action" combo box.

1. Check the configuration settings for `Microsoft.InnerEye.Listener.Processor` in [./Source/Microsoft.Gateway/SampleConfigurations/GatewayProcessorConfig.json](./Source/Microsoft.Gateway/SampleConfigurations/GatewayProcessorConfig.json). More details are in [Processor Configuration](#processor-configuration), but the most important parameters to check are in `ProcessorSettings`:

    - `InferenceUri` is the Uri for the InnerEye-Inference web service from [Getting Started](#getting-started). See [License Keys](#license-keys) for more details.

    - `LicenseKeyEnvVar` is the name of the environment variable which contains the license key for the InnerEye-Inference web service at `InferenceUri`. See [License Keys](#license-keys) for more details.

1. Check the configuration settings for `Microsoft.InnerEye.Listener.Receiver` in [./Source/Microsoft.Gateway/SampleConfigurations/GatewayReceiveConfig.json](./Source/Microsoft.Gateway/SampleConfigurations/GatewayReceiveConfig.json). More details are in [Receiver Configuration](#receiver-configuration), but the most important parameter to check is:

    - `Port` in `GatewayDicomEndPoint` in `ReceiveServiceConfig` - holds the IP port the receiver service listens on.

1. Check the configuration settings for the application entity models in the folder [./Source/Microsoft.Gateway/SampleConfigurations/GatewayModelRulesConfig](./Source/Microsoft.Gateway/SampleConfigurations/GatewayModelRulesConfig). The configuration may be split across multiple JSON files, if desired, and the configuration will be merged. The structure of these files is that each must contain an array of the form:

```json
[
  {
    "CallingAET": << Calling application entity title >>,
    "CalledAET": << Called application entity title >>,
    "AETConfig": {
      "Config": {
        "AETConfigType": << One of "Model", "ModelDryRun", or "ModelWithResultDryRun" >>
        "ModelsConfig": [
          {
            "ModelId": << Model id for model in inference service, e.g. "PassThroughModel:3" >>,
            "ChannelConstraints": [ << Array of channel constraints >> ],
            "TagReplacements": [ << Array of tag replacements >> ]
          },
          ...
        ]
      },
      "Destination": << Destination >>,
      "ShouldReturnImage": << Return image flag >>
    }
  }
```

All JSON files in this folder are loaded and parsed. If the same `CallingAET` and `CalledAET` are found in more than one instance then the `ModelsConfig` arrays are concatenated to create one instance sharing all the other properties (which are taken from the first instance found). More details are in [Model Configuration](#model-configuration).

## Architecture

The gateway runs as two applications or services configured with [JSON](https://en.wikipedia.org/wiki/JSON) files. The two applications communicate, and operate internally, using [message queues](https://en.wikipedia.org/wiki/Message_queue) built on [Sqlite](https://sqlite.org/index.html).

![Sequence](./Docs/sequence.png)

To edit this, see: [Rebuild Diagram](./Diagram.md).

### Receiver Application

The first application is `Microsoft.InnerEye.Listener.Receiver`. This create a DICOM server that listens on an IP port for incoming [DICOM](https://en.wikipedia.org/wiki/DICOM) communications and stores requests that are accepted on a message queue. This is configured in [Receiver Configuration](#receiver-configuration).

In detail, this:

1. Starts a DICOM server that listens for incoming DICOM messages.

1. On a DICOM Association Request, checks against the configured acceptable SOP classes and their transfer syntaxes.

1. On a DICOM C-Store Request, stores the incoming DICOM image file to a subfolder of the RootDicomFolder. There will be one of these for each image slice.

1. On a DICOM Association Release Request, puts a new message on the **Upload** message queue with details of the DICOM Association and location of the files.

### Processor Application

The second application is `Microsoft.InnerEye.Listener.Processor`. This waits for messages on the **Upload** message queue from the [Receiver Application](#receiver-application). When a new message is received it: copies and de-identifies the received DICOM images, sends them to the InnerEye-Inference web service, waits and then downloads the resulting DICOM-RT file. This is then de-anonymised and sent on to the configured destination.

This is configured in [Processor Configuration](#processor-configuration).

In detail, this starts 4 worker tasks: the [Delete Service](#delete-service), [Download Service](#download-service), [Push Service](#push-service), and [Upload Service](#upload-service), which communicate using the message queues: **Delete**, **Download**, **Push**, and **Upload**.

#### Upload Service

This service watches the **Upload** message queue for messages from the [Receiver](#receiver) application that indicate a DICOM request has completed. When a new message is received it:

1. Tries to find an application entity model configured with the matching `CalledAET` and `CallingAET` ([Model Configuration](#model-configuration)).

1. If there is a corresponding model and it is set to `Model` or `ModelWithResultDryRun`:

    a. Reads the received DICOM image files from the subfolder of the RootDicomFolder.

    b. Groups the DICOM image files by Study Instance UID, and then grouped by Series Instance UID.

    c. Compares each group with each channel config until a set of filtered DICOM image files matches the constraints in the channel config.

    d. Copies the image data and only the required DICOM tags to a set of new images, removing the remaining DICOM tags.

    e. Zips the images and POSTs them to the InnerEye-Inference web service.

    f. If the model `ShouldReturnImage` is set then copies all sent data to either the Results or DryRunModelWithResultFolder subfolders of the RootDicomFolder.

    g. Creates a new message on the **Download** queue for this web service request.

    h. Creates a new message on the **Delete** queue to delete the received DICOM image files.

1. If there is a corresponding model and it is set to `ModelDryRun`:

    a. Reads the received DICOM image files from the subfolder of the RootDicomFolder.

    b. Removes the DICOM tags for de-identification.

    c. Saves the de-identified image files to the DryRunModelAnonymizedImage subfolder of the RootDicomFolder.

    d. Creates a new message on the **Delete** queue to delete the received image files.

1. If there is no correspond model then:

    a. Creates a new message on the **Delete** queue to delete the received image files.

#### Download Service

This service watches the **Download** message queue for messages from the [Upload Service](#upload-service) that indicate a set of DICOM files have been sent to the InnerEye-Inference web service. When a new message is received it:

1. Waits for the InnerEye-Inference web service to perform segmentation then downloads the resulting DICOM-RT file.

1. De-anonymises the DICOM-RT file using data from the original received DICOM image files.

1. Saves the de-anonymised file to either the Results or DryRunModelWithResultFolder subfolders of the RootDicomFolder.

1. If this is not `ModelWithResultDryRun` then creates a new message on the **Push** message queue.

#### Push Service

This service watches the **Push** message queue for messages from the [Download Service](#download-service) that indicate that a DICOM-RT file has been downloaded from the InnerEye-Inference web service. When a new message is received it:

1. Tries to find an application entity model configured with the matching `CalledAET` and `CallingAET` ([Model Configuration](#model-configuration)).

1. Reads the de-anonymised DICOM-RT file from the Results subfolder of the RootDicomFolder.

1. Sends them to the DICOM Destination as set in the application entity model.

1. Creates a new message on the **Delete** queue to delete the received DICOM-RT file.

#### Delete Service

This service watches the **Delete** message queue for messages from the other services. If it receives a message it will delete the specified files or folders.

## Anonymisation

The InnerEye Gateway allows users to define a set of identifiers that will be removed before being sent to the InnerEye-Inference web service. The set of identifier tags for removal are defined in InnerEyeSegmentationClient.cs(). The Gateway service processes and de-identifies the DICOM files using the procedure below. 

The process for handling DICOM files is:

1. The [Receiver Application](#receiver-application) saves the incoming DICOM files directly to a subfolder of RootDicomFolder and passes a message to the [Upload Service](#upload-service).

1. The [Upload Service](#upload-service) loads the DICOM files and makes a copy of each file in memory (discarding the image data), as `reference images` for de-anonymisation later. The following tags are kept, along with the tags mentioned below:

```c#
// Patient module
PatientID,
PatientName,
PatientBirthDate,
PatientSex,

// Study module
StudyDate,
StudyTime,
ReferringPhysicianName,
StudyID,
AccessionNumber,
StudyDescription
```

3. The [Upload Service](#upload-service) loads the DICOM files again and makes a second copy of each file in memory, performing the following transformations to the DICOM tags:

a. The following tags are kept unchanged:

```c#
// Geometry
PatientPosition,
Columns,
Rows,
PixelSpacing,
ImagePositionPatient,
ImageOrientationPatient,
SliceLocation,
BodyPartExamined,

// Modality
Modality,
ModalityLUTSequence,
HighBit,
BitsStored,
BitsAllocated,
SamplesPerPixel,
PixelData,
PhotometricInterpretation,
PixelRepresentation,
RescaleIntercept,
RescaleSlope,
ImageType,

// UIDs
SOPClassUID,

// RT
// RT DicomFrameOfReference
RTReferencedStudySequence,

// RT DicomRTContour
ReferencedROINumber,
ROIDisplayColor,
ContourSequence,
ROIContourSequence,

// RT DicomRTContourImageItem
ReferencedSOPClassUID,

// RT DicomRTContourItem
NumberOfContourPoints,
ContourData,
ContourGeometricType,
ContourImageSequence,

// RT DicomRTObservation
RTROIObservationsSequence,
ObservationNumber,

// RT DicomRTReferencedStudy
RTReferencedSeriesSequence,

// RT DicomRTStructureSet
ReferencedFrameOfReferenceSequence,

// RT DicomRTStructureSetROI
ROINumber,
ROIName,
ROIGenerationAlgorithm,
StructureSetROISequence,
```

b. The following DICOM tags are replaced with a hash of their value:

```C#
// UIDs
PatientID,
SeriesInstanceUID,
StudyInstanceUID,
SOPInstanceUID,

// RT
// RT DicomFrameOfReference
FrameOfReferenceUID,

// RT DicomRTContourImageItem
ReferencedSOPInstanceUID,

// RT DicomRTStructureSet
StructureSetLabel,
StructureSetName,

// RT DicomRTStructureSetROI
ReferencedFrameOfReferenceUID
```

c. All other tags are discarded.

d. The transformed DICOM image files are then zipped before sending to the InnerEye-Inference web service.

4. After the [Download Service](#download-service) downloads the DICOM-RT file it performs the following de-anonymisation transformations to the DICOM-RT file:

a. The first set of tags 

```c#
PatientID,
...
StudyDescription
```

are copied from the first `reference image`.

b. All the tags that have been hashed are replaced with tags from the `reference images`.

c. Model configured tag transformations are applied, to either replace a tag entirely or append some fixed text.

## Configuration

### Common Configuration

Both applications share some common configuration.

The InnerEye-Gateway repeatedly checks if its configuration files have been modified, to ensure that it can be updated without interrupting the DICOM processing. This is configured with `ConfigurationServiceConfig`. The algorithm behind this is that each `ConfigurationRefreshDelaySeconds` the application will check connectivity to the InnerEye-Inference service (for the Processor service) and check for changes to the configuration files. If `ConfigCreationDateTime` is different to the last loaded configuration file and `ApplyConfigDateTime` is past then the application will effectively restart with the new configuration.

```json
{
  "ServiceSettings": {
    "RunAsConsole": boolean
  },
  << service specific configuration >>,
  "ConfigurationServiceConfig": {
    "ConfigCreationDateTime": date/time,
    "ApplyConfigDateTime": date/time,
    "ConfigurationRefreshDelaySeconds": number
  }
}
```

For example:

```json
{
  "ServiceSettings": {
    "RunAsConsole": true
  },
  << service specific configuration >>,
  "ConfigurationServiceConfig": {
    "ConfigCreationDateTime": "2020-05-31T20:14:51",
    "ApplyConfigDateTime": "2020-05-31T20:14:51",
    "ConfigurationRefreshDelaySeconds": 60
  }
}
```

Where:

- `RunAsConsole` if true means that this application runs as a normal console application, otherwise it runs as a windows service. This can be used for debugging or testing.

- `ConfigCreationDateTime` is the date and time that this configuration was created. It does not need to be exact, because it is only used to identify when the file has been edited.

- `ApplyConfigDateTime` is the date and time that this configuration should be applied.

- `ConfigurationRefreshDelaySeconds` is the time in seconds the application will wait between checks if a new configuration file is available.

### Processor Configuration

The processor application is configured by [./Source/Microsoft.Gateway/SampleConfigurations/GatewayProcessorConfig.json](./Source/Microsoft.Gateway/SampleConfigurations/GatewayProcessorConfig.json).

The structure of this configuration file is:

```json
{
  "ServiceSettings": {
    "RunAsConsole": boolean
  },
  "ProcessorSettings": {
    "LicenseKeyEnvVar": string,
    "InferenceUri": string
  },
  "DequeueServiceConfig": {
    "MaximumQueueMessageAgeSeconds": number,
    "DeadLetterMoveFrequencySeconds": number
  },
  "DownloadServiceConfig": {
    "DownloadRetryTimespanInSeconds": number,
    "DownloadWaitTimeoutInSeconds": number
  },
  "ConfigurationServiceConfig": {
    "ConfigCreationDateTime": date/time,
    "ApplyConfigDateTime": date/time,
    "ConfigurationRefreshDelaySeconds": number
  }
}
```

For example:

```json
{
  "ServiceSettings": {
    "RunAsConsole": true
  },
  "ProcessorSettings": {
    "LicenseKeyEnvVar": "MYINFERENCELICENSEKEY",
    "InferenceUri": "https://myinnereyeinference.azurewebsites.net"
  },
  "DequeueServiceConfig": {
    "MaximumQueueMessageAgeSeconds": 100,
    "DeadLetterMoveFrequencySeconds": 1
  },
  "DownloadServiceConfig": {
    "DownloadRetryTimespanInSeconds": 5,
    "DownloadWaitTimeoutInSeconds": 3600
  },
  "ConfigurationServiceConfig": {
    "ConfigCreationDateTime": "2020-05-31T20:14:51",
    "ApplyConfigDateTime": "2020-05-31T20:14:51",
    "ConfigurationRefreshDelaySeconds": 60
  }
}
```

Where:

- `ServiceSettings` and `ConfigurationServiceConfig` are as [above](#common-configuration).

- `InferenceUri` is the Uri for the InnerEye-Inference web service from [Getting Started](#getting-started). See [License Keys](#license-keys) for more details.

- `LicenseKeyEnvVar` is the name of the environment variable which contains the license key for the InnerEye-Inference web service at `InferenceUri`. See [License Keys](#license-keys) for more details.

- `MaximumQueueMessageAgeSeconds` is an internal message queue setting - the maximum time in seconds a message may spend in a queue before being automatically deleted.

- `DeadLetterMoveFrequencySeconds` is another internal message queue setting - if there is an error processing a message then it will be moved to a [Dead letter queue](https://en.wikipedia.org/wiki/Dead_letter_queue) and this is the time in seconds before moving messages back from the dead letter queue to the normal queue.

- `DownloadRetryTimespanInSeconds` is the delay in seconds between attempts to download the completed segmentation from the InnerEye-Inference service.

- `DownloadWaitTimeoutInSeconds` is the maximum time in seconds to wait whilst attempting to download the completed segmentation.

### Receiver Configuration

The receiver application is configured by [./Source/Microsoft.Gateway/SampleConfigurations/GatewayReceiveConfig.json](./Source/Microsoft.Gateway/SampleConfigurations/GatewayReceiveConfig.json) and also by model configuration files in the folder [./Source/Microsoft.Gateway/SampleConfigurations/GatewayModelRulesConfig](./Source/Microsoft.Gateway/SampleConfigurations/GatewayModelRulesConfig). The model configuration is explained below: [Model Configuration](#model-configuration).

The structure of GatewayReceiveConfig.json configuration file is:

```json
{
  "ServiceSettings": {
    "RunAsConsole": boolean
  },
  "ReceiveServiceConfig": {
    "GatewayDicomEndPoint": {
      "Title": string,
      "Port": number,
      "Ip": string
    },
    "RootDicomFolder": string,
    "AcceptedSopClassesAndTransferSyntaxesUIDs": object of the form {
      string: array of strings,
      string: array of strings,
      string: array of strings,
      ...
    }
  },
  "ConfigurationServiceConfig": {
    "ConfigCreationDateTime": date/time,
    "ApplyConfigDateTime": date/time,
    "ConfigurationRefreshDelaySeconds": number
  }
}
```

For example:

```json
{
  "ServiceSettings": {
    "RunAsConsole": true
  },
  "ReceiveServiceConfig": {
    "GatewayDicomEndPoint": {
      "Title": "GATEWAY",
      "Port": 111,
      "Ip": "localhost"
    },
    "RootDicomFolder": "C:\\InnerEyeGateway\\",
    "AcceptedSopClassesAndTransferSyntaxesUIDs": {
      "1.2.840.10008.1.1": [ "1.2.840.10008.1.2.1", "1.2.840.10008.1.2" ],
      "1.2.840.10008.5.1.4.1.1.481.3": [ "1.2.840.10008.1.2", "1.2.840.10008.1.2.1" ],
      "1.2.840.10008.5.1.4.1.1.2": [ "1.2.840.10008.1.2", "1.2.840.10008.1.2.1", "1.2.840.10008.1.2.4.57", "1.2.840.10008.1.2.4.70", "1.2.840.10008.1.2.4.80", "1.2.840.10008.1.2.5" ],
      "1.2.840.10008.5.1.4.1.1.4": [ "1.2.840.10008.1.2", "1.2.840.10008.1.2.1", "1.2.840.10008.1.2.4.57", "1.2.840.10008.1.2.4.70", "1.2.840.10008.1.2.4.80", "1.2.840.10008.1.2.5" ]
    }
  },
  "ConfigurationServiceConfig": {
    "ConfigCreationDateTime": "2018-07-25T20:14:51.539351Z",
    "ApplyConfigDateTime": "2018-07-25T20:14:51.539351Z",
    "ConfigurationRefreshDelaySeconds": 60
  }
}
```

Where:

- `ServiceSettings` and `ConfigurationServiceConfig` are as [above](#common-configuration).

- `ReceiveServiceConfig.GatewayDicomEndPoint.Title` is only used for testing, but must be supplied.

- `ReceiveServiceConfig.GatewayDicomEndPoint.Port` is the IP port the DICOM server will listen for DICOM messages on.

- `ReceiveServiceConfig.GatewayDicomEndPoint.Ip` is also only used for testing, but must be supplied.

- `RootDicomFolder` is the folder to be used for temporarily storing DICOM files.

- `AcceptedSopClassesAndTransferSyntaxesUIDs` is a dictionary of [DICOM Service-Object Pair (SOP) Classes](http://dicom.nema.org/dicom/2013/output/chtml/part06/chapter_A.html) and [Transfer Syntax UIDs](http://dicom.nema.org/dicom/2013/output/chtml/part05/chapter_10.html) that the application supports.
    Here:
    - "1.2.840.10008.1.1" is the Verification SOP Class
    - "1.2.840.10008.5.1.4.1.1.481.3" is Radiation Therapy Structure Set Storage.
    - "1.2.840.10008.5.1.4.1.1.2" is CT Image Storage
    - "1.2.840.10008.5.1.4.1.1.4" is MR Image Storage

    For each of these SOPs there is a list of supported Transfer Syntax UIDs. For example:
    - "1.2.840.10008.1.2" is Implicit VR Endian: Default Transfer Syntax for DICOM.
    - "1.2.840.10008.1.2.4.57" is a type of JPEG.

### Model Configuration

The application entity models are configured in the folder [./Source/Microsoft.Gateway/SampleConfigurations/GatewayModelRulesConfig](./Source/Microsoft.Gateway/SampleConfigurations/GatewayModelRulesConfig). The configuration may be split across multiple JSON files, if desired, and the configuration will be merged. The structure of these files is that each must contain an array of the form:

```json
[ array of application entity model config objects of the form:
  {
    "CallingAET": string,
    "CalledAET": string,
    "AETConfig": {
      "Config": {
        "AETConfigType": string, one of "Model", "ModelDryRun", or "ModelWithResultDryRun"
        "ModelsConfig": [ array of models config objects of the form:
          {
            "ModelId": string,
            "ChannelConstraints": [ array of channel constraints objects ],
            "TagReplacements": [ array of tag replacements objects ]
          }
        ]
      },
      "Destination": {
        "Title": string,
        "Port": number,
        "Ip": string
      },
      "ShouldReturnImage": boolean
    }
  }
]
```

For example:

```json
[
  {
    "CallingAET": "RADIOMICS_APP",
    "CalledAET": "PassThroughModel",
    "AETConfig": {
      "Config": {
        "AETConfigType": "Model",
        "ModelsConfig": [
          {
            "ModelId": "PassThroughModel:3",
            "ChannelConstraints": [
              {
                "ChannelID": "ct",
                "ImageFilter": {
                  "Constraints": [
                    {
                      "RequirementLevel": "PresentNotEmpty",
                      "Constraint": {
                        "Function": {
                          "Order": "Equal",
                          "Value": {
                            "Value": "1.2.840.10008.5.1.4.1.1.2",
                            "ComparisonType": 0
                          },
                          "Ordinal": 0
                        },
                        "Index": {
                          "Group": 8,
                          "Element": 22
                        },
                        "discriminator": "UIDStringOrderConstraint"
                      },
                      "discriminator": "RequiredTagConstraint"
                    }
                  ],
                  "Op": "And",
                  "discriminator": "GroupConstraint"
                },
                "ChannelConstraints": {
                  "Constraints": [
                    {
                      "RequirementLevel": "PresentNotEmpty",
                      "Constraint": {
                        "Function": {
                          "Order": "Equal",
                          "Value": {
                            "Value": "1.2.840.10008.5.1.4.1.1.2",
                            "ComparisonType": 0
                          },
                          "Ordinal": 0
                        },
                        "Index": {
                          "Group": 8,
                          "Element": 22
                        },
                        "discriminator": "UIDStringOrderConstraint"
                      },
                      "discriminator": "RequiredTagConstraint"
                    }
                  ],
                  "Op": "And",
                  "discriminator": "GroupConstraint"
                },
                "MinChannelImages": 50,
                "MaxChannelImages": 1000
              }
            ],
            "TagReplacements": [
              {
                "Operation": "UpdateIfExists",
                "DicomTagIndex": {
                  "Group": 12294,
                  "Element": 2
                },
                "Value": "InnerEye"
              },
              {
                "Operation": "AppendIfExists",
                "DicomTagIndex": {
                  "Group": 12294,
                  "Element": 38
                },
                "Value": " NOT FOR CLINICAL USE"
              }
            ]
          }
        ]
      },
      "Destination": {
        "Title": "RADIOMICS_APP",
        "Port": 104,
        "Ip": "127.0.0.1"
      },
      "ShouldReturnImage": false
    }
  }
]
```

Where:

- `CallingAET` is the calling application entity title to be matched.

- `CalledAET` is the called application entity title to be matched.

- `AETConfig` consists of three parts:

    - `Config` consists of the pair:

        - `AETConfigType` is one of "Model", "ModelDryRun", or "ModelWithResultDryRun". "Model" is the normal case, the other two are for debugging. "ModelDryRun" means that the received DICOM image files will be anonymised by removing a user-defined set of identifiers and saved to the DryRunModelAnonymizedImage subfolder of RootDicomFolder. "ModelWithResultDryRun" means almost the same as "Model" except that the DICOM-RT file is downloaded to the DryRunRTResultDeAnonymized subfolder of RootDicomFolder and it is not pushed to a DICOM destination.

        - `ModelsConfig` is an array of [ModelsConfig](#modelsconfig) objects, described below.

    - `Destination` is where to send the resulting DICOM-RT file, consisting of:

        - `Title` is the destination application entity title,

        - `Port` is the destination application entity port,

        - `Ip` is the destination IP addres.

    - `ShouldReturnImage` is true if the original received DICOM image files should be returned when InnerEye-Inference service is complete, false otherwise.

#### ModelsConfig

Each model config has the following structure:

```json
{
  "ModelId": string,
  "ChannelConstraints": [ array of objects of the form
    {
      "ChannelID": string,
      "ImageFilter": {
        "Constraints": [ array of constraints ],
        "Op": string, one of "And" or "Or",
        "discriminator": "GroupConstraint"
      },
      "ChannelConstraints": {
        "Constraints": [ array of constraints ],
        "Op": string, one of "And" or "Or",
        "discriminator": "GroupConstraint"
      },
      "MinChannelImages": number,
      "MaxChannelImages": number
    }
  ],
  "TagReplacements": [ array of objects of the form:
    {
      "Operation": string, one of "UpdateIfExists" or "AppendIfExists",
      "DicomTagIndex": {
        "Group": number,
        "Element": number
      },
      "Value": string
    }
  ]
}
```

For example:

```json
{
  "ModelId": "PassThroughModel:3",
  "ChannelConstraints": [
    {
      "ChannelID": "ct",
      "ImageFilter": {
        "Constraints": [
          {
            "RequirementLevel": "PresentNotEmpty",
            "Constraint": {
              "Function": {
                "Order": "Equal",
                "Value": {
                  "Value": "1.2.840.10008.5.1.4.1.1.2",
                  "ComparisonType": 0
                },
                "Ordinal": 0
              },
              "Index": {
                "Group": 8,
                "Element": 22
              },
              "discriminator": "UIDStringOrderConstraint"
            },
            "discriminator": "RequiredTagConstraint"
          }
        ],
        "Op": "And",
        "discriminator": "GroupConstraint"
      },
      "ChannelConstraints": {
        "Constraints": [
          {
            "RequirementLevel": "PresentNotEmpty",
            "Constraint": {
              "Function": {
                "Order": "Equal",
                "Value": {
                  "Value": "1.2.840.10008.5.1.4.1.1.2",
                  "ComparisonType": 0
                },
                "Ordinal": 0
              },
              "Index": {
                "Group": 8,
                "Element": 22
              },
              "discriminator": "UIDStringOrderConstraint"
            },
            "discriminator": "RequiredTagConstraint"
          }
        ],
        "Op": "And",
        "discriminator": "GroupConstraint"
      },
      "MinChannelImages": 50,
      "MaxChannelImages": 1000
    }
  ],
  "TagReplacements": [
    {
      "Operation": "UpdateIfExists",
      "DicomTagIndex": {
        "Group": 12294,
        "Element": 2
      },
      "Value": "InnerEye"
    },
    {
      "Operation": "AppendIfExists",
      "DicomTagIndex": {
        "Group": 12294,
        "Element": 38
      },
      "Value": " NOT FOR CLINICAL USE"
    }
  ]
}
```

Where:

- `ModelId` is the model identifier that is passed to the InnerEye-Inference service.

- `ChannelConstraints` is an array of [ChannelConstraint](#channelconstraint) that are applied to the received DICOM image files. The algorithm here is that:

    - The image files are first grouped by Study Instance UID (which must exist), and then grouped by Series Instance UID (which must also exist)

    - For each group of shared Study and Series Instance UIDs: the channel constraints for each `ModelsConfig` are applied in order to the image group. If there is a match then the matching ModelsConfig and image group are then used. The [channel constraints](#channelconstraints) are explained below.

- `TagReplacements` is a list of DICOM tag replacements that are performed for DICOM-RT file de-anonymisation. The algorithm here is during de-anonymisation to work through all the `TagReplacements`:

    - If the file contains the tag as specified in `DicomTagIndex`:

        - If the `Operation` is "UpdateIfExists" then replace the existing tag with `Value`;

        - If the `Operation` is "AppendIfExists" then append `Value` to the existing tag.

#### ChannelConstraint

Each channel constraint has the form:

```json
{
  "ChannelID": string,
  "ImageFilter": {
    "Constraints": [ array of constraints ],
    "Op": string, one of "And" or "Or",
    "discriminator": "GroupConstraint"
  },
  "ChannelConstraints": {
    "Constraints": [ array of constraints ],
    "Op": string, one of "And" or "Or",
    "discriminator": "GroupConstraint"
  },
  "MinChannelImages": number,
  "MaxChannelImages": number
}
```

Where:

- `ChannelID` is the id of the channel. It is used in the zip file sent to the InnerEye-Inference service.

- `ImageFilter` is a [GroupConstraint](#groupconstraint) used to filter the DICOM image files before applying the constraints. This selects a subset of images from the same series by filtering unwanted data e.g. extraneous sop classes.

- `ChannelConstraints` is a [GroupConstraint](#groupconstraint) used on the images that have passed through `ImageFilter`.

- `MinChannelImages` is the minimum number of files required for this channel. Use 0 or less to impose no constraint. This is the inclusive lower bound for the number of filtered images.

- `MaxChannelImages` is the maximum number of files allowed for this channel. Use 0 or less to impose no maximum constraint. This is the inclusive upper bound for the number of filtered images.

### DicomConstraint

There are many types of DicomConstraint. So they can be identified when loading the JSON they are all of the form:

```json
{
  << constraint specific data >>,
  "discriminator": string
}
```

Where:

- `discriminator` identifies the DicomConstraint type.

These are split into two groups. The first group contains [GroupConstraint](#groupconstraint) and [RequiredTagConstraint](#requiredtagconstraint) and are sort of container objects. The second group are all instances of [DicomTagConstraint](#dicomtagconstraint) which specify a DICOM tag and a constraint to be applied.

#### GroupConstraint

This constraint acts as a container for a set of other constraints that must either all pass, or at least one of them must pass.

```json
{
  "Constraints": [ array of constraints ],
  "Op": string, one of "And" or "Or",
  "discriminator": "GroupConstraint"
}
```

Where:

- `Constraints` is an array of [DicomConstraint](dicomconstraint).

- `Op` controls whether all constraints ("And") must be met, or at least one of them ("Or").

- `discriminator` is as [DicomConstraint](#dicomconstraint).

#### RequiredTagConstraint

This constraint acts as a container for a tag constraint and a requirement on the tag.

```json
{
  "RequirementLevel": string, one of "PresentNotEmpty", "PresentCanBeEmpty", or "Optional", 
  "Constraint": a dicom tag constraint object,
  "discriminator": "RequiredTagConstraint"
}
```

For example:

```json
{
  "RequirementLevel": "PresentNotEmpty",
  "Constraint": {
    "Function": {
      "Order": "Equal",
      "Value": {
        "Value": "1.2.840.10008.5.1.4.1.1.2",
        "ComparisonType": 0
      },
      "Ordinal": 0
    },
    "Index": {
      "Group": 8,
      "Element": 22
    },
    "discriminator": "UIDStringOrderConstraint"
  },
  "discriminator": "RequiredTagConstraint"
}
```

Where:

- `RequirementLevel` means:

    - "PresentNotEmpty" - the tag must be present and the conditions on the tag must pass.

    - "PresentCanBeEmpty" - the tag must be present and the conditions must pass when the tag is non-empty.

    - "Optional" - the tag does not need to be present but the condition must pass if the tag is present and non-empty.

- `Constraint` is any [DicomTagConstraint](#dicomtagconstraint).

- `discriminator` is as [DicomConstraint](#dicomconstraint).

### DicomTagConstraint

This group of DICOM constraints operate on a DICOM tag. They are all of the form:

```json
{
  << tag constraint specific data >>,
  "Index": {
    "Group": number,
    "Element": number
  },
  "discriminator": string
}
```

Where:

- `Index` identifies the target DICOM tag.

- `discriminator` is as [DicomConstraint](dicomconstraint).

#### GroupTagConstraint

This acts as a container for a set of constraints and a DICOM tag that the constraints apply to.

```json
{
  "Group": group constraint object,
  "Index": {
    "Group": number,
    "Element": number
  },
  "discriminator": "GroupTagConstraint"
}
```

Where:

- `Group` is a [GroupConstraint](#groupconstraint)

- `Index` is as [DicomTagConstraint](#dicomtagconstraint).

- `discriminator` is as [DicomConstraint](dicomconstraint).

#### StringContainsConstraint

This constraint tests that a DICOM tag contains a given value.

```json
{
  "Match": string,
  "Ordinal": number,
  "Index": {
    "Group": number,
    "Element": number
  },
  "discriminator": "StringContainsConstraint"
}
```

For example:

```json
{
  "Match": "AXIAL",
  "Ordinal": -1,
  "Index": {
    "Group": 8,
    "Element": 8
  },
  "discriminator": "StringContainsConstraint"
}
```

Where:

- `Match` is the string that the DICOM tag should contain.

- `Ordinal` is the ordinal to extract from the tag or -1 to extract all.

- `Index` is as [DicomTagConstraint](#dicomtagconstraint).

- `discriminator` is as [DicomConstraint](dicomconstraint).

#### RegexConstraint

This constraint contains a regular expression and a tag to test against.

```json
{
  "Expression": string, a regular expression, suitable for the Regex constructor from .NET System.Text.RegularExpressions,
  "Options": number, one of the RegexOptions from .NET System.Text.RegularExpressions,
  "Ordinal": number,
  "Index": {
    "Group": number,
    "Element": number
  },
  "discriminator": "RegexConstraint"
}
```

Where:

- `Expression` is a regular expression compatible with [.NET Regex](https://docs.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex?view=net-5.0).

- `Options` is the value of the enum in [.NET RegexOptions](https://docs.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regexoptions?view=net-5.0).

- `Ordinal` is the ordinal to extract from the tag or -1 to extract all.

- `Index` is as [DicomTagConstraint](#dicomtagconstraint).

- `discriminator` is as [DicomConstraint](dicomconstraint).

#### OrderedDateTimeConstraint, OrderedDoubleConstraint, OrderedIntConstraint, OrderedStringConstraint

These contain an ordering function and a DICOM tag that the ordering function applies to.

```json
{
  "Function": {
    "Order": string, one of "Never", "LessThan", "Equal", "LessThanOrEqual", "GreaterThan", "NotEqual", "Always",
    "Value": object, one of date/time, number, or for strings {
      "Value": string,
      "ComparisonType": number, 0 for case sensitive string comparisons, 1 for case insensitive.
    },
    "Ordinal": 0
  },
  "Index": {
    "Group": number,
    "Element": number
  },
  "discriminator": string, one of "OrderedDateTimeConstraint", "OrderedDoubleConstraint", "OrderedIntConstraint", "OrderedStringConstraint"
}
```

For example:

```json
{
  "Function": {
    "Order": "Equal",
    "Value": {
      "Value": "HEAD",
      "ComparisonType": 0
    },
    "Ordinal": 0
  },
  "Index": {
    "Group": 24,
    "Element": 21
  },
  "discriminator": "OrderedStringConstraint"
}
```

Where:

- `Function` is the ordering function to apply to the DICOM tag.

    - `Order` is the required relation between the supplied `Value` and the value of the DICOM tag.

    - `Value` is the value to test the DICOM tag against.

    - `Ordinal` is the ordinal to extract from the tag or -1 to extract all.

- `Index` is as [DicomTagConstraint](#dicomtagconstraint).

- `discriminator` is as [DicomConstraint](dicomconstraint).

#### UIDStringOrderConstraint

This is a variant of the OrderedDateTimeConstraint etc, but contains a constraint for a DICOM UID tag.

```json
{
  "Function": {
    "Order": string, order as above,
    "Value": {
      "Value": string,
      "ComparisonType": number, as above
    },
    "Ordinal": number
  },
  "Index": {
    "Group": number,
    "Element": number
  },
  "discriminator": "UIDStringOrderConstraint"
}
```

For example:

```json
{
  "Function": {
    "Order": "Equal",
    "Value": {
      "Value": "1.2.840.10008.5.1.4.1.1.2",
      "ComparisonType": 0
    },
    "Ordinal": 0
  },
  "Index": {
    "Group": 8,
    "Element": 22
  },
  "discriminator": "UIDStringOrderConstraint"
}
```

Where the `Function` and `Index` are as above, except that the constraint test is applied to a DICOM UID tag. 

#### TimeOrderConstraint

This is a variant of the OrderedDateTimeConstraint etc, but for a tag of TM value representation.

```json
{
  "Function": {
    "Order": string, order as above,
    "Value": number, a TimeSpan in .NET Timespan serialization format,
    "Ordinal": number
  },
  "Index": {
    "Group": number,
    "Element": number
  },
  "discriminator": "TimeOrderConstraint"
}
```

For example:

```json
{
  "Function": {
    "Order": "GreaterThanOrEqual",
    "Value": "16:05:42.7380000",
    "Ordinal": 0
  },
  "Index": {
    "Group": 25447,
    "Element": 60116
  },
  "discriminator": "TimeOrderConstraint"
}
```

Where the `Function` and `Index` are as above, except that the constraint test is applied to a DICOM tag TimeOfDay property.

## Licensing

[MIT License](LICENSE)

**You are responsible for the performance, the necessary testing, and if needed any regulatory clearance for
 any of the models produced by this toolbox.**

## Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit [https://cla.opensource.microsoft.com](https://cla.opensource.microsoft.com/) .

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/) . For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Microsoft Open Source Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/) 
