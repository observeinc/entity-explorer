namespace Observe.EntityExplorer.DataObjects
{
    public class ObjectRelationship: IEquatable<ObjectRelationship>// IEqualityComparer<ObjectRelationship>
    {
        public string name { get; set; }

        public ObsObjectRelationshipType RelationshipType { get; set; }

        public ObsObject ThisObject { get; set; }

        public ObsObject RelatedObject { get; set; }

        public override string ToString()
        {
            return String.Format("ObjectRelationship: {2} ({3}) {4} to {0} ({1})",
                this.ThisObject.name,
                this.ThisObject.GetType().Name,
                this.RelatedObject.name,
                this.RelatedObject.GetType().Name,
                this.RelationshipType);
        }


        public string ToString(string format)
        {
            throw new NotImplementedException();
            // if (format == "graphviz")
            // {
            //     if (this.ThisObject != null && this.RelatedObject != null)
            //     {
            //         switch (this.relationship)
            //         {
            //             case "Data":
            //                 return String.Format("\"{0}\"->\"{1}\" [color=\"purple\"]", this.RelatedObject.ToString(format), this.ThisObject.ToString(format));

            //             case "Reference":
            //                 return String.Format("\"{0}\"->\"{1}\" [color=\"blue\"  arrowhead=\"diamond\"]", this.RelatedObject.ToString(format), this.ThisObject.ToString(format));
                            
            //             case "parentAsData":
            //                 return String.Format("\"{0}\"->\"{1}\" [color=\"purple\"]", this.ThisObject.ToString(format), this.RelatedObject.ToString(format));

            //             case "parentAsLink":
            //                 return String.Format("\"{0}\"->\"{1}\" [color=\"blue\" arrowhead=\"diamond\"]", this.ThisObject.ToString(format), this.RelatedObject.ToString(format));
                        
            //             default:
            //                 return String.Format("\"{0}\"->\"{1}\"", this.RelatedObject.ToString(format), this.ThisObject.ToString(format));
            //         }
                    
            //     }
            //     else
            //     {
            //         return this.ToString();
            //     }
            // }
            // else
            // {
            //     return this.ToString();
            // }
        }

        public ObjectRelationship () {}

        public ObjectRelationship(string name, ObsObject sourceObject, ObsObject targetObject, ObsObjectRelationshipType relationshipType)
        {
            this.name = name;
            this.ThisObject = sourceObject;
            this.RelatedObject = targetObject;

            this.RelationshipType = relationshipType;
        }

        public bool Equals(ObjectRelationship other)
        {
            if (this.ThisObject.id == other.ThisObject.id &&
                this.ThisObject.GetType() == other.ThisObject.GetType() &&
                this.RelatedObject.id == other.RelatedObject.id &&
                this.RelatedObject.GetType() == other.RelatedObject.GetType() &&
                this.RelationshipType == other.RelationshipType)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

    }
}