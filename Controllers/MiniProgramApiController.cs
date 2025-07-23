using StudentInformationSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;

namespace StudentInformationSystem.Controllers
{
    [RoutePrefix("api/miniprogram")] // 为整个控制器定义路由前缀
    public class MiniProgramApiController : ApiController
    {
        private Entities db = new Entities();

        // POST: api/miniprogram/login
        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login(Users loginRequest)
        {
            var user = db.Users
                         .FirstOrDefault(u => u.Username == loginRequest.Username && u.Password == loginRequest.Password);

            if (user == null)
            {
                return Unauthorized();
            }

            // --- 新增的逻辑：根据角色查询真实姓名 ---
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

        // 注意：Dispose方法对于释放数据库连接很重要，请确保它存在
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}