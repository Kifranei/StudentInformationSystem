using StudentInformationSystem.Models;
using StudentInformationSystem.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace StudentInformationSystem.Controllers
{
    // 同样继承 BaseController 来确保登录后才能访问
    public class TeacherController : BaseController
    {
        private Entities db = new Entities(); // 你的数据库上下文

        // GET: Teacher/Index
        // 教师登录后的主页
        public ActionResult Index()
        {
            var currentUser = Session["User"] as Users;
            var teacher = db.Teachers.FirstOrDefault(t => t.UserID == currentUser.UserID);
            if (teacher == null) { return View("Error"); }

            var viewModel = new TeacherDashboardViewModel { TeacherName = teacher.TeacherName };

            int dayOfWeek = (int)DateTime.Now.DayOfWeek;
            int ourDayOfWeek = dayOfWeek == 0 ? 7 : dayOfWeek;

            var taughtCourseIds = db.Courses.Where(c => c.TeacherID == teacher.TeacherID)
                                    .Select(c => c.CourseID).ToList();

            viewModel.TodaysClasses = db.ClassSessions.Include("Courses")
                                        .Where(cs => taughtCourseIds.Contains(cs.CourseID) && cs.DayOfWeek == ourDayOfWeek)
                                        .OrderBy(cs => cs.StartPeriod)
                                        .ToList();

            return View(viewModel);
        }

        // GET: /Teacher/GradeEntry?courseId=...
        // 用于显示指定课程的学生列表和成绩输入框
        public ActionResult GradeEntry(int courseId)
        {
            // 1. 根据 courseId 找到所有选了这门课的学生选课记录 (StudentCourses)
            //    我们使用 .Include("Students") 来同时加载关联的学生信息，避免N+1查询
            var enrollments = db.StudentCourses.Include("Students")
                                .Where(sc => sc.CourseID == courseId).ToList();

            // 2. 使用 ViewBag 传递课程信息，用于在页面上显示标题
            ViewBag.Course = db.Courses.Find(courseId);

            // 3. 将查询到的选课记录列表传递给视图
            return View(enrollments);
        }


        // POST: /Teacher/GradeEntry
        // 用于接收并保存提交的成绩
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GradeEntry(int courseId, string[] studentIds, float?[] grades)
        {
            // 我们使用了两个数组来接收所有学生的ID和对应的成绩
            if (studentIds != null && grades != null && studentIds.Length == grades.Length)
            {
                for (int i = 0; i < studentIds.Length; i++)
                {
                    var studentId = studentIds[i];
                    var grade = grades[i];

                    // 找到数据库中对应的选课记录
                    var enrollment = db.StudentCourses
                                       .FirstOrDefault(sc => sc.StudentID == studentId && sc.CourseID == courseId);

                    if (enrollment != null)
                    {
                        // 更新成绩 (如果输入框为空，则grade会是null，数据库里也会是NULL)
                        enrollment.Grade = grade;
                    }
                }

                // 循环结束后，一次性将所有更改保存到数据库
                db.SaveChanges();
            }

            // 处理完成后，重定向回教师的主页（我的课表页面）
            return RedirectToAction("Index");
        }
        // GET: Teacher/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        // POST: Teacher/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUser = Session["User"] as Users;
            var userInDb = db.Users.Find(currentUser.UserID);

            if (userInDb.Password != model.OldPassword)
            {
                ModelState.AddModelError("", "旧密码不正确，请重新输入。");
                return View(model);
            }

            userInDb.Password = model.NewPassword;
            db.Entry(userInDb).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            ViewBag.SuccessMessage = "密码修改成功！";

            return View(model);
        }
        // GET: Teacher/Timetable
        // 接收一个可选的周数参数
        public ActionResult Timetable(int? selectedWeek)
        {
            // 如果没有选择周数，默认显示第一周
            int currentWeek = selectedWeek ?? 1;
            ViewBag.CurrentWeek = currentWeek;

            // 准备一个周数列表给下拉框使用
            ViewBag.WeekList = Enumerable.Range(1, 21).Select(w => new SelectListItem
            {
                Text = "第 " + w + " 周",
                Value = w.ToString()
            });

            // 1. 获取当前登录的用户信息，并找到对应的教师
            var currentUser = Session["User"] as Users;
            var teacher = db.Teachers.FirstOrDefault(t => t.UserID == currentUser.UserID);

            // 2. 获取该教师所教的所有课程的ID列表
            var taughtCourseIds = db.Courses
                                    .Where(c => c.TeacherID == teacher.TeacherID)
                                    .Select(c => c.CourseID)
                                    .ToList();

            // 3. 根据课程ID，从 ClassSessions 表中查出所有相关的课程安排
            //    同时使用 .Include() 加载课程信息，以便显示课程名称
            var classSessions = db.ClassSessions.Include("Courses")
                                  .Where(cs => taughtCourseIds.Contains(cs.CourseID))
                                  .ToList();

            // 4. 将查询到的课表数据传递给视图
            return View(classSessions);
        }

        // GET: Teacher/AddClassSession
        // 显示添加课程安排的表单
        public ActionResult AddClassSession()
        {
            var taughtCourseIds = GetTaughtCourseIds();
            // 下拉列表只包含该教师自己的课程
            ViewBag.CourseID = new SelectList(db.Courses.Where(c => taughtCourseIds.Contains(c.CourseID)), "CourseID", "CourseName");
            return View();
        }

        // POST: Teacher/AddClassSession
        // 处理添加课程安排的表单提交
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddClassSession(ClassSessions session)
        {
            var taughtCourseIds = GetTaughtCourseIds();
            
            // 验证课程是否属于当前教师
            if (!taughtCourseIds.Contains(session.CourseID))
            {
                ModelState.AddModelError("CourseID", "您只能为自己教授的课程添加安排。");
            }

            // 验证周次范围
            if (session.StartWeek > session.EndWeek)
            {
                ModelState.AddModelError("EndWeek", "结束周数不能小于开始周数。");
            }

            // 验证节次范围
            if (session.StartPeriod > session.EndPeriod)
            {
                ModelState.AddModelError("EndPeriod", "结束节次不能小于开始节次。");
            }

            // 检查时间冲突 - 在相同的周次范围内，是否有同一教师在相同时间的其他安排
            var conflictingSessions = db.ClassSessions.Include("Courses")
                .Where(cs => taughtCourseIds.Contains(cs.CourseID) && // 同一教师的课程
                           cs.DayOfWeek == session.DayOfWeek && // 同一天
                           !(session.EndWeek < cs.StartWeek || session.StartWeek > cs.EndWeek) && // 周次有重叠
                           !(session.EndPeriod < cs.StartPeriod || session.StartPeriod > cs.EndPeriod)) // 节次有重叠
                .ToList();

            if (conflictingSessions.Any())
            {
                var conflictDescription = string.Join("; ", conflictingSessions.Select(cs => 
                    $"{cs.Courses.CourseName}(第{cs.StartWeek}-{cs.EndWeek}周, 第{cs.StartPeriod}-{cs.EndPeriod}节)"));
                ModelState.AddModelError("", $"时间冲突！您在该时间段已有以下课程安排：{conflictDescription}");
            }

            if (ModelState.IsValid)
            {
                db.ClassSessions.Add(session);
                db.SaveChanges();
                
                TempData["SuccessMessage"] = $"课程安排添加成功！{db.Courses.Find(session.CourseID).CourseName} - 第{session.StartWeek}-{session.EndWeek}周，星期{GetDayName(session.DayOfWeek)}第{session.StartPeriod}-{session.EndPeriod}节，{session.Classroom}教室。";
                return RedirectToAction("Timetable");
            }

            // 如果验证失败，重新加载课程下拉列表
            ViewBag.CourseID = new SelectList(db.Courses.Where(c => taughtCourseIds.Contains(c.CourseID)), "CourseID", "CourseName", session.CourseID);
            return View(session);
        }

        // 辅助方法：将数字转换为星期名称（更新为完整格式）
        private string GetDayName(int dayOfWeek)
        {
            string[] days = { "", "一", "二", "三", "四", "五", "六", "日" };
            return dayOfWeek >= 1 && dayOfWeek <= 7 ? days[dayOfWeek] : "未知";
        }

        // GET: Teacher/AdjustClass/5
        // 当教师点击"调课"按钮时，会带着 SessionID 跳转到这里
        public ActionResult AdjustClass(int sessionId)
        {
            // 根据 ID 找到要调整的这节课
            var sessionToAdjust = db.ClassSessions.Find(sessionId);
            if (sessionToAdjust == null)
            {
                return HttpNotFound();
            }
            // 将这节课的信息传递给视图
            return View(sessionToAdjust);
        }

        // POST: Teacher/AdjustClass/5
        // 当在调课页面点击"确认"后，表单数据会提交到这里
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdjustClass(ClassSessions session)
        {
            var taughtCourseIds = GetTaughtCourseIds();
            
            // 验证课程是否属于当前教师
            if (!taughtCourseIds.Contains(session.CourseID))
            {
                ModelState.AddModelError("", "您只能调整自己教授的课程。");
                return View(session);
            }

            // 验证周次范围
            if (session.StartWeek > session.EndWeek)
            {
                ModelState.AddModelError("EndWeek", "结束周数不能小于开始周数。");
            }

            // 验证节次范围
            if (session.StartPeriod > session.EndPeriod)
            {
                ModelState.AddModelError("EndPeriod", "结束节次不能小于开始节次。");
            }

            // 检查时间冲突 - 排除当前正在编辑的这个安排
            var conflictingSessions = db.ClassSessions.Include("Courses")
                .Where(cs => cs.SessionID != session.SessionID && // 排除当前编辑的安排
                           taughtCourseIds.Contains(cs.CourseID) && // 同一教师的课程
                           cs.DayOfWeek == session.DayOfWeek && // 同一天
                           !(session.EndWeek < cs.StartWeek || session.StartWeek > cs.EndWeek) && // 周次有重叠
                           !(session.EndPeriod < cs.StartPeriod || session.StartPeriod > cs.EndPeriod)) // 节次有重叠
                .ToList();

            if (conflictingSessions.Any())
            {
                var conflictDescription = string.Join("; ", conflictingSessions.Select(cs => 
                    $"{cs.Courses.CourseName}(第{cs.StartWeek}-{cs.EndWeek}周, 第{cs.StartPeriod}-{cs.EndPeriod}节)"));
                ModelState.AddModelError("", $"时间冲突！您在该时间段已有以下课程安排：{conflictDescription}");
            }

            // 检查模型状态是否有效
            if (ModelState.IsValid)
            {
                // 告诉 Entity Framework，这个对象已经被修改了
                db.Entry(session).State = System.Data.Entity.EntityState.Modified;
                // 保存更改到数据库
                db.SaveChanges();
                
                TempData["SuccessMessage"] = $"课程调整成功！{db.Courses.Find(session.CourseID).CourseName} 已调整为：第{session.StartWeek}-{session.EndWeek}周，星期{GetDayName(session.DayOfWeek)}第{session.StartPeriod}-{session.EndPeriod}节，{session.Classroom}教室。";
                // 调课成功后，重定向回教师的课表页面
                return RedirectToAction("Timetable");
            }
            // 如果失败，则返回原页面
            return View(session);
        }
        // --- 考试管理 ---

        // 获取当前登录教师所教课程的ID列表 (辅助方法)
        private List<int> GetTaughtCourseIds()
        {
            var currentUser = Session["User"] as Users;
            var teacher = db.Teachers.FirstOrDefault(t => t.UserID == currentUser.UserID);
            return db.Courses.Where(c => c.TeacherID == teacher.TeacherID)
                             .Select(c => c.CourseID)
                             .ToList();
        }

        // GET: Teacher/ExamList
        public ActionResult ExamList()
        {
            var taughtCourseIds = GetTaughtCourseIds();
            // 只查询属于该教师课程的考试
            var exams = db.Exams.Include("Courses")
                          .Where(e => taughtCourseIds.Contains(e.CourseID))
                          .ToList();
            return View(exams);
        }

        // GET: Teacher/AddExam
        public ActionResult AddExam()
        {
            var taughtCourseIds = GetTaughtCourseIds();
            // 下拉列表只包含该教师自己的课程
            ViewBag.CourseID = new SelectList(db.Courses.Where(c => taughtCourseIds.Contains(c.CourseID)), "CourseID", "CourseName");
            return View();
        }

        // POST: Teacher/AddExam
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddExam(Exams exam)
        {
            var taughtCourseIds = GetTaughtCourseIds();
            // 安全检查：确保提交的课程ID是该教师的
            if (ModelState.IsValid && taughtCourseIds.Contains(exam.CourseID))
            {
                db.Exams.Add(exam);
                db.SaveChanges();
                return RedirectToAction("ExamList");
            }
            // 如果验证失败或课程ID不属于该教师，则返回表单
            ViewBag.CourseID = new SelectList(db.Courses.Where(c => taughtCourseIds.Contains(c.CourseID)), "CourseID", "CourseName", exam.CourseID);
            return View(exam);
        }

        // GET: Teacher/EditExam/5
        public ActionResult EditExam(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            Exams exam = db.Exams.Find(id);
            if (exam == null) return HttpNotFound();

            // 安全检查：确保要编辑的考试属于该教师的课程
            var taughtCourseIds = GetTaughtCourseIds();
            if (!taughtCourseIds.Contains(exam.CourseID))
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden); // 返回403 Forbidden错误
            }

            ViewBag.CourseID = new SelectList(db.Courses.Where(c => taughtCourseIds.Contains(c.CourseID)), "CourseID", "CourseName", exam.CourseID);
            return View(exam);
        }

        // POST: Teacher/EditExam/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditExam(Exams exam)
        {
            var taughtCourseIds = GetTaughtCourseIds();
            if (ModelState.IsValid && taughtCourseIds.Contains(exam.CourseID))
            {
                db.Entry(exam).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("ExamList");
            }
            ViewBag.CourseID = new SelectList(db.Courses.Where(c => taughtCourseIds.Contains(c.CourseID)), "CourseID", "CourseName", exam.CourseID);
            return View(exam);
        }

        // GET: Teacher/DeleteExam/5
        public ActionResult DeleteExam(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            Exams exam = db.Exams.Find(id);
            if (exam == null) return HttpNotFound();

            // 安全检查
            var taughtCourseIds = GetTaughtCourseIds();
            if (!taughtCourseIds.Contains(exam.CourseID))
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            }

            return View(exam);
        }

        // POST: Teacher/DeleteExam/5
        [HttpPost, ActionName("DeleteExam")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteExamConfirmed(int id)
        {
            Exams exam = db.Exams.Find(id);

            // 安全检查
            var taughtCourseIds = GetTaughtCourseIds();
            if (exam != null && taughtCourseIds.Contains(exam.CourseID))
            {
                db.Exams.Remove(exam);
                db.SaveChanges();
            }
            return RedirectToAction("ExamList");
        }
        // GET: Teacher/CourseList
        public ActionResult CourseList()
        {
            var currentUser = Session["User"] as Users;
            var teacher = db.Teachers.FirstOrDefault(t => t.UserID == currentUser.UserID);

            var courses = db.Courses
                            .Where(c => c.TeacherID == teacher.TeacherID)
                            .ToList();
            return View(courses);
        }
        // GET: Teacher/ClassRoster?courseId=5
        public ActionResult ClassRoster(int courseId)
        {
            // 查询这门课的所有选课记录，并加载学生信息
            var enrollments = db.StudentCourses.Include("Students")
                                .Where(sc => sc.CourseID == courseId)
                                .OrderBy(sc => sc.Students.StudentID) // 按学号排序
                                .ToList();

            // 把课程信息也传递过去，用于显示标题
            ViewBag.Course = db.Courses.Find(courseId);

            return View(enrollments);
        }
        // GET: Teacher/DetailsExam/5
        public ActionResult DetailsExam(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }
            Exams exam = db.Exams.Find(id);
            if (exam == null)
            {
                return HttpNotFound();
            }

            // 安全检查：确保要查看的考试属于该教师的课程
            var taughtCourseIds = GetTaughtCourseIds(); // 使用我们之前创建的辅助方法
            if (!taughtCourseIds.Contains(exam.CourseID))
            {
                // 如果不属于，则返回"禁止访问"错误
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            }

            return View(exam);
        }

        // GET: Teacher/ManageClassSessions
        // 课程安排管理页面
        public ActionResult ManageClassSessions()
        {
            var taughtCourseIds = GetTaughtCourseIds();
            var classSessions = db.ClassSessions.Include("Courses")
                                  .Where(cs => taughtCourseIds.Contains(cs.CourseID))
                                  .OrderBy(cs => cs.Courses.CourseName)
                                  .ThenBy(cs => cs.StartWeek)
                                  .ThenBy(cs => cs.DayOfWeek)
                                  .ThenBy(cs => cs.StartPeriod)
                                  .ToList();
            return View(classSessions);
        }

        // GET: Teacher/DeleteClassSession/5
        public ActionResult DeleteClassSession(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            
            var session = db.ClassSessions.Include("Courses").FirstOrDefault(cs => cs.SessionID == id);
            if (session == null) return HttpNotFound();

            // 安全检查：确保要删除的课程安排属于该教师
            var taughtCourseIds = GetTaughtCourseIds();
            if (!taughtCourseIds.Contains(session.CourseID))
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            }

            return View(session);
        }

        // POST: Teacher/DeleteClassSession/5
        [HttpPost, ActionName("DeleteClassSession")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteClassSessionConfirmed(int id)
        {
            var session = db.ClassSessions.Include("Courses").FirstOrDefault(cs => cs.SessionID == id);
            var taughtCourseIds = GetTaughtCourseIds();
            
            if (session != null && taughtCourseIds.Contains(session.CourseID))
            {
                var courseName = session.Courses.CourseName;
                var scheduleInfo = $"第{session.StartWeek}-{session.EndWeek}周，星期{GetDayName(session.DayOfWeek)}第{session.StartPeriod}-{session.EndPeriod}节，{session.Classroom}教室";
                
                db.ClassSessions.Remove(session);
                db.SaveChanges();
                
                TempData["SuccessMessage"] = $"课程安排删除成功！已删除 {courseName} 的安排：{scheduleInfo}";
            }
            return RedirectToAction("ManageClassSessions");
        }
    }
}