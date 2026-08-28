using AutoMapper;
using FluentValidation;
using SmartX.Application.Requests.Company;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;
using SmartX.Shared.Models;

namespace SmartX.Application.Services.CRUD;

public class CompanyCrudService :
    ICrudService<
        CompanyDto,
        CreateCompanyRequest,
        UpdateCompanyRequest>
{
    private readonly ICompanyRepository _companyRepository;

    private readonly IValidator<CreateCompanyRequest>
        _createValidator;

    private readonly IValidator<UpdateCompanyRequest>
        _updateValidator;

    private readonly IMapper _mapper;

    private readonly AuditLogService _auditLog;

    public CompanyCrudService(
        ICompanyRepository companyRepository,
        IValidator<CreateCompanyRequest> createValidator,
        IValidator<UpdateCompanyRequest> updateValidator,
        IMapper mapper,
        AuditLogService auditLog)
    {
        _companyRepository = companyRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _mapper = mapper;
        _auditLog = auditLog;
    }

    public async Task<Result<IReadOnlyList<CompanyDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var companies = await _companyRepository.GetAllAsync(
            cancellationToken);

        var dtos = _mapper.Map<List<CompanyDto>>(companies);

        return Result<IReadOnlyList<CompanyDto>>.Ok(dtos);
    }

    public async Task<Result<CompanyDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<CompanyDto>.Fail(
                "Company ID is required.");
        }

        var company = await _companyRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (company is null)
        {
            return Result<CompanyDto>.Fail(
                "Company not found.");
        }

        var dto = _mapper.Map<CompanyDto>(company);

        return Result<CompanyDto>.Ok(dto);
    }

    public async Task<Result<Guid>> CreateAsync(
        CreateCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = string.Join(
                "; ",
                validationResult.Errors
                    .Select(x => x.ErrorMessage));

            return Result<Guid>.Fail(errors);
        }

        var now = DateTime.UtcNow;

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _companyRepository.AddAsync(
            company,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "Company",
            entityId: company.Id,
            action: "Created",
            companyId: company.Id,
            details: "Company created.",
            cancellationToken: cancellationToken);

        return Result<Guid>.Ok(company.Id);
    }

    public async Task<Result<bool>> UpdateAsync(
        UpdateCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _updateValidator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = string.Join(
                "; ",
                validationResult.Errors
                    .Select(x => x.ErrorMessage));

            return Result<bool>.Fail(errors);
        }

        var company = await _companyRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (company is null)
        {
            return Result<bool>.Fail(
                "Company not found.");
        }

        company.Name = request.Name;
        company.Description = request.Description;
        company.IsActive = request.IsActive;

        // UpdatedAt is used by WPF synchronization.
        company.UpdatedAt = DateTime.UtcNow;

        await _companyRepository.UpdateAsync(
            company,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "Company",
            entityId: company.Id,
            action: "Updated",
            companyId: company.Id,
            details: "Company updated.",
            cancellationToken: cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<bool>.Fail(
                "Company ID is required.");
        }

        var company = await _companyRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (company is null)
        {
            return Result<bool>.Fail(
                "Company not found.");
        }

        await _companyRepository.DeleteAsync(
            id,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "Company",
            entityId: id,
            action: "Deleted",
            companyId: id,
            details: "Company deleted.",
            cancellationToken: cancellationToken);

        return Result<bool>.Ok(true);
    }
}
