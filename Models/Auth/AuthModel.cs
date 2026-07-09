using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DietManagementWebAPI.Models.Auth
{
    public class LoginAuth
    {
        [Required]
        public string email { get; set; }
        [Required]
        public string password { get; set; }
    }

    public class RegisterAuth
    {
        [Required]
        public string firstName { get; set; }
        [Required]
        public string lastName { get; set; }
        [Required]
        public string email { get; set; }
        [Required]
        public string password { get; set; }
    }
}
