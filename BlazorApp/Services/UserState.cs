namespace BlazorApp.Services
{
    public class UserState
    {
        public string? Email { get; private set; }
        public string? Role { get; private set; }
        public bool IsLoggedIn => !string.IsNullOrEmpty(Email);

        public void SetUser(string email, string role)
        {
            Email = email;
            Role = role;
        }

        public void ClearUser()
        {
            Email = null;
            Role = null;
        }
    }
}