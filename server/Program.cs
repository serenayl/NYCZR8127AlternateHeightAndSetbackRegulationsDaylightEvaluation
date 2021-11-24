
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Hypar.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await HyparServer.StartAsync(
                args,
                Path.GetFullPath(Path.Combine(@"/Users/serenali/Hypar Dropbox/Serena Li/Functions/Daylight Evaluation Functions/CSharpVersion/NYCZR8127DaylightEvaluation/server", "..")),
                typeof(NYCZR8127DaylightEvaluation.Function),
                typeof(NYCZR8127DaylightEvaluation.NYCZR8127DaylightEvaluationInputs));
        }
    }
}