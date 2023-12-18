# Observe Entity Explorer

Observe Entity Explorer allows discovery of dependencies between Observe objects (datasets, worksheets, dashboards, monitors, stages, parameters).

![Dataset Graph](/docs/screenshots/details/dataset/DatasetDependencies.png?raw=true)

## Install Application

Observe Entity Explorer can run on Windows, Mac or Linux. Compiled are in [Releases](../../releases/latest).

### Install on OSX

1. Install .NET 8.0 SDK

    * Download [Microsoft Downloads web site](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) with all instructions, or:
        * Direct [x64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.100-macos-x64-installer) installer
        * Direct [arm64 (M1/M2)](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.100-macos-arm64-installer) installer
    * OR
    * Homebrew [brew install dotnet](https://formulae.brew.sh/cask/dotnet)

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

1. Download [Releases](../../releases/latest)\ `observe-entity-explorer.linux-x64.<version>.zip`

2. Extract the archive:

    ```console
    unzip observe-entity-explorer.linux*.zip
    ```

3. Run the application from the extracted folder:

    ```console
    ./observe-entity-explorer
    ```

## Run from Source

You can also run it from source.

First, install .NET 8.0 SDK from [Microsoft Downloads web site](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

Then clone, build and run:

```bash
# Clone repo to local folder
git clone https://github.com/observeinc/entity-explorer entity-explorer

# Change to local folder
cd entity-explorer

# For Win and Linux, build the project into single self-contained binary. Choose the right runtime
dotnet publish observe-entity-explorer.csproj --self-contained --runtime <linux-x64|win-x64> -p:PublishSingleFile=true -o bin/publish/entity-explorer

# For OSX, build the project into multiple files to be ran by dotnet command later. Choose the right runtime
dotnet publish observe-entity-explorer.csproj --self-contained --runtime <osx-x64|osx-arm64> -p:PublishSingleFile=false -o bin/publish/entity-explorer

# Run the application from where it was published to
bin/publish/entity-explorer/observe-entity-explorer & 
```

## Usage

## Open the Web Site

When application starts, it opens a web interface at http://localhost:50110. Open that web site in your web browser:

```console
./observe-entity-explorer

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

Authenticate options:

* Account
  * Full URL of the environment `https://#####.observeinc.com`
  * Just customer ID of the environment `#####`. Production URL `observeinc.com` is assumed
  * Host name `#####.observeinc.com`
* Username
  * Typically it is your email
* Password
  * Specify your password
* Token
  * Specify your API token if you have one
  * Specify token taken from the GraphQL Bearer authentication in your browser devtools

![Connect](/docs/screenshots/authetication/ConnectNew.png?raw=true)

## Multiple Accounts

Your connections are saved. You can switch between them any time.

You can open new tab with different connection ID.

![Multiple Connections](/docs/screenshots/authetication/ConnectSaved.png?raw=true)

## Entity Summary

Summary screen shows basic statistics about types of entities in your Observe environment and links to the [Dataset List](#dataset-list) [Dashboard List](#dashboard-list) for getting more details.

You can also see the entire tree of relationships of all objects in All Relationships button.

![Environment Summary](/docs/screenshots/summary/EnvironmentSummary.png?raw=true)

## Dataset List

Select Dataset screen shows all datasets grouped by type (DataStream, Event, Resource, Log, Metric, etc).

Column | Description
-- | --
Details | Open the [Dataset Detail](#dataset-detail) page
View | Open object page in Observe
Edit | Open object page in Observe in edit mode
Type | Type of object (`Event`, `Resource`, `Interval`)
Origin | What created this object (`System`, `App`, `Terraform`, `User`, `DataStream`)
ID | Object ID
Name | Object Name
Desc | Description label, if present
Components | Fields, primary and foregn keys, related keys
Uses | What this object uses as data inputs and data links
Used By | What uses this object (`Data`, `Dashboard`, `Monitor`)
Created | Who and when created this object
Updated | Who and when updates this object last

![Dataset List](/docs/screenshots/list/dataset/SelectDataset.png?raw=true)

## Dashboard List

Select Dashboard screen shows all dashboards.

Column | Description
-- | --
Details | Links to the [Dashboard Detail](#dashboard-detail) page
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

## Monitor List

Select Monitor screen shows all monitors.

Column | Description
-- | --
Details | Links to the [Monitor Detail](#monitor-detail) page
Notif | Open notification list page in Observe
Edit | Open object page in Observe in edit mode
Type | Type of the monitor (`Count`, `Promote`, `Threshold`, `Facet`, `Log`)
Origin | What created this object (`System`, `App`, `Terraform`, `User`, `DataStream`)
ID | Object ID
Name | Object Name
Comm. | Comment if it exists
Enabled | Is the monitor enabled
Actions | Number of actions in monitor
Stages | Number of stages in monitor
Uses | What this object uses as data inputs and data links
Created | Who and when created this object
Updated | Who and when updates this object last

![Monitor List](/docs/screenshots/list/monitor/SelectMonitor.png?raw=true)

## Dataset Detail

### Summary - Dataset

Summary information about the dataset.

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

![Dataset Details](/docs/screenshots/details/dataset/DatasetDetails.png?raw=true)

### Ancestors - Dataset

List of all datasets feeding data into this dataset.

![Dataset Ancestors](/docs/screenshots/details/dataset/DatasetAncestors.png?raw=true)

### Descendants - Dataset

List of all datasets using data from this dataset.

![Dataset Descendants](/docs/screenshots/details/dataset/DatasetDescendants.png?raw=true)

### Related Datasets Graph - Dataset

Visual diagram showing relationship between datasets and dashboards related to this dataset.

![Dataset Graph](/docs/screenshots/details/dataset/DatasetDependencies.png?raw=true)

### Stages - Dataset

Information about each stage in the dataset.

Section | Description
-- | --
Details | Link to the Stage in the subsequent List
Type | Type of the stage (`table`, `graph` etc)
Name | Name of the stage
ID | ID of the stage
Uses | Objects providing data and links to this stage
Used By | Objects this stage is providing data and links to

![Dataset Stages List](/docs/screenshots/details/dataset/DatasetStagesList.png?raw=true)

This table is followed by the repeated sections for each stage, containing:

Section | Description
-- | --
Name | Object name and ID
Inputs | Objects providing data and links to this stage
Used By | Objects this stage is providing data and links to
OPAL | The OPAL pipeline for this stage

![Dataset Stage Detail](/docs/screenshots/details/dataset/DatasetStageDetail.png?raw=true)

### Stages Graph - Dataset

Visual diagram showing relationship between inputs and stages of this dataset.

![Dataset Stages Graph](/docs/screenshots/details/dataset/DatasetStageDependencies.png?raw=true)

### Related Dashboards

List of all dashboards using this dataset.

![Dataset Related Dashboards](/docs/screenshots/details/dataset/DatasetRelatedDashboards.png?raw=true)

### Related Monitors

List of all monitors using this dataset.

![Dataset Related Dashboards](/docs/screenshots/details/dataset/DatasetRelatedMonitors.png?raw=true)

## Dashboard Detail

### Summary - Dashboard

Summary information about the dashboard.

Section | Description
-- | --
Observe | Open object page in Observe
Name | Object name and ID
Source | What created this object (`System`, `App`, `Terraform`, `User`, `DataStream`)
Components | Number of various components in dashboard
Created | Who and when created this object
Updated | Who and when updates this object last

![Dashboard Details](/docs/screenshots/details/dashboard/DashboardDetails.png?raw=true)

### Ancestors/Inputs - Dashboard

List of all datasets feeding data into this dashboard.

### Related Datasets - Dashboard

Visual diagram showing relationship between datasets related to this dashboard.

![Dataset Graph](/docs/screenshots/details/Dashboard/DashboardDependencies.png?raw=true)

### Parameters - Dashboard

Information about each parameter in the dashboard.

Section | Description
-- | --
Name | Display name of parameter
ID | ID of parameter
Type | Type of parameter (`resource-input`, `single-select`, `text`, `numeric`, `input`)
Data Type | Data Type of parameter ()
Source Type | What kind of object is providing data to this parameter (`Dataset`, `Stage` or empty for text entry)
Source Object | Link to the object that is used as input
Source Column | The name of the column in the dataset or stage that is being used as value in the drop-down
Default Value | Default value, if specified
Allow Empty | Is empty value allowed

![Dashboard Parameters](/docs/screenshots/details/dashboard/DashboardParameters.png?raw=true)

### Stages - Dashboard

Information about each stage in the dashboard.

Section | Description
-- | --
Details | Link to the stage in the subsequent list
Type | Type of the stage (`table`, `graph` etc)
Name | Name of the stage
ID | ID of the stage
Uses | Objects providing data and links to this stage
Used By | Objects this stage is providing data and links to
Params | Which parameters are being used by the stage

![Dashboard Stages](/docs/screenshots/details/dashboard/DashboardStages.png?raw=true)

This table is followed by the repeated sections for each stage, containing:

Section | Description
-- | --
Name | Object name and ID
Inputs | Objects providing data and links to this stage
Used By | Objects this stage is providing data and links to
OPAL | The OPAL pipeline for this stage

![Dashboard Stage Detail](/docs/screenshots/details/dashboard/DashboardStageDetail.png?raw=true)

### Stages Graph - Dashboard

Visual diagram showing relationship between inputs and stages of this dashboard.

![Dashboard Stages Graph](/docs/screenshots/details/dashboard/DashboardStageDependencies.png?raw=true)

## Monitor Detail

### Summary - Monitor

Summary information about the monitor.

Section | Description
-- | --
Observe | Open object page in Observe
Name | Object name and ID
Comment | Comment if present
Type | Type of the monitor
Source | What created this object (`System`, `App`, `Terraform`, `User`, `DataStream`)
Components | Number of various components in dashboard
Enabled | Is this monitor enabled
Template | Is this monitor template
Created | Who and when created this object
Updated | Who and when updates this object last
Settings | Monitor settings
Notifications | Monitor notification settings
Info | Monitor detail information

![Monitor Details](/docs/screenshots/details/monitor/MonitorDetails.png?raw=true)

### Ancestors - Monitor

List of all datasets providing data to this monitor.

![Monitor Ancestors](/docs/screenshots/details/monitor/MonitorAncestors.png?raw=true)

### Supporting - Monitor

List of all datasets supporting this monitor.

![Monitor Ancestors](/docs/screenshots/details/monitor/MonitorSupporting.png?raw=true)

### Related Datasets Graph - Monitor

Visual diagram showing relationship between datasets related to this monitor.

![Dataset Graph](/docs/screenshots/details/monitor/MonitorDependencies.png?raw=true)

### Stages - Monitor

Information about each stage in the monitor.

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