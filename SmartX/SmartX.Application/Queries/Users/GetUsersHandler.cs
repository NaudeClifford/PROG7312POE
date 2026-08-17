using AutoMapper;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;
using SmartX.Shared.Models;

namespace SmartX.Application.Queries.Users
{
    public class GetUsersHandler
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        public GetUsersHandler(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<Result<IReadOnlyList<UserDto>>> HandleAsync(
            GetUsersQuery query,
            CancellationToken cancellationToken = default)
        {
            var users = await _userRepository.GetAllAsync(
                cancellationToken);

            var dtos = _mapper.Map<List<UserDto>>(users);


            return Result<IReadOnlyList<UserDto>>.Ok(dtos);
        }
    }
}
