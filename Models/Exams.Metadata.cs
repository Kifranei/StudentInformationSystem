using System;
using System.ComponentModel.DataAnnotations;

namespace StudentInformationSystem.Models
{
    public class ExamsMetadata
    {
        [Display(Name = "考试时间")]
        public System.DateTime ExamTime { get; set; }
        [Display(Name = "考试地点")]
        public string Location { get; set; }
        [Display(Name = "备注")]
        public string Details { get; set; }

    }

    [MetadataType(typeof(ExamsMetadata))]
    public partial class Exams
    {
    }
}