using LakeHub.Options;
using LakeHub.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Refit;

var builder = WebApplication.CreateBuilder(args);

// Inject configurations
builder.Services.Configure<IndexOptions>(
    builder.Configuration.GetSection(IndexOptions.Key));

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


// Auth & session
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "Auth";
        options.LoginPath = new PathString("/Auth/Cas/SignIn");
        options.LogoutPath = new PathString("/Auth/Cas/SignOut");
        options.AccessDeniedPath = new PathString("/Auth/Cas/AccessDenied");
    });

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

app.Run();
