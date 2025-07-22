using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace StudentInformationSystem.Helpers
{
    /// <summary>
    /// �й��������ռ�⸨����
    /// </summary>
    public static class HolidayHelper
    {
        /// <summary>
        /// ��ȡָ����ݵ����з��������ܴ�
        /// ����ѧ�ڿ�ʼʱ����㣨һ��Ϊ2������Ѯ��9�³���
        /// </summary>
        /// <param name="year">���</param>
        /// <param name="semesterStartDate">ѧ�ڿ�ʼ����</param>
        /// <returns>�����ܴ��б�</returns>
        public static List<int> GetHolidayWeeks(int year, DateTime semesterStartDate)
        {
            var holidayWeeks = new List<int>();
            var holidays = GetChineseHolidays(year);

            foreach (var holiday in holidays)
            {
                // ֻ����ѧ���ڼ�ļ���
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
        /// ��ȡ��ǰѧ�ڵļ����ܴ�
        /// </summary>
        /// <returns>�����ܴ��б�</returns>
        public static List<int> GetCurrentSemesterHolidayWeeks()
        {
            var now = DateTime.Now;
            var currentYear = now.Year;
            
            // �ж��Ǵ���ѧ�ڻ����＾ѧ��
            DateTime semesterStart;
            if (now.Month >= 2 && now.Month <= 7)
            {
                // ����ѧ�ڣ�2������Ѯ��ʼ
                semesterStart = new DateTime(currentYear, 2, 20);
            }
            else
            {
                // �＾ѧ�ڣ�9�³���ʼ
                semesterStart = new DateTime(currentYear, 9, 1);
            }

            return GetHolidayWeeks(currentYear, semesterStart);
        }

        /// <summary>
        /// ���ָ���ܴ��Ƿ�Ϊ������
        /// </summary>
        /// <param name="weekNumber">�ܴ�</param>
        /// <returns>�Ƿ�Ϊ������</returns>
        public static bool IsHolidayWeek(int weekNumber)
        {
            var holidayWeeks = GetCurrentSemesterHolidayWeeks();
            return holidayWeeks.Contains(weekNumber);
        }

        /// <summary>
        /// ��ȡָ����ݵ��й���������
        /// </summary>
        /// <param name="year">���</param>
        /// <returns>���������б�</returns>
        private static List<DateTime> GetChineseHolidays(int year)
        {
            var holidays = new List<DateTime>();

            try
            {
                // Ԫ����1��1��
                holidays.Add(new DateTime(year, 1, 1));

                // ���ڣ�ũ�����³�һ��ͨ����1-2�£�
                var springFestival = GetChineseNewYear(year);
                if (springFestival != null)
                {
                    holidays.AddRange(GetSpringFestivalWeek(springFestival.Value));
                }

                // �����ڣ�4��4�ջ�5�գ�����ѧ����Ҫ���գ�
                var qingming = GetQingmingFestival(year);
                holidays.Add(qingming);

                // �Ͷ��ڣ�5��1��
                holidays.Add(new DateTime(year, 5, 1));

                // ����ڣ�ũ�����³��壨����ѧ����Ҫ���գ�
                var dragonBoat = GetDragonBoatFestival(year);
                if (dragonBoat != null)
                {
                    holidays.Add(dragonBoat.Value);
                }

                // ����ڣ�ũ������ʮ�壨�＾ѧ����Ҫ���գ�
                var midAutumn = GetMidAutumnFestival(year);
                if (midAutumn != null)
                {
                    holidays.Add(midAutumn.Value);
                }

                // ����ڣ�10��1�գ��＾ѧ����Ҫ���գ�
                var nationalDay = new DateTime(year, 10, 1);
                holidays.AddRange(GetNationalDayWeek(nationalDay));
            }
            catch (Exception)
            {
                // �������ʧ�ܣ����ػ����Ĺ̶�����
                holidays.Clear();
                holidays.Add(new DateTime(year, 4, 5)); // ����
                holidays.Add(new DateTime(year, 5, 1)); // �Ͷ���
                holidays.Add(new DateTime(year, 10, 1)); // ����
            }

            return holidays;
        }

        /// <summary>
        /// ����ũ���������ڣ����㷨��
        /// </summary>
        private static DateTime? GetChineseNewYear(int year)
        {
            // �򻯵�ũ��������㣨ʵ��Ӧ���н���ʹ��רҵ��ũ������⣩
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
        /// ��ȡ����������
        /// </summary>
        private static DateTime GetQingmingFestival(int year)
        {
            // ������һ����4��4��-6��֮��
            return new DateTime(year, 4, 5);
        }

        /// <summary>
        /// �����������ڣ����㷨��
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
        /// ������������ڣ����㷨��
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
        /// ��ȡ���ڼ����ܣ�ͨ��һ�ܣ�
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
        /// ��ȡ��������ܣ�ͨ��һ�ܣ�
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
        /// �������������ѧ�ڿ�ʼ������
        /// </summary>
        private static int GetWeekNumber(DateTime semesterStart, DateTime targetDate)
        {
            var daysDiff = (targetDate - semesterStart).Days;
            return (daysDiff / 7) + 1;
        }

        /// <summary>
        /// ��ȡ�����ܴε�������Ϣ
        /// </summary>
        /// <returns>�����ܴμ�������</returns>
        public static Dictionary<int, string> GetHolidayWeekDescriptions()
        {
            var descriptions = new Dictionary<int, string>();
            var now = DateTime.Now;
            var currentYear = now.Year;
            
            // �ж��Ǵ���ѧ�ڻ����＾ѧ��
            DateTime semesterStart;
            string semesterType;
            if (now.Month >= 2 && now.Month <= 7)
            {
                semesterStart = new DateTime(currentYear, 2, 20);
                semesterType = "����ѧ��";
            }
            else
            {
                semesterStart = new DateTime(currentYear, 9, 1);
                semesterType = "�＾ѧ��";
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
                        descriptions[weekNumber] = $"{holidayName}��{holiday:MM��dd��}��";
                    }
                }
            }

            return descriptions;
        }

        /// <summary>
        /// �������ڻ�ȡ��������
        /// </summary>
        private static string GetHolidayName(DateTime date)
        {
            if (date.Month == 1 && date.Day == 1) return "Ԫ��";
            if (date.Month == 4 && date.Day >= 4 && date.Day <= 6) return "������";
            if (date.Month == 5 && date.Day == 1) return "�Ͷ���";
            if (date.Month == 10 && date.Day == 1) return "�����";
            
            // ũ��������Ҫ�����ӵ��ж�
            var year = date.Year;
            var springFestival = GetChineseNewYear(year);
            if (springFestival.HasValue && Math.Abs((date - springFestival.Value).Days) <= 3)
                return "����";
                
            var dragonBoat = GetDragonBoatFestival(year);
            if (dragonBoat.HasValue && Math.Abs((date - dragonBoat.Value).Days) <= 1)
                return "�����";
                
            var midAutumn = GetMidAutumnFestival(year);
            if (midAutumn.HasValue && Math.Abs((date - midAutumn.Value).Days) <= 1)
                return "�����";

            return "��������";
        }
    }
}