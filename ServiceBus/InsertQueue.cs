using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Azure.ServiceBus;

namespace ServiceBus
{
    public static class InsertQueue
    {
        static IQueueClient queueClient;

        [FunctionName("InsertQueue")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            
            string item = req.Query["item"];
            string id = req.Query["id"];
            string itemProcess = req.Query["itemProcess"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            item = item ?? data?.item;
            id = id ?? data?.id;
            itemProcess = itemProcess ?? data?.itemProcess;

            if (item == null || id == null)
            {
                return new BadRequestObjectResult("Please pass a name on the query string or in the request body");
            }

            var result = new Transaction()
            {
                Id = Int32.Parse(id),
                Item = item,
                ItemProcess = itemProcess
            };

            const string sbConnectionString = "Endpoint=sb://awr-scrape.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=G1ytRwFEjUqcJz8AF6zOu4Jm5zWxlB9sFUm4+V0WdUU=";
            const string sbQueueName = "awr";
            queueClient = new QueueClient(sbConnectionString, sbQueueName);
            var jsonObject = string.Empty;

            try
            {
                jsonObject = JsonConvert.SerializeObject(result);
                var message = new Message(Encoding.UTF8.GetBytes(jsonObject));
                await queueClient.SendAsync(message); 
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult("Exception: "+ ex.Message);
            }
            finally
            {
                await queueClient.CloseAsync();
            }

            return (ActionResult)new OkObjectResult($"Done, {jsonObject}");
        }
    }

    public class Transaction
    {
        public int Id { get; set; }

        public string Item { get; set; }

        public string ItemProcess { get; set; }
    }
}
