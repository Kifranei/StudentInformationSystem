using StudentInformationSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using StudentInformationSystem.Helpers;
using System.Web.Http;
using System.Web.Http.Description;

namespace StudentInformationSystem.Controllers
{
    public class MiniProgramCourseActionRequest
    {
        public int UserId { get; set; }
        public int CourseId { get; set; }
    }

    public class MiniProgramTeacherGradeSaveRequest
    {
        public int UserId { get; set; }
        public int CourseId { get; set; }
        public List<MiniProgramTeacherStudentGradeRequest> Grades { get; set; }
    }

    public class MiniProgramTeacherStudentGradeRequest
    {
        public string StudentId { get; set; }
        public double? Grade { get; set; }
    }

    [RoutePrefix("api/miniprogram")] // 为整个控制器定义路由前缀
    public class MiniProgramApiController : ApiController
    {
        private StudentManagementDBEntities db = new StudentManagementDBEntities();

        // POST: api/miniprogram/login
        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login(Users loginRequest)
        {
            var user = db.Users
                         .FirstOrDefault(u => u.Username == loginRequest.Username);

            bool upgraded;
            if (!PasswordSecurity.VerifyAndUpgrade(user, loginRequest.Password, out upgraded))
            {
                return Unauthorized();
            }

            if (upgraded)
            {
                db.Entry(user).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
            }

            // --- 根据角色查询真实姓名 ---
            string realName = user.Username; // 默认使用用户名

            if (user.Role == 2) // 如果是学生
            {
                var student = db.Students.FirstOrDefault(s => s.UserID == user.UserID);
                if (student != null)
                {
                    realName = student.StudentName;
                }
            }
            else if (user.Role == 1) // 如果是教师
            {
                var teacher = db.Teachers.FirstOrDefault(t => t.UserID == user.UserID);
                if (teacher != null)
                {
                    realName = teacher.TeacherName;
                }
            }
            // --- 逻辑结束 ---


            // 返回一个包含了真实姓名的新对象
            var userViewModel = new
            {
                user.UserID,
                user.Username,
                user.Role,
                RealName = realName // 新增 RealName 字段
            };

            return Ok(userViewModel);
        }

