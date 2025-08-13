namespace envoy_attendance.Services
{
    public interface IPasswordService
    {
        string HashPassword(string password);
        bool VerifyPassword(string providedPassword, string storedPassword);
    }
}