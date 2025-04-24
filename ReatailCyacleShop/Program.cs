using RetailCycleShopAPI.Models;
using RetailCycleShopAPI.Models.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using RetailCycleShopAPI.module;
using CycleRetailShop.API.Utilities;
using RetailCycleShopAPI.Services;
using SendGrid;
using System.Net.Http.Headers;
using RetailCycleShopAPI.Interfaces;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ✅ Fix 1: Register DbContext ONLY ONCE
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IInvitationService, InvitationService>();

// ✅ Email and Custom Services
builder.Services.AddHttpClient();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddScoped<IInventoryTracker, InventoryTracker>();

builder.Services.AddHttpClient<ISendGridClient, SendGridClient>(client =>
{
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", builder.Configuration["SendGrid:ApiKey"]);
});
// Replace the Stripe registration with:
builder.Services.AddSingleton<IPaymentProcessor, DummyPaymentProcessor>();

builder.Services.AddSingleton<EmailTemplateService>();

// ✅ Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ✅ JWT Authentication Configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? string.Empty);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// ✅ CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ✅ Fix 2: Swagger before AddControllers
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CycleRetailShop API", Version = "v1" });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ✅ Fix 3: Add Controllers with JSON serialization config
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

var app = builder.Build();

// ✅ Swagger for Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ✅ Seed Roles/Admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DatabaseSeeder.SeedRolesAndAdminAsync(services);
}

app.UseHttpsRedirection();

// ✅ CORS before authentication
app.UseCors("AllowAll");

// ✅ Auth Middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
