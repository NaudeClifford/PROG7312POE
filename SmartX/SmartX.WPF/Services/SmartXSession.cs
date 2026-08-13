using SmartX.Domain.Entities;
using SmartX.Domain.Enums;
using SmartX.Shared.Models;

namespace SmartX.WPF.Services
{
    public class SmartXSession
    {
        public Guid Id { get; private set; }

        public string? FirebaseUid { get; private set; }

        public string? Email { get; private set; }

        public string? DisplayName { get; private set; }

        public UserRole? Role { get; private set; }

        public bool IsAuthenticated { get; private set; } = false;
        public bool IsGuest { get; private set; } = false;

        public void SignIn(User user) 
        {
            Id = user.Id;
            Email = user.Email;
            Role = user.Role;
            FirebaseUid = user.FirebaseUid;
            IsAuthenticated = true;
            DisplayName = user.DisplayName;
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
