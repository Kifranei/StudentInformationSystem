using StudentInformationSystem.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;

namespace StudentInformationSystem.Controllers
{
    // 继承 BaseController, 这样所有访问AdminController的请求都会先检查是否登录
    public class AdminController : BaseController
    {
        private StudentManagementDBEntities db = new StudentManagementDBEntities();

        // GET: Admin/Index
        public ActionResult Index()
        {
            // 从数据库中查询各项数据的总数
            ViewBag.StudentsCount = db.Students.Count();
            ViewBag.TeachersCount = db.Teachers.Count();
            ViewBag.CoursesCount = db.Courses.Count();
            ViewBag.EnrollmentsCount = db.StudentCourses.Count(); // 总选课人次

            // --- 获取服务器状态信息 ---
            var currentProcess = Process.GetCurrentProcess();
            ViewBag.ServerName = Environment.MachineName;
            ViewBag.ServerSoftware = Request.ServerVariables["SERVER_SOFTWARE"];
            ViewBag.DotNetVersion = Environment.Version.ToString();
            // 将字节转换为MB，并保留两位小数
            ViewBag.MemoryUsage = Math.Round(currentProcess.WorkingSet64 / 1024.0 / 1024.0, 2);
            ViewBag.StartTime = currentProcess.StartTime;
            ViewBag.RunningTime = DateTime.Now - currentProcess.StartTime;
            // --- 信息获取结束 ---

            return View();
        }

        // GET: Admin/StudentList
        // 增加一个可选的 searchString 参数
        public ActionResult StudentList(string searchString)
        {
            // 将当前的搜索词存入 ViewBag，以便在视图中回填到搜索框，实现“记住”搜索词的功能
            ViewBag.CurrentFilter = searchString;

            // 1. 从数据库中获取所有学生的基本查询
            //    注意这里我们先不调用 .ToList()，让查询可以被后续修改
            var students = from s in db.Students.Include("Classes")
                           select s;

            // 2. 判断搜索框是否输入了内容
            if (!String.IsNullOrEmpty(searchString))
            {
                // 3. 如果有内容，就在查询上附加一个 Where 条件
                //    这里我们实现对“学生姓名”或“学号”的模糊查询
                students = students.Where(s => s.StudentName.Contains(searchString)
                                               || s.StudentID.Contains(searchString));
            }

            // 4. 最后，执行查询并把结果传递给视图
            return View(students.ToList());
        }

        // GET: /Admin/AddStudent
        // 这个方法用于显示“添加新学生”的表单页面
        public ActionResult AddStudent(int? classId)
        {
            // 创建一个新的 view model，并设置默认的 ClassID
            var model = new Students
            {
                ClassID = classId
            };

            // 将下拉列表数据源存入 ViewBag
            ViewBag.ClassIDList = new SelectList(db.Classes, "ClassID", "ClassName");

            // 将预设好值的模型传递给视图
            return View(model);
        }

        // POST: /Admin/AddStudent
        // 这个方法用于接收并处理用户在表单中提交的数据
        [HttpPost]
        [ValidateAntiForgeryToken] // 防止跨站请求伪造攻击
        public ActionResult AddStudent(Students student)
        {
            // ModelState.IsValid 会检查提交的数据是否符合模型的基本验证规则
            if (ModelState.IsValid)
            {
                // --- 核心逻辑开始 ---
                // 添加一个新学生，需要同时在 Users 表和 Students 表中创建记录

                // 1. 创建登录用户 (Users)
                // 学号即为登录名，初始密码统一为 "Hzd@123456"
                Users newUser = new Users
                {
                    Username = student.StudentID,
                    Password = "Hzd@123456", // 修改默认密码为统一的 Hzd@123456
                    Role = 2 // 角色为学生
                };

                db.Users.Add(newUser);

                // 2. 创建学生信息 (Students)
                // 将刚刚创建的用户的UserID关联到新学生记录上
                // student.UserID = newUser.UserID; // EF 6 会自动处理导航属性的关联，这句可以不写
                student.Users = newUser;

                db.Students.Add(student);

                // 3. 一次性保存所有更改到数据库
                db.SaveChanges();

                // --- 核心逻辑结束 ---

                // 添加成功后，设置成功消息并重定向到学生列表页面
                TempData["Message"] = $"学生 {student.StudentName} 添加成功！默认密码为：Hzd@123456";
                return RedirectToAction("StudentList");
            }

            // 如果数据验证失败，则重新加载班级下拉列表，并返回表单页面让用户修改
            ViewBag.ClassID = new SelectList(db.Classes, "ClassID", "ClassName", student.ClassID);
            return View(student);
        }
        // GET: Admin/Edit/S2101001
        // 当点击“Edit”链接时，会带着学生的ID跳转到这里
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                // 如果没有提供ID，返回一个错误请求
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }
            // 根据ID从数据库中查找对应的学生记录
            Students student = db.Students.Find(id);
            if (student == null)
            {
                // 如果没有找到学生，返回“未找到”错误
                return HttpNotFound();
            }

