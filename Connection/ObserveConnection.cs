using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Observe.EntityExplorer.DataObjects;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using System.Web;

namespace Observe.EntityExplorer
{

    class ObserveConnection
    {

        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger loggerConsole = LogManager.GetLogger("Observe.EntityExplorer.Console");

        #region Observe Authentication

        public static string Authenticate_Username_Password(AuthenticatedUser currentUser, string password)
        {
            string requestBody = String.Format("{{\"user_email\": \"{0}\", \"user_password\": \"{1}\", \"tokenName\": \"Authentication Token for Observe Entity Explorer\"}}", currentUser.UserName, password);

            // https://docs.observeinc.com/en/latest/content/common-topics/FAQ.html#how-do-i-create-an-access-token-that-can-do-more-than-just-ingest-data
            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                "v1/login",
                "application/json", 
                requestBody,
                "application/json",
                String.Empty, 
                String.Empty);

            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                throw new AuthenticationException(String.Format("Call to {0}v1/login with user {1} returned {2} {3}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, results.Item3, results.Item1));
            }
        }

        public static string Authenticate_SSO_Start(AuthenticatedUser currentUser)
        {
            string requestBody = String.Format("{{\"userEmail\": \"{0}\", \"clientToken\": \"Authentication Token for Observe Entity Explorer requested by {1}@{2} on {3:u}\", \"integration\": \"observe-tool-abdaf0\"}}", currentUser.UserName, Environment.UserName, Environment.MachineName, DateTime.UtcNow);

            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                "v1/login/delegated",
                "application/json", 
                requestBody,
                "application/json",
                String.Empty, 
                String.Empty);

            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                throw new AuthenticationException(String.Format("Call to {0}v1/login/delegated with user {1} returned {2} {3}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, results.Item3, results.Item1));
            }
        }

        public static string Authenticate_SSO_Complete(AuthenticatedUser currentUser, string delegateToken)
        {
            Tuple<string, List<string>, HttpStatusCode> results = apiGET(
                currentUser.CustomerEnvironmentUrl,
                String.Format("v1/login/delegated/{0}", delegateToken),
                "application/json", 
                String.Empty, 
                String.Empty);

            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                throw new AuthenticationException(String.Format("Call to {0}v1/login/delegated/[token] with user {1} returned {2} {3}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, results.Item3, results.Item1));
            }
        }
        #endregion

        #region User, Customer, Workspace Metadata

        public static string currentUser(AuthenticatedUser currentUser)
        {
            string graphQLQuery = @"query CurrentUser {
  currentUser {
    id
    type
    email
    label
    timezone
    status
    role
    comment
    expirationTime
    workspaces {
      id
      createdBy {
        id
        type
        email
        label
        timezone
        status
        role
        comment
        expirationTime
        customer {
          id
          label
        }
        __typename
      }
      createdDate
      label
      timezone
      layout
      visible
      layout
      customer {
        id
        label
        __typename
      }
    }
    customer {
      id
      label
      __typename
    }
    __typename
  }
}";
            
            JObject queryObject = new JObject();
            queryObject.Add("query", graphQLQuery);
            queryObject.Add("variables", new JObject());
            string queryBody = JSONHelper.getCompactSerializedValueOfObject(queryObject);

            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                "v1/meta",
                "application/json", 
                queryBody,
                "application/json",
                currentUser.CustomerName, 
                currentUser.AuthToken);
            
            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                string queryBeginning = graphQLQuery.Split('\n')[0];
                throw new WebException(String.Format("Call to {0}v1/meta for user {1} with '{2}' returned {3} {4}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, queryBeginning, results.Item3, results.Item1));
            }
        }

        #endregion

        #region Datasets Metadata

        public static string datasetSearch_all(AuthenticatedUser currentUser)
        {
            string graphQLQuery = @"query DatasetSearch($labelMatches: [String!], $projects: [ObjectId!], $columnMatches: [String!], $keyMatchTypes: [String!], $foreignKeyTargetMatches: [String!], $reachableFromDataset: ObjectId) {
  datasetSearch(
    labelMatches: $labelMatches
    projects: $projects
    columnMatches: $columnMatches
    keyMatchTypes: $keyMatchTypes
    foreignKeyTargetMatches: $foreignKeyTargetMatches
    reachableFromDataset: $reachableFromDataset
  ) {
    dataset {
      ... on WorkspaceObject {
        id
        name
        description
        iconUrl
        workspaceId
        managedById
        __typename
      }
      ... on FolderObject {
        folderId
        __typename
      }
      ... on AuditedObject {
        createdDate
        createdByInfo {
          userId
          userLabel
          userTimezone
          __typename
        }
        updatedDate
        updatedByInfo {
          userId
          userLabel
          userTimezone
          __typename
        }
        __typename
      }
      version
      versions
      kind
      path
      source
      validFromField
      validToField
      labelField
      iconUrl
      defaultDashboardId
      defaultInstanceDashboardId
      primaryKey
      keys
      groupingKey {
        elements {
          type
          value
          __typename
        }
        __typename
      }
      foreignKeys {
        targetDataset
        srcFields
        dstFields
        id
        targetStageLabel
        targetLabelFieldName
        __typename
      }
      relatedKeys {
        targetDataset
        srcFields
        dstFields
        label
        __typename
      }
      typedef {
        id
        label
        def {
          fields {
            name
            type {
              rep
              nullable
              def {
                linkDesc {
                  targetDataset
                  targetStageLabel
                  targetLabelField
                  label
                  srcFields
                  dstFields
                  __typename
                }
                __typename
              }
              __typename
            }
            isEnum
            isSearchable
            isHidden
            isConst
            isMetric
            __typename
          }
          __typename
        }
        __typename
      }
      inputs {
        datasetId
        inputRole
        __typename
      }
      transform {
        current {
          query {
            outputStage
            stages {
              id
              params
              pipeline
              layout
              input {
                inputName
                inputRole
                datasetId
                datasetPath
                stageId
                __typename
              }
              __typename
            }
            layout
            __typename
          }
          __typename
        }
        __typename
      }      
      latencyDesired
      effectiveSettings {
        dataset {
          freshnessDesired
          snowflakeSharingEnabled
          __typename
        }
        linkify {
          joinSourceDisabled
          joinTargetDisabled
          __typename
        }
        scanner {
          powerLevel
          __typename
        }
      }
      accelerable
      ... on AccelerableObject {
        accelerationDisabled
        accelerationInfo {
          state
          stalenessSeconds
          alwaysAccelerated
          configuredTargetStalenessSeconds
          targetStalenessSeconds
          effectiveTargetStalenessSeconds
          rateLimitOverrideTargetStalenessSeconds
          acceleratedRanges {
            start
            end
            __typename
          }
          targetAcceleratedRanges {
            start
            end
            __typename
          }        
          freshnessTime
          minimumDownstreamTargetStaleness {
            minimumDownstreamTargetStalenessSeconds
            datasetIds
            monitorIds
            shareIds
            __typename
          }
          effectiveOnDemandMaterializationLength
          errors {
            datasetId
            datasetName
            transformId
            time
            errorText
            __typename
          }
          __typename
        }
        __typename
      }
      interfaces {
        path
        mapping {
          interfaceField
          field
          __typename
        }
        interface {
          name
          path
          interfaceFields {
            interfaceField
            rep
            optional
            __typename
          }
          description
          deprecation
          qualifiers
          workspaceId
          __typename
        }
        __typename
      }
      __typename
    }
    __typename
  }
}";

            JObject queryObject = new JObject();
            queryObject.Add("query", graphQLQuery);            
            queryObject.Add("variables", new JObject());
            
            string queryBody = JSONHelper.getCompactSerializedValueOfObject(queryObject);

            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                "v1/meta",
                "application/json", 
                queryBody,
                "application/json",
                currentUser.CustomerName, 
                currentUser.AuthToken);
            
            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                string queryBeginning = graphQLQuery.Split('\n')[0];
                throw new WebException(String.Format("Call to {0}v1/meta for user {1} with '{2}' returned {3} {4}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, queryBeginning, results.Item3, results.Item1));
            }
        }

