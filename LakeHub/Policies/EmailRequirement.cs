using Microsoft.AspNetCore.Authorization;

namespace LakeHub.Policies
{
    public class EmailRequirement : IAuthorizationRequirement
    {
        public bool Verified { get; set; } = true;
    }
}
