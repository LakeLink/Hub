using OpenIddict.Abstractions;
using OpenIddict.Server;
using System.Globalization;
using System.Text.Json;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace LakeHub
{
    public class OidcUserinfoHandler : IOpenIddictServerHandler<HandleUserinfoRequestContext>
    {
        public ValueTask HandleAsync(HandleUserinfoRequestContext context)
        {
            if (context.Principal == null) throw new NullReferenceException("context.Principle == null");

            context.Subject = context.Principal.GetClaim(Claims.Subject);

            if (context.Principal.HasScope(Scopes.Profile))
            {
                context.Claims[Claims.Name] = context.Principal.GetClaim(Claims.Name);
            }

            if (context.Principal.HasScope(Scopes.Email))
            {
                context.Email = context.Principal.GetClaim(Claims.Email);
                context.Claims[Claims.EmailVerified] = context.Principal.GetClaim(Claims.EmailVerified);
            }
            return default;
        }
    }
}
