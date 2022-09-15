using BrazilHolidays.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaselExportLambda
{
    public class DateValidateExport
    {
        public static DateTime ValidateDate(DateTime refDate)
        {
            if(refDate.IsHoliday())
            {
                if (refDate.DayOfWeek == DayOfWeek.Tuesday)
                    return refDate.AddDays(-4);
                else
                    return refDate.AddDays(-2);
            }
            else
            {
                if (refDate.DayOfWeek == DayOfWeek.Monday)
                    return refDate.AddDays(-3);
                else
                    return refDate.AddDays(-1);
            }
        }
    }
}
