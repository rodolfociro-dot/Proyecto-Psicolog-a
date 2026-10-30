using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Psicologia.Api.Data;
using Psicologia.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PsicologiaContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("PsicologiaDB")));

builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddSingleton<TokenService>();

// ---- Autenticación con JWT ----
var jwt = builder.Configuration.GetSection("Jwt");
var clave = jwt["Key"] ?? throw new InvalidOperationException("Falta la sección Jwt:Key en appsettings.json");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// Permite que el HTML (otro origen/puerto) consuma la API
builder.Services.AddCors(o => o.AddPolicy("Front", p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Botón "Authorize" (candado) en Swagger para pegar el token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Pega aquí solo el token que devuelve /api/Auth/login"
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Front");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();