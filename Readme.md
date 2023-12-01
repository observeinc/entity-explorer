# Observe Entity Explorer

Observe Entity Explorer allows discovery of dependencies between Observe objects (datasets, worksheets, dashboards, monitors, stages, parameters).

## Install Application

Observe Entity Explorer can run on Windows, Mac or most Linux distributions. 

Files are in [Releases](../../releases/latest).

### Install on OSX

1. Install .NET 7.0 SDK

    * [Microsoft Downloads web site](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) with all instructions, or:
        * [x64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-7.0.402-macos-x64-installer) installer
        * [arm64 (M1/M2)](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-7.0.402-macos-arm64-installer) installer
    * Run through the installer 

2. Download [Releases](../../releases/latest)\ `observe-entity-explorer.osx-x64.<version>.zip` for Intel or `observe-entity-explorer.osx-arm64.<version>.zip` for M1/M2 machines, but do not extract the archive yet.

3. Remove the quarantine attribute that will otherwise stop the application from running this command in your terminal:

    ```console
    xattr -d com.apple.quarantine observe-entity-explorer.osx*.zip
    ```

4. Extract the archive:

    ```console
    unzip observe-entity-explorer.osx*.zip
    ```

5. Run the application from the extracted folder:

    ```console
    dotnet ./observe-entity-explorer.dll
    ```

<!-- 4. Run `observe-entity-explorer` from the extracted folder -->

### Install on Windows

1. Download [Releases](../../releases/latest)\ `observe-entity-explorer.win-x64.<version>.zip`

2. Extract the archive using File Explorer or:

    ```powershell
    Expand-Archive observe-entity-explorer.win*.zip
    ```

3. Run the application from the extracted folder:

    ```powershell
    .\observe-entity-explorer.exe
    ```

### Install on Linux

Download [Releases](../../releases/latest)\ `observe-entity-explorer.linux.<version>.zip`, extract the archive and run `observe-entity-explorer`

1. Extract the archive:

    ```console
    unzip observe-entity-explorer.linux*.zip
    ```

2. Run the application from the extracted folder:

    ```console
    ./observe-entity-explorer
    ```

## Usage

## Open the Web Site

When application starts, it opens a web interface at http://localhost:50110. Open that web site in your web browser:

```console
./observe-entity-explorer.exe

info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://localhost:50110
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Production
info: Microsoft.Hosting.Lifetime[0]
      Content root path: C:\observe\observe-entity-explorer-releases\2023.10.18.0\observe-entity-explorer.win.2023.10.18.0
```

## Connect

Authenticate with:

* Full URL of the environment `https://#####.observeinc.com`
* Just customer ID of the environment `#####`. Production URL `observeinc.com` is assumed
* Host name `#####.observeinc.com`

![Connect](/docs/screenshots/authetication/ConnectNew.png?raw=true)

## Multiple Accounts

Your connections are saved. You can switch between them any time. 

You can open new tab with different connection ID.

![Multiple Connections](/docs/screenshots/authetication/ConnectSaved.png?raw=true)

## Entity Summary

