# Observe Entity Explorer

Observe Entity Explorer allows discovery of dependencies between Observe objects (datasets, worksheets, dashboards, monitors, stages, parameters).

![Dataset Graph](/docs/screenshots/details/dataset/DatasetDependencies.png?raw=true)

> **_NOTE:_**  This tool uses the Observe GraphQL API, which is not [yet] a publicly documented API. Therefore this tool, or any application that you develop that is derived from it may stop working without notice in the future. If you have specific API needs that are not covered by our currently documented API found at https://developer.observeinc.com/, please contact us for more information.

## Install Application

Observe Entity Explorer can run on Windows, Mac or Linux. Binaries are in [Releases](../../releases/latest).

### Install on OSX

1. Install .NET 8.0 SDK

    * Download [Microsoft Downloads web site](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) with all instructions, or:
        * Direct [x64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.200-macos-x64-installer) installer
        * Direct [arm64 (M1/M2)](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.200-macos-arm64-installer) installer
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

First, install .NET 9.0 SDK from [Microsoft Downloads web site](https://dotnet.microsoft.com/en-us/download/dotnet/9.0).

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

## Open the Web Site

When application starts, it opens a web interface (default http://localhost:50110). Open that web site in your web browser:

```console
./observe-entity-explorer

info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://localhost:50110
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Production
info: Microsoft.Hosting.Lifetime[0]
      Content root path: <path...>\observe-entity-explorer.<version..>
```

You can change the port of the web site in `Kestrel.Endpoints.Http.Url` property in `appsettings.json`.

## Connect Options

### How to Authenticate

* Account can be either
  * Full URL of the environment `https://#####.observeinc.com`
  * Just customer ID of the environment `#####`. Production URL `observeinc.com` is assumed
  * Host name `#####.observeinc.com` without https://
* Username
  * Typically it is your email
* Password
  * Specify your password
* Token
  * Specify your API token if you have one issued to you
  * OR
  * Specify token taken from the GraphQL Bearer authentication in your browser session (not recommended) 
* Delegate (must be > Reader)
  * Log into Observe UI to approve your sign-in
  * Return to this UI to complete sign-in

![Connect](/docs/screenshots/authetication/ConnectNew.png?raw=true)

Your connections are saved in `~/.observe/observe-entity-explorer.auth-cache.json` file. You can switch between them any time.

### Connect with Delegate Login

To connect with delegation, you must have > Reader role. If you have it:

* Specify account and username
* Click "Connect with Observe UI" button
* Click on "Go to Observe to approve login request link"
* In new browser window, authenticate into Observe
* Confirm your sign-in request on "Accounts Settings" page
* Back in Observe Entity Explorer, click "Check for Completion and Connect" button to finish signing in 

![Connect Delegate](/docs/screenshots/authetication/ConnectDelegate.png?raw=true)

### Connect with Multiple Accounts

You can open new tab with different connection ID and watch multiple accounts at once.

![Multiple Connections](/docs/screenshots/authetication/ConnectSaved.png?raw=true)

## Environment Summary

Summary screen shows numbers and types of entities in your Observe environment and links to the detail pages to get more information.

You can see entire tree of relationships of all objects in All Relationships button.

Finally you can search all OPAL in Search OPAL button (regex supported)

![Environment Summary](/docs/screenshots/summary/EnvironmentSummary.png?raw=true)

## Dataset List

Datasets page lists all datasets grouped by type (DataStream, Event, Resource, Log, Metric, etc).

Column | Description
-- | --
View | Open the [Dataset Detail](#dataset-detail) page
Data | Open object page in Observe
Edit | Open object page in Observe in edit mode
Type | Type of object (`Event`, `Resource`, `Interval`)
Origin | What created this object (`System`, `App`, `Terraform`, `User`, `DataStream`)
ID | Object ID
Name | Object Name
Description | Description label, if present
Parts | Fields, primary and foreign keys, related keys
Uses | What this object uses as data inputs and data links
Used By | What uses this object (`Data`, `Dashboard`, `Monitor`)
Created | Who and when created this object
Updated | Who and when updates this object last
Trns+Bfill 1h | `credits_transform` + `credits_backfill` in last 1 hour
Trns+Bfill 1d | `credits_transform` + `credits_backfill` in last 1 day
Trns+Bfill 1w | `credits_transform` + `credits_backfill` in last 1 week
Query 1h | `credits_adhoc_query` + `credits_inlined_query` in last 1 hour
Query 1d | `credits_adhoc_query` + `credits_inlined_query` in last 1 day
Query 1w | `credits_adhoc_query` + `credits_inlined_query` in last 1 week
Accel Conf. | Configured staleness target of the dataset
Accel Eff. | The target staleness of this dataset when taking downstream datasets
Accel Actl. | Staleness of the dataset (averaged over some moving window)
Accel Range | Actual accelerated range of the dataset

![Dataset List](/docs/screenshots/list/dataset/Datasets.png?raw=true)

## Dashboard List

Dashboards list all dashboards and their core statistics.

Column | Description
-- | --
View | Links to the [Dashboard Detail](#dashboard-detail) page
Data | Open object page in Observe
Edit | Open object page in Observe in edit mode
Origin | What created this object (`System`, `App`, `User`)
ID | Object ID
Name | Object Name
Desc. | Description label, if present
Sections | Number of sections in dashboard
Widgets | Number of widgets in dashboard
Stages | Number of stages in dashboard
Parameters | Number of parameters in dashboard
Uses | What this object uses as data inputs and data links
Created | Who and when created this object
Updated | Who and when updates this object last
Query 1h | `credits_adhoc_query` + `credits_inlined_query` in last 1 hour
Query 1d | `credits_adhoc_query` + `credits_inlined_query` in last 1 day
Query 1w | `credits_adhoc_query` + `credits_inlined_query` in last 1 week

![Dashboard List](/docs/screenshots/list/dashboard/Dashboards.png?raw=true)

## Monitor (legacy) List

Monitor (legacy) page shows all legacy monitors grouped by type (Metric, Log, Count, Promotion, Resource).

Column | Description
-- | --
View | Links to the [Monitor (legacy) Detail](#monitor-v1-detail) page
Notif | Open notification list page in Observe
Edit | Open object page in Observe in edit mode
Type | Type of the monitor (`Count`, `Promote`, `Threshold`, `Facet`, `Log`)
Origin | What created this object (`System`, `App`, `Terraform`, `User`)
ID | Object ID
Name | Object Name
Comm. | Comment if it exists
Enabled | Is the monitor enabled
Actions | Number of actions in monitor
Stages | Number of stages in monitor
Uses | What this object uses as data inputs and data links
Created | Who and when created this object
Updated | Who and when updates this object last
Transform 1h | `credits_monitor` in last 1 hour
Transform 1d | `credits_monitor` in last 1 day
Transform 1w | `credits_monitor` in last 1 week
Accel Conf. | Configured staleness target of the dataset
Accel Eff. | The target staleness of this dataset when taking downstream datasets
Accel Actl. | Staleness of the dataset (averaged over some moving window)
Accel Range | Actual accelerated range of the dataset

![Monitor List](/docs/screenshots/list/monitor/Monitors.png?raw=true)

## Monitor v2 List

Monitors v2 page shows all new type monitors grouped by type (Metric, Count, Resource).

Column | Description
-- | --
View | Links to the [Monitor v2 Detail](#monitor-v2-detail) page
Edit | Open object page in Observe in edit mode
Type | Type of the monitor (`Count`, `Promote`, `Threshold`)
Origin | What created this object (`System`, `App`, `Terraform`, `User`)
ID | Object ID
Name | Object Name
Comm. | Comment if it exists
Enabled | Is the monitor enabled
Actions | Number of actions in monitor
Stages | Number of stages in monitor
Uses | What this object uses as data inputs and data links
Created | Who and when created this object
Updated | Who and when updates this object last
Transform 1h | `credits_monitor` in last 1 hour
Transform 1d | `credits_monitor` in last 1 day
Transform 1w | `credits_monitor` in last 1 week

![Monitor List](/docs/screenshots/list/monitorv2/Monitors.png?raw=true)

## Worksheets List

Worksheets page shows all user worksheets.

Column | Description
-- | --
View | Links to the [Worksheet Detail](#worksheet-detail) page
Data | Open object page in Observe in data/edit mode
Origin | What created this object (`System`, `App`, `Terraform`, `User`)
ID | Object ID
Name | Object Name
Stages | Number of stages in worksheet
Parameters | Number of parameters in worksheet
Uses | What this object uses as data inputs and data links
Created | Who and when created this object
Updated | Who and when updates this object last

![Worksheets List](/docs/screenshots/list/worksheet/Worksheets.png?raw=true)

## Metrics List

Metrics page shows all metrics

Column | Description
-- | --
View | Links to the [Dataset Detail](#dataset-detail) page
Origin | What created dataset (`System`, `App`, `Terraform`, `User`)
Dataset ID | Dataset ID of dataset defining this metric
Dataset | Dataset Name of dataset defining this metric
View | Links to the [Metric Detail](#metric-detail) page
Data | Open object page in Observe in data mode
Name | Name of metric
Desc. | Description of metric, if available
Type | Type of metric
Unit | Unit of metric
Rollup | Type of metric rollup
Agg. | Type of metric aggregation
Bucket Size | Size of the default metric aggregation bucket
Cardinality | Detected cardinality
Points | Detected points
Link Labels | Number of detected link labels
Tags | Number of detected tags
State | State of metric
Last Data | When last data point was received

![Metrics List](/docs/screenshots/list/metric/Metrics.png?raw=true)

## RBAC Settings

Role based access control settings for users, groups and statements.

![Users, Groups and Statements](/docs/screenshots/list/rbac/RBACUsersGroupsStatements.png?raw=true)

Role based access control memberships.

![Users, Groups and Statements](/docs/screenshots/list/rbac/RBACMemberships.png?raw=true)

## Search OPAL

Code search results show OPAL with hits highligted. You can show and hide all the code stages, and also navigate to the detail pages.

![Code Search Results](/docs/screenshots/codesearch/CodeSearchResults.png?raw=true)

## Dataset Detail

Summary information about the dataset.

![Dataset Details](/docs/screenshots/details/dataset/DatasetDetails.png?raw=true)

Dataset acceleration settings.

![Dataset Acceleration](/docs/screenshots/details/dataset/DatasetAcceleration.png?raw=true)

List of all objects feeding data into this dataset.

![Dataset Ancestors](/docs/screenshots/details/dataset/DatasetAncestors.png?raw=true)

List of all objects using data from this dataset.

![Dataset Descendants](/docs/screenshots/details/dataset/DatasetDescendants.png?raw=true)

Visual diagram showing relationship between datasets and dashboards related to this dataset.

![Dataset Graph](/docs/screenshots/details/dataset/DatasetDependencies.png?raw=true)

Information about each stage in the dataset. You can click "Show/Hide input and output tables" to remove details and just focus on OPAL code.

![Dataset Stages List](/docs/screenshots/details/dataset/DatasetStagesList.png?raw=true)

This table is followed by the repeated sections for each stage, containing:

![Dataset Stage Detail](/docs/screenshots/details/dataset/DatasetStageDetail.png?raw=true)

Visual diagram showing relationship between inputs and stages of this dataset.

![Dataset Stages Graph](/docs/screenshots/details/dataset/DatasetStageDependencies.png?raw=true)

List of all dashboards using this dataset.

![Dataset Related Dashboards](/docs/screenshots/details/dataset/DatasetRelatedDashboards.png?raw=true)

List of all monitors (legacy) using this dataset.

![Dataset Related Monitors (legacy)](/docs/screenshots/details/dataset/DatasetRelatedMonitors.png?raw=true)

List of all worksheets using this dataset.

![Dataset Related Worksheets](/docs/screenshots/details/dataset/DatasetRelatedWorksheets.png?raw=true)

List of all Metrics defined by this dataset.

![Dataset Metrics](/docs/screenshots/details/dataset/DatasetMetrics.png?raw=true)

## Dashboard Detail

Summary information about the dashboard.

![Dashboard Details](/docs/screenshots/details/dashboard/DashboardDetails.png?raw=true)

List of all objects feeding data into this dashboard.

![Dataset Ancestors](/docs/screenshots/details/dashboard/DashboardAncestors.png?raw=true)

List of all objects using data from this dataset.

Visual diagram showing relationship between datasets related to this dashboard.

![Dataset Graph](/docs/screenshots/details/dashboard/DashboardDependencies.png?raw=true)

Information about each parameter in the dashboard.

![Dashboard Parameters](/docs/screenshots/details/dashboard/DashboardParameters.png?raw=true)

Information about each stage in the dashboard. You can click "Show/Hide input and output tables" to remove details and just focus on OPAL code. You can click "Show/Hide dashboard widget image preview" to remove images and just focus on OPAL code.

![Dashboard Stages](/docs/screenshots/details/dashboard/DashboardStages.png?raw=true)

This table is followed by the repeated sections for each stage, containing:

![Dashboard Stage Detail](/docs/screenshots/details/dashboard/DashboardStageDetail.png?raw=true)

Visual diagram showing relationship between inputs and stages of this dashboard.

![Dashboard Stages Graph](/docs/screenshots/details/dashboard/DashboardStageDependencies.png?raw=true)

## Monitor v1 Detail

Summary information about the monitor (legacy).

![Monitor Details](/docs/screenshots/details/monitor/MonitorDetails.png?raw=true)

List of all objects providing data to this monitor.

![Monitor Ancestors](/docs/screenshots/details/monitor/MonitorAncestors.png?raw=true)

List of all objects supporting this monitor.

![Monitor Supporting](/docs/screenshots/details/monitor/MonitorSupporting.png?raw=true)

Visual diagram showing relationship between datasets related to this monitor.

![Dataset Graph](/docs/screenshots/details/monitor/MonitorDependencies.png?raw=true)

## Monitor v2 Detail

Summary information about the monitor v2.

![Monitor Details](/docs/screenshots/details/monitorv2/MonitorDetails.png?raw=true)

Visual diagram showing relationship between datasets related to this monitor.

![Dataset Graph](/docs/screenshots/details/monitorv2/MonitorDependencies.png?raw=true)

## Worksheet Detail

Summary information about the worksheet.

![Worksheet Details](/docs/screenshots/details/worksheet/WorksheetDetails.png?raw=true)

## Metric Detail

Summary information about the metric, and what components (dataset, monitor, dashboard, worksheet) are using it.

![Metric Details](/docs/screenshots/details/metric/MetricDetails.png?raw=true)

## Datastream and Tokens

Datastreams list.

Column | Description
-- | --
View | Links to the [Dataset Details](#dataset-detail) page for the dataset of this datastream
Data | Open datastream dataset data page
Edit | Open datastream page
Origin | What created this object (`System`, `App`, `Terraform`, `User`)
ID | Object ID
Name | Object Name
Description | Comment if it exists
Enabled | Is the datastream enabled
Tokens | Number of tokens in datastream
Retention | Retention setting in days
Bytes | Bytes ingested in last hour
GB | Gigabytes ingested in last hour
TB | Terabytes ingested in last hour
Created | Who and when created this object
Updated | Who and when updates this object last

Tokens list.

Column | Description
-- | --
Data | Open datastream dataset data page filtered to this token
Edit | Open datastream page
Origin | What created this object (`System`, `App`, `Terraform`, `User`)
Datastream | Datastream this token sends data to
Type | Type of token (`token`, `poller`, `filedrop`)
ID | Object ID
Name | Object Name
Description | Comment if it exists
Enabled | Is the datastream enabled
Tokens | Number of tokens in datastream
Bytes | Bytes ingested in last hour
GB | Gigabytes ingested in last hour
TB | Terabytes ingested in last hour
Start Ingest | When ingestion started
Last Ingest | Last time data was received
Last Error | Last time there was an error receiving data
Created | Who and when created this object
Updated | Who and when updates this object last

![Datastreams and Tokens](/docs/screenshots/list/datastream/DatastreamsAndTokens.png?raw=true)


## Logging

Logs are in `/logs` subfolder below where your application is installed with new files created every day:

```text
logs\Observe.EntityExplorer.Console.2023-10-18.log
logs\Observe.EntityExplorer.Main.2023-10-18.log
logs\Observe.EntityExplorer.ObserveConnection.2023-10-18.log
```
