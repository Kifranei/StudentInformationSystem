using System.Collections.Generic;

namespace StudentInformationSystem.Models
{
    public class CourseSelectionViewModel
    {
        // 按类别分好的可选课程列表
        public List<Courses> MajorElectives { get; set; }
        public List<Courses> PublicElectives { get; set; }
        public List<Courses> SportsElectives { get; set; }
        public List<Courses> PoliticsElectives { get; set; }
        public List<Courses> OtherElectives { get; set; }

        // 需要重修的课程列表
        public List<Courses> RetakeCourses { get; set; }

        // --- 关键：确保这个属性存在 ---
        // 用于存放所有已选课程的详细信息
        public List<StudentCourses> EnrolledCourses { get; set; }

        // 学生已经选了的所有课程的ID，用于判断按钮状态
        public List<int> EnrolledCourseIDs { get; set; }

        // 选课规则满足情况
        public int SportsCoursesTaken { get; set; }
        public int PoliticsCoursesTaken { get; set; }

        public CourseSelectionViewModel()
        {
            MajorElectives = new List<Courses>();
            PublicElectives = new List<Courses>();
            SportsElectives = new List<Courses>();
            PoliticsElectives = new List<Courses>();
            OtherElectives = new List<Courses>();
            RetakeCourses = new List<Courses>();
            EnrolledCourses = new List<StudentCourses>(); // <-- 在构造函数中初始化
            EnrolledCourseIDs = new List<int>();
        }
    }
}