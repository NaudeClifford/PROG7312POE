namespace SmartX.Application.Authentication
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResult> SignInAsync(
            string email,
            string password);
    }
}