using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace GreenWerx.Utilites.Extensions
{
    public static class DateEx
    {
        public static string[] Months = @"january,february,march,april,may,june,july,august,september,october,november,december".Split(',');
        public static string[] MonthsAbbreviated = @"jan,feb,mar,apr,may,jun,jul,aug,sept,oct,nov,dec".Split(',');
        public static string[] DaysOfWeek = @"saturday,sunday,monday,tuesday,wednesday,thursday,friday".Split(',');

        public static DateTime ConvertFromUnixTimestamp(this double timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return origin.AddSeconds(timestamp);
        }

        public static double ConvertToUnixTimestamp(this DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return Math.Floor(diff.TotalSeconds);
        }

        public static string CleanDateString(string date)
        {
            return date
                .Replace(" &ndash;", "")
                .Replace("&nbsp;", "")
                .Replace("&quot;", "")
                .Replace("st,", ",")
                .Replace("nd,", ",")
                .Replace("th,", ",")
                .Replace("\\t,", " ")
                .Replace("pm", " pm")// these standardize the time. some come 2am and 2 am
                .Replace("am", " am")
                 .Replace("-", " - ")
                .Replace("  ", " ")
                 .Replace("\n", " ")
                .Replace(Environment.NewLine, " ").ToLower();
        }

        //Replaces some values with a token instead of blank
        public static string CleanDateStringWithToken(string date, string token)
        {
            return date
                .Replace(" &ndash;", "")
                .Replace("&nbsp;", "")
                .Replace("&quot;", "")
                .Replace("st,", ",")
                .Replace("nd,", ",")
                .Replace("th,", ",")
                .Replace("\\t,", " ")
                .Replace("pm", " pm")// these standardize the time. some come 2am and 2 am
                .Replace("am", " am")
                 .Replace("-", " - ")
                .Replace("  ", " ")
                 .Replace("\n", token)
                .Replace(Environment.NewLine, token).ToLower();
        }

        // Tuesday Dec 31, 9pm - Wednesday, Jan 01 2020 , 4am
        private static Tuple<DateTime, DateTime> ParseSplitDates(string date)
        {
            string currentYear = DateTime.Now.Year.ToString();
            string nextYear = DateTime.Now.AddYears(1).Year.ToString();// for dates that start on new years eve etc and end on end next day or after
            DateTime startDate = DateTime.MinValue;// so if defaulted to this it won't show up in list
            DateTime endDate = DateTime.MinValue; //startDate.AddDays(100);


            foreach (string tmpDay in DateEx.DaysOfWeek)
                date = date.Replace(tmpDay, "");

            string[] dates = date.Split('-');

            #region Parse Start Date
            string tmp = "";
            if (dates[0].Contains(currentYear) || dates[0].Contains(nextYear))
                tmp = dates[0].Split(',')[0];
            else
                tmp = dates[0].Split(',')[0] + ", " + DateTime.Now.Year.ToString();

            DateTime.TryParse(tmp, out startDate);
            #endregion

            #region Parse Start Time
            string ampm = "pm";
            int hour = -1;
            tmp = dates[0].Split(',')[1];
            if (tmp.Contains("am"))
                ampm = "am";

            tmp = tmp.Replace(ampm, "");
            int.TryParse(tmp, out hour);

            hour = hour > 12 ? hour % 12 : hour;
            if (ampm.ToLower() == "pm")
                hour += 12;

            var time = new TimeSpan(hour, 0, 0);
            startDate = startDate + time;
            #endregion

            #region Parse End Date
            tmp = "";
            if (dates[1].Contains(currentYear) || dates[1].Contains(nextYear))
                tmp = dates[1].Split(',')[1];// there's a 1 on second array because two commas
            else
                tmp = dates[1].Split(',')[1] + ", " + DateTime.Now.Year.ToString();

            DateTime.TryParse(tmp, out endDate);
            #endregion

            #region Parse End Time
            ampm = "pm";
            hour = -1;
            tmp = dates[1].Split(',')[2];
            if (tmp.Contains("am"))
                ampm = "am";

            tmp = tmp.Replace(ampm, "");
            int.TryParse(tmp, out hour);

            hour = hour > 12 ? hour % 12 : hour;
            if (ampm.ToLower() == "pm")
                hour += 12;

            time = new TimeSpan(hour, 0, 0);
            endDate = endDate + time;
            #endregion  

            if (endDate < startDate)
            {
                if (startDate.Month == 12 && startDate.Day == 31 && endDate.Year == startDate.Year)
                    endDate = endDate.AddYears(1);
            }

            return new Tuple<DateTime, DateTime>(startDate, endDate);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="monthFound">this is the month that was found during the initial processing.</param>
        /// <returns></returns>
        public static Tuple<DateTime, DateTime> ParseForStartAndEndDates(string dateTime, string source = "") 
        {
            if (string.IsNullOrWhiteSpace(source))
                source = "modernlifestyle";

            switch (source.ToLower()) {
                case "blisscruises":
                    return ParseBlissCruisesDates(dateTime);
                case "modernlifestyle.web":
                    return ParseModernLifestyleWebDates(dateTime);
            }
            return ParseModernLifestyleDates(dateTime);
        }

        // April 20 - 25, 2020
        private static Tuple<DateTime, DateTime> ParseBlissCruisesDates(string dateTime)
        {
            if (string.IsNullOrWhiteSpace(dateTime))
                return null;

            string currentYear = DateTime.Now.Year.ToString();
            string nextYear = DateTime.Now.AddYears(1).Year.ToString();
            string twoYears = DateTime.Now.AddYears(2).Year.ToString();

            DateTime startDate = DateTime.MinValue;
            DateTime endDate = DateTime.MinValue;

            string date = CleanDateString(dateTime).Trim();
            date = ReplaceAbbreviatedMonths(date);

            string month = DateEx.GetMonth(date);
            string startDay = "";
            string endDay = "";
            string year = currentYear;

            if (dateTime.Contains(nextYear))
                year = nextYear;


            if (dateTime.Contains(twoYears))
                year = twoYears;


            string[] dateSplit = date.Split(' ');
            for (int i = 0; i < dateSplit.Length; i++)
            {
                if (dateSplit[i] == "-") {
                    startDay = dateSplit[i - 1];
                    endDay = dateSplit[i + 1].ToSafeString(true);
                }
            }

            DateTime.TryParse($"{month} {startDay}, {year}", out startDate);
            DateTime.TryParse($"{month} {endDay}, {year}", out endDate);

            return new Tuple<DateTime, DateTime>(startDate, endDate);

        }

        private static Tuple<DateTime, DateTime> ParseModernLifestyleWebDates(string dateTime)
        {
            if (string.IsNullOrWhiteSpace(dateTime))
                return null;

            string currentYear = DateTime.Now.Year.ToString();
            string nextYear = DateTime.Now.AddYears(1).Year.ToString();
            string twoYears = DateTime.Now.AddYears(2).Year.ToString();

            DateTime startDate = DateTime.MinValue;
            DateTime endDate = DateTime.MinValue;

            string date = CleanDateString(dateTime).Trim();
            date = ReplaceAbbreviatedMonths(date);

            string startMonth = DateEx.GetMonth(date);
            string endMonth = DateEx.GetMonth(date);
            string startDay = "";
            string endDay = "";
            string year = currentYear;

            if (dateTime.Contains(nextYear))
                year = nextYear;


            if (dateTime.Contains(twoYears))
                year = twoYears;

            bool found = false;
            string[] dateSplit = date.Split(' ');
            for (int i = 0; i < dateSplit.Length; i++)
            {
                if (dateSplit[i] == "-")
                {
                    startDay = dateSplit[i - 1];
                    endDay = dateSplit[i + 1].ToSafeString(true);
                    int tmp = -1;
                    if (!int.TryParse(startDay.Trim(), out tmp))
                        continue;

                    if (!int.TryParse(endDay.Trim(), out tmp))
                    {
                        endMonth = DateEx.GetMonth(endDay);
                        endDay = dateSplit[i + 2].ToSafeString(true); //skip ahead two
                    }

                    if (!int.TryParse(endDay.Trim(), out tmp))
                    {
                        Debug.Assert(false, "shouldn't get here");
                        continue;
                    }
                    found = true;
                }
            }

            if (!found)
            {
                //  Fri, Mar 13
                for (int i = 0; i < dateSplit.Length; i++)
                {
                    int day = -1;
                    if (int.TryParse(dateSplit[i].Trim(), out day))
                    {
                        startDay = day.ToString();
                        endDay = startDay;
                        break;
                    }
                    
                }
            }

            if (string.IsNullOrWhiteSpace(endDay))
                endDay = startDay;

            DateTime.TryParse($"{startMonth} {startDay}, {year}", out startDate);
            DateTime.TryParse($"{endMonth} {endDay}, {year}", out endDate);

            if (endDate == DateTime.MinValue)
                endDate = startDate;

            return new Tuple<DateTime, DateTime>(startDate, endDate);

        }


        private static Tuple<DateTime, DateTime> ParseModernLifestyleDates(string dateTime) {
            string currentYear = DateTime.Now.Year.ToString();
            string nextYear = DateTime.Now.AddYears(1).Year.ToString();// for dates that start on new years eve etc and end on end next day or after

            DateTime startDate = DateTime.MinValue;// so if defaulted to this it won't show up in list
            DateTime endDate = DateTime.MinValue; //startDate.AddDays(100);

            string date = CleanDateString(dateTime);
            date = ReplaceAbbreviatedMonths(date);

            int monthCount = GetMonthCount(date);

            if (monthCount > 1)
                return ParseSplitDates(date);

            string month = DateEx.GetMonth(date);
            string day = "";
            string year = "";

            // //Fri-Tue, Jan 24-31

            #region Try an determine the end token for the date line. It should be year, am or pm.
            string endToken = "";

            if (date.EndsWith(currentYear))
            {
                endToken = currentYear;
                year = currentYear;
            }
            else if (date.EndsWith(nextYear))
            {
                endToken = nextYear;
                year = nextYear;
            }
            else if (date.EndsWith("am"))
                endToken = "am";
            else if (date.EndsWith("pm"))
                endToken = "pm";
            else
                return new Tuple<DateTime, DateTime>(startDate, endDate);

            #endregion // get end token

            // Get year
            if (string.IsNullOrWhiteSpace(year))
            {
                if (date.Contains(nextYear))
                    year = nextYear;
                else
                    year = currentYear;
            }

            // Get Day
            int dayNum = -1;
            string[] dateSplit = date.Split(' ');
            day = dateSplit[1];// should be 
            if (int.TryParse(day, out dayNum) == false)
                day = dateSplit[2];

            // if day is 02 try parse int to get it to number
            if (day.StartsWith("0"))
            {
                int test = 0;
                if (int.TryParse(day, out test))
                    day = test.ToString();
            }

            if (endToken == "pm")
            { //we'll have to get 
                int startIndex = date.IndexOf(month);
                date = date.Substring(startIndex);
            }
            else
                date = date.Substring(month, endToken, true);

            string input = "";
            try
            {
                if (DateTime.TryParse(date, out startDate) == false)
                {  // it might be missing the year
                    if (DateTime.TryParse($"{month} {day}, {year}", out startDate) == false)
                        return new Tuple<DateTime, DateTime>(startDate, endDate);
                }

                if (endToken == "am")
                    endDate = startDate.AddDays(1);
                else// if(endToken == "pm")
                    endDate = startDate;
                //if it's just a start date we'll make it the end date also because the query loks at the end date when populating the list
            }
            catch
            {
                Debug.Assert(false, "failed to parse date.");
            }
            return new Tuple<DateTime, DateTime>(startDate, endDate);
        }

        public static Tuple<TimeSpan, TimeSpan> ParseForStartAndEndTimes(string dateTime)//, string monthFound)
        {
            string date = CleanDateString(dateTime);
            TimeSpan startTime = new TimeSpan();
            TimeSpan endTime = new TimeSpan();

            string timeSplitToken = "to";

            if (date.Contains("to") == false)
                timeSplitToken = "-";
            //todo add space between -
            string[] dateSplices = date.Split(' ');

            if(dateSplices.Length < 4 )
                return new Tuple<TimeSpan, TimeSpan>(startTime, endTime);

            int startIndex = dateSplices.Length - 5;
            if(startIndex < 0 )
                return new Tuple<TimeSpan, TimeSpan>(startTime, endTime);

            //"Sunday Feb 02 3:30 pm - 6 pm
            string startTimeTemp = dateSplices[startIndex];
            string startAmPmTemp = dateSplices[startIndex + 1];
        
            int startHour = -1;
            int startMinutes = 0;
           
            if (startTimeTemp.Contains(":"))
            {
                string[] timeTokens = startTimeTemp.Split(':');
                int.TryParse(timeTokens[0], out startHour);
                int.TryParse(timeTokens[1], out startMinutes);
            }
            else
            {
                int.TryParse(startTimeTemp, out startHour);
            }
            startHour = startHour > 12 ? startHour % 12 : startHour;
            if (startAmPmTemp.ToLower() == "pm")
                startHour += 12;

            startTime = new TimeSpan(startHour, startMinutes, 0);

            string endTimeTemp = dateSplices[startIndex + 3];
            string endAmPmTemp = dateSplices[startIndex + 4];
            int endHour = -1;
            int endMinutes = 0;
            
            if (endTimeTemp.Contains(":"))
            {
                string[] timeTokens = endTimeTemp.Split(':');
                int.TryParse(timeTokens[0], out endHour);
                int.TryParse(timeTokens[1], out endMinutes);
            }
            else
            {
                int.TryParse(endTimeTemp, out endHour);
            }
            endHour = endHour > 12 ? endHour % 12 : endHour;
            if (endAmPmTemp.ToLower() == "pm")
                endHour += 12;

            endTime = new TimeSpan(endHour, endMinutes, 0);
         
          
            return new Tuple<TimeSpan, TimeSpan>(startTime, endTime);
        }

        public static string ReplaceAbbreviatedMonths(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            for(int i = 0; i < MonthsAbbreviated.Length; i++)   //string month in MonthsAbbreviated)
            {
                input = input.Replace(MonthsAbbreviated[i], Months[i]);
            }
            return input;
        }

        public static string GetMonth(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;
            input = input.ToLower();
            input = ReplaceAbbreviatedMonths(input);
            foreach (string month in Months)
            {
                if (input.Contains(month))
                    return month;
            }

            return string.Empty;
        }

        public static int GetMonthCount(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return 0;
            input = input.ToLower();
            input = ReplaceAbbreviatedMonths(input);
            int count = 0;
            foreach (string month in Months)
            {
                if (input.Contains(month))
                    count++;
            }

            return count;
        }
    }
}


/// <summary>
/// depricated
/// parses date time string from events parsing. If isEndDate is true
/// it'll add a day since the format is 3:00pm to 3:00am
/// </summary>
/// <param name="dateTime"></param>
/// <param name="isEndDate"></param>
/// <returns></returns>

//public static DateTime ParseDate(string dateTime, string month,  bool isEndDate)
//{
//    DateTime res = DateTime.Now.AddYears(100);// so if defaulted to this it won't show up in list
//    var tmpDateTime = dateTime
//              .Replace("&nbsp;", "")
//              .Replace("&quot;", "")
//              .Replace("st,", ",")
//               .Replace("nd,", ",")
//                 .Replace("th,", ",")
//               .Replace("\\t,", " ")
//              .Replace(Environment.NewLine, "").ToLower();

//    tmpDateTime = tmpDateTime.Substring(month, "am", true);

//    if (string.IsNullOrWhiteSpace(tmpDateTime))
//    {
//        tmpDateTime = dateTime
//              .Replace("&nbsp;", "").Replace("&quot;", "")
//              .Replace("st,", ",").Replace("nd,", ",")
//                 .Replace("th,", ",").Replace("\\t,", " ")
//              .Replace(Environment.NewLine, "").ToLower();
//        if (!tmpDateTime.EndsWith("pm"))
//            return DateTime.MinValue;
//       // tmpDateTime = tmpDateTime.Substring(month, "pm", true); //this din't  work because this has two pm's
//                                                                // we need to pull the last one.


//    }
//    string currentYear = DateTime.Now.Year.ToString();

//    string endToken = "";
//    if (tmpDateTime.EndsWith("pm"))
//        endToken = "pm";
//    else
//        endToken = currentYear;

//    // Saturday, February 1st, 2020
//    // Sunday, Feb 02 3pm - 9pm
//    string date = tmpDateTime.Substring(month, endToken, true);
//    if (string.IsNullOrWhiteSpace(date))
//    {
//        //if nothing above check next year in case we're in december
//        string nextYear = DateTime.Now.AddYears(1).Year.ToString();
//        date = tmpDateTime.Substring(month, nextYear, true);
//    }
//    int hour = -1;
//    int minutes = -1;
//    string AMPM = "";
//    string input = "";
//    try
//    {
//        res = DateTime.Parse(date);
//        string tmpTime = tmpDateTime.Replace(date, "").Trim();

//        string token = "to";

//        if (tmpTime.Contains("to") == false)
//            token = "-";

//        string[] times = tmpTime.Split(token);
//        if (times.Length == 2)
//        {
//            string startTime = times[0].Trim();


//            DateTime result;
//            input = startTime.Replace("pm", " pm").Replace("am", " am");
//            input = input.Replace("  ", " ");
//            if (!isEndDate)
//            {
//                //use algorithm
//                if (DateTime.TryParseExact(input, "h:mm tt",
//                    CultureInfo.CurrentCulture,
//                    DateTimeStyles.None, out result))
//                {
//                    //end result
//                    hour = result.Hour > 12 ? result.Hour % 12 : result.Hour;
//                    minutes = result.Minute;
//                    AMPM = result.ToString("tt");
//                    if (AMPM.ToLower() == "pm")
//                        hour += 12;

//                    TimeSpan ts = new TimeSpan(hour, minutes, 0);
//                    res = res + ts;
//                }
//            }

//            if (isEndDate)
//            {
//                string endTime = times[1].Trim();
//                input = endTime.Replace("pm", " pm").Replace("am", " am");
//                input = input.Replace("  ", " ");
//                AMPM = "";
//                //use algorithm
//                if (DateTime.TryParseExact(input, "h:mm tt",
//                    CultureInfo.CurrentCulture,
//                    DateTimeStyles.None, out result))
//                {
//                    //end result
//                     hour = result.Hour > 12 ? result.Hour % 12 : result.Hour;
//                    minutes = result.Minute;
//                    AMPM = result.ToString("tt");
//                    if (AMPM.ToLower() == "pm")
//                        hour += 12;
//                }

//                //since its end date and in the am, bump to next day because format only has end time
//                if ( AMPM.ToLower() == "am")
//                    res = res.AddDays(1);

//                TimeSpan ts = new TimeSpan(hour, minutes, 0);
//                res = res + ts;
//            }

//        }
//        // < br />3:00pm to 3:00am
//    }
//    catch { }
//    return res;
//}