using System;
using System.ComponentModel.DataAnnotations;

namespace StudentInformationSystem.Models
{
    public class ClassesMetadata
    {
        [Display(Name = "班级名称")]
        public string ClassName { get; set; }
        [Display(Name = "专业")]
        public string Major { get; set; }
        [Display(Name = "学年")]
        public Nullable<int> AcademicYear { get; set; }
        [Display(Name = "班号")]
        public Nullable<int> ClassNumber { get; set; }

    }

    [MetadataType(typeof(ClassesMetadata))]
    public partial class Classes
    {
    }
}