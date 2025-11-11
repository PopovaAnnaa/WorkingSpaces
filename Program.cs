using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WorkingSpaces.Data;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5189, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});

builder.Services.AddHttpClient();
builder.Services.AddControllersWithViews();
builder.Services.AddControllers().AddJsonOptions(opts => {   opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()); });
string provider = builder.Configuration.GetValue("DatabaseProvider", "InMemory")!;
string? connectionString = builder.Configuration.GetConnectionString(provider);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    switch (provider)
    {
        case "MsSql":
            options.UseSqlServer(builder.Configuration.GetConnectionString("MsSql"));
            break;
        case "Postgres":
            options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
            break;
        case "Sqlite":
            options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite"));
            break;
        case "InMemory":
        default:
            options.UseInMemoryDatabase("WorkingSpacesDb");
            break;
    }
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Okta";
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

    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");

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

            var nameClaim = context.Principal?.FindFirst("name")?.Value ?? "";
            if (!string.IsNullOrEmpty(nameClaim))
            {
                var identity = context.Principal?.Identity as ClaimsIdentity;
                identity?.AddClaim(new Claim("FullName", nameClaim));
            }

            return Task.CompletedTask;
        },
        OnRemoteFailure = context =>
        {
            Console.WriteLine("Remote failure: " + context.Failure?.Message);
            context.HandleResponse();
            return Task.CompletedTask;
        }
    };
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        if (context.Database.IsInMemory())
        {
            context.Database.EnsureCreated(); 
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

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
