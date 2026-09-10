
namespace SmartX.Domain.Interfaces
{
    public interface IFirebaseUserService
    {
        Task DeleteUserAsync(
            string firebaseUid,
            CancellationToken cancellationToken = default);
    }

}
