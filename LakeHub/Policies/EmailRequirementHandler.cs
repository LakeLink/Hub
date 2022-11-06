using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LakeHub.Policies
{
    //https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-6.0#use-a-handler-for-one-requirement
    //https://learn.microsoft.com/en-us/aspnet/core/security/authorization/dependencyinjection?view=aspnetcore-6.0
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