        public static string datasetSearch_related(AuthenticatedUser currentUser, string reachableFromDatasetId)
        {
            string graphQLQuery = @"query DatasetSearch($labelMatches: [String!], $projects: [ObjectId!], $columnMatches: [String!], $keyMatchTypes: [String!], $foreignKeyTargetMatches: [String!], $reachableFromDataset: ObjectId) {
  datasetSearch(
    labelMatches: $labelMatches
    projects: $projects
    columnMatches: $columnMatches
    keyMatchTypes: $keyMatchTypes
    foreignKeyTargetMatches: $foreignKeyTargetMatches
    reachableFromDataset: $reachableFromDataset
  ) {
    dataset {
      ... on WorkspaceObject {
        id
        name
        description
        iconUrl
        workspaceId
        managedById
        __typename
      }
      ... on FolderObject {
        folderId
        __typename
      }
      ... on AuditedObject {
        createdDate
        createdByInfo {
          userId
          userLabel
          userTimezone
          __typename
        }
        updatedDate
        updatedByInfo {
          userId
          userLabel
          userTimezone
          __typename
        }
        __typename
      }
      version
      versions
      kind
      path
      source
      validFromField
      validToField
      labelField
      iconUrl
      defaultDashboardId
      defaultInstanceDashboardId
      primaryKey
      keys
      groupingKey {
        elements {
          type
          value
          __typename
        }
        __typename
      }
      foreignKeys {
        targetDataset
        srcFields
        dstFields
        id
        targetStageLabel
        targetLabelFieldName
        __typename
      }
      relatedKeys {
        targetDataset
        srcFields
        dstFields
        label
        __typename
      }
      typedef {
        id
        label
        def {
          fields {
            name
            type {
              rep
              nullable
              def {
                linkDesc {
                  targetDataset
                  targetStageLabel
                  targetLabelField
                  label
                  srcFields
                  dstFields
                  __typename
                }
                __typename
              }
              __typename
            }
            isEnum
            isSearchable
            isHidden
            isConst
            isMetric
            __typename
          }
          __typename
        }
        __typename
      }
      inputs {
        datasetId
        inputRole
        __typename
      }
      metrics {
        name
        nameWithPath
        type
        unit
        description
        rollup
        aggregate
        interval
        suggestedBucketSize
        userDefined
        state
        __typename
      }
      latencyDesired
      effectiveSettings {
        dataset {
          freshnessDesired
          snowflakeSharingEnabled
          __typename
        }
        linkify {
          joinSourceDisabled
          joinTargetDisabled
          __typename
        }
        scanner {
          powerLevel
          __typename
        }
      }
      accelerable
      ... on AccelerableObject {
        accelerationDisabled
        accelerationInfo {
          state
          stalenessSeconds
          alwaysAccelerated
          configuredTargetStalenessSeconds
          targetStalenessSeconds
          effectiveTargetStalenessSeconds
          rateLimitOverrideTargetStalenessSeconds
          acceleratedRanges {
            start
            end
            __typename
          }
          targetAcceleratedRanges {
            start
            end
            __typename
          }
          freshnessTime
          minimumDownstreamTargetStaleness {
            minimumDownstreamTargetStalenessSeconds
            datasetIds
            monitorIds
            __typename
          }
          effectiveOnDemandMaterializationLength
          __typename
        }
        __typename
      }
      interfaces {
        path
        mapping {
          interfaceField
          field
          __typename
        }
        interface {
          name
          path
          interfaceFields {
            interfaceField
            rep
            optional
            __typename
          }
          description
          deprecation
          qualifiers
          workspaceId
          __typename
        }
        __typename
      }
      __typename
    }
    __typename
  }
}";

            JObject queryObject = new JObject();
            queryObject.Add("query", graphQLQuery);
            
            JObject reachableFromDatasetObject = new JObject();
            reachableFromDatasetObject.Add("reachableFromDataset", reachableFromDatasetId);
            queryObject.Add("variables", reachableFromDatasetObject);
            
