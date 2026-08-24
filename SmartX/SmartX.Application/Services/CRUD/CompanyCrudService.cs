using SmartX.Application.Commands.Company;
using SmartX.Application.Queries.Company;
using SmartX.Application.Queries.Gateway;
using SmartX.Domain.Entities;
using SmartX.Shared.DTOs;
using SmartX.Shared.Models;

namespace SmartX.Application.Services.CRUD;

public class CompanyCrudService :
    ICrudService<
        CompanyDto,
        CreateCompanyCommand,
        UpdateCompanyCommand>
{
    private readonly GetCompanysHandler _getCompanies;
    private readonly GetCompanyByIdHandler _getCompanyById;
    private readonly CreateCompanyHandler _createCompany;
    private readonly UpdateCompanyHandler _updateCompany;
    private readonly DeleteCompanyHandler _deleteCompany;
    private readonly AuditLogService _auditLog;

    public CompanyCrudService(
        GetCompanysHandler getCompanies,
        GetCompanyByIdHandler getCompanyById,
        CreateCompanyHandler createCompany,
        UpdateCompanyHandler updateCompany,
        DeleteCompanyHandler deleteCompany,
        AuditLogService auditLog)
    {
        _getCompanies = getCompanies;
        _getCompanyById = getCompanyById;
        _createCompany = createCompany;
        _updateCompany = updateCompany;
        _deleteCompany = deleteCompany;
        _auditLog = auditLog;

    }

    public Task<Result<IReadOnlyList<CompanyDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return _getCompanies.HandleAsync(
            new GetCompanysQuery(),
            cancellationToken);
    }

    public Task<Result<CompanyDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _getCompanyById.HandleAsync(
            new GetCompanyByIdQuery
            {
                Id = id
            },
            cancellationToken);
    }

    public async Task<Result<Guid>> CreateAsync(
        CreateCompanyCommand command,
        CancellationToken cancellationToken = default)
    {
        var result =
            await _createCompany.HandleAsync(
            command,
            cancellationToken);

        if (!result.Success)
            return result;

        await _auditLog.LogAsync(
            entityType: "Company",
            entityId: result.Data,
            action: "Created",
            companyId: result.Data,
            details: "Company created.",
            cancellationToken: cancellationToken);

        return result;
    }

    public async Task<Result<bool>> UpdateAsync(
        UpdateCompanyCommand command,
        CancellationToken cancellationToken = default)
    {

        var result =
            await _updateCompany.HandleAsync(
            command,
            cancellationToken);

        if (!result.Success)
            return result;

        await _auditLog.LogAsync(
            entityType: "Company",
            entityId: command.Id,
            action: "Updated",
            companyId: command.Id,
            details: "Company updated.",
            cancellationToken: cancellationToken);

        return result;
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _deleteCompany.HandleAsync(
            new DeleteCompanyCommand
            {
                Id = id
            },
            cancellationToken);

        if (!result.Success)
            return result;

        await _auditLog.LogAsync(
            entityType: "Company",
            entityId: id,
            action: "Deleted",
            companyId: id,
            details: "Company deleted.",
            cancellationToken: cancellationToken);

        return result;
    }
}