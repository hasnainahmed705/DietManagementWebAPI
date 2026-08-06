using DietManagementWebAPI.Models;
using DietManagementWebAPI.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
// --- NEW HANGFIRE USINGS ---
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Resend;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

string firebaseCredentialPath = "/secrets/firebase-adminsdk.json";
FirebaseApp.Create(new AppOptions()
{
    Credential = CredentialFactory
        .FromFile<ServiceAccountCredential>(firebaseCredentialPath)
        .ToGoogleCredential()
});

//var firebaseCredentialPath = Path.Combine(
//    builder.Environment.ContentRootPath,
//    "Firebase", 
//    "macromate-96750-firebase-adminsdk-fbsvc-41de704a92.json"   // ← change to your real file name
//);

//FirebaseApp.Create(new AppOptions()
//{
//    Credential = CredentialFactory
//        .FromFile<ServiceAccountCredential>(firebaseCredentialPath)
//        .ToGoogleCredential()
//});

// Services
builder.Services.AddControllers();
builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddSingleton<QueryBuilderService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrEmpty(jwtKey))
{
    throw new Exception("JWT Key is missing from appsettings.json");
}

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Debugging
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("JWT Authentication Failed:");
                Console.WriteLine(context.Exception.Message);
                return Task.CompletedTask;
            },

            OnTokenValidated = context =>
            {
                Console.WriteLine("JWT Token Validated Successfully");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Diet Management API", Version = "v1" });

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
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new List<string>()
        }
    });
});

builder.Services.Configure<ResendClientOptions>(
    options =>
    {
        options.ApiToken =
            builder.Configuration["ResendSettings:ApiKey"];
    });


builder.Services.AddHttpClient<IResend, ResendClient>();

builder.Services.AddTransient<EmailService>();
builder.Services.AddSingleton<FirebaseNotificationService>();


// --- NEW HANGFIRE CONFIGURATION ---
// Get MongoDB Connection String from appsettings.json
var mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"]
                            ?? throw new Exception("MongoDB ConnectionString is missing.");
var mongoDatabaseName = builder.Configuration["MongoDB:DatabaseName"]
                        ?? "DietManagementDB";

var mongoUrlBuilder = new MongoUrlBuilder(mongoConnectionString);
var mongoClient = new MongoClient(mongoUrlBuilder.ToMongoUrl());

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMongoStorage(mongoClient, mongoDatabaseName, new MongoStorageOptions
    {
        MigrationOptions = new MongoMigrationOptions
        {
            MigrationStrategy = new MigrateMongoMigrationStrategy(),
            BackupStrategy = new CollectionMongoBackupStrategy()
        },
        Prefix = "hangfire",
        CheckConnection = true
    })
);

builder.Services.AddHangfireServer();
// ----------------------------------


var app = builder.Build();

// Middleware Pipeline (Important Order)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Diet Management API v1");
    c.RoutePrefix = "swagger";
});

// --- NEW HANGFIRE DASHBOARD ---
// You can optionally restrict access to this dashboard using authorization filters.
app.UseHangfireDashboard("/hangfire");
// ------------------------------

app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();           // ← Ye pehle
app.UseAuthorization();            // ← Phir ye

app.MapControllers();
app.MapGet("/", () => Results.Ok("API is running"));


// --- SCHEDULE THE NOTIFICATION JOB ---
// This runs the GymNotificationJob exactly at the 0th second of every minute
RecurringJob.AddOrUpdate<DietManagementWebAPI.Services.GymNotificationJob>(
    "gym-notification-job",
    job => job.ProcessGymNotificationsAsync(),
    Cron.Minutely
);
// -------------------------------------


var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");

