using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace StudentInformationSystem.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            // 将其重定向到 AccountController 的 Login 方法
            return RedirectToAction("Login", "Account");
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}