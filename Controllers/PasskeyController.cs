using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Fido2NetLib;
using Fido2NetLib.Objects;
using StudentInformationSystem.Models;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace StudentInformationSystem.Controllers
{
    public class PasskeyController : Controller
    {
        private readonly IFido2 _lib;
        // 这里的 StudentManagementDBEntities 请替换为你实际的 EF DbContext 名字
        private StudentManagementDBEntities db = new StudentManagementDBEntities();

        public PasskeyController()
        {
            // 初始化 Fido2 配置
            var config = new Fido2Configuration()
            {
                ServerDomain = "localhost", // 开发环境填 localhost
                ServerName = "学生信息管理系统",
                // 注意：这里的端口号 44332 是从你项目 csproj 里读取的 IIS Express SSL 端口
                Origin = "https://localhost:44332",
                TimestampDriftTolerance = 300000
            };
            _lib = new Fido2(config);
        }

        /// <summary>
        /// 1. 获取注册凭证的选项 (返回 Challenge 等参数给前端)
        /// </summary>
        [HttpPost]
        public ActionResult MakeCredentialOptions() // 注意：这里从 JsonResult 改成了 ActionResult
        {
            try
            {
                var loggedInUser = Session["User"] as Users;
                if (loggedInUser == null)
                {
                    return Json(new { status = "error", errorMessage = "用户未登录！" });
                }

                var userBytes = BitConverter.GetBytes(loggedInUser.UserID);
                var user = new Fido2User
                {
                    Id = userBytes,
                    Name = loggedInUser.Username,
                    DisplayName = loggedInUser.Username
                };

                var existingKeys = db.Passkeys
                    .Where(p => p.UserId == loggedInUser.UserID)
                    .Select(p => p.CredentialId)
                    .ToList();
                var exKeys = existingKeys.Select(c => new PublicKeyCredentialDescriptor(c)).ToList();

                var authenticatorSelection = new AuthenticatorSelection
                {
                    RequireResidentKey = true,
                    UserVerification = UserVerificationRequirement.Preferred
                };

                var options = _lib.RequestNewCredential(user, exKeys, authenticatorSelection, AttestationConveyancePreference.None);
                Session["fido2.attestationOptions"] = options.ToJson();

                // 【核心修改1】：不使用 MVC 的 Json()，直接返回 Fido2 标准格式的纯字符串！
                return Content(options.ToJson(), "application/json");
            }
            catch (Exception e)
            {
                return Json(new { status = "error", errorMessage = e.Message });
            }
        }

        // ==========================================
        // 1. 修改原有的 MakeCredential 方法，接收 name 参数
        // ==========================================
        [HttpPost]
        public async Task<ActionResult> MakeCredential(string name) // <-- 这里加了 string name 参数
        {
            try
            {
                Request.InputStream.Position = 0;
                string json;
                using (var reader = new System.IO.StreamReader(Request.InputStream))
                {
                    json = reader.ReadToEnd();
                }

                var attestationResponse = JsonConvert.DeserializeObject<AuthenticatorAttestationRawResponse>(json);
                var optionsJson = Session["fido2.attestationOptions"] as string;
                var options = CredentialCreateOptions.FromJson(optionsJson);

                IsCredentialIdUniqueToUserAsyncDelegate callback = async (args) =>
                {
                    var usersCount = db.Passkeys.Count(p => p.CredentialId == args.CredentialId);
                    return await Task.FromResult(usersCount == 0);
                };

                var success = await _lib.MakeNewCredentialAsync(attestationResponse, options, callback);

                var loggedInUser = Session["User"] as Users;
                var newPasskey = new Passkeys
                {
                    UserId = loggedInUser.UserID,
                    CredentialId = success.Result.CredentialId,
                    PublicKey = success.Result.PublicKey,
                    UserHandle = success.Result.User.Id,
                    SignatureCounter = success.Result.Counter,
                    CredType = success.Result.CredType,
                    RegDate = DateTime.Now,
                    AaGuid = success.Result.Aaguid,
                    // 如果前端没传名字，给个默认值
                    Name = string.IsNullOrWhiteSpace(name) ? "未命名设备" : name
                };

                db.Passkeys.Add(newPasskey);
                db.SaveChanges();

                return Json(new { status = "ok" });
            }
            catch (Exception e)
            {
                return Json(new { status = "error", errorMessage = e.Message });
            }
        }

        // ==========================================
        // 2. 新增：获取当前用户的通行密钥列表
        // ==========================================
        [HttpGet]
        public JsonResult GetUserPasskeys()
        {
            var loggedInUser = Session["User"] as Users;
            if (loggedInUser == null) return Json(new { status = "error", errorMessage = "未登录" }, JsonRequestBehavior.AllowGet);

            // 格式化数据，避免 MVC 默认的 JSON 序列化把时间变成奇怪的格式
            var list = db.Passkeys
                .Where(p => p.UserId == loggedInUser.UserID)
                .ToList() // 先查到内存里
                .Select(p => new {
                    Id = p.Id,
                    Name = p.Name,
                    RegDate = p.RegDate.ToString("yyyy-MM-dd HH:mm:ss")
                });

            return Json(new { status = "ok", data = list }, JsonRequestBehavior.AllowGet);
        }

        // ==========================================
        // 3. 新增：删除指定的通行密钥
        // ==========================================
        [HttpPost]
        public JsonResult DeletePasskey(int id)
        {
            var loggedInUser = Session["User"] as Users;
            if (loggedInUser == null) return Json(new { status = "error" });

            // 加上 UserId 的条件，防止越权删除别人的密钥（管理员如果需要删除别人的，可以另写一个接口）
            var pk = db.Passkeys.FirstOrDefault(p => p.Id == id && p.UserId == loggedInUser.UserID);
            if (pk != null)
            {
                db.Passkeys.Remove(pk);
                db.SaveChanges();
                return Json(new { status = "ok" });
            }
            return Json(new { status = "error", errorMessage = "找不到该密钥或无权删除" });
        }

        // ==========================================
        // 4. 获取登录用的 Challenge (AssertionOptions)
        // 【修复1】：将方法重命名为 GetAssertionOptions，避免与 Fido2 类名冲突
        // ==========================================
        [HttpPost]
        public ActionResult GetAssertionOptions()
        {
            try
            {
                var options = _lib.GetAssertionOptions(
                    new List<PublicKeyCredentialDescriptor>(),
                    UserVerificationRequirement.Preferred
                );

                Session["fido2.assertionOptions"] = options.ToJson();
                return Content(options.ToJson(), "application/json");
            }
            catch (Exception e)
            {
                return Json(new { status = "error", errorMessage = e.Message });
            }
        }

        // ==========================================
        // 5. 验证登录授权并真正执行登录操作
        // ==========================================
        [HttpPost]
        public async Task<ActionResult> MakeAssertion()
        {
            try
            {
                Request.InputStream.Position = 0;
                string json;
                using (var reader = new System.IO.StreamReader(Request.InputStream))
                {
                    json = reader.ReadToEnd();
                }

                var assertionResponse = JsonConvert.DeserializeObject<AuthenticatorAssertionRawResponse>(json);
                var optionsJson = Session["fido2.assertionOptions"] as string;

                // 【修复1】：强制指定使用 Fido2NetLib 命名空间下的类
                var options = Fido2NetLib.AssertionOptions.FromJson(optionsJson);

                var creds = db.Passkeys.Where(p => p.CredentialId == assertionResponse.Id).ToList();
                if (!creds.Any())
                {
                    return Json(new { status = "error", errorMessage = "未找到该通行密钥，请确认是否已在当前系统绑定。" });
                }

                var passkey = creds.First();

                var user = db.Users.FirstOrDefault(u => u.UserID == passkey.UserId);
                if (user == null)
                {
                    return Json(new { status = "error", errorMessage = "通行密钥关联的用户已不存在。" });
                }

                // 【修复2】：2.0.2 版本的委托没有 Delegate 后缀
                IsUserHandleOwnerOfCredentialIdAsync callback = async (args) =>
                {
                    var storedCreds = await Task.FromResult(db.Passkeys.Where(p => p.CredentialId == args.CredentialId).ToList());
                    return storedCreds.Any(c => c.UserHandle.SequenceEqual(args.UserHandle));
                };

                var success = await _lib.MakeAssertionAsync(
                    assertionResponse,
                    options,
                    passkey.PublicKey,
                    (uint)passkey.SignatureCounter,
                    callback
                );

                passkey.SignatureCounter = success.Counter;
                db.SaveChanges();

                // ==============================================
                // 8. 授权登录！(与你的 AccountController 保持一致)
                // ==============================================
                Session["User"] = user;

                System.Web.Security.FormsAuthentication.SetAuthCookie(user.Username, true);

                // 【修复3】：使用你系统原生的 int 类型角色判断
                string redirectUrl = "/Student/Index";
                if (user.Role == 0) redirectUrl = "/Admin/Index";
                else if (user.Role == 1) redirectUrl = "/Teacher/Index";

                return Json(new { status = "ok", redirectUrl = redirectUrl });
            }
            catch (Exception e)
            {
                return Json(new { status = "error", errorMessage = "验证失败: " + e.Message });
            }
        }
    }
}