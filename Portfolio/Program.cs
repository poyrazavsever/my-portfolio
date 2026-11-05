using Microsoft.EntityFrameworkCore;
using Portfolio.Context;
using Portfolio.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<PortfolioContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PortfolioConnection")));

// IImageStorage isteyen her controller'a ImageStorage verilir (istek başına bir tane).
builder.Services.AddScoped<IImageStorage, ImageStorage>();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Default}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
