using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Resume_parsing.Data;
using Resume_parsing.Models;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add and configure the session service.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure the database context using the connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("HrvmsDBEntities");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add the HttpClient for the CvParsingService
builder.Services.AddHttpClient<CvParsingService>(client =>
{
    client.BaseAddress = new Uri("http://34.229.118.216:8000/");
    client.Timeout = TimeSpan.FromSeconds(300);
});

// Configure FtpSettings from the "FtpSettings" section of appsettings.json
builder.Services.Configure<FtpSettings>(builder.Configuration.GetSection("FtpSettings"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add the session middleware here, after UseRouting but before UseAuthorization.
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Parsing}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "apiStart",
    pattern: "start",
    defaults: new { controller = "Parsing", action = "StartJob" });

app.MapControllerRoute(
    name: "apiStatus",
    pattern: "status/{jobDatabaseId}",
    defaults: new { controller = "Parsing", action = "CheckJobStatus" });

app.Run();