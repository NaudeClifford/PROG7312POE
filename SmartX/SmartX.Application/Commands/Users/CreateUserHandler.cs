using FluentValidation;
using SmartX.Application.Commands;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using SmartX.Shared.Models;

namespace SmartX.Application.Commands.Users
{
    public class CreateUserHandler
    {

        private readonly IUserRepository _userRepository;
        private readonly IValidator<CreateUserCommand> _validator;

        public CreateUserHandler(
            IUserRepository userRepository,
            IValidator<CreateUserCommand> validator)
        {
            _userRepository = userRepository;
            _validator = validator;
        }

        public async Task<Result<Guid>> HandleAsync(
            CreateUserCommand command,
            CancellationToken cancellationToken = default)
        {
            var validationResult = await _validator.ValidateAsync(
                command, cancellationToken);

            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult
                    .Errors.Select(x => x.ErrorMessage));

                return Result<Guid>.Fail(errors);
            }
            var now = DateTime.UtcNow;

            var user = new User
            {
                Id = Guid.NewGuid(),
                CompanyId = command.CompanyId,
                FirebaseUid = command.FirebaseUid,
                Email = command.Email,
                DisplayName = command.DisplayName,
                Role = command.Role,
                IsActive = command.IsActive,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _userRepository.AddAsync(
                user,
                cancellationToken);

            return Result<Guid>.Ok(user.Id);
        }
    }
}
