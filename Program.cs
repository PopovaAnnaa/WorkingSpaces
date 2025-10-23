using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Cookie Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Okta"; // зовнішній провайдер за замовчуванням
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login"; // куди перекидає неавторизованого
    options.LogoutPath = "/Account/Logout"; // шлях для виходу
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // час життя кукі
})
.AddOpenIdConnect("Okta", options =>
{
    options.ClientId = "Mf1FSPAZ0IBZ0ILlUgsRbPP8HLZDZDXY";
    options.ClientSecret = "kRl5jPkhs53kB5HXx1QsRw6edTT_m8iO-cPhV_nFno1SmRypZcdpNDvdE52Px3VA";
    options.Authority = "https://dev-2a7o8mgzl3i7lh4m.us.auth0.com/";
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.CallbackPath = "/Account/ExternalLoginCallback"; 
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // додаємо аутентифікацію перед авторизацією
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
