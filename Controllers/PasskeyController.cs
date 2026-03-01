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
        private StudentManagementDBEntities db = new StudentManagementDBEntities();

        public PasskeyController()
        {
            // 初始化 Fido2 配置
            var config = new Fido2Configuration()
            {
                ServerDomain = "localhost", // 开发环境为本地 localhost
                ServerName = "学生信息管理系统",
                // 本地 IIS Express SSL 端口
                Origin = "https://localhost:44332",
                TimestampDriftTolerance = 300000
            };
            _lib = new Fido2(config);
        }

        /// <summary>
        /// 1. 获取注册凭证的选项 (返回 Challenge 等参数给前端)
        /// </summary>
        [HttpPost]
        public ActionResult MakeCredentialOptions()
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
        public async Task<ActionResult> MakeCredential(string name)
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
        // 2. 获取当前用户的通行密钥列表
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
        // 3. 删除指定的通行密钥
        // ==========================================
        [HttpPost]
        public JsonResult DeletePasskey(int id)
        {
            var loggedInUser = Session["User"] as Users;
            if (loggedInUser == null) return Json(new { status = "error" });

            // 加上 UserId 的条件，防止越权删除别人的密钥（管理员也无权删除其他用户的密钥）
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