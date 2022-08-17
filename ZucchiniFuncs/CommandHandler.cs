using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zucchetti;

namespace ZucchiniFuncs
{
    internal class CommandHandler
    {
        private readonly ZucchettiClient client;

        public CommandHandler(ZucchettiClient client)
        {
            this.client = client;
        }

        public Task<string> HandleAsync(string command)
        {
            return command switch
            {
                "/in" => HandleClockInAsync(),
                "/out" => HandleClockOutAsync(),
                "/stamps" => HandleRetrieveStampsAsync(),
                _ => HandleUknownCommandAsync(),
            };
        }

        private async Task<string> HandleClockInAsync()
        {
            await client.ClockInAsync();
            return "Let's get working 🥵";
        }

        private async Task<string> HandleClockOutAsync()
        {
            await client.ClockOutAsync();
            return "Over and out 😎";
        }

        private async Task<string> HandleRetrieveStampsAsync()
        {
            var result = new StringBuilder();
            var stamps = await client.RetrieveStampsAsync(DateOnly.FromDateTime(DateTime.Now));

            if (stamps.Count() == 0)
            {
                return "No stamps today 🤐";
            }

            foreach (var stamp in stamps)
            {
                var direction = stamp.Direction == StampDirection.In ? "➡ " : "⬅ ";
                result.AppendLine($"{direction} -- {stamp.Time.ToString("g")}");
            }

            TimeSpan difference = new TimeSpan();
            var pairs = stamps.Select((value, index) => new { value, index })
                              .GroupBy(x => x.index / 2, x => x.value);
            foreach (var pair in pairs)
            {
                var clockInStamp = pair.First();
                var clockOutStamp = pair.Last();

                difference += clockOutStamp.Time - clockInStamp.Time;
            }

            result.AppendLine(String.Empty);
            result.AppendLine($"⏳ -- {difference.Hours}h {difference.Minutes}m");

            return result.ToString();
        }

        private Task<string> HandleUknownCommandAsync()
        {
            return Task.FromResult("😵");
        }
    }
}
