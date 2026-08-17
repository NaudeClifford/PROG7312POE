using SmartX.Domain.Interfaces;
using SmartX.Shared.Models;

namespace SmartX.Application.Commands.Users
{
    public class DeleteUserHandler
    {
        private readonly IUserRepository _userRepository;

        public DeleteUserHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<bool>> HandleAsync(
            DeleteUserCommand command,
            CancellationToken cancellationToken = default)
        {

            if (command.UserId == Guid.Empty)
            {
                return Result<bool>.Fail("User ID is required.");
            }

            var user = await _userRepository.GetByIdAsync(
                command.UserId,
                cancellationToken);

            if (user is null)
            {
                return Result<bool>.Fail("User not found.");
            }

            await _userRepository.DeleteAsync(
                command.UserId,
                cancellationToken);

            return Result<bool>.Ok(true);
        }
    }
}
