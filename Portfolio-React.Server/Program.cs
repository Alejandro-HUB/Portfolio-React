using Microsoft.AspNetCore.Cors; // Import the namespace

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HttpClient service
builder.Services.AddHttpClient();

// Inside your Startup.cs ConfigureServies method:
builder.Services.AddHttpContextAccessor();

// In your Startup.cs file, within the ConfigureServices method:
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", // Policy name is for your reference
        builder => builder.WithOrigins("https://localhost:5173", "https://localhost:3000") // Replace with your exact frontend port
                          .AllowAnyHeader()
                          .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors("AllowSpecificOrigin"); // Apply the CORS policy
app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
