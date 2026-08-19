using AutoMapper;
using SmartX.Shared.Models;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;

namespace SmartX.Application.Queries.Company
{
    public class GetCompanyByIdHandler
    {
        private readonly IMapper _mapper;
        private readonly ICompanyRepository _companyRepository;

        public GetCompanyByIdHandler(ICompanyRepository companyRepository,
            IMapper mapper)
        {
            _companyRepository = companyRepository;
            _mapper = mapper;
        }

        public async Task<Result<CompanyDto>> HandleAsync(
            GetCompanyByIdQuery query,
            CancellationToken cancellationToken = default)
        {
            var company = await _companyRepository.GetByIdAsync(query.Id,
                cancellationToken);

            if (company is null)
            {
                return Result<CompanyDto>.Fail("company not found.");
            }

            var dto = _mapper.Map<CompanyDto>(company);

            return Result<CompanyDto>.Ok(dto);
        }
    }
}
