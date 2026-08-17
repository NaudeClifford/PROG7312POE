using FluentValidation;
using SmartX.Application.Commands.Sensors;
using SmartX.Domain.Interfaces;
using SmartX.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Application.Commands.Users
{
    public class UpdateUserHandler
    {
        private readonly IUserRepository _userRepository;
        private readonly IValidator<UpdateUserCommand> _validator;

        public UpdateUserHandler(
            IUserRepository userRepository,
            IValidator<UpdateUserCommand> validator)
        {
            _userRepository = userRepository;
            _validator = validator;
        }

        public async Task<Result<bool>> HandleAsync(
            UpdateUserCommand command,
            CancellationToken cancellationToken = default)
        {
            var validationResult = await _validator.ValidateAsync(
                command,
                cancellationToken);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(
                    "; ",
                    validationResult.Errors.Select(x => x.ErrorMessage));

                return Result<bool>.Fail(errors);
            }

            var user = await _userRepository.GetByIdAsync(
                command.Id,
                cancellationToken);

            if (user is null) return Result<bool>.Fail("User not found.");

            user.FirebaseUid = command.FirebaseUid;
            user.DisplayName = command.DisplayName;
            user.Email = command.Email;
            user.Role = command.Role;
            user.IsActive = command.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(
                user,
                cancellationToken);

            return Result<bool>.Ok(true);
        }
    }
}