        // GET: api/MiniProgramApi/timetable?userId=...
        [HttpGet]
        [Route("timetable")] // 定义此方法的具体路由
        public IHttpActionResult GetTimetable(int userId)
        {
            var user = db.Users.Find(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (user.Role == 2) // 学生
            {
                var student = db.Students.FirstOrDefault(s => s.UserID == userId);
                if (student == null) return NotFound();

                var enrolledCourseIds = db.StudentCourses
                                          .Where(sc => sc.StudentID == student.StudentID)
                                          .Select(sc => sc.CourseID).ToList();

                var classSessions = db.ClassSessions.Include("Courses.Teachers")
                                      .Where(cs => enrolledCourseIds.Contains(cs.CourseID))
                                      .Select(cs => new {
                                          cs.Courses.CourseName,
                                          cs.DayOfWeek,
                                          cs.StartPeriod,
                                          cs.EndPeriod,
                                          cs.Classroom,
                                          cs.StartWeek,
                                          cs.EndWeek,
                                          TeacherName = cs.Courses.Teachers.TeacherName
                                      })
                                      .ToList();
                return Ok(classSessions);
            }
            else if (user.Role == 1) // 教师
            {
                var teacher = db.Teachers.FirstOrDefault(t => t.UserID == userId);
                if (teacher == null) return NotFound();

                var taughtCourseIds = db.Courses.Where(c => c.TeacherID == teacher.TeacherID)
                                        .Select(c => c.CourseID).ToList();

                var classSessions = db.ClassSessions.Include("Courses")
                                      .Where(cs => taughtCourseIds.Contains(cs.CourseID))
                                      .Select(cs => new {
                                          cs.Courses.CourseName,
                                          cs.DayOfWeek,
                                          cs.StartPeriod,
                                          cs.EndPeriod,
                                          cs.Classroom,
                                          cs.StartWeek,
                                          cs.EndWeek
                                      })
                                      .ToList();
                return Ok(classSessions);
            }

            return Ok(new object[0]);
        }

        // GET: api/miniprogram/grades?userId=...
        // 功能：学生查询成绩
        [HttpGet]
        [Route("grades")]
        public IHttpActionResult GetGrades(int userId)
        {
            var user = db.Users.Find(userId);
            if (user == null || user.Role != 2) // 2 代表学生
            {
                return BadRequest("当前用户不是学生");
            }

            var student = db.Students.FirstOrDefault(s => s.UserID == userId);
            if (student == null) return NotFound();

            // 查询该学生的选课记录及成绩
            var grades = db.StudentCourses.Include("Courses")
                           .Where(sc => sc.StudentID == student.StudentID)
                           .Select(sc => new
                           {
                               CourseName = sc.Courses.CourseName,
                               Credits = sc.Courses.Credits,
                               // 如果 Grade 是 null，返回 "暂无"，否则返回具体分数
                               Grade = sc.Grade.HasValue ? sc.Grade.ToString() : "暂无"
                           })
                           .ToList();

            return Ok(grades);
        }

        // GET: api/miniprogram/mycourses?userId=...
        // 功能：教师查询我的课程
        [HttpGet]
        [Route("mycourses")]
        public IHttpActionResult GetMyCourses(int userId)
        {
            var user = db.Users.Find(userId);
            if (user == null || user.Role != 1) // 1 代表教师
            {
                return BadRequest("当前用户不是教师");
            }

            var teacher = db.Teachers.FirstOrDefault(t => t.UserID == userId);
            if (teacher == null) return NotFound();

            var courseRows = db.Courses
                .Where(c => c.TeacherID == teacher.TeacherID)
                .Select(c => new
                {
                    c.CourseID,
                    c.CourseName,
                    c.Credits,
                    Type = c.CourseType
                })
                .ToList();

            var courseIds = courseRows.Select(c => c.CourseID).ToList();

            var enrolledCounts = db.StudentCourses
                .Where(sc => courseIds.Contains(sc.CourseID))
                .GroupBy(sc => sc.CourseID)
                .ToDictionary(g => g.Key, g => g.Count());

            var courses = courseRows
                .Select(c => new
                {
                    c.CourseID,
                    c.CourseName,
                    c.Credits,
                    c.Type,
                    TypeText = GetCourseTypeText(c.Type),
                    EnrolledStudentCount = enrolledCounts.ContainsKey(c.CourseID) ? enrolledCounts[c.CourseID] : 0
                })
                .ToList();

            return Ok(courses);
        }

        // GET: api/miniprogram/teacher-grade-entry?userId=...&courseId=...
        // 功能：教师查看某门课的成绩录入列表
        [HttpGet]
        [Route("teacher-grade-entry")]
        public IHttpActionResult GetTeacherGradeEntry(int userId, int courseId)
        {
            IHttpActionResult errorResult;
            var course = GetTeacherOwnedCourse(userId, courseId, out errorResult);
            if (course == null)
            {
                return errorResult;
            }

            return Ok(BuildTeacherGradeEntryPayload(course));
        }

        // POST: api/miniprogram/teacher-grade-entry/save
        // 功能：教师批量保存课程成绩
        [HttpPost]
        [Route("teacher-grade-entry/save")]
        public IHttpActionResult SaveTeacherGrades(MiniProgramTeacherGradeSaveRequest request)
        {
            if (request == null || request.UserId <= 0 || request.CourseId <= 0)
            {
                return BadRequest("请求参数不完整");
            }

            IHttpActionResult errorResult;
            var course = GetTeacherOwnedCourse(request.UserId, request.CourseId, out errorResult);
            if (course == null)
            {
                return errorResult;
            }

            if (request.Grades == null || !request.Grades.Any())
            {
                return Content(HttpStatusCode.BadRequest, new { message = "未提交任何成绩数据。" });
            }

            var enrollments = db.StudentCourses
                .Where(sc => sc.CourseID == course.CourseID)
                .ToList();

            foreach (var item in request.Grades)
            {
                if (item == null)
                {
                    return Content(HttpStatusCode.BadRequest, new { message = "存在无效的成绩记录。" });
                }

                var studentId = (item.StudentId ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(studentId))
                {
                    return Content(HttpStatusCode.BadRequest, new { message = "存在缺少学号的成绩记录。" });
                }

                if (item.Grade.HasValue && (item.Grade.Value < 0d || item.Grade.Value > 100d))
                {
                    return Content(HttpStatusCode.BadRequest, new { message = "成绩必须在 0-100 之间。" });
                }

                var enrollment = enrollments.FirstOrDefault(sc => sc.StudentID == studentId);
                if (enrollment == null)
                {
                    return Content(HttpStatusCode.BadRequest, new { message = "提交的学生不属于当前课程。" });
                }

                enrollment.Grade = item.Grade;
            }

            db.SaveChanges();
            return Ok(new { message = "成绩保存成功。" });
        }

        // GET: api/miniprogram/stats?userId=...
        // 功能：管理员查看系统统计信息
        [HttpGet]
        [Route("stats")]
        public IHttpActionResult GetSystemStats(int userId)
        {
            var user = db.Users.Find(userId);
            // 0 代表管理员 (根据之前的逻辑：1=教师, 2=学生)
            if (user == null || user.Role != 0)
            {
                return BadRequest("当前用户不是管理员");
            }

            // 统计各个表的数据量
            // 注意：这里假设你的数据库上下文中有这些 DbSet
            var stats = new
            {
                StudentCount = db.Students.Count(),
                TeacherCount = db.Teachers.Count(),
                CourseCount = db.Courses.Count(),
                ClassCount = db.Classes.Count(),
                UserCount = db.Users.Count()
            };

            return Ok(stats);
        }

        // GET: api/miniprogram/course-selection?userId=...
        // 功能：学生查询在线选课列表
        [HttpGet]
        [Route("course-selection")]
        public IHttpActionResult GetCourseSelection(int userId)
        {
            var user = db.Users.Find(userId);
            if (user == null || user.Role != 2)
            {
                return BadRequest("当前用户不是学生");
            }

            var student = db.Students.FirstOrDefault(s => s.UserID == userId);
            if (student == null)
            {
                return NotFound();
            }

            return Ok(BuildStudentCourseSelectionPayload(student));
        }

        // GET: api/miniprogram/course-selection/enrolled?userId=...
        // 功能：学生查询已选课程信息与数量
        [HttpGet]
        [Route("course-selection/enrolled")]
        public IHttpActionResult GetSelectedCourses(int userId)
        {
            var user = db.Users.Find(userId);
            if (user == null || user.Role != 2)
            {
                return BadRequest("当前用户不是学生");
            }

            var student = db.Students.FirstOrDefault(s => s.UserID == userId);
            if (student == null)
            {
                return NotFound();
            }

            var payload = BuildStudentCourseSelectionPayload(student);
            return Ok(new
            {
                payload.StudentId,
                payload.StudentName,
                payload.EnrolledCourseCount,
                payload.EnrolledElectiveCount,
                payload.RequiredCourseCount,
                payload.SportsCoursesTaken,
                payload.OtherCoursesTaken,
                payload.EnrolledCourses,
                payload.EnrolledElectives,
                payload.RequiredCourses
            });
        }

        // POST: api/miniprogram/course-selection/select
        // 功能：学生提交选课
        [HttpPost]
        [Route("course-selection/select")]
        public IHttpActionResult SelectCourse(MiniProgramCourseActionRequest request)
        {
            if (request == null || request.UserId <= 0 || request.CourseId <= 0)
            {
                return BadRequest("请求参数不完整");
            }

            var user = db.Users.Find(request.UserId);
            if (user == null || user.Role != 2)
            {
                return BadRequest("当前用户不是学生");
            }

            var student = db.Students.FirstOrDefault(s => s.UserID == request.UserId);
            if (student == null)
            {
                return NotFound();
            }

            var course = db.Courses.Find(request.CourseId);
            if (course == null)
            {
                return BadRequest("课程不存在");
            }

            if (course.CourseType != 3 && course.CourseType != 4 && course.CourseType != 5)
            {
                return Content(HttpStatusCode.BadRequest, new { message = "该课程不允许学生自主选课。" });
            }

            bool isEnrolled = db.StudentCourses.Any(sc => sc.StudentID == student.StudentID && sc.CourseID == request.CourseId);
            if (isEnrolled)
            {
                return Content(HttpStatusCode.BadRequest, new { message = "您已经选过这门课了。" });
            }

            if (course.CourseType == 5)
            {
                bool hasPECourse = db.StudentCourses.Any(sc => sc.StudentID == student.StudentID && sc.Courses.CourseType == 5);
                if (hasPECourse)
                {
                    return Content(HttpStatusCode.BadRequest, new { message = "体育选修课每人限选一门，请先退选原体育课后再选！" });
                }
            }

            var selectionConflicts = ScheduleConflictHelper.GetStudentConflictsForCourseSelection(db, student.StudentID, request.CourseId);
            if (selectionConflicts.Any())
            {
                return Content(HttpStatusCode.BadRequest, new
                {
                    message = ScheduleConflictHelper.BuildStudentConflictMessage(
                        selectionConflicts,
                        "选课失败，当前课程与已选课程时间冲突：")
                });
            }

            db.StudentCourses.Add(new StudentCourses
            {
                StudentID = student.StudentID,
                CourseID = request.CourseId,
                Grade = null
            });
            db.SaveChanges();

            return Ok(new { message = "选课成功！" });
        }

        // POST: api/miniprogram/course-selection/withdraw
        // 功能：学生退课
        [HttpPost]
        [Route("course-selection/withdraw")]
        public IHttpActionResult WithdrawCourse(MiniProgramCourseActionRequest request)
        {
            if (request == null || request.UserId <= 0 || request.CourseId <= 0)
            {
                return BadRequest("请求参数不完整");
            }

            var user = db.Users.Find(request.UserId);
            if (user == null || user.Role != 2)
            {
                return BadRequest("当前用户不是学生");
            }

            var student = db.Students.FirstOrDefault(s => s.UserID == request.UserId);
            if (student == null)
            {
                return NotFound();
            }

            var course = db.Courses.Find(request.CourseId);
            if (course == null)
            {
                return BadRequest("课程不存在");
            }

            if (course.CourseType == 1 || course.CourseType == 2)
            {
                return Content(HttpStatusCode.BadRequest, new { message = "必修课程为教务处统一排课，学生不可自行退选！" });
            }

            var enrollment = db.StudentCourses
                .FirstOrDefault(sc => sc.StudentID == student.StudentID && sc.CourseID == request.CourseId);

            if (enrollment == null)
            {
                return Content(HttpStatusCode.BadRequest, new { message = "未找到对应选课记录。" });
            }

            db.StudentCourses.Remove(enrollment);
            db.SaveChanges();

            return Ok(new { message = "退课成功！" });
        }

        private static string GetCourseTypeText(int courseType)
        {
            switch (courseType)
            {
                case 1:
                    return "专业必修";
                case 2:
                    return "公共必修";
                case 3:
                    return "专业选修";
                case 4:
                    return "公共选修";
                case 5:
                    return "体育选修";
                default:
                    return "其他课程";
            }
        }

        private static string BuildCourseScheduleSummary(Dictionary<int, string> sessionSummaryLookup, int courseId)
        {
            string summary;
            if (!sessionSummaryLookup.TryGetValue(courseId, out summary) || string.IsNullOrWhiteSpace(summary))
            {
                return "暂未安排";
            }

            return summary;
        }

        private Courses GetTeacherOwnedCourse(int userId, int courseId, out IHttpActionResult errorResult)
        {
            errorResult = null;

            var user = db.Users.Find(userId);
            if (user == null || user.Role != 1)
            {
                errorResult = BadRequest("当前用户不是教师");
                return null;
            }

            var teacher = db.Teachers.FirstOrDefault(t => t.UserID == userId);
            if (teacher == null)
            {
                errorResult = NotFound();
                return null;
            }

            var course = db.Courses.Include("Teachers")
                .FirstOrDefault(c => c.CourseID == courseId && c.TeacherID == teacher.TeacherID);
            if (course == null)
            {
                errorResult = Content(HttpStatusCode.Forbidden, new { message = "该课程不属于当前教师。" });
                return null;
            }

            return course;
        }

        private object BuildTeacherGradeEntryPayload(Courses course)
        {
            var enrollments = db.StudentCourses
                .Include("Students.Classes")
                .Where(sc => sc.CourseID == course.CourseID)
                .OrderBy(sc => sc.Students.StudentID)
                .ToList();

            return new
            {
                course.CourseID,
                course.CourseName,
                course.Credits,
                CourseType = course.CourseType,
                CourseTypeText = GetCourseTypeText(course.CourseType),
                TeacherName = course.Teachers == null ? "待安排" : course.Teachers.TeacherName,
                StudentCount = enrollments.Count,
                Students = enrollments.Select(sc => new
                {
                    StudentID = sc.StudentID,
                    StudentName = sc.Students == null ? string.Empty : sc.Students.StudentName,
                    ClassName = sc.Students != null && sc.Students.Classes != null ? sc.Students.Classes.ClassName : string.Empty,
                    Grade = sc.Grade
                }).ToList()
            };
        }

        private StudentCourseSelectionPayload BuildStudentCourseSelectionPayload(Students student)
        {
            var allEnrollments = db.StudentCourses.Include("Courses.Teachers")
                .Where(sc => sc.StudentID == student.StudentID)
                .ToList();

            var enrolledCourseIds = allEnrollments.Select(sc => sc.CourseID).ToList();
            var retakeCourseIds = allEnrollments
                .Where(sc => sc.Grade < 60)
                .Select(sc => sc.CourseID)
                .ToList();

            var selectableCourses = db.Courses.Include("Teachers")
                .Where(c => (c.CourseType == 3 || c.CourseType == 4 || c.CourseType == 5)
                    && !enrolledCourseIds.Contains(c.CourseID)
                    && !retakeCourseIds.Contains(c.CourseID))
                .ToList();

            var relatedCourseIds = selectableCourses.Select(c => c.CourseID)
                .Union(allEnrollments.Select(sc => sc.CourseID))
                .Distinct()
                .ToList();

            var sessionSummaryLookup = db.ClassSessions
                .Where(cs => relatedCourseIds.Contains(cs.CourseID))
                .OrderBy(cs => cs.DayOfWeek)
                .ThenBy(cs => cs.StartPeriod)
                .ThenBy(cs => cs.StartWeek)
                .ToList()
                .GroupBy(cs => cs.CourseID)
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(" / ", g.Select(cs =>
                        string.Format("周{0} 第{1}-{2}节 · 第{3}-{4}周 · {5}",
                            GetDayChar(cs.DayOfWeek),
                            cs.StartPeriod,
                            cs.EndPeriod,
                            cs.StartWeek,
                            cs.EndWeek,
                            cs.Classroom))));

            Func<Courses, StudentCourseItem> toSelectableVm = course => new StudentCourseItem
            {
                CourseID = course.CourseID,
                CourseName = course.CourseName,
                Credits = course.Credits,
                CourseType = course.CourseType,
                CourseTypeText = GetCourseTypeText(course.CourseType),
                TeacherName = course.Teachers == null ? "待安排" : course.Teachers.TeacherName,
                ScheduleSummary = BuildCourseScheduleSummary(sessionSummaryLookup, course.CourseID),
                CanWithdraw = false
            };

            Func<StudentCourses, StudentCourseItem> toEnrolledVm = enrollment => new StudentCourseItem
            {
                CourseID = enrollment.CourseID,
                CourseName = enrollment.Courses.CourseName,
                Credits = enrollment.Courses.Credits,
                CourseType = enrollment.Courses.CourseType,
                CourseTypeText = GetCourseTypeText(enrollment.Courses.CourseType),
                TeacherName = enrollment.Courses.Teachers == null ? "待安排" : enrollment.Courses.Teachers.TeacherName,
                ScheduleSummary = BuildCourseScheduleSummary(sessionSummaryLookup, enrollment.CourseID),
                Grade = enrollment.Grade,
                CanWithdraw = enrollment.Courses.CourseType != 1 && enrollment.Courses.CourseType != 2
            };

            var enrolledCourseItems = allEnrollments.Select(toEnrolledVm).ToList();
            var enrolledElectiveItems = allEnrollments
                .Where(sc => sc.Courses.CourseType == 3 || sc.Courses.CourseType == 4 || sc.Courses.CourseType == 5)
                .Select(toEnrolledVm)
                .ToList();
            var requiredCourseItems = allEnrollments
                .Where(sc => sc.Courses.CourseType == 1 || sc.Courses.CourseType == 2)
                .Select(toEnrolledVm)
                .ToList();

            return new StudentCourseSelectionPayload
            {
                StudentId = student.StudentID,
                StudentName = student.StudentName,
                EnrolledCourseCount = enrolledCourseItems.Count,
                EnrolledElectiveCount = enrolledElectiveItems.Count,
                RequiredCourseCount = requiredCourseItems.Count,
                SportsCoursesTaken = allEnrollments.Count(sc => sc.Courses.CourseType == 5),
                OtherCoursesTaken = allEnrollments.Count(sc => sc.Courses.CourseType == 3 || sc.Courses.CourseType == 4),
                EnrolledCourses = enrolledCourseItems,
                EnrolledElectives = enrolledElectiveItems,
                RequiredCourses = requiredCourseItems,
                SportsElectives = selectableCourses
                    .Where(c => c.CourseType == 5)
                    .Select(toSelectableVm)
                    .ToList(),
                OtherElectives = selectableCourses
                    .Where(c => c.CourseType == 3 || c.CourseType == 4)
                    .Select(toSelectableVm)
                    .ToList()
            };
        }

