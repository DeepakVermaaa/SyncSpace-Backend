namespace SyncSpaceBackend.Models
{
    public class GoogleAuthRequest
    {
        public string? IdToken { get; set; }
        public bool IsSignUp { get; set; }
    }
}
