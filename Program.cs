var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<MongoDbService>();

// Simple Swagger Setup (Minimal)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();   // ← Simple version

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();

app.Run();