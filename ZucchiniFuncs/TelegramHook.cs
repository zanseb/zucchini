using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ZucchiniFuncs.Models;
using Telegram.Bot;
using Zucchetti;
using System.Text;

namespace ZucchiniFuncs
{
    public static class TelegramHook
    {
        [FunctionName(nameof(TelegramHook))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Telegram hook invoke requested");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            TelegramUpdateDTO telegramUpdate;
            try
            {
                telegramUpdate = JsonSerializer.Deserialize<TelegramUpdateDTO>(requestBody);
            }
            catch (JsonException)
            {
                return new BadRequestResult();
            }

            var telegramApiKey = Environment.GetEnvironmentVariable("TelegramApiKey");
            var zucchettiBaseUrl = Environment.GetEnvironmentVariable("ZucchettiBaseUrl");
            var zucchettiUser = Environment.GetEnvironmentVariable("ZucchettiUser");
            var zucchettiPassword = Environment.GetEnvironmentVariable("ZucchettiPassword");

            var zucchettiClient = new ZucchettiClient(zucchettiBaseUrl, zucchettiUser, zucchettiPassword);
            await zucchettiClient.LoginAsync();
            var stamps = await zucchettiClient.RetrieveStampsAsync(DateOnly.FromDateTime(DateTime.Now));

            var message = new StringBuilder();
            foreach (var stamp in stamps)
            {
                message.AppendLine($"{(stamp.Direction == StampDirection.In ? "😩" : "😁")} - {stamp.Time}");
            }

            var botClient = new TelegramBotClient(telegramApiKey);
            await botClient.SendTextMessageAsync(telegramUpdate.message.chat.id, message.ToString());

            log.LogInformation(requestBody);
            return new OkResult();
        }
    }
}
