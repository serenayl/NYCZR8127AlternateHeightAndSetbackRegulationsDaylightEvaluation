//----------------------
// <auto-generated>
//     Generated using the NJsonSchema v10.1.21.0 (Newtonsoft.Json v12.0.0.0) (http://NJsonSchema.org)
// </auto-generated>
//----------------------
using Elements;
using Elements.GeoJSON;
using Elements.Geometry;
using Elements.Geometry.Solids;
using Elements.Spatial;
using Elements.Validators;
using Elements.Serialization.JSON;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Line = Elements.Geometry.Line;
using Polygon = Elements.Geometry.Polygon;

namespace Elements
{
    #pragma warning disable // Disable all warnings

    /// <summary>an element of unknown type, imported as an extrusion from Rhino</summary>
    [JsonConverter(typeof(Elements.Serialization.JSON.JsonInheritanceConverter), "discriminator")]
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.21.0 (Newtonsoft.Json v12.0.0.0)")]
    public partial class RhinoExtrusion : RhinoImportElement
    {
        [JsonConstructor]
        public RhinoExtrusion(Profile @profile, System.Guid @rhinoObjectId, bool @assignableRhinoObject, Transform @transform, Material @material, Representation @representation, bool @isElementDefinition, System.Guid @id, string @name)
            : base(profile, rhinoObjectId, assignableRhinoObject, transform, material, representation, isElementDefinition, id, name)
        {
            }
        
        // Empty constructor
        public RhinoExtrusion()
            : base()
        {
        }
    
    
    }
}