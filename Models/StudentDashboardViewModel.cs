using System.Collections.Generic;

namespace StudentInformationSystem.Models
{
    public class StudentDashboardViewModel
    {
        public string StudentName { get; set; }

        // 用于存放今天的课程安排
        public List<ClassSessions> TodaysClasses { get; set; }

        // 用于存放所有已出成绩的课程
        public List<StudentCourses> GradedCourses { get; set; }
    }
}