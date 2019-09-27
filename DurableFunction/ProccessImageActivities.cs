using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DurableFunction
{
    public static class ProccessImageActivities
    {
        [FunctionName("A_GetBlockBlobStorageItemsUris")]
        public static async Task<List<string>> GetBlockBlobStorageItemsNames(
            [ActivityTrigger]string containerName,
            ILogger logger)
        {
            CloudBlobContainer cloudBlobContainer = await GetCloudBlobContainer(containerName);

            int i = 0;
            BlobContinuationToken continuationToken = null;
            List<string> results = new List<string>();
            do
            {
                BlobResultSegment resultSegment = await cloudBlobContainer
                    .ListBlobsSegmentedAsync("", true, BlobListingDetails.All, 10, continuationToken, null, null);
                if (resultSegment.Results.Any())
                {
                    Console.WriteLine("Page {0}:", ++i);
                }
                foreach (IListBlobItem item in resultSegment.Results)
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    await blob.FetchAttributesAsync();
                    logger.LogInformation(blob.Name);
                    results.Add(blob.Name);
                }
            } while (continuationToken != null);
            return results;
        }

        [FunctionName("A_AnalizeImage")]
        public static async Task<AnalysisResult> AnalizeImageAndSaveImage(
            [ActivityTrigger] ImageInformation imageInformation,
            ILogger logger)
        {
            logger.LogInformation("image analysis has started");
            AnalysisResult analysisResult = null;
            CloudBlobContainer cloudBlobContainer =
                await GetCloudBlobContainer(imageInformation.ContainerName);
            CloudBlockBlob blockBlob =
                cloudBlobContainer.GetBlockBlobReference(imageInformation.BlobName);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                await blockBlob.DownloadToStreamAsync(memoryStream);
                try
                {
                    analysisResult = await AnalizeImageAsync(memoryStream.GetBuffer());
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error {nameof(AnalizeImageAndSaveImage)} \n{blockBlob?.Name}");
                }

            }
            return analysisResult;
        }

        [FunctionName("A_SaveResult")]
        public static async Task SaveResult(
            [ActivityTrigger]AnalysisResult analysisResult,
            [CosmosDB(
            databaseName: "AnalysisResult",
            collectionName: "ImageAnalysis",
            ConnectionStringSetting = "CosmosDBConnection",
        CreateIfNotExists =true)]IAsyncCollector<AnalysisResult> analysisResultItems,
            ILogger logger)
        {
            logger.LogInformation("Saving data in cosmosdb started");
            if (analysisResult != null)
            {
                await analysisResultItems.AddAsync(analysisResult);
            }
        }

        private static async Task<AnalysisResult> AnalizeImageAsync(byte[] imageArray)
        {
            HttpClient httpClient = new HttpClient();
            string token = Environment.GetEnvironmentVariable("ComputerValidationToken");
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", token);
            HttpResponseMessage response;
            string computerVisionUrl = Environment.GetEnvironmentVariable("ComputerVisionUrl");
            using (StreamContent content =
                new StreamContent(new MemoryStream(imageArray)))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response =
                    await httpClient.PostAsync(computerVisionUrl, content);
            }

            response.EnsureSuccessStatusCode();
            string responseString = await
                response.Content.ReadAsStringAsync();
            AnalysisResult analysisResult = JsonConvert.DeserializeObject<AnalysisResult>(responseString);

            return analysisResult;
        }

        private static async Task<CloudBlobContainer> GetCloudBlobContainer(string containerName)
        {
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient cloudBlobClient =
                cloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer cloudBlobContainer =
                cloudBlobClient.GetContainerReference(containerName);
            await cloudBlobContainer.CreateIfNotExistsAsync();
            return cloudBlobContainer;
        }

    }
}