            string queryBody = JSONHelper.getCompactSerializedValueOfObject(queryObject);

            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                "v1/meta",
                "application/json", 
                queryBody,
                "application/json",
                currentUser.CustomerName, 
                currentUser.AuthToken);
            
            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                string queryBeginning = graphQLQuery.Split('\n')[0];
                throw new WebException(String.Format("Call to {0}v1/meta for user {1} with '{2}' returned {3} {4}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, queryBeginning, results.Item3, results.Item1));
            }
        }

        public static string dataset_single(AuthenticatedUser currentUser, string datasetId)
        {
            string graphQLQuery = @"query Dataset($id: ObjectId!) {
  dataset(id: $id) {
    ... on WorkspaceObject {
      id
      name
      description
      iconUrl
      workspaceId
      managedById
      __typename
    }
    ... on FolderObject {
      folderId
      __typename
    }
    ... on AuditedObject {
      createdDate
      createdByInfo {
        userId
        userLabel
        userTimezone
        __typename
      }
      updatedDate
      updatedByInfo {
        userId
        userLabel
        userTimezone
        __typename
      }
      __typename
    }
    version
    versions
    kind
    path
    source
    validFromField
    validToField
    labelField
    iconUrl
    defaultDashboardId
    defaultInstanceDashboardId
    primaryKey
    keys
    groupingKey {
      elements {
        type
        value
        __typename
      }
      __typename
    }
    foreignKeys {
      targetDataset
      srcFields
      dstFields
      id
      targetStageLabel
      targetLabelFieldName
      __typename
    }
    relatedKeys {
      targetDataset
      srcFields
      dstFields
      label
      __typename
    }
    typedef {
      id
      label
      def {
        fields {
          name
          type {
            rep
            nullable
            def {
              linkDesc {
                targetDataset
                targetStageLabel
                targetLabelField
                label
                srcFields
                dstFields
                __typename
              }
              __typename
            }
            __typename
          }
          isEnum
          isSearchable
          isHidden
          isConst
          isMetric
          __typename
        }
        __typename
      }
      __typename
    }
    inputs {
      datasetId
      inputRole
      __typename
    }
    transform {
      current {
        query {
          outputStage
          stages {
            id
            params
            pipeline
            layout
            input {
              inputName
              inputRole
              datasetId
              datasetPath
              stageId
              __typename
            }
            __typename
          }
          layout
          __typename
        }
        __typename
      }
      __typename
    }
    latencyDesired
    effectiveSettings {
      dataset {
        freshnessDesired
        snowflakeSharingEnabled
        __typename
      }
      linkify {
        joinSourceDisabled
        joinTargetDisabled
        __typename
      }
      scanner {
        powerLevel
        __typename
      }
    }
    accelerable
    ... on AccelerableObject {
      accelerationDisabled
      accelerationInfo {
        state
        stalenessSeconds
        alwaysAccelerated
        configuredTargetStalenessSeconds
        targetStalenessSeconds
        effectiveTargetStalenessSeconds
        rateLimitOverrideTargetStalenessSeconds
        acceleratedRanges {
          start
          end
          __typename
        }
        targetAcceleratedRanges {
          start
          end
          __typename
        }        
        freshnessTime
        minimumDownstreamTargetStaleness {
          minimumDownstreamTargetStalenessSeconds
          datasetIds
          monitorIds
          shareIds
          __typename
        }
        effectiveOnDemandMaterializationLength
        errors {
          datasetId
          datasetName
          transformId
          time
          errorText
          __typename
        }
        __typename
      }
      __typename
    }
    interfaces {
      path
      mapping {
        interfaceField
        field
        __typename
      }
      interface {
        name
        path
        interfaceFields {
          interfaceField
          rep
          optional
          __typename
        }
        description
        deprecation
        qualifiers
        workspaceId
        __typename
      }
      __typename
    }
    __typename
  }
  __typename
}";

            JObject queryObject = new JObject();
            queryObject.Add("query", graphQLQuery);
            JObject idObject = new JObject();
            idObject.Add("id", datasetId);
            queryObject.Add("variables", idObject);
            
            string queryBody = JSONHelper.getCompactSerializedValueOfObject(queryObject);

            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                "v1/meta",
                "application/json", 
                queryBody,
                "application/json",
                currentUser.CustomerName, 
                currentUser.AuthToken);
            
            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                string queryBeginning = graphQLQuery.Split('\n')[0];
                throw new WebException(String.Format("Call to {0}v1/meta for user {1} with '{2}' returned {3} {4}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, queryBeginning, results.Item3, results.Item1));
            }
        }

        #endregion

        #region Dashboards Metadata

        public static string dashboardSearch_all(AuthenticatedUser currentUser, string workspaceId)
        {
            string graphQLQuery = @"query DashboardSearch($terms: DWSearchInput!, $maxCount: Int64) {
  dashboardSearch(terms: $terms, maxCount: $maxCount) {
    dashboards {
      dashboard {
        ... on WorkspaceObject {
          id
          name
          description
          iconUrl
          workspaceId
          managedById
          __typename
        }
        ... on FolderObject {
          folderId
          __typename
        }
        ... on AuditedObject {
          createdDate
          createdByInfo {
            userId
            userLabel
            userTimezone
            __typename
          }
          updatedDate
          updatedByInfo {
            userId
            userLabel
            userTimezone
            __typename
          }
          __typename
        }
        ... on IWorksheetLike {
          parameters {
            id
            name
            defaultValue {
              bool
              float64
              int64
              string
              timestamp
              duration
              __typename
            }
            valueKind {
              type
              arrayItemType {
                keyForDatasetId
              }
              keyForDatasetId
              __typename
            }
            __typename
          }
          parameterValues {
            id
            value {
              bool
              float64
              int64
              string
              timestamp
              duration
              __typename
            }
            __typename
          }
          stages {
            id
            params
            pipeline
            layout
            input {
              inputName
              inputRole
              datasetId
              datasetPath
              stageId
              __typename
            }
            __typename
          }
          layout
        }
        defaultForDatasets
        effectiveSettings {
          scanner {
            powerLevel
            __typename
          }
        }
        __typename
      }
      __typename      
    }
    __typename
  }
}";

            JObject queryObject = new JObject();
            queryObject.Add("query", graphQLQuery);
            JArray workspaceIdsArray = new JArray();
            workspaceIdsArray.Add(workspaceId);
            JObject workspaceIdObject = new JObject();
            workspaceIdObject.Add("workspaceId", workspaceIdsArray);
            JObject variablesObject = new JObject();
            variablesObject.Add("terms", workspaceIdObject);
            queryObject.Add("variables", variablesObject);
            
            string queryBody = JSONHelper.getCompactSerializedValueOfObject(queryObject);

            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                "v1/meta",
                "application/json", 
                queryBody,
                "application/json",
                currentUser.CustomerName, 
                currentUser.AuthToken);
            
            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                string queryBeginning = graphQLQuery.Split('\n')[0];
                throw new WebException(String.Format("Call to {0}v1/meta for user {1} with '{2}' returned {3} {4}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, queryBeginning, results.Item3, results.Item1));
            }
        }

        public static string dashboard_single(AuthenticatedUser currentUser, string dashboardId)
        {
            string graphQLQuery = @"query Dashboard($id: ObjectId!) {
  dashboard(id: $id) {
    ... on WorkspaceObject {
      id
      name
      description
      iconUrl
      workspaceId
      managedById
      __typename
    }
    ... on FolderObject {
      folderId
      __typename
    }
    ... on AuditedObject {
      createdDate
      createdByInfo {
        userId
        userLabel
        userTimezone
        __typename
      }
      updatedDate
      updatedByInfo {
        userId
        userLabel
        userTimezone
        __typename
      }
      __typename
    }
    parameters {
      id
      name
      defaultValue {
        bool
        float64
        int64
        string
        timestamp
        duration
        __typename
      }
      valueKind {
        type
        arrayItemType {
          keyForDatasetId
        }
        keyForDatasetId
        __typename
      }
      __typename
    }
    parameterValues {
      id
      value {
        bool
        float64
        int64
        string
        timestamp
        duration
        __typename
      }
      __typename
    }
    defaultForDatasets
    stages {
      id
      params
      pipeline
      layout
      input {
        inputName
        inputRole
        datasetId
        datasetPath
        stageId
        __typename
      }
      __typename
    }
    layout
        effectiveSettings {
          scanner {
            powerLevel
            __typename
          }
        }
    __typename
  }
  __typename
}";

            JObject queryObject = new JObject();
            queryObject.Add("query", graphQLQuery);
            JObject idObject = new JObject();
            idObject.Add("id", dashboardId);
            queryObject.Add("variables", idObject);
            
            string queryBody = JSONHelper.getCompactSerializedValueOfObject(queryObject);

            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                "v1/meta",
                "application/json", 
                queryBody,
                "application/json",
                currentUser.CustomerName, 
                currentUser.AuthToken);
            
            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                string queryBeginning = graphQLQuery.Split('\n')[0];
                throw new WebException(String.Format("Call to {0}v1/meta for user {1} with '{2}' returned {3} {4}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, queryBeginning, results.Item3, results.Item1));
            }
        }

        #endregion

        #region Monitors Metadata

        public static string monitorSearch_all(AuthenticatedUser currentUser, string workspaceId)
        {
            string graphQLQuery = @"query MonitorSearchInWorkspace($workspaceId: ObjectId!) {
  monitorsInWorkspace(workspaceId: $workspaceId) {
    ... on WorkspaceObject {
      id
      name
      description
      iconUrl
      workspaceId
      managedById
      managedBy {
        id
        name
        description
        iconUrl
        workspaceId
        managedById
        __typename
      }    
      __typename
    }
    ... on AuditedObject {
      createdDate
      createdByInfo {
        userId
        userLabel
        userTimezone
        __typename
      }
      updatedDate
      updatedByInfo {
        userId
        userLabel
        userTimezone
        __typename
      }
      __typename
    }
    comment
    isTemplate
    source
    resourceInputLinkName
    useDefaultFreshness
    effectiveSettings {
      monitor {
        freshnessGoal
        __typename
      }
      scanner {
        powerLevel
        __typename
      }
    }        
    notificationSpec {
      importance
      merge
      reminderFrequency
      notifyOnReminder
      notifyOnClose
      __typename
    }
    activeMonitorInfo {
      accelerationDisabled
      accelerationInfo {
        state
        stalenessSeconds
        alwaysAccelerated
        configuredTargetStalenessSeconds
        targetStalenessSeconds
        effectiveTargetStalenessSeconds
        rateLimitOverrideTargetStalenessSeconds
        acceleratedRanges {
          start
          end
          __typename
        }
        targetAcceleratedRanges {
          start
          end
          __typename
        }        
        freshnessTime
        minimumDownstreamTargetStaleness {
          minimumDownstreamTargetStalenessSeconds
          datasetIds
          monitorIds
          shareIds
          __typename
        }
        effectiveOnDemandMaterializationLength
        errors {
          datasetId
          datasetName
          transformId
          time
          errorText
          __typename
        }
        __typename
      }
      statusInfo {
        status
        errors {
          errorText
          __typename
        }
        __typename
      }
      generatedDatasetIds {
        role
        datasetId
        __typename
      }
      notificationInfo {
        lookbackTime
        count
        __typename
      }
      __typename
    }
    rule {
      ruleKind
      layout
      sourceColumn
      groupByGroups {
        columns
        groupName
        columnPath {
          column
          path
          __typename
        }
        __typename
      }
      __typename
    }
    # definition
    actions {
      id
      name
      description
      iconUrl
      workspaceId
      createdDate
      createdByInfo {
        userId
        userLabel
        userTimezone
        __typename
      }
      updatedDate
      updatedByInfo {
        userId
        userLabel
        userTimezone
        __typename
      }
      rateLimit
      notifyOnClose
      notifyOnReminder
      monitors {
        id
      }
      __typename
    }
		query {
      outputStage
      stages {
        id
        params
        pipeline
        layout
        input {
          inputName
          inputRole
          datasetId
          datasetPath
          stageId
          __typename
        }
        __typename
      }
      layout
      __typename
    }    
    __typename
  }
}";

            JObject queryObject = new JObject();
            queryObject.Add("query", graphQLQuery);
            JObject idObject = new JObject();
            idObject.Add("workspaceId", workspaceId);
            queryObject.Add("variables", idObject);
            
            string queryBody = JSONHelper.getCompactSerializedValueOfObject(queryObject);

            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                "v1/meta",
                "application/json", 
                queryBody,
                "application/json",
                currentUser.CustomerName, 
                currentUser.AuthToken);
            
            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                string queryBeginning = graphQLQuery.Split('\n')[0];
                throw new WebException(String.Format("Call to {0}v1/meta for user {1} with '{2}' returned {3} {4}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, queryBeginning, results.Item3, results.Item1));
            }
        }

        public static string monitorActionsSearch_all(AuthenticatedUser currentUser, string workspaceId)
        {
            string graphQLQuery = @"query SearchMonitorActions($workspaceId: ObjectId!) {
  searchMonitorActions(workspaceId: $workspaceId) {
    id
    name
    description
    iconUrl
    workspaceId
    createdDate
    createdByInfo {
      userId
      userLabel
      userTimezone
      __typename
    }
    updatedDate
    updatedByInfo {
      userId
      userLabel
      userTimezone
      __typename
    }
    rateLimit
    notifyOnClose
    notifyOnReminder
    isPrivate
    monitors {
      id
      name
      __typename
    }
    ... on EmailAction {
      ...ChannelActionEmail
      __typename
    }
    ... on WebhookAction {
      ...ChannelActionWebhook
      __typename
    }
    ... on UnknownAction {
      ...ChannelActionUnknown
      __typename
    }
    __typename
  }
}

