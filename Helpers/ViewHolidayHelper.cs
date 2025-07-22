using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using StudentInformationSystem.Helpers;

namespace StudentInformationSystem.Helpers
{
    /// <summary>
    /// 为视图提供假日检测功能的扩展方法
    /// </summary>
    public static class ViewHolidayHelper
    {
        /// <summary>
        /// 在Razor视图中获取假日周次描述
        /// </summary>
        /// <param name="htmlHelper">HTML助手</param>
        /// <returns>假日周次描述字典</returns>
        public static Dictionary<int, string> GetHolidayWeekDescriptions(this HtmlHelper htmlHelper)
        {
            return HolidayHelper.GetHolidayWeekDescriptions();
        }

        /// <summary>
        /// 在Razor视图中检查是否为假日周
        /// </summary>
        /// <param name="htmlHelper">HTML助手</param>
        /// <param name="weekNumber">周次</param>
        /// <returns>是否为假日周</returns>
        public static bool IsHolidayWeek(this HtmlHelper htmlHelper, int weekNumber)
        {
            return HolidayHelper.IsHolidayWeek(weekNumber);
        }

        /// <summary>
        /// 在Razor视图中获取当前学期假日周次列表
        /// </summary>
        /// <param name="htmlHelper">HTML助手</param>
        /// <returns>假日周次列表</returns>
        public static List<int> GetCurrentSemesterHolidayWeeks(this HtmlHelper htmlHelper)
        {
            return HolidayHelper.GetCurrentSemesterHolidayWeeks();
        }

        /// <summary>
        /// 获取星期名称的完整格式
        /// </summary>
        /// <param name="htmlHelper">HTML助手</param>
        /// <param name="dayOfWeek">星期数字（1-7）</param>
        /// <returns>星期名称</returns>
        public static string GetDayName(this HtmlHelper htmlHelper, int dayOfWeek)
        {
            string[] days = { "", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六", "星期日" };
            return dayOfWeek >= 1 && dayOfWeek <= 7 ? days[dayOfWeek] : "未知星期";
        }

        /// <summary>
        /// 获取星期名称的简短格式
        /// </summary>
        /// <param name="htmlHelper">HTML助手</param>
        /// <param name="dayOfWeek">星期数字（1-7）</param>
        /// <returns>星期名称简短格式</returns>
        public static string GetDayNameShort(this HtmlHelper htmlHelper, int dayOfWeek)
        {
            string[] days = { "", "一", "二", "三", "四", "五", "六", "日" };
            return dayOfWeek >= 1 && dayOfWeek <= 7 ? days[dayOfWeek] : "未知";
        }
    }
}