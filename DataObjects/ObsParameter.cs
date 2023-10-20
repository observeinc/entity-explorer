using Newtonsoft.Json.Linq;

namespace Observe.EntityExplorer.DataObjects
{
    public class ObsParameter : ObsObject
    {
        public string type { get; set; }
        public string defaultValue { get; set; }
        // Dataset or a Stage
        public ObsObject RelatedObject { get; set; }

        public ObsCompositeObject Parent { get; set; }

        public override string ToString()
        {
            return String.Format(
                "ObsParameter: {0} ({1}) in {2} [{3}]",
                this.name,
                this.type,
                this.Parent.name,
                this.Parent.id);
        }

        public ObsParameter () {}

        public ObsParameter(JObject entityObject, AuthenticatedUser authenticatedUser, ObsCompositeObject parentObject)
        {
            this._raw = entityObject;

            #region Examples            
// [{
//         "id": "cluster",
//         "name": "Cluster",
//         "defaultValue": {
//             "link": null
//         },
//         "valueKind": {
//             "type": "LINK",
//             "arrayItemType": null,
//             "keyForDatasetId": "41218445",
//             "__typename": "ValueTypeSpec"
//         },
//         "__typename": "ParameterSpec"
//     }, {
//         "id": "container",
//         "name": "Container",
//         "defaultValue": {
//             "string": "opentelemetry-collector"
//         },
//         "valueKind": {
//             "type": "STRING",
//             "arrayItemType": null,
//             "keyForDatasetId": null,
//             "__typename": "ValueTypeSpec"
//         },
//         "__typename": "ParameterSpec"
//     }, {
//         "id": "pod",
//         "name": "Pod",
//         "defaultValue": {
//             "string": null
//         },
//         "valueKind": {
//             "type": "STRING",
//             "arrayItemType": null,
//             "keyForDatasetId": null,
//             "__typename": "ValueTypeSpec"
//         },
//         "__typename": "ParameterSpec"
//     }, {
//         "id": "input-parameter-bhusra4m",
//         "name": "container",
//         "defaultValue": {
//             "datasetref": {
//                 "datasetId": "41218461"
//             }
//         },
//         "valueKind": {
//             "type": "DATASETREF",
//             "arrayItemType": null,
//             "keyForDatasetId": null,
//             "__typename": "ValueTypeSpec"
//         },
//         "__typename": "ParameterSpec"
//     }, {
//         "id": "company",
//         "name": "Company",
//         "defaultValue": {
//             "link": null
//         },
//         "valueKind": {
//             "type": "LINK",
//             "arrayItemType": null,
//             "keyForDatasetId": "41272874",
//             "__typename": "ValueTypeSpec"
//         },
//         "__typename": "ParameterSpec"
//     }, {
//         "id": "duration",
//         "name": "Duration (minutes) at least",
//         "defaultValue": {
//             "int64": "0"
//         },
//         "valueKind": {
//             "type": "INT64",
//             "arrayItemType": null,
//             "keyForDatasetId": null,
//             "__typename": "ValueTypeSpec"
//         },
//         "__typename": "ParameterSpec"
//     }
// ]
            
            #endregion

            this.id = JSONHelper.getStringValueFromJToken(entityObject, "id");
            this.name = JSONHelper.getStringValueFromJToken(entityObject, "name");
            
            this.defaultValue = JSONHelper.getStringValueOfObjectFromJToken(entityObject, "defaultValue");

            JObject valueKindObject = (JObject)JSONHelper.getJTokenValueFromJToken(entityObject, "valueKind");
            if (valueKindObject != null)
            {
                this.type = JSONHelper.getStringValueFromJToken(valueKindObject, "type");

                string referenceDatasetId = String.Empty;

                switch (this.type)
                {
                    case "DATASETREF":
                        referenceDatasetId = JSONHelper.getStringValueOfObjectFromJToken(JSONHelper.getStringValueOfObjectFromJToken(JSONHelper.getStringValueOfObjectFromJToken(entityObject, "defaultValue"), "datasetref"), "datasetId");
                        break;

                    case "LINK":
                        referenceDatasetId = JSONHelper.getStringValueFromJToken(valueKindObject, "keyForDatasetId");
                        break;

                    default:
                        break;
                }

                //if (referenceDatasetId != String.Empty && allDatasetsDict != null)
                //{
                    //this.RelatedDataset = allDatasetsDict[referenceDatasetId];
                //}

            }

            this.Parent = parentObject;
        }

    }
}