fragment ChannelActionEmail on EmailAction {
  targetUsers
  targetAddresses
  targetEmailStates {
    email
    state
    __typename
  }
  subjectTemplate
  bodyTemplate
  isHtml
  fragments
  __typename
}

fragment ChannelActionWebhook on WebhookAction {
  urlTemplate
  method
  headers {
    header
    valueTemplate
    __typename
  }
  bodyTemplate
  fragments
  templateName
  __typename
}

fragment ChannelActionUnknown on UnknownAction {
  payload
  __typename
}";

            JObject queryObject = new JObject();
            queryObject.Add("query", graphQLQuery);
            JObject idObject = new JObject();
            idObject.Add("workspaceId", workspaceId);
            queryObject.Add("variables", idObject);
            
            string queryBody = JSONHelper.getCompactSerializedValueOfObject(queryObject);

            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                "v1/meta",
                "application/json", 
                queryBody,
                "application/json",
                currentUser.CustomerName, 
                currentUser.AuthToken);
            
            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                string queryBeginning = graphQLQuery.Split('\n')[0];
                throw new WebException(String.Format("Call to {0}v1/meta for user {1} with '{2}' returned {3} {4}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, queryBeginning, results.Item3, results.Item1));
            }
        }

        public static string monitorv2Search_all(AuthenticatedUser currentUser, string workspaceId)
        {
            string graphQLQuery = @"query MonitorV2InfoSearch($workspaceId: ObjectId, $folderId: ObjectId, $nameExact: String, $nameSubstring: String) {
  searchMonitorV2(
    workspaceId: $workspaceId
    folderId: $folderId
    nameExact: $nameExact
    nameSubstring: $nameSubstring
  ) {
    results {
      ... on WorkspaceObject {
        id
        name
        description
        iconUrl
        workspaceId
        managedById
        managedBy {
          id
          name
          description
          iconUrl
          workspaceId
          managedById
          __typename
        }
        __typename
      }
      ... on AuditedObject {
        createdDate
        createdByInfo {
          userId
          userLabel
          userTimezone
          __typename
        }
        updatedDate
        updatedByInfo {
          userId
          userLabel
          userTimezone
          __typename
        }
        __typename
      }
      description
      disabled
      ruleKind
      meta {
        ... on MonitorV2Meta {
          lastRunStats {
            ... on MonitorV2Stats {
              monitorID
              outputDatasetID
              dataFreshnessTime
              stabilityBookmarkTime
              windowStart
              windowEnd
              enqueueTime
              startTime
              endTime
              numDatasetRows
              numPackedAlarmStates
              numEventsGenerated
              evaluationID
              __typename
            }
            __typename
          }
          isInactive
          lastErrorTime
          lastWarningTime
          lastAlarmTime
          nextScheduledTime
          lastScheduleBookmark
          outputDatasetID
          alertSchema {
            columns {
              ... on MonitorV2Column {
                columnType {
                  tag
                  __typename
                }
                linkColumn {
                  name
                  meta {
                    srcFields {
                      name
                      path
                      __typename
                    }
                    dstFields
                    targetDataset
                    __typename
                  }
                  __typename
                }
                columnPath {
                  name
                  path
                  __typename
                }
                __typename
              }
              __typename
            }
            __typename
          }
        }
        __typename
      }
      definition {
        ... on MonitorV2Definition {
          inputQuery {
            outputStage
            stages {
              id
              params
              pipeline
              layout
              input {
                inputName
                inputRole
                datasetId
                datasetPath
                stageId
                __typename
              }
              __typename
            }
          }
          lookbackTime
          dataStabilizationDelay
          maxAlertsPerHour
          groupings {
            ... on MonitorV2Column {
              columnType {
                tag
                __typename
              }
              linkColumn {
                name
                meta {
                  srcFields {
                    name
                    path
                    __typename
                  }
                  dstFields
                  targetDataset
                  __typename
                }
                __typename
              }
              columnPath {
                name
                path
                __typename
              }
              __typename
            }
          }
          scheduling {
            ... on MonitorV2Scheduling {
              transform {
                freshnessGoal
                __typename
              }
              __typename
            }
          }
          __typename
        }
        __typename
      }
      actionRules {
        ... on MonitorV2ActionRule {
          actionID
          levels
          conditions {
            compareTerms {
              comparison {
                compareFn
                compareValue {
                  bool
                  float64
                  int64
                  string
                  timestamp
                  duration
                  __typename
                }
                __typename
              }
              column {
                columnType {
                  tag
                  __typename
                }
                linkColumn {
                  name
                  meta {
                    srcFields {
                      name
                      path
                      __typename
                    }
                    dstFields
                    targetDataset
                    __typename
                  }
                  __typename
                }
                columnPath {
                  name
                  path
                  __typename
                }
                __typename
              }
              __typename
            }
          }
          sendEndNotifications
          sendRemindersInterval
          definition {
            ... on MonitorV2ActionDefinition {
              inline
              type
              email {
                users
                addresses
                subject
                body
                fragments
                __typename
              }
              webhook {
                url
                method
                headers {
                  header
                  value
                }
                body
                fragments
                __typename
              }
            }
          }
        }
      }
      activeAlarms {
        __typename
      }
      activeAlarmCount
      mutes {
        __typename
      }
      __typename
    }
    __typename
  }
}";

            JObject queryObject = new JObject();
            queryObject.Add("query", graphQLQuery);
            JObject idObject = new JObject();
            idObject.Add("workspaceId", workspaceId);
            queryObject.Add("variables", idObject);
            
            string queryBody = JSONHelper.getCompactSerializedValueOfObject(queryObject);

            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                "v1/meta",
                "application/json", 
                queryBody,
                "application/json",
                currentUser.CustomerName, 
                currentUser.AuthToken);
            
            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                string queryBeginning = graphQLQuery.Split('\n')[0];
                throw new WebException(String.Format("Call to {0}v1/meta for user {1} with '{2}' returned {3} {4}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, queryBeginning, results.Item3, results.Item1));
            }
        }

        public static string monitorv2ActionsSearch_all(AuthenticatedUser currentUser, string workspaceId)
        {
            string graphQLQuery = @"query MonitorV2SearchAction($workspaceId: ObjectId, $folderId: ObjectId, $nameExact: String, $nameSubstring: String) {
  searchMonitorV2Action(
    workspaceId: $workspaceId
    folderId: $folderId
    nameExact: $nameExact
    nameSubstring: $nameSubstring
  ) {
    results {
      ...MonitorV2Action
      __typename
    }
    __typename
  }
}

