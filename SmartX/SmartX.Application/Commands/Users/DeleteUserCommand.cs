using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Application.Commands.Users
{
    public class DeleteUserCommand
    {
        public Guid UserId { get; set; }

        public Guid CompanyId { get; set; }
    }
}
