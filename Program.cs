using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---- Services ----

builder.Services.AddControllers();

// MongoDB — singleton because MongoClient is thread-safe and manages its own connection pool
builder.Services.AddSingleton<MongoDbService>();

// CORS — must define the policy before you can call UseCors("AllowAll")
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"] ?? "MySuperSecretKey1234567890")),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WEBAPI", Version = "v1" });

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
            new List<string>()
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "WEBAPI v1");
    c.RoutePrefix = "swagger"; // → yourapp.onrender.com/swagger
    c.DefaultModelsExpandDepth(-1);   // hides bottom "Schemas" section entirely
    c.DefaultModelExpandDepth(-1);    // collapses per-endpoint model tree, keeps example JSON visible
});

// ---- Middleware pipeline ----

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "WEBAPI v1");
    c.RoutePrefix = "swagger"; // → yourapp.onrender.com/swagger
});

// app.UseHttpsRedirection(); // Render terminates HTTPS at the edge/load balancer already;
// leaving this on can cause redirect loops behind Render's proxy

app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();   // must come BEFORE UseAuthorization
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Ok("API is running")); // Render health check

// Bind to Render's dynamic port
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");