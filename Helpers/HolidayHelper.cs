using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace StudentInformationSystem.Helpers
{
    /// <summary>
    /// 中国法定假日检测辅助类
    /// </summary>
    public static class HolidayHelper
    {
        /// <summary>
        /// 获取指定年份的所有法定假日周次
        /// 基于学期开始时间计算（一般为2月中下旬或9月初）
        /// </summary>
        /// <param name="year">年份</param>
        /// <param name="semesterStartDate">学期开始日期</param>
        /// <returns>假日周次列表</returns>
        public static List<int> GetHolidayWeeks(int year, DateTime semesterStartDate)
        {
            var holidayWeeks = new List<int>();
            var holidays = GetChineseHolidays(year);

            foreach (var holiday in holidays)
            {
                // 只考虑学期期间的假日
                if (holiday >= semesterStartDate && holiday <= semesterStartDate.AddDays(21 * 7))
                {
                    int weekNumber = GetWeekNumber(semesterStartDate, holiday);
                    if (weekNumber >= 1 && weekNumber <= 21)
                    {
                        holidayWeeks.Add(weekNumber);
                    }
                }
            }

            return holidayWeeks.Distinct().OrderBy(w => w).ToList();
        }

        /// <summary>
        /// 获取当前学期的假日周次
        /// </summary>
        /// <returns>假日周次列表</returns>
        public static List<int> GetCurrentSemesterHolidayWeeks()
        {
            var now = DateTime.Now;
            var currentYear = now.Year;
            
            // 判断是春季学期还是秋季学期
            DateTime semesterStart;
            if (now.Month >= 2 && now.Month <= 7)
            {
                // 春季学期：2月中下旬开始
                semesterStart = new DateTime(currentYear, 2, 20);
            }
            else
            {
                // 秋季学期：9月初开始
                semesterStart = new DateTime(currentYear, 9, 1);
            }

            return GetHolidayWeeks(currentYear, semesterStart);
        }

        /// <summary>
        /// 检查指定周次是否为假日周
        /// </summary>
        /// <param name="weekNumber">周次</param>
        /// <returns>是否为假日周</returns>
        public static bool IsHolidayWeek(int weekNumber)
        {
            var holidayWeeks = GetCurrentSemesterHolidayWeeks();
            return holidayWeeks.Contains(weekNumber);
        }

        /// <summary>
        /// 获取指定年份的中国法定假日
        /// </summary>
        /// <param name="year">年份</param>
        /// <returns>假日日期列表</returns>
        private static List<DateTime> GetChineseHolidays(int year)
        {
            var holidays = new List<DateTime>();

            try
            {
                // 元旦：1月1日
                holidays.Add(new DateTime(year, 1, 1));

                // 春节：农历正月初一（通常在1-2月）
                var springFestival = GetChineseNewYear(year);
                if (springFestival != null)
                {
                    holidays.AddRange(GetSpringFestivalWeek(springFestival.Value));
                }

                // 清明节：4月4日或5日（春季学期重要假日）
                var qingming = GetQingmingFestival(year);
                holidays.Add(qingming);

                // 劳动节：5月1日
                holidays.Add(new DateTime(year, 5, 1));

                // 端午节：农历五月初五（春季学期重要假日）
                var dragonBoat = GetDragonBoatFestival(year);
                if (dragonBoat != null)
                {
                    holidays.Add(dragonBoat.Value);
                }

                // 中秋节：农历八月十五（秋季学期重要假日）
                var midAutumn = GetMidAutumnFestival(year);
                if (midAutumn != null)
                {
                    holidays.Add(midAutumn.Value);
                }

                // 国庆节：10月1日（秋季学期重要假日）
                var nationalDay = new DateTime(year, 10, 1);
                holidays.AddRange(GetNationalDayWeek(nationalDay));
            }
            catch (Exception)
            {
                // 如果计算失败，返回基本的固定假日
                holidays.Clear();
                holidays.Add(new DateTime(year, 4, 5)); // 清明
                holidays.Add(new DateTime(year, 5, 1)); // 劳动节
                holidays.Add(new DateTime(year, 10, 1)); // 国庆
            }

            return holidays;
        }

        /// <summary>
        /// 计算农历新年日期（简化算法）
        /// </summary>
        private static DateTime? GetChineseNewYear(int year)
        {
            // 简化的农历新年计算（实际应用中建议使用专业的农历计算库）
            var baseYears = new Dictionary<int, DateTime>
            {
                { 2024, new DateTime(2024, 2, 10) },
                { 2025, new DateTime(2025, 1, 29) },
                { 2026, new DateTime(2026, 2, 17) },
                { 2027, new DateTime(2027, 2, 6) },
                { 2028, new DateTime(2028, 1, 26) }
            };

            return baseYears.ContainsKey(year) ? baseYears[year] : (DateTime?)null;
        }

        /// <summary>
        /// 获取清明节日期
        /// </summary>
        private static DateTime GetQingmingFestival(int year)
        {
            // 清明节一般在4月4日-6日之间
            return new DateTime(year, 4, 5);
        }

        /// <summary>
        /// 计算端午节日期（简化算法）
        /// </summary>
        private static DateTime? GetDragonBoatFestival(int year)
        {
            var baseYears = new Dictionary<int, DateTime>
            {
                { 2024, new DateTime(2024, 6, 10) },
                { 2025, new DateTime(2025, 5, 31) },
                { 2026, new DateTime(2026, 6, 19) },
                { 2027, new DateTime(2027, 6, 9) },
                { 2028, new DateTime(2028, 5, 28) }
            };

            return baseYears.ContainsKey(year) ? baseYears[year] : (DateTime?)null;
        }

        /// <summary>
        /// 计算中秋节日期（简化算法）
        /// </summary>
        private static DateTime? GetMidAutumnFestival(int year)
        {
            var baseYears = new Dictionary<int, DateTime>
            {
                { 2024, new DateTime(2024, 9, 17) },
                { 2025, new DateTime(2025, 10, 6) },
                { 2026, new DateTime(2026, 9, 25) },
                { 2027, new DateTime(2027, 9, 15) },
                { 2028, new DateTime(2028, 10, 3) }
            };

            return baseYears.ContainsKey(year) ? baseYears[year] : (DateTime?)null;
        }

        /// <summary>
        /// 获取春节假期周（通常一周）
        /// </summary>
        private static List<DateTime> GetSpringFestivalWeek(DateTime springFestival)
        {
            var dates = new List<DateTime>();
            for (int i = -2; i <= 4; i++)
            {
                dates.Add(springFestival.AddDays(i));
            }
            return dates;
        }

        /// <summary>
        /// 获取国庆假期周（通常一周）
        /// </summary>
        private static List<DateTime> GetNationalDayWeek(DateTime nationalDay)
        {
            var dates = new List<DateTime>();
            for (int i = 0; i <= 6; i++)
            {
                dates.Add(nationalDay.AddDays(i));
            }
            return dates;
        }

        /// <summary>
        /// 计算日期相对于学期开始的周数
        /// </summary>
        private static int GetWeekNumber(DateTime semesterStart, DateTime targetDate)
        {
            var daysDiff = (targetDate - semesterStart).Days;
            return (daysDiff / 7) + 1;
        }

        /// <summary>
        /// 获取假日周次的描述信息
        /// </summary>
        /// <returns>假日周次及其描述</returns>
        public static Dictionary<int, string> GetHolidayWeekDescriptions()
        {
            var descriptions = new Dictionary<int, string>();
            var now = DateTime.Now;
            var currentYear = now.Year;
            
            // 判断是春季学期还是秋季学期
            DateTime semesterStart;
            string semesterType;
            if (now.Month >= 2 && now.Month <= 7)
            {
                semesterStart = new DateTime(currentYear, 2, 20);
                semesterType = "春季学期";
            }
            else
            {
                semesterStart = new DateTime(currentYear, 9, 1);
                semesterType = "秋季学期";
            }

            var holidays = GetChineseHolidays(currentYear);
            
            foreach (var holiday in holidays)
            {
                if (holiday >= semesterStart && holiday <= semesterStart.AddDays(21 * 7))
                {
                    int weekNumber = GetWeekNumber(semesterStart, holiday);
                    if (weekNumber >= 1 && weekNumber <= 21)
                    {
                        string holidayName = GetHolidayName(holiday);
                        descriptions[weekNumber] = $"{holidayName}（{holiday:MM月dd日}）";
                    }
                }
            }

            return descriptions;
        }

        /// <summary>
        /// 根据日期获取假日名称
        /// </summary>
        private static string GetHolidayName(DateTime date)
        {
            if (date.Month == 1 && date.Day == 1) return "元旦";
            if (date.Month == 4 && date.Day >= 4 && date.Day <= 6) return "清明节";
            if (date.Month == 5 && date.Day == 1) return "劳动节";
            if (date.Month == 10 && date.Day == 1) return "国庆节";
            
            // 农历节日需要更复杂的判断
            var year = date.Year;
            var springFestival = GetChineseNewYear(year);
            if (springFestival.HasValue && Math.Abs((date - springFestival.Value).Days) <= 3)
                return "春节";
                
            var dragonBoat = GetDragonBoatFestival(year);
            if (dragonBoat.HasValue && Math.Abs((date - dragonBoat.Value).Days) <= 1)
                return "端午节";
                
            var midAutumn = GetMidAutumnFestival(year);
            if (midAutumn.HasValue && Math.Abs((date - midAutumn.Value).Days) <= 1)
                return "中秋节";

            return "法定假日";
        }
    }
}