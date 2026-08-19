using AutoMapper;
using SmartX.Shared.Models;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;

namespace SmartX.Application.Queries.Company;

public class GetCompanysHandler
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IMapper _mapper;
    public GetCompanysHandler(ICompanyRepository companyRepository, IMapper mapper)
    {
        _companyRepository = companyRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<CompanyDto>>> HandleAsync(
        GetCompanysQuery query,
        CancellationToken cancellationToken = default)
    {
        var companys = await _companyRepository.GetAllAsync(
            cancellationToken);

        var dtos = _mapper.Map<List<CompanyDto>>(companys);


        return Result<IReadOnlyList<CompanyDto>>.Ok(dtos);
    }

}