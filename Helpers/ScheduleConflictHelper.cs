using StudentInformationSystem.Models;
using System;
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
        public static List<ClassSessions> GetTeacherSessionConflicts(
            StudentManagementDBEntities db,
            string teacherId,
            int dayOfWeek,
            int startWeek,
            int endWeek,
            int startPeriod,
            int endPeriod,
            int? excludeSessionId = null)
        {
            if (db == null || string.IsNullOrWhiteSpace(teacherId))
            {
                return new List<ClassSessions>();
            }

            var query = db.ClassSessions.Include("Courses")
                .Where(cs => cs.Courses.TeacherID == teacherId
                    && cs.DayOfWeek == dayOfWeek
                    && !(endWeek < cs.StartWeek || startWeek > cs.EndWeek)
                    && !(endPeriod < cs.StartPeriod || startPeriod > cs.EndPeriod));

            if (excludeSessionId.HasValue)
            {
                int sessionId = excludeSessionId.Value;
                query = query.Where(cs => cs.SessionID != sessionId);
            }

            return query.ToList();
        }

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

        public static List<StudentScheduleConflictInfo> GetStudentConflictsForCourseAssignment(
            StudentManagementDBEntities db,
            string studentId,
            int courseId)
        {
            return GetStudentConflictsForCourseSelection(db, studentId, courseId);
        }

        public static List<StudentScheduleConflictInfo> GetConflictsForEnrolledStudentsWhenScheduling(
            StudentManagementDBEntities db,
            int courseId,
            int dayOfWeek,
            int startWeek,
            int endWeek,
            int startPeriod,
            int endPeriod,
            int? excludeSessionId = null)
        {
            if (db == null || courseId <= 0)
            {
                return new List<StudentScheduleConflictInfo>();
            }

            var enrolledStudents = db.StudentCourses.Include("Students")
                .Where(sc => sc.CourseID == courseId && sc.StudentID != null)
                .Select(sc => new
                {
                    sc.StudentID,
                    StudentName = sc.Students == null ? sc.StudentID : sc.Students.StudentName
                })
                .ToList();

            var studentIds = enrolledStudents.Select(s => s.StudentID).Distinct().ToList();
            if (!studentIds.Any())
            {
                return new List<StudentScheduleConflictInfo>();
            }

            var relatedEnrollments = db.StudentCourses
                .Where(sc => studentIds.Contains(sc.StudentID) && sc.CourseID != courseId)
                .Select(sc => new { sc.StudentID, sc.CourseID })
                .ToList();

            var otherCourseIds = relatedEnrollments.Select(e => e.CourseID).Distinct().ToList();
            if (!otherCourseIds.Any())
            {
                return new List<StudentScheduleConflictInfo>();
            }

            var sessionQuery = db.ClassSessions.Include("Courses")
                .Where(cs => otherCourseIds.Contains(cs.CourseID)
                    && cs.DayOfWeek == dayOfWeek
                    && !(endWeek < cs.StartWeek || startWeek > cs.EndWeek)
                    && !(endPeriod < cs.StartPeriod || startPeriod > cs.EndPeriod));

            if (excludeSessionId.HasValue)
            {
                int sessionId = excludeSessionId.Value;
                sessionQuery = sessionQuery.Where(cs => cs.SessionID != sessionId);
            }

            var otherSessions = sessionQuery.ToList();
            if (!otherSessions.Any())
            {
                return new List<StudentScheduleConflictInfo>();
            }

            var conflicts = new List<StudentScheduleConflictInfo>();
            foreach (var student in enrolledStudents)
            {
                var conflictCourseIds = relatedEnrollments
                    .Where(e => e.StudentID == student.StudentID)
                    .Select(e => e.CourseID)
                    .Distinct()
                    .ToList();

                foreach (var session in otherSessions.Where(s => conflictCourseIds.Contains(s.CourseID)))
                {
                    conflicts.Add(new StudentScheduleConflictInfo
                    {
                        StudentID = student.StudentID,
                        StudentName = student.StudentName,
                        CourseName = session.Courses == null ? "未知课程" : session.Courses.CourseName,
                        DayOfWeek = session.DayOfWeek,
                        StartWeek = session.StartWeek,
                        EndWeek = session.EndWeek,
                        StartPeriod = session.StartPeriod,
                        EndPeriod = session.EndPeriod
                    });
                }
            }

            return DistinctConflicts(conflicts);
        }

        public static string BuildTeacherConflictMessage(IEnumerable<ClassSessions> conflicts, string prefix)
        {
            var conflictList = conflicts == null ? new List<ClassSessions>() : conflicts.ToList();
            if (!conflictList.Any())
            {
                return string.Empty;
            }

            string description = string.Join("；", conflictList.Select(cs =>
                $"{(cs.Courses == null ? "课程" : cs.Courses.CourseName)}(第{cs.StartWeek}-{cs.EndWeek}周, 第{cs.StartPeriod}-{cs.EndPeriod}节)"));
            return prefix + description;
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
                .Select(c => $"{(string.IsNullOrWhiteSpace(c.StudentName) ? c.StudentID : c.StudentName)} 与 {c.CourseName}(周{c.DayOfWeek} 第{c.StartPeriod}-{c.EndPeriod}节, 第{c.StartWeek}-{c.EndWeek}周)"));

            if (conflictList.Count > 5)
            {
                description += $"；另有 {conflictList.Count - 5} 条冲突未展开";
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
