namespace Studiobook_backend.Services.Interfaces
{
    public interface IPasswordHasherService
    {
        bool Verify(string hashedPassword, string inputPassword);
        string Hash(string password);
    }
}