fragment WorkspaceEntity on WorkspaceObject {
  id
  name
  description
  iconUrl
  workspaceId
  managedById
  __typename
}

fragment UserInfo on UserInfo {
  userLabel
  userId
  userTimezone
  __typename
}

fragment AuditedEntity on AuditedObject {
  createdBy
  createdDate
  createdByInfo {
    ...UserInfo
    __typename
  }
  updatedBy
  updatedDate
  updatedByInfo {
    ...UserInfo
    __typename
  }
  __typename
}

fragment FolderEntity on FolderObject {
  folderId
  __typename
}

fragment MonitorV2EmailAction on MonitorV2EmailAction {
  subject
  body
  fragments
  users
  addresses
  __typename
}

fragment MonitorV2WebhookHeader on MonitorV2WebhookHeader {
  header
  value
  __typename
}

fragment MonitorV2WebhookAction on MonitorV2WebhookAction {
  headers {
    ...MonitorV2WebhookHeader
    __typename
  }
  body
  fragments
  url
  method
  __typename
}

fragment MonitorV2Action on MonitorV2Action {
  ...WorkspaceEntity
  ...AuditedEntity
  ...FolderEntity
  id
  inline
  type
  email {
    ...MonitorV2EmailAction
    __typename
  }
  webhook {
    ...MonitorV2WebhookAction
    __typename
  }
  __typename
}";

            JObject queryObject = new JObject();
            queryObject.Add("query", graphQLQuery);
            JObject idObject = new JObject();
            idObject.Add("workspaceId", workspaceId);
            queryObject.Add("variables", idObject);
            
            string queryBody = JSONHelper.getCompactSerializedValueOfObject(queryObject);

            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                "v1/meta",
                "application/json", 
                queryBody,
                "application/json",
                currentUser.CustomerName, 
                currentUser.AuthToken);
            
            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                string queryBeginning = graphQLQuery.Split('\n')[0];
                throw new WebException(String.Format("Call to {0}v1/meta for user {1} with '{2}' returned {3} {4}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, queryBeginning, results.Item3, results.Item1));
            }
        }

        #endregion

        #region Worksheets Metadata

        public static string worksheetSearch_all(AuthenticatedUser currentUser, string workspaceId)
        {
            string graphQLQuery = @"query WorksheetSearch($terms: DWSearchInput!, $maxCount: Int64) {
  worksheetSearch(terms: $terms, maxCount: $maxCount) {
    worksheets {      
      worksheet {
        ... on WorkspaceObject {
          id
          name
          description
          iconUrl
          workspaceId
          managedById
          __typename
        }
        ... on FolderObject {
          folderId
          __typename
        }
        ... on AuditedObject {
          createdDate
          createdByInfo {
            userId
            userLabel
            userTimezone
            __typename
          }
          updatedDate
          updatedByInfo {
            userId
            userLabel
            userTimezone
            __typename
          }
          __typename
        }
        ... on IWorksheetLike {
          parameters {
            id
            name
            defaultValue {
              bool
              float64
              int64
              string
              timestamp
              duration
              __typename
            }
            valueKind {
              type
              arrayItemType {
                keyForDatasetId
              }
              keyForDatasetId
              __typename
            }
            __typename
          }
          parameterValues {
            id
            value {
              bool
              float64
              int64
              string
              timestamp
              duration
              __typename
            }
            __typename
          }
          stages {
            id
            params
            pipeline
            layout
            input {
              inputName
              inputRole
              datasetId
              datasetPath
              stageId
              __typename
            }
            __typename
          }
          layout
        }
        effectiveSettings {
          scanner {
            powerLevel
            __typename
          }
        }
        __typename
      }
      score
      inWorkspace
      numParameters
      numInputs
      __typename
    }
    warnings
    __typename
  }
}";

            JObject queryObject = new JObject();
            queryObject.Add("query", graphQLQuery);
            JArray workspaceIds = new JArray();
            workspaceIds.Add(workspaceId);
            JObject workspaceIdObject = new JObject();
            workspaceIdObject.Add("workspaceId", workspaceIds);
            JObject variablesObject = new JObject();
            variablesObject.Add("terms", workspaceIdObject);
            queryObject.Add("variables", variablesObject);
            
            string queryBody = JSONHelper.getCompactSerializedValueOfObject(queryObject);

            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                "v1/meta",
                "application/json", 
                queryBody,
                "application/json",
                currentUser.CustomerName, 
                currentUser.AuthToken);
            
            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                string queryBeginning = graphQLQuery.Split('\n')[0];
                throw new WebException(String.Format("Call to {0}v1/meta for user {1} with '{2}' returned {3} {4}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, queryBeginning, results.Item3, results.Item1));
            }
        }

        public static string worksheet_single(AuthenticatedUser currentUser, string worksheetId)
        {
            string graphQLQuery = @"query LoadWorksheet($id: ObjectId!) {
  worksheet(id: $id) {
    ... on WorkspaceObject {
      id
      name
      description
      iconUrl
      workspaceId
      managedById
      __typename
    }
    ... on FolderObject {
      folderId
      __typename
    }
    ... on AuditedObject {
      createdDate
      createdByInfo {
        userId
        userLabel
        userTimezone
        __typename
      }
      updatedDate
      updatedByInfo {
        userId
        userLabel
        userTimezone
        __typename
      }
      __typename
    }
    ... on IWorksheetLike {
      parameters {
        id
        name
        defaultValue {
          bool
          float64
          int64
          string
          timestamp
          duration
          __typename
        }
        valueKind {
          type
          arrayItemType {
            keyForDatasetId
          }
          keyForDatasetId
          __typename
        }
        __typename
      }
      parameterValues {
        id
        value {
          bool
          float64
          int64
          string
          timestamp
          duration
          __typename
        }
        __typename
      }
      stages {
        id
        params
        pipeline
        layout
        input {
          inputName
          inputRole
          datasetId
          datasetPath
          stageId
          __typename
        }
        __typename
      }
      layout
    }
    effectiveSettings {
      scanner {
        powerLevel
        __typename
      }
    }
    __typename
  }
}";

            JObject queryObject = new JObject();
            queryObject.Add("query", graphQLQuery);
            JObject idObject = new JObject();
            idObject.Add("id", worksheetId);
            queryObject.Add("variables", idObject);
            
            string queryBody = JSONHelper.getCompactSerializedValueOfObject(queryObject);

            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                "v1/meta",
                "application/json", 
                queryBody,
                "application/json",
                currentUser.CustomerName, 
                currentUser.AuthToken);
            
            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                string queryBeginning = graphQLQuery.Split('\n')[0];
                throw new WebException(String.Format("Call to {0}v1/meta for user {1} with '{2}' returned {3} {4}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, queryBeginning, results.Item3, results.Item1));
            }
        }

        #endregion

        #region Datastreams and Tokens Metadata

        public static string datastreams_all(AuthenticatedUser currentUser, string workspaceId)
        {
            string graphQLQuery = @"query datastreams($workspaceId: ObjectId!, $appId: ObjectId, $moduleId: String) {
  datastreams(workspaceId: $workspaceId) {
    ...DataStream
    __typename
  }
}

fragment WorkspaceEntity on WorkspaceObject {
  id
  name
  description
  iconUrl
  workspaceId
  managedById
  __typename
}

fragment UserInfo on UserInfo {
  userLabel
  userId
  userTimezone
  __typename
}

fragment AuditedEntity on AuditedObject {
  createdBy
  createdDate
  createdByInfo {
    ...UserInfo
    __typename
  }
  updatedBy
  updatedDate
  updatedByInfo {
    ...UserInfo
    __typename
  }
  __typename
}

fragment DatastreamSourceAppMetadata on DatastreamSourceAppMetadata {
  appId
  moduleId
  instructions
  datasourceName
  __typename
}

fragment DataStreamSourceStats on DatastreamSourceStats {
  firstIngest
  lastIngest
  lastError
  observations {
    time
    value
    __typename
  }
  volumeBytes {
    time
    value
    __typename
  }
  errors {
    time
    message
    code
    __typename
  }
  __typename
}

