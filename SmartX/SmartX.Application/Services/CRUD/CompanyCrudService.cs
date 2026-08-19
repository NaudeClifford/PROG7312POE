using SmartX.Application.Commands.Company;
using SmartX.Application.Queries.Company;
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

    public CompanyCrudService(
        GetCompanysHandler getCompanies,
        GetCompanyByIdHandler getCompanyById,
        CreateCompanyHandler createCompany,
        UpdateCompanyHandler updateCompany,
        DeleteCompanyHandler deleteCompany)
    {
        _getCompanies = getCompanies;
        _getCompanyById = getCompanyById;
        _createCompany = createCompany;
        _updateCompany = updateCompany;
        _deleteCompany = deleteCompany;
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

    public Task<Result<Guid>> CreateAsync(
        CreateCompanyCommand command,
        CancellationToken cancellationToken = default)
    {
        return _createCompany.HandleAsync(
            command,
            cancellationToken);
    }

    public Task<Result<bool>> UpdateAsync(
        UpdateCompanyCommand command,
        CancellationToken cancellationToken = default)
    {
        return _updateCompany.HandleAsync(
            command,
            cancellationToken);
    }

    public Task<Result<bool>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _deleteCompany.HandleAsync(
            new DeleteCompanyCommand
            {
                Id = id
            },
            cancellationToken);
    }
}