using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Kestrel слухає HTTPS на 5189
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5189, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});

// MVC
builder.Services.AddControllersWithViews();

// Authentication & Authorization
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; // "Cookies"
    options.DefaultChallengeScheme = "Okta"; // OpenID Connect схема
})
.AddCookie()
.AddOpenIdConnect("Okta", options =>
{
    options.ClientId = "Mf1FSPAZ0IBZ0ILlUgsRbPP8HLZDZDXY";
    options.ClientSecret = "kRl5jPkhs53kB5HXx1QsRw6edTT_m8iO-cPhV_nFno1SmRypZcdpNDvdE52Px3VA";
    options.Authority = "https://dev-2a7o8mgzl3i7lh4m.us.auth0.com/";
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.CallbackPath = "/signin-oidc";

;

    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");

    // Додаткове логування для діагностики
    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = context =>
        {
            Console.WriteLine("Redirecting to IdP: " + context.ProtocolMessage.RedirectUri);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("Token validated!");
            return Task.CompletedTask;
        },
        OnRemoteFailure = context =>
        {
            Console.WriteLine("Remote failure: " + context.Failure?.Message);
            context.HandleResponse();
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
