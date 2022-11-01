using LakeHub.Options;
using LakeHub.Services;
using LakeHub;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Refit;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<IndexOptions>(
    builder.Configuration.GetSection(IndexOptions.Key));

builder.Services.Configure<DiscourseOptions>(
    builder.Configuration.GetSection(DiscourseOptions.Key));

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
        );
        break;
    case "PostgreSql":
        builder.Services.AddDbContext<LakeHubContext, PostgreSqlDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString(dbProvider) ?? throw new InvalidOperationException("Connection string 'PostgreSql' not found.")
            )
        );
        break;
}

var keyStorePath = builder.Configuration.GetValue<string>("DPKeyStorePath");
if (keyStorePath != null)
{
    //https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-6.0#persistkeystofilesystem
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keyStorePath));
}


// Add services to the container.
builder.Services.AddRazorPages();

builder.Services
    .AddRefitClient<ICASRestProtocol>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://sso.westlake.edu.cn"));

builder.Services.AddHttpClient<WeComBotService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "Auth";
        options.LoginPath = new PathString("/Auth/SignIn");
        options.LogoutPath = new PathString("/Auth/SignOut");
    });
builder.Services.AddAuthorization();

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

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapRazorPages();
app.MapControllers();

app.Run();
