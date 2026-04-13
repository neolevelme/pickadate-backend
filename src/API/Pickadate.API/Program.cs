using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pickadate.API.Middleware;
using Pickadate.Application.Behaviors;
using Pickadate.Application.Contracts;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Anniversaries;
using Pickadate.Domain.AntiAbuse;
using Pickadate.Domain.Auth;
using Pickadate.Domain.Invitations;
using Pickadate.Domain.Notifications;
using Pickadate.Domain.Safety;
using Pickadate.Domain.Users;
using Pickadate.Infrastructure;
using Pickadate.Infrastructure.Persistence;
using Pickadate.Infrastructure.Repositories;
using Pickadate.Infrastructure.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Database
builder.Services.AddDbContext<PickadateDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PickadateDb")));

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Pickadate.Application.AssemblyMarker).Assembly));

// FluentValidation + pipeline behavior
builder.Services.AddValidatorsFromAssembly(typeof(Pickadate.Application.AssemblyMarker).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Options
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<PushOptions>(builder.Configuration.GetSection(PushOptions.SectionName));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IVerificationCodeRepository, VerificationCodeRepository>();
builder.Services.AddScoped<IInvitationRepository, InvitationRepository>();
builder.Services.AddScoped<ICounterProposalRepository, CounterProposalRepository>();
builder.Services.AddScoped<IDeclineRecordRepository, DeclineRecordRepository>();
builder.Services.AddScoped<ISafetyCheckRepository, SafetyCheckRepository>();
builder.Services.AddScoped<IAnniversaryRepository, AnniversaryRepository>();
builder.Services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();

// Services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IClientContext, ClientContext>();
builder.Services.AddScoped<IOwnerTokenContext, OwnerTokenContext>();
builder.Services.AddScoped<Pickadate.Application.Invitations.Authorization.IInvitationOwnerAuthorizer, Pickadate.Application.Invitations.Authorization.InvitationOwnerAuthorizer>();
builder.Services.AddScoped<Pickadate.Application.Users.Commands.IDeleteMyAccountService, DeleteMyAccountService>();
builder.Services.AddSingleton<IVerificationCodeGenerator, VerificationCodeGenerator>();
builder.Services.AddSingleton<ISlugGenerator, SlugGenerator>();
builder.Services.AddSingleton<IOwnerTokenGenerator, OwnerTokenGenerator>();
builder.Services.AddSingleton<ISafetyTokenGenerator, SafetyTokenGenerator>();

// Weather forecast (Open-Meteo) — typed HttpClient with a 6h in-memory cache
// baked into the service itself.
builder.Services.AddHttpClient<IWeatherService, OpenMeteoWeatherService>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(5);
});

// Notifications — swap real Web Push delivery in when VAPID keys are
// configured; otherwise log to the console so the rest of the pipeline
// still fires during local dev without any secrets to manage.
var pushSection = builder.Configuration.GetSection(PushOptions.SectionName);
var hasVapid = !string.IsNullOrWhiteSpace(pushSection["PublicKey"]) && !string.IsNullOrWhiteSpace(pushSection["PrivateKey"]);
if (hasVapid)
{
    builder.Services.AddScoped<INotificationService, WebPushNotificationService>();
}
else
{
    builder.Services.AddScoped<INotificationService, LoggingNotificationService>();
}

// Background jobs
builder.Services.AddHostedService<InvitationPurgeHostedService>();
builder.Services.AddHostedService<SafetyCheckAlertHostedService>();
builder.Services.AddHostedService<AnniversaryDetectionHostedService>();
builder.Services.AddHostedService<InvitationReminderHostedService>();

builder.Services.AddHttpContextAccessor();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "pickadate.me API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4321", "http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Auto-migrate database on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PickadateDbContext>();
    if ((await db.Database.GetPendingMigrationsAsync()).Any())
    {
        await db.Database.MigrateAsync();
    }
}

app.Run();
