
using ESPG_Helpdesk_API.Model;
using ESPG_Helpdesk_API.Entity;
using ESPG_Helpdesk_API.Services_Businesslogic;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ESPG_Helpdesk_API.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using System;
using ESPG_Helpdesk_API.Repositories;
using Azure.Identity;
using Microsoft.Graph;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;


// ✅ Register SecondaryDbContext SERVICE_DESK
builder.Services.AddDbContext<ESPG_SERVICE_DESK_Db>(options =>
    options.UseSqlServer(configuration.GetConnectionString("ConnectionString_SERVICE_DESK")));

// ✅ Register PrimaryDbContext AX TEST
builder.Services.AddDbContext<TAAX63TEST_DbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("ConnectionString_AX")));

// ✅ Register PrimaryDbContext AX_LIKE
builder.Services.AddDbContext<TAAX63LIVE_DbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("ConnectionString_AX_LIKE")));

// Load SMTP Settings
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

builder.Services.AddScoped<ApproveRepository>(); // Repository
builder.Services.AddScoped<WebApproveBL>();      // BL


//Register ConnectionString Database TAAX63 LIVE & TEST
//builder.Services.AddDbContext<TAAX63TEST_DbContext>(db => db.UseSqlServer(builder.Configuration.GetConnectionString("ConnectionString_AX")), ServiceLifetime.Singleton);
//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseSqlServer(connectionString));

//Register ConnectionString Database ESPG_SERVICE_DESK_Db
//builder.Services.AddDbContext<ESPG_SERVICE_DESK_Db>(db => db.UseSqlServer(builder.Configuration.GetConnectionString("ConnectionString_ESPG_SERVICE_DESK")), ServiceLifetime.Singleton);
//Add services to the container
//builder.Services.AddSingleton<IWeb_SalesForecast_UserPermissionService, Web_SalesForecast_UserPermissionService>();

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddScoped<IWeb_SalesForecast_UserPermissionService, Web_SalesForecast_UserPermissionService>();

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
//builder.Services.AddSwaggerGen(swagger =>
//{
//    //This is to generate the Default UI of Swagger Documentation
//    swagger.SwaggerDoc("v1", new OpenApiInfo
//    {
//        Version = "v1",
//        Title = "JWT Token Authentication API",
//        Description = ".NET 8 Web API"
//    });
//    // To Enable authorization using Swagger (JWT)
//    swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
//    {
//        Name = "Authorization",
//        Type = SecuritySchemeType.ApiKey,
//        Scheme = "Bearer",
//        BearerFormat = "JWT",
//        In = ParameterLocation.Header,
//        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
//    });
//    swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
//                {
//                    {
//                          new OpenApiSecurityScheme
//                            {
//                                Reference = new OpenApiReference
//                                {
//                                    Type = ReferenceType.SecurityScheme,
//                                    Id = "Bearer"
//                                }
//                            },
//                            new string[] {}

//                    }
//                });
//});
// ✅ เพิ่ม JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://localhost:7131"; // ระบุ URL ของ Auth Server (เช่น IdentityServer, Firebase, Auth0)
        options.Audience = "your-api-resource"; // ระบุ API Audience
    });

// ✅ เพิ่ม Swagger และกำหนดให้รองรับ JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "กรุณาใส่ Token ในรูปแบบ: Bearer {your_token}"
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
            new string[] { }
        }
    });
});



builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
         policy => policy.WithOrigins(
                          "https://itservicedesk.espg.co.th",
                          "https://www.itservicedesk.espg.co.th", 

                          "https://172.23.102.6:8083",
                          "https://itservicedesk.espg.co.th:8083",
                          "https://itservicedesk.espg.co.th:8083/Login/Index",
                          "https://172.23.102.6:8084",
                          "https://itservicedesk.espg.co.th:8084",

                          //---- Local Test Server Test UAT 172.10.20.253
                          "https://172.10.20.253:8092",


                          //---- Local Test เครื่องส่วนตัว
                          "https://localhost:7187",

                          //---- Local Test Server Test UAT 172.10.20.253
                          "https://172.23.102.6:8076",
                          "https://172.23.102.6:8077"


                          )
                            .AllowAnyHeader()
                            .AllowAnyMethod());
});

// new Register Services 
var tenantId = builder.Configuration["8f377f77-44e5-4560-83dc-75feef76bebd"];
var clientId = builder.Configuration["08ffca5a-3c13-4f17-9e31-8c4a74bfdf61"];

// 👉 ใช้ Device Code Flow (Delegated)
var scopes = new[] { "Chat.ReadWrite", "ChatMessage.Send", "User.Read" };

var credential = new DeviceCodeCredential(new DeviceCodeCredentialOptions
{
    TenantId = tenantId,
    ClientId = clientId,
    DeviceCodeCallback = (code, cancellation) =>
    {
        Console.WriteLine(code.Message);
        return Task.CompletedTask;
    }
});
var graphClient = new GraphServiceClient(credential, scopes);
// Register service
builder.Services.AddSingleton<Teams_Chat>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowLocalhost");
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.UseMiddleware<JwtMiddleware>();
app.Run();