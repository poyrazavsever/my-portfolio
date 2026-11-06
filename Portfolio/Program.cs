using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Portfolio.Context;
using Portfolio.Services;

// ---------------------------------------------------------------------------
// Komut satırı yardımcıları (web sunucusu başlatmadan çalışır)
//   dotnet run -- --hash-password   -> admin şifresi için PBKDF2 hash üretir
// ---------------------------------------------------------------------------
if (args.Contains("--hash-password"))
{
    Console.Write("Admin şifresi: ");
    var password = ReadHidden();
    Console.Write("Tekrar: ");
    if (password != ReadHidden() || password.Length < 12)
    {
        Console.WriteLine("Şifreler eşleşmedi ya da 12 karakterden kısa.");
        return;
    }
    Console.WriteLine();
    Console.WriteLine(AdminPasswordHasher.Hash(password));
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<PortfolioContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PortfolioConnection")));

// IImageStorage isteyen her controller'a ImageStorage verilir (istek başına bir tane).
builder.Services.AddScoped<IImageStorage, ImageStorage>();
builder.Services.AddScoped<ICvStorage, CvStorage>();

// ---- Admin girişi: cookie tabanlı oturum
builder.Services.Configure<AdminAuthOptions>(builder.Configuration.GetSection(AdminAuthOptions.Section));
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Account/Login";
        options.LogoutPath = "/Admin/Account/Logout";
        options.AccessDeniedPath = "/Admin/Account/Login";
        options.Cookie.Name = "pa.admin";
        options.Cookie.HttpOnly = true;                          // JS cookie'yi okuyamaz (XSS'e karşı)
        options.Cookie.SameSite = SameSiteMode.Lax;              // başka sitelerden gelen POST'larda gönderilmez
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

// Form koruma cookie'si de HTTPS'te "Secure" olsun (varsayılanı bu bayrağı koymuyor).
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.HttpOnly = true;
});

// ---- Giriş denemelerine hız sınırı: IP başına dakikada 5 deneme
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

// ---- Ters vekil (Cloudflare Tunnel) arkasında doğru istemci IP'si ve şeması (https)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Uygulamaya yalnızca Docker ağındaki tunnel ulaşabildiği için ağ kısıtlamasını kaldırıyoruz.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// ---- Data Protection anahtarları: oturum cookie'si ve form token'ları bunlarla şifrelenir.
// Konteyner yeniden başladığında anahtarlar kaybolursa herkesin oturumu düşer; kalıcı klasörde tutuyoruz.
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    builder.Services.AddDataProtection()
        .SetApplicationName("Portfolio")
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

var app = builder.Build();

// Üretimde admin bilgileri yoksa hiç başlama: panel ya açık kalır ya da kimse giremez, ikisi de kötü.
var adminAuth = builder.Configuration.GetSection(AdminAuthOptions.Section).Get<AdminAuthOptions>() ?? new AdminAuthOptions();
if (!app.Environment.IsDevelopment() && !adminAuth.IsConfigured && !args.Contains("--seed") && !args.Contains("--migrate"))
{
    throw new InvalidOperationException("AdminAuth__Username ve AdminAuth__PasswordHash ortam değişkenleri tanımlanmalı.");
}

// "dotnet run -- --seed": portfolyo içeriğini (Data/PortfolioSeeder) DB'ye yükleyip çık.
// "dotnet run -- --migrate": yalnızca bekleyen migration'ları uygulayıp çık (içeriğe dokunmaz).
// Web sunucusu başlatılmaz; komut satırından bir kerelik bakım işi gibi çalışır.
if (args.Contains("--seed") || args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<PortfolioContext>();
    await context.Database.MigrateAsync();
    Console.WriteLine("Migration'lar uygulandı.");

    if (args.Contains("--seed"))
    {
        await Portfolio.Data.PortfolioSeeder.SeedAsync(context);
        Console.WriteLine("Seed tamamlandı: portfolyo içeriği yüklendi.");
    }
    return;
}

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();   // en başta: sonraki her şey doğru şemayı ve IP'yi görsün

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// MapStaticAssets yalnızca build anında wwwroot'ta olan dosyaları bilir.
// Admin'den çalışma zamanında yüklenen görseller (/uploads/...) için klasik middleware gerekli.
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();
app.UseAuthentication();   // "bu kim?"  (cookie'yi oku)
app.UseAuthorization();    // "buna izni var mı?"

app.MapStaticAssets();

// Admin area'sının tamamı giriş ister. AccountController [AllowAnonymous] ile bu kuralın istisnası.
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .RequireAuthorization()
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Default}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// Şifreyi ekrana basmadan oku
static string ReadHidden()
{
    var buffer = new System.Text.StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return buffer.ToString(); }
        if (key.Key == ConsoleKey.Backspace && buffer.Length > 0) { buffer.Length--; continue; }
        if (!char.IsControl(key.KeyChar)) buffer.Append(key.KeyChar);
    }
}
