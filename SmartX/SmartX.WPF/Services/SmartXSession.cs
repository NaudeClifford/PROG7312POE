using SmartX.Domain.Enums;
using SmartX.Shared.DTOs;

namespace SmartX.WPF.Services
{
    public class SmartXSession
    {
        public Guid Id { get; private set; }

        public string? FirebaseUid { get; private set; }

        public Guid CompanyId { get; private set; }

        public Guid UserId { get; private set; }

        public string? Email { get; private set; }

        public string? DisplayName { get; private set; }

        public UserRole? Role { get; private set; }

        public bool IsAuthenticated { get; private set; }

        public bool IsGuest { get; private set; }

        public void SignIn(UserDto user)
        {
            Id = user.Id;
            Email = user.Email;
            Role = user.Role;
            FirebaseUid = user.FirebaseUid;
            DisplayName = user.DisplayName;

            IsAuthenticated = true;
            IsGuest = false;
        }

        public void StartGuestSession(string name)
        {
            Id = Guid.NewGuid();
            Email = null;
            Role = null;
            FirebaseUid = null;
            IsAuthenticated = false;
            DisplayName = name;
            IsGuest = true;
        }
    }
}