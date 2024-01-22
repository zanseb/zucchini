using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Zucchetti;
using Zucchini.Bot;

namespace ZucchiniFuncs
{
    internal class CommandHandler
    {
        private readonly ZucchettiClient client;

        public CommandHandler(ZucchettiClient client)
        {
            this.client = client;
        }

        public async Task<string> HandleAsync(string command)
        {
            try
            {
                Task<string> task = command switch
                {
                    "/in" => HandleClockInAsync(),
                    "/out" => HandleClockOutAsync(),
                    "/stamps" => HandleRetrieveStampsAsync(),
                    "/month" => HandleRetrieveWorkHoursOfMonth(command.Split(' ')[1..]),
                    _ => HandleUnknownCommandAsync(),
                };

                return await task;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
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
            DateTime currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time"));
            var stamps = await client.RetrieveStampsAsync(DateOnly.FromDateTime(currentTime));
            stamps = stamps.OrderBy((stamp) => stamp.Time);

            if (!stamps.Any())
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

            if (stamps.Count() % 2 != 0)
            {
                difference += currentTime - stamps.Last().Time;
                var estimatedClockoutTime = currentTime + (TimeSpan.FromHours(8) - difference);

                result.AppendLine(String.Empty);
                result.AppendLine($"👋 -- {estimatedClockoutTime.ToString("g")}");
            }

            result.AppendLine(String.Empty);
            result.AppendLine($"⏳ -- {difference.Hours}h {difference.Minutes}m");

            return result.ToString();
        }

        private static IEnumerable<DateTime> AllDatesInMonth(int year, int month)
        {
            int days = DateTime.DaysInMonth(year, month);
            for (int day = 1; day <= days; day++)
            {
                yield return new DateTime(year, month, day);
            }
        }

        private static TimeSpan CalculateWorkingHours(IEnumerable<Stamp> stamps)
        {
            TimeSpan difference = new TimeSpan();
            var pairs = stamps.Select((value, index) => new { value, index })
                              .GroupBy(x => x.index / 2, x => x.value);
            foreach (var pair in pairs)
            {
                var clockInStamp = pair.First();
                var clockOutStamp = pair.Last();

                difference += clockOutStamp.Time - clockInStamp.Time;
            }

            return difference;
        }

        private async Task<string> HandleRetrieveWorkHoursOfMonth(string[] parameters)
        {
            var result = new StringBuilder();
            DateTime currentTime;
            if (parameters.Length == 2)
            {
                currentTime = new DateTime(int.Parse(parameters[0]), int.Parse(parameters[1]), 1);
            }
            else
            {
                currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time"));
            }
            SouthTyrolPublicHoliday southTyrolPublicHoliday = new();

            var totalWorkingHours = TimeSpan.Zero;
            var totalOvertime = TimeSpan.Zero;
            var belowWorkTime = TimeSpan.Zero;
            var giftedWorkTime = TimeSpan.Zero;

            TimeSpan workingHourTarget = TimeSpan.FromHours(8);
            TimeSpan overTimeStartTarget = workingHourTarget + TimeSpan.FromMinutes(30);

            foreach (var day in AllDatesInMonth(currentTime.Year, currentTime.Month))
            {
                if (day > DateTime.Today)
                {
                    break;
                }

                var stamps = await client.RetrieveStampsAsync(DateOnly.FromDateTime(day));
                stamps = stamps.OrderBy((stamp) => stamp.Time);

                bool isWorkingDay = southTyrolPublicHoliday.IsWorkingDay(day);
                TimeSpan workingHours = CalculateWorkingHours(stamps);
                if (workingHours == TimeSpan.Zero && !isWorkingDay)
                {
                    continue;
                }

                var status = string.Empty;
                if (day.DayOfWeek == DayOfWeek.Saturday)
                {
                    var overtime = workingHours;
                    overtime = TimeSpan.FromMinutes(15 * Math.Floor(overtime.TotalMinutes / 15));

                    status = $" 🔺{overtime:hh\\:mm}";

                    totalOvertime += overtime;
                }
                else if (workingHours > overTimeStartTarget)
                {
                    var overtime = workingHours - workingHourTarget;
                    overtime = TimeSpan.FromMinutes(15 * Math.Floor(overtime.TotalMinutes / 15));

                    status = $" 🔺{overtime:hh\\:mm}";

                    totalOvertime += overtime;
                }
                else if (day == DateTime.Today)
                {
                    status = " 🕒";
                }
                else if (workingHours < workingHourTarget)
                {
                    var undertime = workingHourTarget - workingHours;
                    status = $" 🔻{undertime:hh\\:mm}";

                    belowWorkTime += undertime;
                }
                else
                {
                    giftedWorkTime += workingHours - workingHourTarget;
                }

                result.AppendLine($"<code>{day.ToString("ddd dd.MM")}</code>: {workingHours.ToString(@"hh\:mm")}{status}");
            }

            result.AppendLine();
            result.AppendLine($"Overtime: {totalOvertime.ToString(@"hh\:mm")}");
            result.AppendLine($"Below WorkTime: {belowWorkTime.ToString(@"hh\:mm")}");
            result.AppendLine($"Gifted WorkTime: {giftedWorkTime.ToString(@"hh\:mm")}");

            if (belowWorkTime > totalOvertime)
            {
                result.AppendLine($"Total 🔻 {(belowWorkTime - totalOvertime).ToString(@"hh\:mm")}");
            }
            else
            {
                result.AppendLine($"Total 🔺 {(totalOvertime - belowWorkTime).ToString(@"hh\:mm")}");
            }

            return result.ToString();
        }

        private Task<string> HandleUnknownCommandAsync()
        {
            return Task.FromResult("😵");
        }
    }
}
