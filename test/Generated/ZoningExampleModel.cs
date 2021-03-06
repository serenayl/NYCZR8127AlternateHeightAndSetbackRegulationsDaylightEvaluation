
// This code was generated by Hypar.
// Edits to this code will be overwritten the next time you run 'hypar test generate'.
// DO NOT EDIT THIS FILE.

using Elements;
using Xunit;
using System.IO;
using System.Collections.Generic;
using Elements.Serialization.glTF;

namespace NYCZR8127DaylightEvaluation
{
    public class ZoningExampleModel
    {
        [Fact]
        public void TestExecute()
        {
            var input = GetInput();

            var modelDependencies = new Dictionary<string, Model> {
                {"Envelope", Model.FromJson(File.ReadAllText(@"/Users/serenali/Hypar Dropbox/Serena Li/Functions/Daylight Evaluation Functions/CSharpVersion/NYCZR8127DaylightEvaluation/test/Generated/ZoningExampleModel/model_dependencies/Envelope/model.json")) },
                {"Site", Model.FromJson(File.ReadAllText(@"/Users/serenali/Hypar Dropbox/Serena Li/Functions/Daylight Evaluation Functions/CSharpVersion/NYCZR8127DaylightEvaluation/test/Generated/ZoningExampleModel/model_dependencies/Site/model.json")) },
            };

            var result = NYCZR8127DaylightEvaluation.Execute(modelDependencies, input);
            result.Model.ToGlTF("../../../Generated/ZoningExampleModel/results/ZoningExampleModel.gltf", false);
            result.Model.ToGlTF("../../../Generated/ZoningExampleModel/results/ZoningExampleModel.glb");
            File.WriteAllText("../../../Generated/ZoningExampleModel/results/ZoningExampleModel.json", result.Model.ToJson());

            Assert.True(result.OverallDaylightScore.ToString().StartsWith("72.4569"));
        }

        public NYCZR8127DaylightEvaluationInputs GetInput()
        {
            var inputText = @"
            {
  ""model_input_keys"": {
    ""Envelope"": ""30655285-19ae-4448-af88-e6c0daa658d8_ea819d61-44ee-4b0c-a27d-777e47b5a722_elements.zip"",
    ""Site"": ""a5cb8db7-6019-4d8e-9220-a65b04f07c24_c6b5241e-fef1-47a9-a167-fc71ddd30cb3_elements.zip""
  },
  ""Qualify for East Midtown Subdistrict"": false,
  ""Skip Subdivide"": false,
  ""Vantage Streets"": [
    {
      ""Line"": {
        ""Start"": {
          ""X"": -12.83372175735282,
          ""Y"": -8.941496555023171,
          ""Z"": 0
        },
        ""End"": {
          ""X"": 59.49823544103082,
          ""Y"": -6.3205322091929474,
          ""Z"": 0
        },
        ""discriminator"": ""Elements.Geometry.Line""
      },
      ""Street Wall Continuity"": false,
      ""Block Depth In Feet"": 200,
      ""Width"": ""100ft"",
      ""Name"": ""South Street""
    }
  ],
  ""Debug Visualization"": false
}
            ";
            return Newtonsoft.Json.JsonConvert.DeserializeObject<NYCZR8127DaylightEvaluationInputs>(inputText);
        }
    }
}