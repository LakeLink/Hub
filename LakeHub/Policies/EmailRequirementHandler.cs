using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LakeHub.Policies
{
    public class EmailRequirementHandler : AuthorizationHandler<EmailRequirement>
    {
        private readonly LakeHubContext _db;
        public EmailRequirementHandler(LakeHubContext dbCtx)
        {
            _db = dbCtx;
        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, EmailRequirement requirement)
        {
            var casId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (_db.User.Find(casId)?.EmailVerified != true)
            {
                context.Fail(new AuthorizationFailureReason(this, "No email address or unverified."));
            }
            else
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}
