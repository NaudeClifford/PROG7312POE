using AutoMapper;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;
using SmartX.Shared.Models;

namespace SmartX.Application.Queries.Users
{
    public class GetUserByIdHandler
    {
        private readonly IMapper _mapper;
        private readonly IUserRepository _userRepository;

        public GetUserByIdHandler(IUserRepository userRepository,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<Result<UserDto>> HandleAsync(
            GetUserByIdQuery query,
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(query.UserId,
                cancellationToken);

            if (user is null)
            {
                return Result<UserDto>.Fail("User not found.");
            }

            var dto = _mapper.Map<UserDto>(user);

            return Result<UserDto>.Ok(dto);
        }

    }
}