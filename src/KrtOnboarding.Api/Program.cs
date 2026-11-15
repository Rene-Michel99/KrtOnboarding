using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using KrtOnboarding.Application.Caching;
using KrtOnboarding.Application.Messaging;
using KrtOnboarding.Infrastructure.Caching;
using KrtOnboarding.Infrastructure.Persistence;
using KrtOnboarding.Infrastructure.Messaging;
using KrtOnboarding.Domain.Repositories;
using KrtOnboarding.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Configure a connection string using ConnectionStrings__DefaultConnection.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));

builder.Services.AddScoped<IAccountRepository, AccountRepository>();

var redisConfiguration = builder.Configuration["Redis:Configuration"]
    ?? throw new InvalidOperationException("Configure Redis:Configuration.");

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConfiguration;
    options.InstanceName = builder.Configuration["Redis:InstanceName"];
});
builder.Services.AddSingleton<IRedisCaching, RedisCaching>();

var rabbitMqHost = builder.Configuration["RabbitMq:Host"]
    ?? throw new InvalidOperationException("Configure RabbitMq:Host.");
var rabbitMqUser = builder.Configuration["RabbitMq:User"]
    ?? throw new InvalidOperationException("Configure RabbitMq:User.");
var rabbitMqPassword = builder.Configuration["RabbitMq:Password"]
    ?? throw new InvalidOperationException("Configure RabbitMq:Password.");
var rabbitMqVirtualHost = builder.Configuration["RabbitMq:VirtualHost"] ?? "/";
var rabbitMqPort = builder.Configuration.GetValue("RabbitMq:Port", 5672);

builder.Services.AddSingleton<IMessagePublisher>(_ => new RabbitMqPublisher(
    rabbitMqHost,
    rabbitMqPort,
    rabbitMqUser,
    rabbitMqPassword,
    rabbitMqVirtualHost));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
