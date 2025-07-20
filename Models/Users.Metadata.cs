using System;
using System.ComponentModel.DataAnnotations;

namespace StudentInformationSystem.Models
{
    public class UsersMetadata
    {
        [Display(Name = "登录名")]
        public string Username { get; set; }

    }

    [MetadataType(typeof(UsersMetadata))]
    public partial class Users
    {
    }
}