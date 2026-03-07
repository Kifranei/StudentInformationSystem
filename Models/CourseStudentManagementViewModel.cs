using System.Collections.Generic;
using System.Web.Mvc;

namespace StudentInformationSystem.Models
{
    public class CourseStudentManagementViewModel
    {
        public int? SelectedCourseId { get; set; }
        public Courses SelectedCourse { get; set; }
        public List<SelectListItem> CourseOptions { get; set; }
        public List<StudentCourses> EnrolledStudents { get; set; }
        public List<SelectListItem> AvailableStudents { get; set; }

        public CourseStudentManagementViewModel()
        {
            CourseOptions = new List<SelectListItem>();
            EnrolledStudents = new List<StudentCourses>();
            AvailableStudents = new List<SelectListItem>();
        }
    }
}
