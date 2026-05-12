using StudentInformationSystem.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace StudentInformationSystem.Helpers
{
    public class StudentExamConflictInfo
    {
        public string StudentID { get; set; }
        public string StudentName { get; set; }
        public string CourseName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public static class ExamConflictHelper
    {
        public static List<Exams> GetTeacherExamConflicts(
            StudentManagementDBEntities db,
            string teacherId,
            DateTime startTime,
            DateTime endTime,
            int? excludeExamId = null)
        {
            if (db == null || string.IsNullOrWhiteSpace(teacherId))
            {
                return new List<Exams>();
            }

            var query = db.Exams
                .Include("Courses")
                .Where(e => e.Courses.TeacherID == teacherId && e.StartTime < endTime && e.EndTime > startTime);

            if (excludeExamId.HasValue)
            {
                int examId = excludeExamId.Value;
                query = query.Where(e => e.ExamID != examId);
            }

            return query.ToList();
        }

        public static List<StudentExamConflictInfo> GetStudentExamConflictsForCourse(
            StudentManagementDBEntities db,
            int courseId,
            DateTime startTime,
            DateTime endTime,
            int? excludeExamId = null)
        {
            if (db == null || courseId <= 0)
            {
                return new List<StudentExamConflictInfo>();
            }

            var studentIds = db.StudentCourses
                .Where(sc => sc.CourseID == courseId && sc.StudentID != null)
                .Select(sc => sc.StudentID)
                .Distinct()
                .ToList();

            if (!studentIds.Any())
            {
                return new List<StudentExamConflictInfo>();
            }

            var query = db.Exams
                .Include("Courses")
                .Where(e => e.StartTime < endTime && e.EndTime > startTime && e.CourseID != courseId);

            if (excludeExamId.HasValue)
            {
                int examId = excludeExamId.Value;
                query = query.Where(e => e.ExamID != examId);
            }

            var otherExams = query.ToList();
            if (!otherExams.Any())
            {
                return new List<StudentExamConflictInfo>();
            }

            var otherCourseIds = otherExams.Select(e => e.CourseID).Distinct().ToList();
            var relatedEnrollments = db.StudentCourses
                .Include("Students")
                .Where(sc => studentIds.Contains(sc.StudentID) && otherCourseIds.Contains(sc.CourseID))
                .ToList();

            var examMap = otherExams.ToDictionary(e => e.CourseID, e => e);
            var conflicts = new List<StudentExamConflictInfo>();

            foreach (var enrollment in relatedEnrollments)
            {
                Exams conflictExam;
                if (!examMap.TryGetValue(enrollment.CourseID, out conflictExam))
                {
                    continue;
                }

                conflicts.Add(new StudentExamConflictInfo
                {
                    StudentID = enrollment.StudentID,
                    StudentName = enrollment.Students == null ? enrollment.StudentID : enrollment.Students.StudentName,
                    CourseName = conflictExam.Courses == null ? "未知课程" : conflictExam.Courses.CourseName,
                    StartTime = conflictExam.StartTime,
                    EndTime = conflictExam.EndTime
                });
            }

            return conflicts
                .GroupBy(c => new { c.StudentID, c.CourseName, c.StartTime, c.EndTime })
                .Select(g => g.First())
                .OrderBy(c => c.StudentID)
                .ThenBy(c => c.CourseName)
                .ToList();
        }

        public static List<Exams> GetLocationExamConflicts(
            StudentManagementDBEntities db,
            string location,
            DateTime startTime,
            DateTime endTime,
            int? excludeExamId = null)
        {
            if (db == null || string.IsNullOrWhiteSpace(location))
            {
                return new List<Exams>();
            }

            string normalizedLocation = location.Trim();
            var query = db.Exams
                .Include("Courses")
                .Where(e =>
                    e.Location != null &&
                    e.Location.Trim() == normalizedLocation &&
                    e.StartTime < endTime &&
                    e.EndTime > startTime);

            if (excludeExamId.HasValue)
            {
                int examId = excludeExamId.Value;
                query = query.Where(e => e.ExamID != examId);
            }

            return query.ToList();
        }

        public static string BuildTeacherExamConflictMessage(IEnumerable<Exams> conflicts, string prefix)
        {
            var conflictList = conflicts == null ? new List<Exams>() : conflicts.ToList();
            if (!conflictList.Any())
            {
                return string.Empty;
            }

            string description = string.Join("；", conflictList.Select(e =>
                $"{(e.Courses == null ? "课程" : e.Courses.CourseName)}({e.StartTime:yyyy-MM-dd HH:mm} - {e.EndTime:HH:mm})"));
            return prefix + description;
        }

        public static string BuildStudentExamConflictMessage(IEnumerable<StudentExamConflictInfo> conflicts, string prefix)
        {
            var conflictList = conflicts == null ? new List<StudentExamConflictInfo>() : conflicts.ToList();
            if (!conflictList.Any())
            {
                return string.Empty;
            }

            string description = string.Join("；", conflictList
                .Take(5)
                .Select(c => $"{(string.IsNullOrWhiteSpace(c.StudentName) ? c.StudentID : c.StudentName)} 与 {c.CourseName}({c.StartTime:yyyy-MM-dd HH:mm} - {c.EndTime:HH:mm})"));

            if (conflictList.Count > 5)
            {
                description += $"；另有 {conflictList.Count - 5} 条冲突未展开";
            }

            return prefix + description;
        }

        public static string BuildLocationExamConflictMessage(IEnumerable<Exams> conflicts, string prefix)
        {
            var conflictList = conflicts == null ? new List<Exams>() : conflicts.ToList();
            if (!conflictList.Any())
            {
                return string.Empty;
            }

            string description = string.Join("；", conflictList.Select(e =>
                $"{(e.Courses == null ? "课程" : e.Courses.CourseName)}({e.StartTime:yyyy-MM-dd HH:mm} - {e.EndTime:HH:mm})"));
            return prefix + description;
        }
    }
}
