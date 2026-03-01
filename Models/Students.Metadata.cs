using System.ComponentModel.DataAnnotations;

namespace StudentInformationSystem.Models
{
    // 1. 定义一个私有的元数据类
    public class StudentsMetadata
    {
        // 属性名必须和 Students.cs 中一致
        [Display(Name = "学号")]
        public string StudentID { get; set; }
        [Display(Name = "学生姓名")]
        public string StudentName { get; set; }
        [Display(Name = "性别")]
        public string Gender { get; set; }

    }

    // 2. 创建一个同名的“分布类 (Partial Class)”
    // 并用 MetadataType 特性把它和上面的元数据类“绑定”
    [MetadataType(typeof(StudentsMetadata))]
    public partial class Students
    {
    }
}