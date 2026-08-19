using FluentValidation;
using SmartX.Domain.Interfaces;
using SmartX.Shared.Models;

namespace SmartX.Application.Commands.Company;

public class UpdateCompanyHandler
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IValidator<UpdateCompanyCommand> _validator;

    public UpdateCompanyHandler(
        ICompanyRepository companyRepository,
        IValidator<UpdateCompanyCommand> validator)
    {
        _companyRepository = companyRepository;
        _validator = validator;
    }

    public async Task<Result<bool>> HandleAsync(
        UpdateCompanyCommand command,
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

        var company = await _companyRepository.GetByIdAsync(
            command.Id,
            cancellationToken);

        if (company is null)
        {
            return Result<bool>.Fail("Company not found.");
        }

        company.Name = command.Name;
        company.Description = command.Description;
        company.IsActive = command.IsActive;

        company.UpdatedAt = DateTime.UtcNow;

        await _companyRepository.UpdateAsync(
            company,
            cancellationToken);

        return Result<bool>.Ok(true);
    }
}