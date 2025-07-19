using System.ComponentModel.DataAnnotations;

namespace StudentInformationSystem.Models
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "旧密码不能为空。")]
        [DataType(DataType.Password)]
        [Display(Name = "旧密码")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "新密码不能为空。")]
        [StringLength(100, ErrorMessage = "{0} 的长度至少必须为 {2} 个字符。", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "新密码")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "确认新密码")]
        [Compare("NewPassword", ErrorMessage = "新密码和确认密码不匹配。")]
        public string ConfirmPassword { get; set; }
    }
}