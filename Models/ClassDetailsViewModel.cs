using System.Collections.Generic;

namespace StudentInformationSystem.Models
{
    public class ClassDetailsViewModel
    {
        // 用来存放班级的详细信息 (专业, 学年等)
        public Classes ClassInfo { get; set; }

        // 用来存放这个班级下的所有学生列表
        public List<Students> StudentsInClass { get; set; }
    }
}