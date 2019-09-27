using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DurableFunction
{

    public class ProcessImageOrchestrators
    {
        [FunctionName("O_ProccessImage")]
        public static async Task
            ProccessImage(
            [OrchestrationTrigger]DurableOrchestrationContext context,
            ILogger logger)
        {
            string containerName = context.GetInput<string>();

            List<string> blobList = await
                context.CallActivityAsync<List<string>>("A_GetBlockBlobStorageItemsUris", containerName);

            Task<AnalysisResult>[] analysisTasks =
                new Task<AnalysisResult>[blobList.Count];

            for (int i = 0; i < blobList.Count; i++)
            {
                ImageInformation imageInformation = new ImageInformation()
                {
                    BlobName = blobList[i],
                    ContainerName = containerName
                };
                analysisTasks[i] = context.CallActivityAsync<AnalysisResult>("A_AnalizeImage",
                    imageInformation);
            }
            await Task.WhenAll(analysisTasks);
            Task[] saveTasks =
                new Task[analysisTasks.Length];
            for (int i = 0; i < analysisTasks.Length; i++)
            {
                saveTasks[i] = context.CallActivityAsync("A_SaveResult",
                    analysisTasks[i].Result);
            }
            await Task.WhenAll(saveTasks);
        }
    }
}
