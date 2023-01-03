using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

using Telegram.Bot;

using Zucchetti;

using ZucchiniFuncs.Models;

namespace ZucchiniFuncs
{
    public static class TelegramHook
    {
        [FunctionName(nameof(TelegramHook))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {

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

            var allowedChatId = Convert.ToInt32(Environment.GetEnvironmentVariable("TelegramChatId"));
            var chatId = telegramUpdate.message.chat.id;
            if (chatId != allowedChatId)
            {
                // Tell telegram that everything is OK so that it does not retry continuously
                return new OkResult();
            }

            var telegramApiKey = Environment.GetEnvironmentVariable("TelegramApiKey");
            var zucchettiBaseUrl = Environment.GetEnvironmentVariable("ZucchettiBaseUrl");
            var zucchettiUser = Environment.GetEnvironmentVariable("ZucchettiUser");
            var zucchettiPassword = Environment.GetEnvironmentVariable("ZucchettiPassword");

            var botClient = new TelegramBotClient(telegramApiKey);

            DateTime dateTime = UnixTimeStampToDateTime(telegramUpdate.message.date);
            DateTime utcNow = DateTime.UtcNow;
            TimeSpan timeSpan = dateTime - utcNow;

            if (Math.Abs(timeSpan.TotalMinutes) > 1)
            {
                await botClient.SendTextMessageAsync(chatId, "❌ Snap, old messages will be ignored 🤷");
                return new OkResult();
            }

            var zucchettiClient = new ZucchettiClient(zucchettiBaseUrl, zucchettiUser, zucchettiPassword);
            try
            {
                await zucchettiClient.LoginAsync();
            }
            catch (Exception)
            {
                await botClient.SendTextMessageAsync(chatId, "❌ Snap, I can't log in ❌");
                throw;
            }

            var handler = new CommandHandler(zucchettiClient);
            var responseMessage = await handler.HandleAsync(telegramUpdate.message.text);

            await botClient.SendTextMessageAsync(telegramUpdate.message.chat.id, responseMessage);

            return new OkResult();
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            return DateTime.UnixEpoch.AddSeconds(unixTimeStamp).ToLocalTime();
        }
    }
}
