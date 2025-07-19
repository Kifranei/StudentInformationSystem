using System.Collections.Generic;

namespace StudentInformationSystem.Models
{
    public class TeacherDashboardViewModel
    {
        public string TeacherName { get; set; }
        public List<ClassSessions> TodaysClasses { get; set; }
    }
}