fragment DataStreamToken on DatastreamToken {
  id
  name
  description
  datastreamId
  appMetadata {
    ...DatastreamSourceAppMetadata
    __typename
  }
  createdBy
  disabled
  createdByInfo {
    userId
    userLabel
    __typename
  }
  updatedBy
  updatedByInfo {
    userId
    userLabel
    __typename
  }
  createdDate
  updatedDate
  stats {
    ...DataStreamSourceStats
    __typename
  }
  secret
  managedById
  __typename
}

fragment AppVariable on AppVariable {
  name
  type
  description
  required
  sensitive
  default
  value
  __typename
}

fragment DataStreamPollerAppMetadata on PollerAppMetadata {
  ...DatastreamSourceAppMetadata
  sourceUrl
  variables {
    ...AppVariable
    __typename
  }
  __typename
}

fragment DataStreamPoller on Poller {
  ...WorkspaceEntity
  ...AuditedEntity
  kind
  customerId
  datastreamId
  datastreamTokenId
  disabled
  stats {
    ...DataStreamSourceStats
    __typename
  }
  appMetadata {
    ...DataStreamPollerAppMetadata
    __typename
  }
  __typename
}

fragment DataStreamFiledropConfig on FiledropConfig {
  provider {
    type
    ... on FiledropProviderAwsConfig {
      region
      roleArn
      __typename
    }
    __typename
  }
  __typename
}

fragment DataStreamFiledropEndpoint on FiledropEndpoint {
  type
  ... on FiledropS3Endpoint {
    arn
    bucket
    prefix
    __typename
  }
  __typename
}

fragment DataStreamFiledrop on Filedrop {
  ...WorkspaceEntity
  ...AuditedEntity
  status
  datastreamID
  datastreamTokenID
  disabled
  config {
    ...DataStreamFiledropConfig
    __typename
  }
  endpoint {
    ...DataStreamFiledropEndpoint
    __typename
  }
  stats {
    ...DataStreamSourceStats
    __typename
  }
  __typename
}

