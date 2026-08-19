using FluentValidation;
using SmartX.Shared.Models;
using SmartX.Domain.Interfaces;

namespace SmartX.Application.Commands.Gateway;

public class UpdateGatewayHandler
{
    private readonly IGatewayRepository _gatewayRepository;
    private readonly IValidator<UpdateGatewayCommand> _validator;

    public UpdateGatewayHandler(
        IGatewayRepository gatewayRepository,
        IValidator<UpdateGatewayCommand> validator)
    {
        _gatewayRepository = gatewayRepository;
        _validator = validator;
    }

    public async Task<Result<bool>> HandleAsync(
        UpdateGatewayCommand command,
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

        var gateway = await _gatewayRepository.GetByIdAsync(
            command.Id,
            cancellationToken);

        if (gateway is null)
        {
            return Result<bool>.Fail("gateway not found.");
        }

        gateway.Name = command.Name;
        gateway.IpAddress = command.IpAddress;  
        gateway.UpdatedAt = command.UpdatedAt;
        gateway.CreatedAt = command.CreatedAt;
        gateway.Description = command.Description;
        gateway.CompanyId = command.CompanyId;
        gateway.SerialNumber = command.SerialNumber;

        gateway.UpdatedAt = DateTime.UtcNow;

        await _gatewayRepository.UpdateAsync(
            gateway,
            cancellationToken);

        return Result<bool>.Ok(true);
    }
}