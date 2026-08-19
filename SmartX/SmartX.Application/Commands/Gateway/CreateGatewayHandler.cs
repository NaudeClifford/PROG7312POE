using FluentValidation;
using SmartX.Domain.Interfaces;
using SmartX.Shared.Models;
using SmartX.Domain.Entities;


namespace SmartX.Application.Commands.Gateway;

public class CreateGatewayHandler
{
    private readonly IGatewayRepository _gatewayRepository;
    private readonly IValidator<CreateGatewayCommand> _validator;

    public CreateGatewayHandler(
        IGatewayRepository gatewayRepository,
        IValidator<CreateGatewayCommand> validator)
    {
        _gatewayRepository = gatewayRepository;
        _validator = validator;
    }

    public async Task<Result<Guid>> HandleAsync(
        CreateGatewayCommand command,
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

        var gateway = new Gateway
        {
            Id = Guid.NewGuid(),
            CompanyId = command.CompanyId

        };

        await _gatewayRepository.AddAsync(
            gateway,
            cancellationToken);

        return Result<Guid>.Ok(gateway.Id);
    }
}