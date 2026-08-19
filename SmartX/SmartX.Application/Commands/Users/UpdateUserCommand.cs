using SmartX.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Application.Commands.Users
{
    public class UpdateUserCommand
    {
        public Guid Id { get; set; }

        public Guid CompanyId { get; set; }

        public string FirebaseUid { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Viewer;

        public bool IsActive { get; set; } = true;

    }
}
