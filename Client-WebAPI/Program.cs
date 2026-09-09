using Crm.ClientWebAPI.Extensions;
using Crm.ClientWebAPI.Models;
using Crm.ClientWebAPI.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.Configure<CrmConfig>(
    builder.Configuration.GetSection("CrmConfig"));

builder.Services.AddCrmServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("CRM Client Web API");
    });
}

app.MapControllers();

app.Run();
