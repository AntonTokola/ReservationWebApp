using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using VibrationMonitorReservation.Models;
using VibrationMonitorReservation.Services;
using Microsoft.AspNetCore.Mvc.Formatters;
using VibrationMonitorReservation.Controllers;
using System.Reflection;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// NewtonSoftin lisäys PATCH-prosessin JSON-kääntöä varten
builder.Services.AddControllers()
    .AddNewtonsoftJson();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IJwtService, JwtService>();

//OpenAPI:n asetus debugausta varten
builder.Services.Configure<ApiBehaviorOptions>(options => { options.SuppressModelStateInvalidFilter = true; });

//XML-kommenttien käyttöönotto OpenAPI:n dokumentaatioon (Swashbuckle)
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Tietokantakontekstin konfigurointi + kannan yhteysmerkkijonon haku appsettingsistä
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentikoinnin konfigurointi
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

//CORSit
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

//SMTP-sähköposti API:n konfigurointi
var emailSettings = builder.Configuration.GetSection("EmailSettings");
builder.Services.Configure<EmailSettings>(emailSettings);
builder.Services.AddScoped<EmailService>();

//Palvelu hyllyjen luomista varten
builder.Services.AddTransient<ShelfGenerator>();

// Käyttäjäroolien konfigurointi authentikointia varten (Administrator ja Storagehandler)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IsAdmin", policy =>
    policy.RequireClaim("IsAdmin", "True"));
    options.AddPolicy("IsStorageHandler", policy =>
    policy.RequireClaim("IsStorageHandler", "True"));

});

// JWT authentikoinnin konfigurointi
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettings);

var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings["Secret"]));
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(cfg =>
{
    cfg.RequireHttpsMetadata = false;
    cfg.SaveToken = true;
    cfg.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = signingKey,
        ClockSkew = TimeSpan.Zero
    };
});


// OpenAPI:n konfiguraatio = tokenin käytön mahdollistaminen OpenAPI:ssa debugausta varten
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Vibration Monitor Reservation", Version = "v1" });

    // Määritä JWT:n Bearer-autentikaation turvallisuusjärjestelmä
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Syötä 'Bearer ' seurattuna validilla JWT-tokenilla",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    // Määritä, että jokainen operaatio vaatii JWT-tokenin Swaggerissa
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    //Kirjoita token kenttään 'Bearer' + token
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    c.OperationFilter<UnauthenticatedOperationFilterForSwagger>();
});

var app = builder.Build();


//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
});
//}

app.UseCors("AllowAllOrigins");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

