using StudentInformationSystem.Models;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace StudentInformationSystem.Controllers
{
    // 继承 BaseController 确保只有登录学生才能访问
    public class StudentController : BaseController
    {
        private Entities db = new Entities(); // 你的数据库上下文

        // GET: Student/Index
        // 学生登录后的主页，即“我的成绩”页面
        public ActionResult Index()
        {
            // 1. 从 Session 获取当前登录的用户信息
            var currentUser = Session["User"] as Users;

            // 2. 根据用户信息，找到对应的学生ID (StudentID)
            var student = db.Students.FirstOrDefault(s => s.UserID == currentUser.UserID);

            if (student == null)
            {
                // 如果在学生表中找不到记录，可以返回错误页
                return View("Error");
            }

            // 3. 使用学生ID，在 StudentCourses 表中查找该生的所有选课记录
            //    .Include("Courses") 会同时把每条记录关联的课程信息也加载进来
            var enrollments = db.StudentCourses.Include("Courses")
                                .Where(sc => sc.StudentID == student.StudentID).ToList();

            // 4. 将该生的所有选课记录传递给视图
            return View(enrollments);
        }
        // GET: Student/CourseSelection
        // 显示所有可选课程的列表
        public ActionResult CourseSelection()
        {
            // 1. 获取当前登录的学生信息
            var currentUser = Session["User"] as Users;
            var student = db.Students.FirstOrDefault(s => s.UserID == currentUser.UserID);

            // 2. 获取该生已经选了的所有课程的ID列表
            var enrolledCourseIds = db.StudentCourses
                                        .Where(sc => sc.StudentID == student.StudentID)
                                        .Select(sc => sc.CourseID)
                                        .ToList();

            // 3. 将这个ID列表存入 ViewBag，方便视图进行判断
            ViewBag.EnrolledCourseIDs = enrolledCourseIds;

            // 4. 获取系统中的所有课程，并传递给视图作为主模型
            var allCourses = db.Courses.Include("Teachers").ToList();
            return View(allCourses);
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