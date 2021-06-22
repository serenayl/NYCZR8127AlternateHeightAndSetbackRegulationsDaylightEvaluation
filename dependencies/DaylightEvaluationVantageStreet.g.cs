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
using System;
using System.Collections.Generic;
using System.Linq;
using Line = Elements.Geometry.Line;
using Polygon = Elements.Geometry.Polygon;

namespace Elements
{
    #pragma warning disable // Disable all warnings

    /// <summary>A vantage street for use with NYC ZR 81-27.</summary>
    [Newtonsoft.Json.JsonConverter(typeof(Elements.Serialization.JSON.JsonInheritanceConverter), "discriminator")]
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.21.0 (Newtonsoft.Json v12.0.0.0)")]
    public partial class DaylightEvaluationVantageStreet : Element
    {
        [Newtonsoft.Json.JsonConstructor]
        public DaylightEvaluationVantageStreet(double @score, double @numberOfVantagePoints, double @centerlineDistance, System.Guid @id = default, string @name = null)
            : base(id, name)
        {
            var validator = Validator.Instance.GetFirstValidatorForType<DaylightEvaluationVantageStreet>();
            if(validator != null)
            {
                validator.PreConstruct(new object[]{ @score, @numberOfVantagePoints, @centerlineDistance, @id, @name});
            }
        
            this.Score = @score;
            this.NumberOfVantagePoints = @numberOfVantagePoints;
            this.CenterlineDistance = @centerlineDistance;
            
            if(validator != null)
            {
                validator.PostConstruct(this);
            }
        }
    
        /// <summary>Score for the street.</summary>
        [Newtonsoft.Json.JsonProperty("Score", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double Score { get; set; }
    
        /// <summary>How many vantage points were generated for this street.</summary>
        [Newtonsoft.Json.JsonProperty("NumberOfVantagePoints", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double NumberOfVantagePoints { get; set; }
    
        /// <summary>Calculated centerline distance of street</summary>
        [Newtonsoft.Json.JsonProperty("CenterlineDistance", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double CenterlineDistance { get; set; }
    
    
    }
}