using StudentInformationSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace StudentInformationSystem.Controllers
{
    // 继承 BaseController 确保只有登录学生才能访问
    public class StudentController : BaseController
    {
        private StudentManagementDBEntities db = new StudentManagementDBEntities(); // 你的数据库上下文

        // GET: Student/Index
        // 学生登录后的主页，即“我的成绩”页面
        public ActionResult Index()
        {
            // 1. 获取当前登录的学生信息
            var currentUser = Session["User"] as Users;
            var student = db.Students.FirstOrDefault(s => s.UserID == currentUser.UserID);
            if (student == null) { return View("Error"); }

            // 2. 创建 ViewModel 实例
            var viewModel = new StudentDashboardViewModel
            {
                StudentName = student.StudentName,
                TodaysClasses = new List<ClassSessions>(),
                GradedCourses = new List<StudentCourses>()
            };

            // 3. 查询今天的课程
            // 将 .NET 的 DayOfWeek (Sunday = 0) 转换为我们约定的 (Monday = 1)
            int dayOfWeek = (int)DateTime.Now.DayOfWeek;
            int ourDayOfWeek = dayOfWeek == 0 ? 7 : dayOfWeek;

            var enrolledCourseIds = db.StudentCourses
                                      .Where(sc => sc.StudentID == student.StudentID)
                                      .Select(sc => sc.CourseID).ToList();

            viewModel.TodaysClasses = db.ClassSessions.Include("Courses")
                                        .Where(cs => enrolledCourseIds.Contains(cs.CourseID) && cs.DayOfWeek == ourDayOfWeek)
                                        .OrderBy(cs => cs.StartPeriod)
                                        .ToList();

            // 4. 查询所有已出成绩的课程
            viewModel.GradedCourses = db.StudentCourses.Include("Courses")
                                        .Where(sc => sc.StudentID == student.StudentID && sc.Grade != null)
                                        .OrderByDescending(sc => sc.SC_ID) // 按最近的选课记录排序
                                        .ToList();

            // 5. 将打包好的 viewModel 传递给视图
            return View(viewModel);
        }
        // GET: Student/CourseSelection
        // 显示所有可选课程的列表
        public ActionResult CourseSelection()
        {
            var currentUser = Session["User"] as Users;
            var student = db.Students.FirstOrDefault(s => s.UserID == currentUser.UserID);
            if (student == null) { return View("Error"); }

            var viewModel = new CourseSelectionViewModel();

            // 1. 获取该生所有选课记录 (保持不变)
            var allEnrollments = db.StudentCourses.Include("Courses.Teachers") // 预加载课程和教师信息
                                   .Where(sc => sc.StudentID == student.StudentID)
                                   .ToList();

            // --- 新增的逻辑：将完整的已选课程列表存入ViewModel ---
            viewModel.EnrolledCourses = allEnrollments;

            viewModel.EnrolledCourseIDs = allEnrollments.Select(sc => sc.CourseID).ToList();

            // 2. 找出需要重修的课程 (保持不变)
            viewModel.RetakeCourses = allEnrollments
                                        .Where(sc => sc.Grade < 60)
                                        .Select(sc => sc.Courses)
                                        .ToList();

            // 3. 计算体育课和思政课已选门数 (保持不变)
            viewModel.SportsCoursesTaken = allEnrollments.Count(sc => sc.Courses.CourseType == 3);
            viewModel.PoliticsCoursesTaken = allEnrollments.Count(sc => sc.Courses.CourseType == 4);

            // 4. 获取所有可选课程 (保持不变)
            var allAvailableCourses = db.Courses.Include("Teachers")
                                        .Where(c => !viewModel.EnrolledCourseIDs.Contains(c.CourseID) &&
                                                    !viewModel.RetakeCourses.Select(rc => rc.CourseID).Contains(c.CourseID))
                                        .ToList();

            // 5. 按类别对可选课程进行分组 (保持不变)
            viewModel.MajorElectives = allAvailableCourses.Where(c => c.CourseType == 1).ToList();
            viewModel.PublicElectives = allAvailableCourses.Where(c => c.CourseType == 2).ToList();
            viewModel.SportsElectives = allAvailableCourses.Where(c => c.CourseType == 3).ToList();
            viewModel.PoliticsElectives = allAvailableCourses.Where(c => c.CourseType == 4).ToList();
            viewModel.OtherElectives = allAvailableCourses.Where(c => c.CourseType == 5).ToList();

            // --- 新增：获取所有课程的课程安排信息 ---
            var allCourseIds = allAvailableCourses.Select(c => c.CourseID)
                                .Union(viewModel.RetakeCourses.Select(rc => rc.CourseID))
                                .Union(viewModel.EnrolledCourseIDs)
                                .ToList();

            var courseSchedules = db.ClassSessions
                                    .Where(cs => allCourseIds.Contains(cs.CourseID))
                                    .OrderBy(cs => cs.CourseID)
                                    .ThenBy(cs => cs.StartWeek)
                                    .ThenBy(cs => cs.DayOfWeek)
                                    .ThenBy(cs => cs.StartPeriod)
                                    .ToList();

            ViewBag.CourseSchedules = courseSchedules;
            ViewBag.EnrolledCourseIDs = viewModel.EnrolledCourseIDs;
            return View(viewModel);
        }

        // POST: Student/SelectCourse
        // 处理“选课”按钮的提交
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SelectCourse(int courseId)
        {
            var currentUser = Session["User"] as Users;
            var student = db.Students.FirstOrDefault(s => s.UserID == currentUser.UserID);

            // 检查是否已经选过该课程，防止重复提交
            bool isEnrolled = db.StudentCourses.Any(sc => sc.StudentID == student.StudentID && sc.CourseID == courseId);
            if (!isEnrolled)
            {
                // 创建一条新的选课记录
                var enrollment = new StudentCourses
                {
                    StudentID = student.StudentID,
                    CourseID = courseId,
                    Grade = null // 成绩初始为空
                };
                db.StudentCourses.Add(enrollment);
                db.SaveChanges();
            }
            // 处理完成后，重定向回选课页面
            return RedirectToAction("CourseSelection");
        }

        // POST: Student/WithdrawCourse
        // 处理“退选”按钮的提交
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult WithdrawCourse(int courseId)
        {
            var currentUser = Session["User"] as Users;
            var student = db.Students.FirstOrDefault(s => s.UserID == currentUser.UserID);

            // 找到要删除的选课记录
            var enrollment = db.StudentCourses
                               .FirstOrDefault(sc => sc.StudentID == student.StudentID && sc.CourseID == courseId);

            if (enrollment != null)
            {
                db.StudentCourses.Remove(enrollment);
                db.SaveChanges();
            }
            // 处理完成后，重定向回选课页面
            return RedirectToAction("CourseSelection");
        }
        // GET: Student/ChangePassword
        // 显示修改密码的表单页面
        public ActionResult ChangePassword()
        {
            return View();
        }

        // POST: Student/ChangePassword
        // 处理提交的修改密码表单
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            // 检查模型验证（比如字段是否为空，两次密码是否一致等）是否通过
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 1. 获取当前登录的用户
            var currentUser = Session["User"] as Users;
            var userInDb = db.Users.Find(currentUser.UserID);

            // 2. 验证旧密码是否正确
            if (userInDb.Password != model.OldPassword)
            {
                // 向模型状态中添加一个自定义错误
                ModelState.AddModelError("", "旧密码不正确，请重新输入。");
                return View(model);
            }

            // 3. 更新密码
            userInDb.Password = model.NewPassword;
            db.Entry(userInDb).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            // 4. 显示成功消息
            ViewBag.SuccessMessage = "密码修改成功！";

            return View(model);
        }
        // GET: Student/Timetable
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

            // 1. 获取当前登录的学生
            var currentUser = Session["User"] as Users;
            var student = db.Students.FirstOrDefault(s => s.UserID == currentUser.UserID);

            // 2. 获取该生所有已选课程的ID
            var enrolledCourseIds = db.StudentCourses
                                        .Where(sc => sc.StudentID == student.StudentID)
                                        .Select(sc => sc.CourseID)
                                        .ToList();

            // 3. 根据课程ID，从新表 ClassSessions 中查出所有相关的课程安排
            var classSessions = db.ClassSessions.Include("Courses")
                                  .Where(cs => enrolledCourseIds.Contains(cs.CourseID))
                                  .ToList();

            // 4. 将查询到的课表数据传递给视图
            return View(classSessions);
        }
        public ActionResult MyExams()
        {
            // 1. 从 Session 获取当前登录的用户信息
            var currentUser = Session["User"] as Users;
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // 2. 根据 Session 中的用户信息，找到对应的学生 (Student)
            var student = db.Students.FirstOrDefault(s => s.UserID == currentUser.UserID);

            if (student == null)
            {
                return View(new List<Exams>());
            }

            // 3. 后续逻辑保持不变
            var enrolledCourseIds = db.StudentCourses
                                        .Where(sc => sc.StudentID == student.StudentID)
                                        .Select(sc => sc.CourseID).ToList();
            var exams = db.Exams.Include("Courses")
                              .Where(e => enrolledCourseIds.Contains(e.CourseID))
                              .OrderBy(e => e.ExamTime).ToList();
            return View(exams);
        }

    }
}