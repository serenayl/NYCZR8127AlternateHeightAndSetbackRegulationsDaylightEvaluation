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

    /// <summary>A vantage point for use with NYC ZR 81-27.</summary>
    [Newtonsoft.Json.JsonConverter(typeof(Elements.Serialization.JSON.JsonInheritanceConverter), "discriminator")]
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.21.0 (Newtonsoft.Json v12.0.0.0)")]
    public partial class DaylightEvaluationVantagePoint : Element
    {
        [Newtonsoft.Json.JsonConstructor]
        public DaylightEvaluationVantagePoint(Vector3 @position, double @daylightBlockage, double @unblockedDaylightCredit, double @profileDaylightBlockage, double @availableDaylight, double @daylightRemaining, double @daylightScore, System.Guid @id = default, string @name = null)
            : base(id, name)
        {
            var validator = Validator.Instance.GetFirstValidatorForType<DaylightEvaluationVantagePoint>();
            if(validator != null)
            {
                validator.PreConstruct(new object[]{ @position, @daylightBlockage, @unblockedDaylightCredit, @profileDaylightBlockage, @availableDaylight, @daylightRemaining, @daylightScore, @id, @name});
            }
        
            this.Position = @position;
            this.DaylightBlockage = @daylightBlockage;
            this.UnblockedDaylightCredit = @unblockedDaylightCredit;
            this.ProfileDaylightBlockage = @profileDaylightBlockage;
            this.AvailableDaylight = @availableDaylight;
            this.DaylightRemaining = @daylightRemaining;
            this.DaylightScore = @daylightScore;
            
            if(validator != null)
            {
                validator.PostConstruct(this);
            }
        }
    
        /// <summary>The position of the vantage point.</summary>
        [Newtonsoft.Json.JsonProperty("Position", Required = Newtonsoft.Json.Required.AllowNull)]
        public Vector3 Position { get; set; }
    
        /// <summary>Count the number of blocked daylight squares and subsquares which are above the curved line representing an elevation of 70 degrees. A negative sign is to be given to this number.</summary>
        [Newtonsoft.Json.JsonProperty("DaylightBlockage", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double DaylightBlockage { get; set; }
    
        /// <summary>Count the number of unblocked daylight squares which are below the curved line representing an elevation of 70 degrees and within the area defined by the intersection of the far lot line with the vantage street line and the intersection of the near lot line with the vantage street line. The total is given a positive value and multiplied by 0.3, the value of these daylight squares. This provision is not applicable where the vantage street is a designated street on which street wall continuity is required by the provisions of Section 81-43 (Street Wall Continuity Along Designated Streets).</summary>
        [Newtonsoft.Json.JsonProperty("UnblockedDaylightCredit", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double UnblockedDaylightCredit { get; set; }
    
        /// <summary>Count the number of blocked daylight squares which are entirely on the far side of the profile curve when viewed from the vantage point and the number of blocked or partially blocked subsquares which are on the far side of the profile curve. All of these daylight squares and subsquares are given a negative sign, multiplied by their respective weighted values in the table in paragraph (a)(4) of this Section and the products added. Subsquares are counted as one tenth of a daylight square.</summary>
        [Newtonsoft.Json.JsonProperty("ProfileDaylightBlockage", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double ProfileDaylightBlockage { get; set; }
    
        /// <summary>Count the number of daylight squares available to the site. This is the total number of daylight squares and subsquares, calculated to the nearest tenth, that are above the curved line representing the boundaries of the potential sky area available to the site, said boundaries being delineated in accordance with the provisions of paragraph (f) of Section 81-273 (Rules for plotting buildings on the daylight evaluation chart).</summary>
        [Newtonsoft.Json.JsonProperty("AvailableDaylight", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double AvailableDaylight { get; set; }
    
        /// <summary>Calculate the remaining or unblocked daylight by adding the results of paragraphs (b) through (e) of this Section.</summary>
        [Newtonsoft.Json.JsonProperty("DaylightRemaining", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double DaylightRemaining { get; set; }
    
        /// <summary>Compute the remaining daylight score from paragraph (f) of this Section, as a percentage of the available daylight from paragraph (e) of this Section. The percentage is the daylight score for the proposed building from that vantage point.</summary>
        [Newtonsoft.Json.JsonProperty("DaylightScore", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double DaylightScore { get; set; }
    
    
    }
}