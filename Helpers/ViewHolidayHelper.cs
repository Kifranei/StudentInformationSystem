using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using StudentInformationSystem.Helpers;

namespace StudentInformationSystem.Helpers
{
    /// <summary>
    /// Ϊ��ͼ�ṩ���ռ�⹦�ܵ���չ����
    /// </summary>
    public static class ViewHolidayHelper
    {
        /// <summary>
        /// ��Razor��ͼ�л�ȡ�����ܴ�����
        /// </summary>
        /// <param name="htmlHelper">HTML����</param>
        /// <returns>�����ܴ������ֵ�</returns>
        public static Dictionary<int, string> GetHolidayWeekDescriptions(this HtmlHelper htmlHelper)
        {
            return HolidayHelper.GetHolidayWeekDescriptions();
        }

        /// <summary>
        /// ��Razor��ͼ�м���Ƿ�Ϊ������
        /// </summary>
        /// <param name="htmlHelper">HTML����</param>
        /// <param name="weekNumber">�ܴ�</param>
        /// <returns>�Ƿ�Ϊ������</returns>
        public static bool IsHolidayWeek(this HtmlHelper htmlHelper, int weekNumber)
        {
            return HolidayHelper.IsHolidayWeek(weekNumber);
        }

        /// <summary>
        /// ��Razor��ͼ�л�ȡ��ǰѧ�ڼ����ܴ��б�
        /// </summary>
        /// <param name="htmlHelper">HTML����</param>
        /// <returns>�����ܴ��б�</returns>
        public static List<int> GetCurrentSemesterHolidayWeeks(this HtmlHelper htmlHelper)
        {
            return HolidayHelper.GetCurrentSemesterHolidayWeeks();
        }

        /// <summary>
        /// ��ȡ�������Ƶ�������ʽ
        /// </summary>
        /// <param name="htmlHelper">HTML����</param>
        /// <param name="dayOfWeek">�������֣�1-7��</param>
        /// <returns>��������</returns>
        public static string GetDayName(this HtmlHelper htmlHelper, int dayOfWeek)
        {
            string[] days = { "", "����һ", "���ڶ�", "������", "������", "������", "������", "������" };
            return dayOfWeek >= 1 && dayOfWeek <= 7 ? days[dayOfWeek] : "δ֪����";
        }

        /// <summary>
        /// ��ȡ�������Ƶļ�̸�ʽ
        /// </summary>
        /// <param name="htmlHelper">HTML����</param>
        /// <param name="dayOfWeek">�������֣�1-7��</param>
        /// <returns>�������Ƽ�̸�ʽ</returns>
        public static string GetDayNameShort(this HtmlHelper htmlHelper, int dayOfWeek)
        {
            string[] days = { "", "һ", "��", "��", "��", "��", "��", "��" };
            return dayOfWeek >= 1 && dayOfWeek <= 7 ? days[dayOfWeek] : "δ֪";
        }
    }
}