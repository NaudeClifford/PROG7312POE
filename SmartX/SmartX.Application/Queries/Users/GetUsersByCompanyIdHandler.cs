using AutoMapper;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;
using SmartX.Shared.Models;

namespace SmartX.Application.Queries.Users;

public class GetUsersByCompanyIdHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUsersByCompanyIdHandler(
        IUserRepository userRepository,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<UserDto>>> HandleAsync(
        GetUsersByCompanyIdQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.CompanyId == Guid.Empty)
        {
            return Result<IReadOnlyList<UserDto>>.Fail(
                "Company ID is required.");
        }

        var users =
            await _userRepository.GetByCompanyIdAsync(
                query.CompanyId,
                cancellationToken);

        var dtos =
            _mapper.Map<IReadOnlyList<UserDto>>(users);

        return Result<IReadOnlyList<UserDto>>.Ok(dtos);
    }
}