using System;
using System.Globalization;

namespace Unify.Extensions;

public static class DateTimeExtensions
{
    public static DateTime GetFirstDayOfWeek(this DateTime dayInWeek, CultureInfo cultureInfo = default)
    {
        cultureInfo ??= CultureInfo.CurrentCulture;
        
        var firstDay = cultureInfo.DateTimeFormat.FirstDayOfWeek;
        
        var firstDayInWeek = dayInWeek.Date;
        
        while (firstDayInWeek.DayOfWeek != firstDay)
            firstDayInWeek = firstDayInWeek.AddDays(-1);
        
        return firstDayInWeek;
    }
}