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
                return new UnauthorizedResult();
            }

            var telegramApiKey = Environment.GetEnvironmentVariable("TelegramApiKey");
            var zucchettiBaseUrl = Environment.GetEnvironmentVariable("ZucchettiBaseUrl");
            var zucchettiUser = Environment.GetEnvironmentVariable("ZucchettiUser");
            var zucchettiPassword = Environment.GetEnvironmentVariable("ZucchettiPassword");

            var botClient = new TelegramBotClient(telegramApiKey);
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
;
            return new OkResult();
        }
    }
}
