using System;
using System.Collections.Generic;
using PublicHoliday;

namespace Zucchini.Bot;
internal class SouthTyrolPublicHoliday : ItalyPublicHoliday
{
    //
    // Summary:
    //     Pentecost Monday
    //
    // Parameters:
    //   year:
    //     The year.
    //
    // Returns:
    //     Date of in the given year.
    private static DateTime PentecostMonday(int year)
    {
        return EasterMonday(year).AddDays(7 * 7);
    }

    public override IDictionary<DateTime, string> PublicHolidayNames(int year)
    {
        IDictionary<DateTime, string> holidayNames = base.PublicHolidayNames(year);
        return holidayNames;
    }

    public override bool IsPublicHoliday(DateTime dt)
    {
        bool isPublicHoliday = base.IsPublicHoliday(dt);
        if (!isPublicHoliday)
        {
            isPublicHoliday = dt == PentecostMonday(dt.Year);
        }

        return isPublicHoliday;
    }
}
