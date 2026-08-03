using FirebaseAdmin.Auth;

namespace SmartX.API.Authentication
{
    public class FirebaseAuthService
    {

        public async Task MakeAdmin(string uid)
        {
            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(
                uid,
                new Dictionary<string, object>
                {
            { "role", "Admin" }
                });
        }
    }
}
