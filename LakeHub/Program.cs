using LakeHub;
using LakeHub.Options;
using LakeHub.Policies;
using LakeHub.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Refit;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

var builder = WebApplication.CreateBuilder(args);

// Inject configurations
builder.Services.Configure<IndexOptions>(
    builder.Configuration.GetSection(IndexOptions.Key));
builder.Services.Configure<DiscourseOptions>(
    builder.Configuration.GetSection(DiscourseOptions.Key));
builder.Services.Configure<MailOptions>(
    builder.Configuration.GetSection(MailOptions.Key));

// Configure database
var dbProvider = builder.Configuration.GetValue<string>("DbProvider");
//https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers
switch (dbProvider)
{
    case "SqlServer":
        // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection
        builder.Services.AddDbContext<LakeHubContext, SqlServerDbContext>(options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString(dbProvider) ?? throw new InvalidOperationException("Connection string 'SqlServer' not found.")
            )
            .UseOpenIddict()
        );
        break;
    case "PostgreSql":
        builder.Services.AddDbContext<LakeHubContext, PostgreSqlDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString(dbProvider) ?? throw new InvalidOperationException("Connection string 'PostgreSql' not found.")
            )
            .UseOpenIddict()
        );
        break;
}


// Configure DataProtection key path
var keyStorePath = builder.Configuration.GetValue<string>("DPKeyStorePath");
if (keyStorePath != null)
{
    //https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-6.0#persistkeystofilesystem
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keyStorePath));
}


// Add services to the container.
builder.Services.AddRazorPages();


// CAS integration
builder.Services
    .AddRefitClient<ICASRestProtocol>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://sso.westlake.edu.cn"));


// WeCom bot
builder.Services.AddHttpClient<WeComBotService>();


// OpenIddict
builder.Services.AddOpenIddict()

    // Register the OpenIddict core components.
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<LakeHubContext>();
    })

    // Register the OpenIddict server components.
    .AddServer(options =>
    {
        // Enable the authorization and token endpoints.
        options.SetAuthorizationEndpointUris("/Auth/Oidc/Authorize")
               .SetTokenEndpointUris("/Auth/Oidc/Token")
               .SetUserinfoEndpointUris("/Auth/Oidc/Userinfo");

        // Note: this sample only uses the authorization code flow but you can enable
        // the other flows if you need to support implicit, password or client credentials.
        options.AllowAuthorizationCodeFlow();
        options.AllowImplicitFlow();

        // Register the signing and encryption credentials.
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
        //
        // Note: unlike other samples, this sample doesn't use token endpoint pass-through
        // to handle token requests in a custom MVC action. As such, the token requests
        // will be automatically handled by OpenIddict, that will reuse the identity
        // resolved from the authorization code to produce access and identity tokens.
        //
        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough();
        options.AddEventHandler<HandleUserinfoRequestContext>(options =>
            options.UseSingletonHandler<OidcUserinfoHandler>());
    })

    // Register the OpenIddict validation components.
    .AddValidation(options =>
    {
        // Import the configuration from the local OpenIddict server instance.
        options.UseLocalServer();

        // Register the ASP.NET Core host.
        options.UseAspNetCore();
    });


// Auth & session
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "Auth";
        options.LoginPath = new PathString("/Auth/Cas/SignIn");
        options.LogoutPath = new PathString("/Auth/Cas/SignOut");
        options.AccessDeniedPath = new PathString("/Auth/Cas/AccessDenied");
    });
//https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-6.0
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EmailRequired", policy =>
    {
        policy.AuthenticationSchemes.Add(CookieAuthenticationDefaults.AuthenticationScheme);
        policy.Requirements.Add(new EmailRequirement { Verified = true });
    });
});
//https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-6.0#handler-registration
builder.Services.AddScoped<IAuthorizationHandler, EmailRequirementHandler>();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(5);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});



var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
    );
app.MapRazorPages();
//app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
    var dbgApp = new OpenIddictApplicationDescriptor
    {
        ClientId = "debugid",
        ClientSecret = "debugsecret",
        DisplayName = "Debug",
        RedirectUris = { new Uri("https://oidcdebugger.com/debug"), new Uri("https://latex.yif.fyi/oauth/callback") },
        Permissions =
        {
            Permissions.Endpoints.Authorization,
            Permissions.Endpoints.Token,
            Permissions.Endpoints.Introspection,

            Permissions.GrantTypes.AuthorizationCode,
            Permissions.ResponseTypes.Code
        }
    };
    if (await manager.FindByClientIdAsync("debugid") is null)
    {
        await manager.CreateAsync(dbgApp);
    }
}

app.Run();