fragment DataStream on Datastream {
  ...WorkspaceEntity
  ...AuditedEntity
  customerId
  datasetId
  state
  tokens(appId: $appId, moduleId: $moduleId) {
    ...DataStreamToken
    __typename
  }
  pollers(appId: $appId, moduleId: $moduleId) {
    ...DataStreamPoller
    __typename
  }
  filedrops {
    ...DataStreamFiledrop
    __typename
  }
  stats {
    firstIngest
    lastIngest
    lastError
    numTokens
    observations {
      time
      value
      __typename
    }
    volumeBytes {
      time
      value
      __typename
    }
    totalObservations
    totalVolumeBytes
    __typename
  }
  directWrite {
    prometheus {
      datasetId
      metadataDatasetId
      __typename
    }
    otelLogs {
      datasetId
      __typename
    }
    k8sEntity {
      datasetId
      __typename
    }
    otelTrace {
      spanDatasetId
      spanEventDatasetId
      __typename
    }
    __typename
  }
  effectiveSettings {
    dataRetention {
      periodDays
      __typename
    }
    __typename
  }
  __typename
}";

            JObject queryObject = new JObject();
            queryObject.Add("query", graphQLQuery);
            JObject idObject = new JObject();
            idObject.Add("workspaceId", workspaceId);
            queryObject.Add("variables", idObject);
            
            string queryBody = JSONHelper.getCompactSerializedValueOfObject(queryObject);

            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                "v1/meta",
                "application/json", 
                queryBody,
                "application/json",
                currentUser.CustomerName, 
                currentUser.AuthToken);
            
            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                string queryBeginning = graphQLQuery.Split('\n')[0];
                throw new WebException(String.Format("Call to {0}v1/meta for user {1} with '{2}' returned {3} {4}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, queryBeginning, results.Item3, results.Item1));
            }
        }

        #endregion

        #region RBAC

        public static string rbacGroups(AuthenticatedUser currentUser)
        {
            string graphQLQuery = @"query RbacGroups {
  rbacGroups {
    id
    name
    description
    memberUserIds
    memberGroupIds
    memberOfGroupIds
    ... on AuditedObject {
      createdDate
      createdByInfo {
        userId
        userLabel
        userTimezone
        __typename
      }
      updatedDate
      updatedByInfo {
        userId
        userLabel
        userTimezone
        __typename
      }
      __typename
    }
    __typename
  }
}";

            JObject queryObject = new JObject();
            queryObject.Add("query", graphQLQuery);            
            queryObject.Add("variables", new JObject());
            
            string queryBody = JSONHelper.getCompactSerializedValueOfObject(queryObject);

            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                "v1/meta",
                "application/json", 
                queryBody,
                "application/json",
                currentUser.CustomerName, 
                currentUser.AuthToken);
            
            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                string queryBeginning = graphQLQuery.Split('\n')[0];
                throw new WebException(String.Format("Call to {0}v1/meta for user {1} with '{2}' returned {3} {4}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, queryBeginning, results.Item3, results.Item1));
            }
        }

        public static string rbacStatements(AuthenticatedUser currentUser)
        {
            string graphQLQuery = @"query RbacStatements {
  rbacStatements {
    id
    description
    subject {
      userId
      groupId
      all
      __typename
    }
    object {
      objectId
      folderId
      workspaceId
      type
      name
      owner
      all
      __typename
    }
    role
    ... on AuditedObject {
      createdDate
      createdByInfo {
        userId
        userLabel
        userTimezone
        __typename
      }
      updatedDate
      updatedByInfo {
        userId
        userLabel
        userTimezone
        __typename
      }
      __typename
    }
    __typename
  }
}";

            JObject queryObject = new JObject();
            queryObject.Add("query", graphQLQuery);            
            queryObject.Add("variables", new JObject());
            
            string queryBody = JSONHelper.getCompactSerializedValueOfObject(queryObject);

            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                "v1/meta",
                "application/json", 
                queryBody,
                "application/json",
                currentUser.CustomerName, 
                currentUser.AuthToken);
            
            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                string queryBeginning = graphQLQuery.Split('\n')[0];
                throw new WebException(String.Format("Call to {0}v1/meta for user {1} with '{2}' returned {3} {4}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, queryBeginning, results.Item3, results.Item1));
            }
        }

        public static string rbacGroupMembers(AuthenticatedUser currentUser)
        {
            string graphQLQuery = @"query RbacGroupmembers {
  rbacGroupmembers {
    id
    description
    groupId
    memberUserId
    memberGroupId
    ... on AuditedObject {
      createdDate
      createdByInfo {
        userId
        userLabel
        userTimezone
        __typename
      }
      updatedDate
      updatedByInfo {
        userId
        userLabel
        userTimezone
        __typename
      }
      __typename
    }
    __typename
  }
}";

            JObject queryObject = new JObject();
            queryObject.Add("query", graphQLQuery);            
            queryObject.Add("variables", new JObject());
            
            string queryBody = JSONHelper.getCompactSerializedValueOfObject(queryObject);

            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                "v1/meta",
                "application/json", 
                queryBody,
                "application/json",
                currentUser.CustomerName, 
                currentUser.AuthToken);
            
            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                string queryBeginning = graphQLQuery.Split('\n')[0];
                throw new WebException(String.Format("Call to {0}v1/meta for user {1} with '{2}' returned {3} {4}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, queryBeginning, results.Item3, results.Item1));
            }
        }
        public static string users(AuthenticatedUser currentUser)
        {
            string graphQLQuery = @"query CurrentCustomer {
  currentCustomer {
    id
    label
    users {
      id
      type
      email
      label
      role
      status
      comment
      expirationTime
      timezone
      __typename
    }
    __typename
  }
}";

            JObject queryObject = new JObject();
            queryObject.Add("query", graphQLQuery);            
            queryObject.Add("variables", new JObject());
            
            string queryBody = JSONHelper.getCompactSerializedValueOfObject(queryObject);

            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                "v1/meta",
                "application/json", 
                queryBody,
                "application/json",
                currentUser.CustomerName, 
                currentUser.AuthToken);
            
            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                string queryBeginning = graphQLQuery.Split('\n')[0];
                throw new WebException(String.Format("Call to {0}v1/meta for user {1} with '{2}' returned {3} {4}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, queryBeginning, results.Item3, results.Item1));
            }
        }

        #endregion

        #region Usage data

        public static string getUsageData_transform(AuthenticatedUser currentUser, int intervalHours)
        {
            string exportQueryTemplate = @"{{
  ""query"": {{
    ""outputStage"": ""exportData"",
    ""stages"": [
      {{
        ""input"": [
          {{
            ""inputName"": ""usage"",
            ""datasetPath"": ""{0}.usage/Observe Usage Metrics""
          }}
        ],
        ""stageID"": ""exportData"",
        ""pipeline"": ""filter metric = \""credits_transform\""\nstatsby Credits:sum(value), group_by(PackageName:label(^Package), DatasetName:label(^Dataset), DatasetID:dataset_id)""
      }}
    ]
  }},
  ""rowCount"": ""10000""
}}";

            string exportQuery = String.Format(exportQueryTemplate, currentUser.WorkspaceName);
            
            return executeQueryAndGetData(currentUser, exportQuery, String.Format("{0}h", intervalHours));
        }

        public static string getUsageData_monitor(AuthenticatedUser currentUser, int numHoursBack)
        {
            string exportQueryTemplate = @"{{
  ""query"": {{
    ""outputStage"": ""exportData"",
    ""stages"": [
      {{
        ""input"": [
          {{
            ""inputName"": ""usage"",
            ""datasetPath"": ""{0}.usage/Observe Usage Metrics""
          }}
        ],
        ""stageID"": ""exportData"",
        ""pipeline"": ""filter metric = \""credits_monitor\""\nstatsby Credits:sum(value), group_by(PackageName:label(^Package), MonitorName:label(^Monitor), MonitorID:monitor_id)""
      }}
    ]
  }},
  ""rowCount"": ""10000""
}}";

            string exportQuery = String.Format(exportQueryTemplate, currentUser.WorkspaceName);

            return executeQueryAndGetData(currentUser, exportQuery, String.Format("{0}h", numHoursBack));
        }

        public static string getUsageData_query(AuthenticatedUser currentUser, int numHoursBack)
        {
            string exportQueryTemplate = @"{{
  ""query"": {{
    ""outputStage"": ""exportData"",
    ""stages"": [
      {{
        ""input"": [
          {{
            ""inputName"": ""usage"",
            ""datasetPath"": ""{0}.usage/Observe Usage Metrics""
          }}
        ],
        ""stageID"": ""exportData"",
        ""pipeline"": ""filter metric = \""credits_adhoc_query\""\nstatsby Credits:sum(value), group_by(PackageName:label(^Package), DatasetName:label(^Dataset), DatasetID:dataset_id, UserName:label(^User))""
      }}
    ]
  }},
  ""rowCount"": ""10000""
}}";

            string exportQuery = String.Format(exportQueryTemplate, currentUser.WorkspaceName);
            
            return executeQueryAndGetData(currentUser, exportQuery, String.Format("{0}h", numHoursBack));
        }

        public static string executeQueryAndGetData(AuthenticatedUser currentUser, string queryBody, string intervalValue)
        {
            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                String.Format("v1/meta/export/query?interval={0}", intervalValue),
                "text/csv",
                queryBody,
                "application/json", 
                currentUser.CustomerName, 
                currentUser.AuthToken);
            
            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                throw new WebException(String.Format("Call to {0}v1/meta/export/query for user {1} returned {2} {3}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, results.Item3, results.Item1));
            }
        }

        #endregion

        #region Metrics Metadata

        public static string metricsSearch_all(AuthenticatedUser currentUser)
        {
            string graphQLQuery = @"query MetricSearch($workspaces: [ObjectId!], $inDatasets: [ObjectId!], $linkToDatasets: [ObjectId!], $correlationTagMatches: [String!], $match: String!, $heuristicsOptions: MetricHeuristicsOptions) {
  metricSearch(
    workspaces: $workspaces
    inDatasets: $inDatasets
    linkToDatasets: $linkToDatasets
    correlationTagMatches: $correlationTagMatches
    match: $match
    heuristicsOptions: $heuristicsOptions
  ) {
    matches {
      datasetId
      metric {
        name
        nameWithPath
        type
        unit
        description
        rollup
        aggregate
        interval
        suggestedBucketSize
        userDefined
        state
        heuristics {
          cardinality
          interval
          intervalStddev
          numOfPoints
          validLinkLabels
          tags {
            path
            column
            __typename
          }
          lastReported
          __typename
        }
        __typename
      }
      __typename
    }
    numSearched
    __typename
  }
}";

            JObject queryObject = new JObject();
            queryObject.Add("query", graphQLQuery);
            JObject heuristicsOptionsObject = new JObject();
            heuristicsOptionsObject.Add("globalLimit", "100000");
            heuristicsOptionsObject.Add("perDatasetLimit", "100000");
            JObject variablesObject = new JObject();
            variablesObject.Add("heuristicsOptions", heuristicsOptionsObject);
            variablesObject.Add("match", "");
            queryObject.Add("variables", variablesObject);
            
            string queryBody = JSONHelper.getCompactSerializedValueOfObject(queryObject);

            Tuple<string, List<string>, HttpStatusCode> results = apiPOST(
                currentUser.CustomerEnvironmentUrl,
                "v1/meta",
                "application/json", 
                queryBody,
                "application/json",
                currentUser.CustomerName, 
                currentUser.AuthToken);
            
            if (results.Item3 == HttpStatusCode.OK)
            {
                return results.Item1;
            }
            else
            {
                string queryBeginning = graphQLQuery.Split('\n')[0];
                throw new WebException(String.Format("Call to {0}v1/meta for user {1} with '{2}' returned {3} {4}", currentUser.CustomerEnvironmentUrl, currentUser.UserName, queryBeginning, results.Item3, results.Item1));
            }
        }

        #endregion

        #region Retrieval methods

        /// Returns Tuple<string, List<string>, HttpStatusCode>
        ///               ^^^^^^                                    results of the page
        ///                       ^^^^^^^^^^^^                      list of cookies
        ///                                     ^^^^^^^^^^^^^^^     HTTP Result Code
        private static Tuple<string, List<string>, HttpStatusCode> apiGET(
            Uri baseUri, string restAPIUrl, string acceptHeader, string customerName, string authenticationToken)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                HttpClientHandler httpClientHandler = new HttpClientHandler();
                httpClientHandler.UseCookies = true;
                httpClientHandler.CookieContainer = new CookieContainer();
                httpClientHandler.AllowAutoRedirect = false;

                // If customer certificates are not in trusted store, let's not fail
                httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

                using (HttpClient httpClient = new HttpClient(httpClientHandler))
                {
                    httpClient.Timeout = new TimeSpan(0, 5, 0);
                    httpClient.BaseAddress = baseUri;

                    var productValue = new ProductInfoHeaderValue("Observe-Entity-Explorer", Assembly.GetExecutingAssembly().GetName().Version.ToString());
                    var commentValue = new ProductInfoHeaderValue("(https://www.observeinc.com/)");
                    httpClient.DefaultRequestHeaders.UserAgent.Add(productValue);
                    httpClient.DefaultRequestHeaders.UserAgent.Add(commentValue);

                    if (customerName != null && customerName.Length> 0 &&
                        authenticationToken != null && authenticationToken.Length > 0)
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", String.Format("{0} {1}", customerName, authenticationToken));
                    }

                    MediaTypeWithQualityHeaderValue accept = new MediaTypeWithQualityHeaderValue(acceptHeader);
                    if (httpClient.DefaultRequestHeaders.Accept.Contains(accept) == false)
                    {
                        httpClient.DefaultRequestHeaders.Accept.Add(accept);
                    }

                    // Call the REST API
                    // Explicit sync over async intended
                    var task1 = Task.Run(() => httpClient.GetAsync(restAPIUrl)); 
                    task1.Wait();
                    HttpResponseMessage response = task1.Result;

                    // Read the results
                    var task2 = Task.Run(() => response.Content.ReadAsStringAsync()); 
                    task2.Wait();
                    string resultString = task2.Result;
                    if (resultString == null) resultString = String.Empty;

                    // Read the cookies
                    IEnumerable<string> cookiesList = new List<string>(); 
                    if (response.Headers.Contains("Set-Cookie") == true)
                    {
                        cookiesList = response.Headers.GetValues("Set-Cookie"); 
                    }

                    // Remove sensitive data from logging
                    httpClient.DefaultRequestHeaders.Remove("Authorization");

                    NLog.LogLevel logLevel = NLog.LogLevel.Info;
                    if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Found)
                    {
                    }
                    else if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        logLevel = NLog.LogLevel.Warn;
                    }
                    else
                    {
                        logLevel = NLog.LogLevel.Error;
                    }

                    if (resultString.Length > 0)
                    {
                        logger.Log(
                            logLevel, 
                            "GET {0}{1} returned {2} ({3})\nRequest Headers:\n{4}Cookies:\n{5}\nResponse Length {6}:\n{7}",
                            baseUri, 
                            restAPIUrl, 
                            (int)response.StatusCode, 
                            response.ReasonPhrase, 
                            httpClient.DefaultRequestHeaders, 
                            String.Join('\n', cookiesList), 
                            resultString.Length, 
                            resultString);
                    }
                    else
                    {
                        logger.Log(
                            logLevel, 
                            "GET {0}{1} returned {2} ({3})\nRequest Headers:\n{4}Cookies:\n{5}\nResponse Length {6}",
                            baseUri, 
                            restAPIUrl, 
                            (int)response.StatusCode, 
                            response.ReasonPhrase, 
                            httpClient.DefaultRequestHeaders, 
                            String.Join('\n', cookiesList), 
                            resultString.Length);
                    }

                    return new Tuple<string, List<string>, HttpStatusCode>(resultString, cookiesList.ToList(), response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                logger.Error("GET {0}{1} threw {2} ({3})", baseUri, restAPIUrl, ex.Message, ex.Source);
                logger.Error(ex);

                loggerConsole.Error("GET {0}{1} threw {2} ({3})", baseUri, restAPIUrl, ex.Message, ex.Source);

                return new Tuple<string, List<string>, HttpStatusCode>(String.Empty, new List<string>(0), HttpStatusCode.InternalServerError);
            }
            finally
            {
                stopWatch.Stop();
                logger.Info("GET {0}{1} took {2:c} ({3} ms)", baseUri, restAPIUrl, stopWatch.Elapsed.ToString("c"), stopWatch.ElapsedMilliseconds);
            }
        }

        /// Returns Tuple<string, List<string>, HttpStatusCode>
        ///               ^^^^^^                                    results of the page
        ///                       ^^^^^^^^^^^^                      list of cookies
        ///                                     ^^^^^^^^^^^^^^^     HTTP Result Code
        private static Tuple<string, List<string>, HttpStatusCode> apiPOST(
            Uri baseUri, string restAPIUrl, string acceptHeader, string requestBody, string requestTypeHeader, string customerName, string authenticationToken)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            string requestBodyFragment = requestBody;
            if (requestBody.Length > 64)
            {
              requestBodyFragment = String.Format("|{0}", requestBody.Substring(0, 64).Replace("\n", ""));
            }

            try
            {
                HttpClientHandler httpClientHandler = new HttpClientHandler();
                httpClientHandler.UseCookies = true;
                httpClientHandler.CookieContainer = new CookieContainer();
                httpClientHandler.AllowAutoRedirect = false;

                // If customer certificates are not in trusted store, let's not fail
                httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

                using (HttpClient httpClient = new HttpClient(httpClientHandler))
                {
                    httpClient.Timeout = new TimeSpan(0, 5, 0);
                    httpClient.BaseAddress = baseUri;

                    var productValue = new ProductInfoHeaderValue("Observe-Entity-Explorer", Assembly.GetExecutingAssembly().GetName().Version.ToString());
                    var commentValue = new ProductInfoHeaderValue("(https://www.observeinc.com/)");
                    httpClient.DefaultRequestHeaders.UserAgent.Add(productValue);
                    httpClient.DefaultRequestHeaders.UserAgent.Add(commentValue);

                    if (customerName != null && customerName.Length> 0 &&
                        authenticationToken != null && authenticationToken.Length > 0)
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", String.Format("{0} {1}", customerName, authenticationToken));
                    }

                    MediaTypeWithQualityHeaderValue accept = new MediaTypeWithQualityHeaderValue(acceptHeader);
                    if (httpClient.DefaultRequestHeaders.Accept.Contains(accept) == false)
                    {
                        httpClient.DefaultRequestHeaders.Accept.Add(accept);
                    }

                    StringContent content = new StringContent(requestBody, null, new MediaTypeWithQualityHeaderValue(requestTypeHeader));

                    // Call the REST API
                    // Explicit sync over async intended
                    var task1 = Task.Run(() => httpClient.PostAsync(restAPIUrl, content)); 
                    task1.Wait();
                    HttpResponseMessage response = task1.Result;

                    // Read the results
                    var task2 = Task.Run(() => response.Content.ReadAsStringAsync()); 
                    task2.Wait();
                    string resultString = task2.Result;
                    if (resultString == null) resultString = String.Empty;

                    // Read the cookies
                    IEnumerable<string> cookiesList = new List<string>(); 
                    if (response.Headers.Contains("Set-Cookie") == true)
                    {
                        cookiesList = response.Headers.GetValues("Set-Cookie"); 
                    }

                    // Remove sensitive data from logging
                    if (restAPIUrl.StartsWith("v1/login"))
                    {
                        var pattern = "\"user_password\": \"(.*)\",";
                        requestBody = Regex.Replace(requestBody, pattern, "\"user_password\": \"****\",", RegexOptions.IgnoreCase); 
                    }
                    //httpClient.DefaultRequestHeaders.Remove("Authorization");

                    NLog.LogLevel logLevel = NLog.LogLevel.Info;
                    if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Found)
                    {
                    }
                    else if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        logLevel = NLog.LogLevel.Warn;
                    }
                    else
                    {                        
                        logLevel = NLog.LogLevel.Error;
                    }

                    if (resultString.Length > 0)
                    {
                        logger.Log(
                            logLevel, 
                            "POST {0}{1} returned {2} ({3})\nRequest Headers:\n{4}Cookies:\n{5}\nRequest:\n{6}\nResponse Length {7}:\n{8}",
                            baseUri, 
                            restAPIUrl, 
                            (int)response.StatusCode, 
                            response.ReasonPhrase, 
                            httpClient.DefaultRequestHeaders, 
                            String.Join('\n', cookiesList), 
                            requestBody,
                            resultString.Length, 
                            resultString);
                    }
                    else
                    {
                        logger.Log(
                            logLevel, 
                            "POST {0}{1} returned {2} ({3})\nRequest Headers:\n{4}Cookies:\n{5}\nRequest:\n{6}\nResponse Length {7}",
                            baseUri, 
                            restAPIUrl, 
                            (int)response.StatusCode, 
                            response.ReasonPhrase, 
                            httpClient.DefaultRequestHeaders, 
                            String.Join('\n', cookiesList),
                            requestBody,
                            resultString.Length);
                    }

                    return new Tuple<string, List<string>, HttpStatusCode>(resultString, cookiesList.ToList(), response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                logger.Error("POST {0}{1}{4} threw {2} ({3})", baseUri, restAPIUrl, ex.Message, ex.Source, requestBodyFragment);
                logger.Error(ex);

                loggerConsole.Error("POST {0}{1}{4} threw {2} ({3})", baseUri, restAPIUrl, ex.Message, ex.Source, requestBodyFragment);

                return new Tuple<string, List<string>, HttpStatusCode>(String.Empty, new List<string>(0), HttpStatusCode.InternalServerError);
            }
            finally
            {
                stopWatch.Stop();
                logger.Info("POST {0}{1}{4} took {2:c} ({3} ms)", baseUri, restAPIUrl, stopWatch.Elapsed.ToString("c"), stopWatch.ElapsedMilliseconds, requestBodyFragment);
            }
        }

        #endregion
    }
}