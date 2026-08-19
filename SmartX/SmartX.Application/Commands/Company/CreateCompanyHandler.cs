using FluentValidation;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using SmartX.Shared.Models;

namespace SmartX.Application.Commands.Company;

public class CreateCompanyHandler
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IValidator<CreateCompanyCommand> _validator;

    public CreateCompanyHandler(
        ICompanyRepository companyRepository,
        IValidator<CreateCompanyCommand> validator)
    {
        _companyRepository = companyRepository;
        _validator = validator;
    }

    public async Task<Result<Guid>> HandleAsync(
        CreateCompanyCommand command,
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

        var company = new Domain.Entities.Company
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Description = command.Description,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _companyRepository.AddAsync(
            company,
            cancellationToken);

        return Result<Guid>.Ok(company.Id);
    }
}