using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace StudentInformationSystem.Controllers
{
    public class BaseController : Controller
    {
        // 这个方法在执行任何Action之前都会先被调用
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            // 检查Session中是否存有用户信息
            if (Session["User"] == null)
            {
                // 如果没有登录，就直接重定向到登录页面
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary
                    {
                        { "controller", "Account" },
                        { "action", "Login" }
                    });
            }
        }
    }
}

