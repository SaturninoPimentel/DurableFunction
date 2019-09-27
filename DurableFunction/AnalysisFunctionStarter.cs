using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace DurableFunction
{
    public static class AnalysisFunctionStarter
    {
        [FunctionName("AnalysisFunction")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function,
            "get", Route = null)] HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClient client,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string container = req.RequestUri.ParseQueryString()["container"];

            if (string.IsNullOrEmpty(container))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest,
                    "Please provide the name of the container");
            }

            log.LogInformation($"Orchestation started for the container {container}");

            string orchestrationId = await client.StartNewAsync("O_ProccessImage", container);
            return client.CreateCheckStatusResponse(req, orchestrationId);
        }
    }
}
