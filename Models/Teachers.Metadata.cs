using System;
using System.ComponentModel.DataAnnotations;

namespace StudentInformationSystem.Models
{
    public class TeachersMetadata
    {
        [Display(Name = "教师工号")]
        public string TeacherID { get; set; }
        [Display(Name = "教师姓名")]
        public string TeacherName { get; set; }
        [Display(Name = "职称")]
        public string Title { get; set; }

    }

    [MetadataType(typeof(TeachersMetadata))]
    public partial class Teachers
    {
    }
}