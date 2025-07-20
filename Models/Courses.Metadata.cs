using System;
using System.ComponentModel.DataAnnotations;

namespace StudentInformationSystem.Models
{
    public class CoursesMetadata
    {
        [Display(Name = "课程名称")]
        public string CourseName { get; set; }
        [Display(Name = "学分")]
        public double Credits { get; set; }
        [Display(Name = "教师名称")]
        public string TeacherID { get; set; }
        [Display(Name = "课程类别")]
        public int CourseType { get; set; }

    }

    [MetadataType(typeof(CoursesMetadata))]
    public partial class Courses
    {
    }
}