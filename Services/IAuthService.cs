using DietManagementWebAPI.Models;
using DietManagementWebAPI.Models.Auth;
using DietManagementWebAPI.Models.DBModels;

namespace YourProject.Services
{
    public interface IAuthService
    {
        //Task<AuthResponse> RegisterAsync(RegisterAuth registerData);
        Task<bool> EmailExistsAsync(string email);
        Task<UsersDBModel?> GetUserByEmailAsync(string email);
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public UsersDBModel? User { get; set; }
    }
}
