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

    /// <summary>A vantage street for use with NYC ZR 81-27.</summary>
    [JsonConverter(typeof(Elements.Serialization.JSON.JsonInheritanceConverter), "discriminator")]
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.21.0 (Newtonsoft.Json v12.0.0.0)")]
    public partial class NYCDaylightEvaluationVantageStreet : GeometricElement
    {
        [JsonConstructor]
        public NYCDaylightEvaluationVantageStreet(double @score, double @centerlineDistance, Line @frontLotLine, Line @centerline, bool @streetWallContinuity, IList<Line> @lotLines, double @blockDepth, Transform @transform = null, Material @material = null, Representation @representation = null, bool @isElementDefinition = false, System.Guid @id = default, string @name = null)
            : base(transform, material, representation, isElementDefinition, id, name)
        {
            this.Score = @score;
            this.CenterlineDistance = @centerlineDistance;
            this.FrontLotLine = @frontLotLine;
            this.Centerline = @centerline;
            this.StreetWallContinuity = @streetWallContinuity;
            this.LotLines = @lotLines;
            this.BlockDepth = @blockDepth;
            }
        
        // Empty constructor
        public NYCDaylightEvaluationVantageStreet()
            : base()
        {
        }
    
        /// <summary>Score for the street.</summary>
        [JsonProperty("Score", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double Score { get; set; }
    
        /// <summary>Calculated centerline distance of street.</summary>
        [JsonProperty("CenterlineDistance", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double CenterlineDistance { get; set; }
    
        /// <summary>Front lot line of the vantage street.</summary>
        [JsonProperty("FrontLotLine", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public Line FrontLotLine { get; set; }
    
        /// <summary>Centerline used for calculation: offset of the FrontLotLine the CenterlineDistance.</summary>
        [JsonProperty("Centerline", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public Line Centerline { get; set; }
    
        /// <summary>Whether your vantage street is on a street designated in the zoning code as 'desired street wall continuity.' See Section 81-43: https://zr.planning.nyc.gov/article-viii/chapter-1#81-43.</summary>
        [JsonProperty("StreetWallContinuity", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool StreetWallContinuity { get; set; }
    
        /// <summary>All the lot lines for the vantage street.</summary>
        [JsonProperty("LotLines", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public IList<Line> LotLines { get; set; }
    
        /// <summary>The depth of the block (not lot!) from this vantage street. Is used to calculate daylight boundaries, and only matters if block depth is less than 200'.</summary>
        [JsonProperty("BlockDepth", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double BlockDepth { get; set; }
    
    
    }
}