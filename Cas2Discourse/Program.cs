using Cas2Discourse.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Refit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// CAS integration
builder.Services
    .AddRefitClient<ICASRestProtocol>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://sso.westlake.edu.cn"));

// Discourse SSO integration
builder.Services.AddSingleton<DiscourseSsoProtocol>();


// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

app.UsePathBase("/_sso");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
