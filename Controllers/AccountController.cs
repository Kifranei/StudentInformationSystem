using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using StudentInformationSystem.Models; // 引入模型命名空间


namespace StudentInformationSystem.Controllers
{
    public class AccountController : Controller
    {
        // 实例化数据库上下文，用于操作数据库
        private StudentManagementDBEntities db = new StudentManagementDBEntities();

        // 1. GET请求: 显示登录页面
        // 当用户直接访问 /Account/Login 时，执行此方法
        public ActionResult Login()
        {
            return View();
        }

        // 2. POST请求: 处理用户提交的登录表单
        // 当用户在登录页面点击“登录”按钮时，执行此方法
        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            // 使用LINQ在Users表中查找匹配的用户名和密码
            var user = db.Users.FirstOrDefault(u => u.Username == username && u.Password == password);

            // 如果找到了用户
            if (user != null)
            {
                // 使用Session来记录用户的登录状态
                Session["User"] = user;

                // 根据角色判断跳转到哪里
                if (user.Role == 0) // 管理员
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if (user.Role == 1) // 教师
                {
                    // 跳转到教师控制器的主页
                    return RedirectToAction("Index", "Teacher");
                }
                else // 学生
                {
                    // 跳转到学生控制器的主页
                    return RedirectToAction("Index", "Student");
                }
            }
            else // 如果没找到用户
            {
                // 在页面上显示错误提示
                ViewBag.ErrorMessage = "用户名或密码错误！";
                // 返回登录页面，让用户重新输入
                return View();
            }
        }

        // 3. 注销功能
        public ActionResult Logout()
        {
            // 清空Session，实现用户退出
            Session.Clear();
            // 跳转回登录页面
            return RedirectToAction("Login", "Account");
        }
    }
}