            // 和“添加”页面一样，我们需要准备一个班级下拉列表
            // 第四个参数 student.ClassID 的作用是让下拉列表默认选中该学生当前的班级
            ViewBag.ClassID = new SelectList(db.Classes, "ClassID", "ClassName", student.ClassID);

            // 将找到的学生对象传递给视图
            return View(student);
        }

        // POST: Admin/Edit/S2101001
        // 当在编辑页面点击“保存”按钮时，会将表单数据提交到这里
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Students student)
        {
            if (ModelState.IsValid)
            {
                // 告知 Entity Framework，这个对象已被修改
                db.Entry(student).State = System.Data.Entity.EntityState.Modified;

                // 保存更改到数据库
                db.SaveChanges();

                // 编辑成功后，重定向回学生列表页面
                return RedirectToAction("StudentList");
            }

            // 如果模型验证失败，则重新加载班级下拉列表并返回编辑页面
            ViewBag.ClassID = new SelectList(db.Classes, "ClassID", "ClassName", student.ClassID);
            return View(student);
        }
        // GET: Admin/Details/S2101001
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }
            Students student = db.Students.Find(id);
            if (student == null)
            {
                return HttpNotFound();
            }
            // 直接将学生对象传递给视图进行展示
            return View(student);
        }
        // GET: Admin/Delete/S2101001
        // 显示删除确认页面
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }
            Students student = db.Students.Find(id);
            if (student == null)
            {
                return HttpNotFound();
            }
            return View(student);
        }

        // POST: Admin/Delete/S2101001
        // 用户点击“删除”按钮后，执行此方法
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            // --- 核心逻辑开始 ---
            // 删除学生，需要同时删除 Students 表和 Users 表中的关联记录

            // 1. 找到要删除的学生记录
            Students studentToDelete = db.Students.Find(id);

            // 2. 找到与该学生关联的登录用户记录
            Users userToDelete = db.Users.Find(studentToDelete.UserID);

            // 3. 从数据库中移除这两条记录
            db.Students.Remove(studentToDelete);
            if (userToDelete != null)
            {
                db.Users.Remove(userToDelete);
            }

            // 4. 保存更改
            db.SaveChanges();

            // --- 核心逻辑结束 ---

            return RedirectToAction("StudentList");
        }
        // GET: Admin/TeacherList
        public ActionResult TeacherList(string searchString)
        {
            ViewBag.CurrentFilter = searchString;

            var teachers = from t in db.Teachers.Include("Users")
                           select t;

            if (!String.IsNullOrEmpty(searchString))
            {
                teachers = teachers.Where(t => t.TeacherName.Contains(searchString)
                                           || t.TeacherID.Contains(searchString));
            }

            return View(teachers.ToList());
        }
        // GET: Admin/AddTeacher
        public ActionResult AddTeacher()
        {
            return View();
        }

        // POST: Admin/AddTeacher
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddTeacher(Teachers teacher)
        {
            if (ModelState.IsValid)
            {
                Users newUser = new Users
                {
                    Username = teacher.TeacherID,
                    Password = "Hzd@123456", // 修改默认密码为统一的 Hzd@123456
                    Role = 1 // 角色为教师
                };
                db.Users.Add(newUser);

                teacher.Users = newUser;
                db.Teachers.Add(teacher);

                db.SaveChanges();
                
                // 添加成功消息
                TempData["Message"] = $"教师 {teacher.TeacherName} 添加成功！默认密码为：Hzd@123456";
                return RedirectToAction("TeacherList");
            }
            return View(teacher);
        }

        // GET: Admin/EditTeacher/T001
        public ActionResult EditTeacher(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }
            Teachers teacher = db.Teachers.Find(id);
            if (teacher == null)
            {
                return HttpNotFound();
            }
            return View(teacher);
        }

        // POST: Admin/EditTeacher/T001
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditTeacher(Teachers teacher)
        {
            if (ModelState.IsValid)
            {
                db.Entry(teacher).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("TeacherList");
            }
            return View(teacher);
        }

        // GET: Admin/DetailsTeacher/T001
        public ActionResult DetailsTeacher(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }
            Teachers teacher = db.Teachers.Find(id);
            if (teacher == null)
            {
                return HttpNotFound();
            }
            return View(teacher);
        }

        // GET: Admin/DeleteTeacher/T001
        public ActionResult DeleteTeacher(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }
            Teachers teacher = db.Teachers.Find(id);
            if (teacher == null)
            {
                return HttpNotFound();
            }
            return View(teacher);
        }

        // POST: Admin/DeleteTeacher/T001
        [HttpPost, ActionName("DeleteTeacher")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteTeacherConfirmed(string id)
        {
            // 使用 .Include() 一次性把教师和其教授的课程都从数据库里查出来
            Teachers teacherToDelete = db.Teachers.Include("Courses").FirstOrDefault(t => t.TeacherID == id);

            if (teacherToDelete != null)
            {
                // 直接使用导航属性 teacherToDelete.Courses，无需额外查询
                foreach (var course in teacherToDelete.Courses.ToList()) // 使用 .ToList() 创建副本以安全修改
                {
                    course.TeacherID = null;
                }

                Users userToDelete = db.Users.Find(teacherToDelete.UserID);

                db.Teachers.Remove(teacherToDelete);
                if (userToDelete != null)
                {
                    db.Users.Remove(userToDelete);
                }
                db.SaveChanges();
            }

            return RedirectToAction("TeacherList");
        }
        public ActionResult CourseList(string searchString)
        {
            ViewBag.CurrentFilter = searchString;

            // 将查询语句的开头修改成和 StudentList 一样的格式
            var courses = from c in db.Courses.Include("Teachers")
                          select c;

            if (!String.IsNullOrEmpty(searchString))
            {
                courses = courses.Where(c => c.CourseName.Contains(searchString)
                                          || c.Teachers.TeacherName.Contains(searchString));
            }

            return View(courses.ToList());
        }
        // GET: Admin/AddCourse
        public ActionResult AddCourse()
        {
            // 准备教师下拉列表数据, "TeacherID"是值, "TeacherName"是显示的文本
            ViewBag.TeacherID = new SelectList(db.Teachers, "TeacherID", "TeacherName");
            ViewBag.CourseTypeList = GetCourseTypes();
            return View();
        }

        // POST: Admin/AddCourse
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddCourse(Courses course)
        {
            if (ModelState.IsValid)
            {
                db.Courses.Add(course);
                db.SaveChanges();
                return RedirectToAction("CourseList");
            }

            // 如果验证失败，需要重新加载教师列表
            ViewBag.TeacherID = new SelectList(db.Teachers, "TeacherID", "TeacherName", course.TeacherID);
            ViewBag.CourseTypeList = GetCourseTypes(course.CourseType);
            return View(course);
        }
        // GET: Admin/EditCourse/5
        public ActionResult EditCourse(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            Courses course = db.Courses.Find(id);
            if (course == null) return HttpNotFound();
            // 准备教师下拉列表，并选中当前课程的教师
            ViewBag.TeacherID = new SelectList(db.Teachers, "TeacherID", "TeacherName", course.TeacherID);
            ViewBag.CourseTypeList = GetCourseTypes(course.CourseType);
            return View(course);
        }

        // POST: Admin/EditCourse/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCourse(Courses course)
        {
            if (ModelState.IsValid)
            {
                db.Entry(course).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("CourseList");
            }
            ViewBag.TeacherID = new SelectList(db.Teachers, "TeacherID", "TeacherName", course.TeacherID);
            ViewBag.CourseTypeList = GetCourseTypes(course.CourseType);
            return View(course);
        }

        // GET: Admin/DetailsCourse/5
        public ActionResult DetailsCourse(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            Courses course = db.Courses.Find(id);
            if (course == null) return HttpNotFound();
            return View(course);
        }

        // GET: Admin/DeleteCourse/5
        public ActionResult DeleteCourse(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            Courses course = db.Courses.Find(id);
            if (course == null) return HttpNotFound();
            return View(course);
        }

        // POST: Admin/DeleteCourse/5
        [HttpPost, ActionName("DeleteCourse")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCourseConfirmed(int id)
        {
            // --- 检查课程是否已被选择 ---
            bool isEnrolled = db.StudentCourses.Any(sc => sc.CourseID == id);
            if (isEnrolled)
            {
                TempData["ErrorMessage"] = "删除失败！该课程已被学生选择，无法删除。";
                return RedirectToAction("CourseList");
            }
            // --- 检查结束 ---

            Courses course = db.Courses.Find(id);
            db.Courses.Remove(course);
            db.SaveChanges();
            TempData["Message"] = "课程删除成功！";
            return RedirectToAction("CourseList");
        }
        // GET: Admin/EnrollmentList
        public ActionResult EnrollmentList(string searchString)
        {
            ViewBag.CurrentFilter = searchString;

            // 1. 建立基础查询，并预加载学生和课程信息
            var enrollments = from e in db.StudentCourses.Include("Students").Include("Courses")
                              select e;

            // 2. 如果搜索框有内容，则增加筛选条件
            if (!String.IsNullOrEmpty(searchString))
            {
                enrollments = enrollments.Where(e =>
                    e.Students.StudentName.Contains(searchString) || // 按学生姓名搜
                    e.Students.StudentID.Contains(searchString) || // 按学号搜
                    e.Courses.CourseName.Contains(searchString)      // 按课程名搜
                );
            }

            // 3. 执行查询并返回结果
            return View(enrollments.ToList());
        }
        // GET: Admin/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        // POST: Admin/ChangePassword
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
        // POST: Admin/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(int userId)
        {
            var userToReset = db.Users.Find(userId);
            if (userToReset != null)
            {
                // 将密码重置为默认的 "Hzd@123456"
                userToReset.Password = "Hzd@123456";
                db.Entry(userToReset).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                // 使用 TempData 存储成功消息
                TempData["Message"] = $"用户 {userToReset.Username} 的密码已成功重置为 \"Hzd@123456\"。";
            }

            // 判断该用户是学生还是教师，以便跳转回对应的列表页面
            if (userToReset.Role == 2) // 学生
            {
                return RedirectToAction("StudentList");
            }
            else // 教师
            {
                return RedirectToAction("TeacherList");
            }
        }
        // --- 考试管理 ---

        // GET: Admin/ExamList
        public ActionResult ExamList(string searchString)
        {
            ViewBag.CurrentFilter = searchString;

            // 预加载课程信息
            var exams = from e in db.Exams.Include("Courses")
                        select e;

            if (!String.IsNullOrEmpty(searchString))
            {
                exams = exams.Where(e =>
                    e.Courses.CourseName.Contains(searchString) || // 按课程名搜索
                    e.Location.Contains(searchString)             // 按地点搜索
                );
            }

            return View(exams.ToList());
        }

        // GET: Admin/AddExam
        public ActionResult AddExam()
        {
            ViewBag.CourseID = new SelectList(db.Courses, "CourseID", "CourseName");
            return View();
        }

        // POST: Admin/AddExam
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddExam(Exams exam)
        {
            if (ModelState.IsValid)
            {
                db.Exams.Add(exam);
                db.SaveChanges();
                return RedirectToAction("ExamList");
            }
            ViewBag.CourseID = new SelectList(db.Courses, "CourseID", "CourseName", exam.CourseID);
            return View(exam);
        }
        // GET: Admin/EditExam/5
        public ActionResult EditExam(int? id)
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
            ViewBag.CourseID = new SelectList(db.Courses, "CourseID", "CourseName", exam.CourseID);
            return View(exam);
        }

        // 编辑考试记录
        // POST: Admin/EditExam/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditExam(Exams exam)
        {
            if (ModelState.IsValid)
            {
                db.Entry(exam).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("ExamList");
            }
            ViewBag.CourseID = new SelectList(db.Courses, "CourseID", "CourseName", exam.CourseID);
            return View(exam);
        }

        // 考试详情页面
        // GET: Admin/DetailsExam/5
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
            return View(exam);
        }

        // 删除考试记录
        // GET: Admin/DeleteExam/5
        public ActionResult DeleteExam(int? id)
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
            return View(exam);
        }

        // POST: Admin/DeleteExam/5
        [HttpPost, ActionName("DeleteExam")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteExamConfirmed(int id)
        {
            Exams exam = db.Exams.Find(id);
            db.Exams.Remove(exam);
            db.SaveChanges();
            return RedirectToAction("ExamList");
        }
        // --- 班级管理 (Class Management) ---

        // 1. 显示班级列表 (List)
        // GET: Admin/ClassList
        public ActionResult ClassList(string searchString)
        {
            ViewBag.CurrentFilter = searchString;

            var classes = from c in db.Classes
                          select c;

            if (!String.IsNullOrEmpty(searchString))
            {
                classes = classes.Where(c =>
                    c.Major.Contains(searchString) ||
                    c.ClassName.Contains(searchString) ||
                    c.AcademicYear.ToString().Contains(searchString)
                );
            }

            return View(classes.ToList());
        }

        // 2. 显示“添加班级”的表单页面 (Add GET)
        // GET: Admin/AddClass
        public ActionResult AddClass()
        {
            return View();
        }

        // 3. 处理提交的“添加班级”表单数据 (Add POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddClass(Classes classModel)
        {
            if (ModelState.IsValid)
            {
                // 自动根据专业、学年、班号生成完整的班级名称
                classModel.ClassName = $"{classModel.Major}{classModel.AcademicYear.Value.ToString().Substring(2, 2)}{classModel.ClassNumber.Value.ToString("D2")}班";
                db.Classes.Add(classModel);
                db.SaveChanges();
                return RedirectToAction("ClassList");
            }
            return View(classModel);
        }

        // 4. 显示“编辑班级”的表单页面 (Edit GET)
        // GET: Admin/EditClass/1
        public ActionResult EditClass(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            Classes classModel = db.Classes.Find(id);
            if (classModel == null) return HttpNotFound();
            return View(classModel);
        }

        // 5. 处理提交的“编辑班级”表单数据 (Edit POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditClass(Classes classModel)
        {
            if (ModelState.IsValid)
            {
                // 自动更新完整的班级名称
                classModel.ClassName = $"{classModel.Major}{classModel.AcademicYear.Value.ToString().Substring(2, 2)}{classModel.ClassNumber.Value.ToString("D2")}班";
                db.Entry(classModel).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("ClassList");
            }
            return View(classModel);
        }

        // 6. 显示“删除班级”的确认页面 (Delete GET)
        // GET: Admin/DeleteClass/1
        public ActionResult DeleteClass(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            Classes classModel = db.Classes.Find(id);
            if (classModel == null) return HttpNotFound();
            return View(classModel);
        }

        // 7. 处理“删除班级”的确认操作 (Delete POST)
        [HttpPost, ActionName("DeleteClass")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteClassConfirmed(int id)
        {
            // --- 检查班级下是否有学生 ---
            bool hasStudents = db.Students.Any(s => s.ClassID == id);
            if (hasStudents)
            {
                // 如果有学生，则不允许删除，并给出提示
                TempData["ErrorMessage"] = "删除失败！该班级下仍有学生，请先转移或删除学生。";
                return RedirectToAction("ClassList");
            }
            // --- 检查结束 ---

            Classes classModel = db.Classes.Find(id);
            db.Classes.Remove(classModel);
            db.SaveChanges();
            TempData["Message"] = "班级删除成功！";
            return RedirectToAction("ClassList");
        }

        // 8. 显示班级详情和学生名单 (Details GET)
        // GET: Admin/ClassDetails/1
        public ActionResult ClassDetails(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }
            var viewModel = new ClassDetailsViewModel
            {
                ClassInfo = db.Classes.Find(id)
            };
            if (viewModel.ClassInfo == null)
            {
                return HttpNotFound();
            }
            viewModel.StudentsInClass = db.Students
                                          .Where(s => s.ClassID == id)
                                          .ToList();
            return View(viewModel);
        }

        private SelectList GetCourseTypes(int? selectedValue = null)
        {
            var courseTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "专业必修" },
                new SelectListItem { Value = "2", Text = "公共必修" },
                new SelectListItem { Value = "3", Text = "专业选修" },
                new SelectListItem { Value = "4", Text = "公共选修" },
                new SelectListItem { Value = "5", Text = "体育选修" }
            };
            return new SelectList(courseTypes, "Value", "Text", selectedValue);
        }

        // --- 课程安排管理 (Class Sessions Management) ---

        // GET: Admin/CourseSchedule?courseId=5
        // 查看指定课程的所有课程安排
        public ActionResult CourseSchedule(int courseId)
        {
            var course = db.Courses.Include("Teachers").FirstOrDefault(c => c.CourseID == courseId);
            if (course == null)
            {
                return HttpNotFound();
            }

            ViewBag.Course = course;

            // 获取该课程的所有课程安排，并按时间排序
            var classSessions = db.ClassSessions.Include("Courses")
                                  .Where(cs => cs.CourseID == courseId)
                                  .OrderBy(cs => cs.StartWeek)
                                  .ThenBy(cs => cs.DayOfWeek)
                                  .ThenBy(cs => cs.StartPeriod)
                                  .ToList();

            return View(classSessions);
        }

        // GET: Admin/AddCourseSchedule?courseId=5
        // 为指定课程添加课程安排
        public ActionResult AddCourseSchedule(int courseId)
        {
            var course = db.Courses.Find(courseId);
            if (course == null)
            {
                return HttpNotFound();
            }

            ViewBag.Course = course;

            var session = new ClassSessions
            {
                CourseID = courseId
            };

            return View(session);
        }

        // POST: Admin/AddCourseSchedule
        // 处理添加课程安排的表单提交
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddCourseSchedule(ClassSessions session)
        {
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

            // 检查该课程是否有教师
            var course = db.Courses.Find(session.CourseID);
            if (course?.TeacherID == null)
            {
                ModelState.AddModelError("", "该课程尚未分配教师，无法添加课程安排。请先为课程分配教师。");
            }
            else
            {
                // 检查时间冲突 - 同一教师在相同时间段的其他课程安排
                var conflictingSessions = db.ClassSessions.Include("Courses")
                    .Where(cs => cs.Courses.TeacherID == course.TeacherID && // 同一教师
                               cs.DayOfWeek == session.DayOfWeek && // 同一天
                               !(session.EndWeek < cs.StartWeek || session.StartWeek > cs.EndWeek) && // 周次有重叠
                               !(session.EndPeriod < cs.StartPeriod || session.StartPeriod > cs.EndPeriod)) // 节次有重叠
                    .ToList();

                if (conflictingSessions.Any())
                {
                    var conflictDescription = string.Join("; ", conflictingSessions.Select(cs => 
                        $"{cs.Courses.CourseName}(第{cs.StartWeek}-{cs.EndWeek}周, 第{cs.StartPeriod}-{cs.EndPeriod}节)"));
                    ModelState.AddModelError("", $"时间冲突！该教师在此时间段已有以下课程安排：{conflictDescription}");
                }
            }

            if (ModelState.IsValid)
            {
                db.ClassSessions.Add(session);
                db.SaveChanges();
                
                TempData["SuccessMessage"] = $"课程安排添加成功！{course.CourseName} - 第{session.StartWeek}-{session.EndWeek}周，星期{GetDayNameForAdmin(session.DayOfWeek)}第{session.StartPeriod}-{session.EndPeriod}节，{session.Classroom}教室。";
                return RedirectToAction("CourseSchedule", new { courseId = session.CourseID });
            }

            // 如果验证失败，重新加载课程信息
            ViewBag.Course = course;
            return View(session);
        }

        // GET: Admin/EditCourseSchedule/5
        // 编辑课程安排
        public ActionResult EditCourseSchedule(int sessionId)
        {
            var sessionToEdit = db.ClassSessions.Include("Courses").FirstOrDefault(cs => cs.SessionID == sessionId);
            if (sessionToEdit == null)
            {
                return HttpNotFound();
            }

            ViewBag.Course = sessionToEdit.Courses;
            return View(sessionToEdit);
        }

        // POST: Admin/EditCourseSchedule/5
        // 处理编辑课程安排的表单提交
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCourseSchedule(ClassSessions session)
        {
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

            var course = db.Courses.Find(session.CourseID);
            if (course?.TeacherID != null)
            {
                // 检查时间冲突 - 排除当前正在编辑的安排
                var conflictingSessions = db.ClassSessions.Include("Courses")
                    .Where(cs => cs.SessionID != session.SessionID && // 排除当前编辑的安排
                               cs.Courses.TeacherID == course.TeacherID && // 同一教师
                               cs.DayOfWeek == session.DayOfWeek && // 同一天
                               !(session.EndWeek < cs.StartWeek || session.StartWeek > cs.EndWeek) && // 周次有重叠
                               !(session.EndPeriod < cs.StartPeriod || session.StartPeriod > cs.EndPeriod)) // 节次有重叠
                    .ToList();

                if (conflictingSessions.Any())
                {
                    var conflictDescription = string.Join("; ", conflictingSessions.Select(cs => 
                        $"{cs.Courses.CourseName}(第{cs.StartWeek}-{cs.EndWeek}周, 第{cs.StartPeriod}-{cs.EndPeriod}节)"));
                    ModelState.AddModelError("", $"时间冲突！该教师在此时间段已有以下课程安排：{conflictDescription}");
                }
            }

            if (ModelState.IsValid)
            {
                db.Entry(session).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                
                TempData["SuccessMessage"] = $"课程安排修改成功！{course.CourseName} 已调整为：第{session.StartWeek}-{session.EndWeek}周，星期{GetDayNameForAdmin(session.DayOfWeek)}第{session.StartPeriod}-{session.EndPeriod}节，{session.Classroom}教室。";
                return RedirectToAction("CourseSchedule", new { courseId = session.CourseID });
            }

            // 如果验证失败，重新加载课程信息
            ViewBag.Course = course;
            return View(session);
        }

        // GET: Admin/DeleteCourseSchedule/5
        // 删除课程安排确认页面
        public ActionResult DeleteCourseSchedule(int sessionId)
        {
            var sessionToDelete = db.ClassSessions.Include("Courses").FirstOrDefault(cs => cs.SessionID == sessionId);
            if (sessionToDelete == null)
            {
                return HttpNotFound();
            }

            return View(sessionToDelete);
        }

        // POST: Admin/DeleteCourseSchedule/5
        // 确认删除课程安排
        [HttpPost, ActionName("DeleteCourseSchedule")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCourseScheduleConfirmed(int sessionId)
        {
            var sessionToDelete = db.ClassSessions.Include("Courses").FirstOrDefault(cs => cs.SessionID == sessionId);
            
            if (sessionToDelete != null)
            {
                var courseId = sessionToDelete.CourseID;
                var courseName = sessionToDelete.Courses.CourseName;
                var scheduleInfo = $"第{sessionToDelete.StartWeek}-{sessionToDelete.EndWeek}周，星期{GetDayNameForAdmin(sessionToDelete.DayOfWeek)}第{sessionToDelete.StartPeriod}-{sessionToDelete.EndPeriod}节，{sessionToDelete.Classroom}教室";
                
                db.ClassSessions.Remove(sessionToDelete);
                db.SaveChanges();
                
                TempData["SuccessMessage"] = $"课程安排删除成功！已删除 {courseName} 的安排：{scheduleInfo}";
                return RedirectToAction("CourseSchedule", new { courseId });
            }

            return RedirectToAction("CourseList");
        }

        // 辅助方法：将数字转换为星期名称
        private string GetDayNameForAdmin(int dayOfWeek)
        {
            string[] days = { "", "一", "二", "三", "四", "五", "六", "日" };
            return dayOfWeek >= 1 && dayOfWeek <= 7 ? days[dayOfWeek] : "未知";
        }

    }
}