// This code was generated by Hypar.
// Edits to this code will be overwritten the next time you run 'hypar init'.
// DO NOT EDIT THIS FILE.

using Elements;
using Elements.GeoJSON;
using Elements.Geometry;
using Hypar.Functions;
using Hypar.Functions.Execution;
using Hypar.Functions.Execution.AWS;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace NYCZR8127DaylightEvaluation
{
    public class NYCZR8127DaylightEvaluationOutputs: SystemResults
    {
		/// <summary>
		/// A number below 66 means this design is not passing
		/// </summary>
		[JsonProperty("Lowest Street Score")]
		public double LowestStreetScore {get; set;}

		/// <summary>
		/// A number below 75 means the lot does not pass, or below 66 if this is in the East Midtown Subdistrict
		/// </summary>
		[JsonProperty("Overall Daylight Score")]
		public double OverallDaylightScore {get; set;}

		/// <summary>
		/// An ESTIMATE of whether your design is passing according to this calculation method.
		/// </summary>
		[JsonProperty("Result")]
		public string Result {get; set;}



        /// <summary>
        /// Construct a NYCZR8127DaylightEvaluationOutputs with default inputs.
        /// This should be used for testing only.
        /// </summary>
        public NYCZR8127DaylightEvaluationOutputs() : base()
        {

        }


        /// <summary>
        /// Construct a NYCZR8127DaylightEvaluationOutputs specifying all inputs.
        /// </summary>
        /// <returns></returns>
        [JsonConstructor]
        public NYCZR8127DaylightEvaluationOutputs(double lowestStreetScore, double overallDaylightScore, string result): base()
        {
			this.LowestStreetScore = lowestStreetScore;
			this.OverallDaylightScore = overallDaylightScore;
			this.Result = result;

		}

		public override string ToString()
		{
			var json = JsonConvert.SerializeObject(this);
			return json;
		}
	}
}