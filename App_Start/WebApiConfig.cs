using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace StudentInformationSystem
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API 配置和服务

            // ## 关键：启用特性路由 ##
            // 确保这一行存在且没有被注释。它负责扫描你项目中所有的 [Route] 和 [RoutePrefix] 特性。
            config.MapHttpAttributeRoutes();

            // 这是默认的路由规则，可以保留作为备用
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}