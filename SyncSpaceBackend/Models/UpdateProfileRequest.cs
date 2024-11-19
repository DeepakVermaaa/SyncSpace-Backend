using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SyncSpaceBackend.Models
{
    public class UpdateProfileRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
        public string CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        public IFormFile? ProfilePicture { get; set; }
    }
}