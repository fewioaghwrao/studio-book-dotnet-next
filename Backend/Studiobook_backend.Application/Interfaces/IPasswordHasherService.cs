namespace Studiobook_backend.Application.Interfaces
{
    public interface IPasswordHasherService
    {
        bool Verify(string hashedPassword, string inputPassword);
        string Hash(string password);
    }
}
