using StudentInformationSystem.Models;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace StudentInformationSystem.Helpers
{
    public class StudentScheduleConflictInfo
    {
        public string StudentID { get; set; }
        public string StudentName { get; set; }
        public string CourseName { get; set; }
        public int DayOfWeek { get; set; }
        public int StartWeek { get; set; }
        public int EndWeek { get; set; }
        public int StartPeriod { get; set; }
        public int EndPeriod { get; set; }
    }

    public static class ScheduleConflictHelper
    {
        public static List<StudentScheduleConflictInfo> GetStudentConflictsForCourseSelection(
            StudentManagementDBEntities db,
            string studentId,
            int courseId)
        {
            if (db == null || string.IsNullOrWhiteSpace(studentId) || courseId <= 0)
            {
                return new List<StudentScheduleConflictInfo>();
            }

            var targetSessions = db.ClassSessions.Include("Courses")
                .Where(cs => cs.CourseID == courseId)
                .ToList();

            if (!targetSessions.Any())
            {
                return new List<StudentScheduleConflictInfo>();
            }

            var enrollmentCourseIds = db.StudentCourses
                .Where(sc => sc.StudentID == studentId && sc.CourseID != courseId)
                .Select(sc => sc.CourseID)
                .Distinct()
                .ToList();

            if (!enrollmentCourseIds.Any())
            {
                return new List<StudentScheduleConflictInfo>();
            }

            var existingSessions = db.ClassSessions.Include("Courses")
                .Where(cs => enrollmentCourseIds.Contains(cs.CourseID))
                .ToList();

            var student = db.Students.Find(studentId);
            var conflicts = new List<StudentScheduleConflictInfo>();

            foreach (var targetSession in targetSessions)
            {
                foreach (var existingSession in existingSessions)
                {
                    if (!IsTimeOverlap(targetSession.StartWeek, targetSession.EndWeek, targetSession.StartPeriod, targetSession.EndPeriod,
                        existingSession.StartWeek, existingSession.EndWeek, existingSession.StartPeriod, existingSession.EndPeriod,
                        targetSession.DayOfWeek, existingSession.DayOfWeek))
                    {
                        continue;
                    }

                    conflicts.Add(new StudentScheduleConflictInfo
                    {
                        StudentID = studentId,
                        StudentName = student == null ? studentId : student.StudentName,
                        CourseName = existingSession.Courses == null ? "未知课程" : existingSession.Courses.CourseName,
                        DayOfWeek = existingSession.DayOfWeek,
                        StartWeek = existingSession.StartWeek,
                        EndWeek = existingSession.EndWeek,
                        StartPeriod = existingSession.StartPeriod,
                        EndPeriod = existingSession.EndPeriod
                    });
                }
            }

            return DistinctConflicts(conflicts);
        }

        public static string BuildStudentConflictMessage(IEnumerable<StudentScheduleConflictInfo> conflicts, string prefix)
        {
            var conflictList = conflicts == null ? new List<StudentScheduleConflictInfo>() : conflicts.ToList();
            if (!conflictList.Any())
            {
                return string.Empty;
            }

            string description = string.Join("；", conflictList
                .Take(5)
                .Select(c => string.Format("{0} 与 {1}(周{2} 第{3}-{4}节, 第{5}-{6}周)",
                    string.IsNullOrWhiteSpace(c.StudentName) ? c.StudentID : c.StudentName,
                    c.CourseName,
                    c.DayOfWeek,
                    c.StartPeriod,
                    c.EndPeriod,
                    c.StartWeek,
                    c.EndWeek)));

            if (conflictList.Count > 5)
            {
                description += string.Format("；另有 {0} 条冲突未展开", conflictList.Count - 5);
            }

            return prefix + description;
        }

        private static bool IsTimeOverlap(
            int startWeekA,
            int endWeekA,
            int startPeriodA,
            int endPeriodA,
            int startWeekB,
            int endWeekB,
            int startPeriodB,
            int endPeriodB,
            int dayOfWeekA,
            int dayOfWeekB)
        {
            return dayOfWeekA == dayOfWeekB
                && !(endWeekA < startWeekB || startWeekA > endWeekB)
                && !(endPeriodA < startPeriodB || startPeriodA > endPeriodB);
        }

        private static List<StudentScheduleConflictInfo> DistinctConflicts(IEnumerable<StudentScheduleConflictInfo> conflicts)
        {
            return conflicts
                .GroupBy(c => new
                {
                    c.StudentID,
                    c.CourseName,
                    c.DayOfWeek,
                    c.StartWeek,
                    c.EndWeek,
                    c.StartPeriod,
                    c.EndPeriod
                })
                .Select(g => g.First())
                .OrderBy(c => c.StudentID)
                .ThenBy(c => c.DayOfWeek)
                .ThenBy(c => c.StartPeriod)
                .ToList();
        }
    }
}