        private static string GetDayChar(int dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case 1: return "一";
                case 2: return "二";
                case 3: return "三";
                case 4: return "四";
                case 5: return "五";
                case 6: return "六";
                case 7: return "日";
                default: return dayOfWeek.ToString();
            }
        }

        // Dispose方法对于释放数据库连接很重要，请确保它存在
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class StudentCourseSelectionPayload
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public int EnrolledCourseCount { get; set; }
        public int EnrolledElectiveCount { get; set; }
        public int RequiredCourseCount { get; set; }
        public int SportsCoursesTaken { get; set; }
        public int OtherCoursesTaken { get; set; }
        public List<StudentCourseItem> EnrolledCourses { get; set; }
        public List<StudentCourseItem> EnrolledElectives { get; set; }
        public List<StudentCourseItem> RequiredCourses { get; set; }
        public List<StudentCourseItem> SportsElectives { get; set; }
        public List<StudentCourseItem> OtherElectives { get; set; }
    }

    public class StudentCourseItem
    {
        public int CourseID { get; set; }
        public string CourseName { get; set; }
        public double Credits { get; set; }
        public int CourseType { get; set; }
        public string CourseTypeText { get; set; }
        public string TeacherName { get; set; }
        public string ScheduleSummary { get; set; }
        public double? Grade { get; set; }
        public bool CanWithdraw { get; set; }
    }
}