Summary screen shows basic statistics about types of entities in your Observe environment and links to the [Dataset List](#dataset-list) [Dashboard List](#dashboard-list) for getting more details.

![Environment Summary](/docs/screenshots/summary/EnvironmentSummary.png?raw=true)

## Dataset List

Select Dataset screen shows all datasets grouped by type (DataStream, Event, Resource, Log, Metric, etc).

Column | Description
-- | --
Details | Open the [Dataset Details](#dataset-detail) page
View | Open object page in Observe
Edit | Open object page in Observe in edit mode
Type | Type of object (`Event`, `Resource`, `Interval`)
Origin | What created this object (`System`, `App`, `Terraform`, `User`, `DataStream`)
ID | Object ID
Name | Object Name
Component | Fields, primary and foregn keys, related keys
Uses | What this object uses as data inputs and data links
Used By | What uses this object (`Data`, `Dashboard`, `Monitor`)
Created | Who and when created this object
Updated | Who and when updates this object last

![Dataset List](/docs/screenshots/list/dataset/SelectDataset.png?raw=true)

## Dashboard List

Select Dashboard screen shows all dashboards.

Column | Description
-- | --
Details | Links to the [Dashboard Details](#dashboard-detail) page
View | Open object page in Observe
Edit | Open object page in Observe in edit mode
Origin | What created this object (`System`, `App`, `Terraform`, `User`, `DataStream`)
ID | Object ID
Name | Object Name
Sections | Number of sections in dashboard
Widgets | Number of widgets in dashboard
Stages | Number of stages in dashboard
Parameters | Number of parameters in dashboard
Uses | What this object uses as data inputs and data links
Created | Who and when created this object
Updated | Who and when updates this object last

![Dashboard List](/docs/screenshots/list/dashboard/SelectDashboard.png?raw=true)

## Dataset Detail

![Dataset Detail](/docs/screenshots/details/dataset/DatasetDetails.png?raw=true)

### Summary - Dataset

Summary information about the Dataset.

Section | Description
-- | --
Observe | Open object page in Observe
Name | Object name and ID
Description | Dataset description
Type | Type of dataset
Icon | Icon of the object
Origin | What created this object (`System`, `App`, `Terraform`, `User`, `DataStream`)
Created | Who and when created this object
Updated | Who and when updates this object last
Important Fields | From/To/Label designated fields
Fields | Table of fields and their datatypes
Related Key | List of related keys
Foreign Key | List of foreign keys
Acceleration | Dataset acceleration settings

### Ancestors

List of all datasets feeding data into this dataset.

### Descendants

List of all datasets using data from this dataset.

### Related Datasets Graph

Visual diagram showing relationship between datasets and dashboards related to this dataset.

![Dataset Graph](/docs/screenshots/details/dataset/DatasetDependencies.png?raw=true)

### Stages - Dataset

Information about each Stage in the Dataset.

Section | Description
-- | --
Name | Object name and ID
Inputs | Objects providing data and links to this stage
Inputs | Objects this stage is providing data and links
OPAL | The OPAL pipeline for this stage

### Stages Graph - Dataset

Visual diagram showing relationship between inputs and stages of this dataset.

![Dataset Stages Graph](/docs/screenshots/details/dataset/DatasetStageDependencies.png?raw=true)

### Related Dashboards

## Dashboard Detail

![Dataset Detail](/docs/screenshots/details/dashboard/DashboardDetails.png?raw=true)

### Summary - Dashboard

Summary information about the Dashboard.

Section | Description
-- | --
Observe | Open object page in Observe
Name | Object name and ID
Source | What created this object (`System`, `App`, `Terraform`, `User`, `DataStream`)
Components | Number of various components in dashboaard
Created | Who and when created this object
Updated | Who and when updates this object last

### Stages - Dashboard

Information about each Stage in the Dashboard.

Section | Description
-- | --
Name | Object name and ID
Inputs | Objects providing data and links to this stage
Inputs | Objects this stage is providing data and links
OPAL | The OPAL pipeline for this stage

### Stages Graph - Dashboard

Visual diagram showing relationship between inputs and stages of this dashboard.

## Logging

Logs are in `/logs` subfolder below where your application is installed with new files created every day:

```text
logs\Observe.EntityExplorer.Console.2023-10-18.log
logs\Observe.EntityExplorer.Main.2023-10-18.log
logs\Observe.EntityExplorer.ObserveConnection.2023-10-18.log
```

## Links

[Notes in Notion](https://www.notion.so/observeinc/Observe-Entity-Explorer-f9fa4862610f452780f652b676142ecd?pvs=4)

[#entity-explorer in Slack](https://observeinc.slack.com/archives/C05V6JN78SX)
