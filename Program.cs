using GranitWebApi.Configurations;
using GranitWebApi.Data;
using GranitWebApi.Helpers;
using GranitWebApi.Services;
using GranitWebApi.Services.BackgroundServices;
using GranitWebApi.Services.Email;
using GranitWebApi.Services.Notifications;
using GranitWebApi.Services.Reports.Uretim;
using GranitWebApi.Services.Sales;
using GranitWebApi.Services.Sales.Definitions;



//using GranitWebApi.Services.Background;
using GranitWebApi.Services.Trendyol;
using GranitWebApi.Services.Ui;
using GranitWebApi.Services.Upload;
using GranitWebApi.Services.Upload.Interfaces;
using GranitWebApi.Workflow.Interfaces;
using GranitWebApi.Workflow.Resolvers;
using GranitWebApi.Workflow.Services;
//using GranitWebApi.Services.XML;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); 

// ✅ CORS EKLE
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",
        policy =>
        {
            policy.WithOrigins(
                    "http://localhost:3000",
                    "https://localhost:3000",
                    "http://192.168.1.222",
                    "http://192.168.1.222:8081")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserContext>();


// 🔐 JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!)
        ),

        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine(context.Exception.ToString());
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Bearer {token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

//WORKFLOW SERVİS EKLE
builder.Services.AddScoped<IApprovalResolver, NoneResolver>();
builder.Services.AddScoped<IApprovalResolver, ManagerChainResolver>();
builder.Services.AddScoped<IApprovalResolver, RoleResolver>();
builder.Services.AddScoped<IApprovalResolver, UserResolver>();
builder.Services.AddScoped<IApprovalResolver, SqlResolver>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();

// Mail Settings, Email ve Bildirim Service ekle
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<WorkflowNotificationService>();

builder.Services.AddHostedService<EmailQueueWorker>();
// PDF Servis ekle
builder.Services.AddSingleton<PdfService>();

builder.Services.AddHttpClient();

// DB Context ekle
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.UseCompatibilityLevel(120);
        });
    options.EnableSensitiveDataLogging();
    options.LogTo(Console.WriteLine);
});

builder.Services.AddDbContext<ErpDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ERPConnection")));

builder.Services.AddHttpClient<ITrendyolService, TrendyolService>();
//Upload Servisler
builder.Services.AddScoped<IPromanageOeeUploadService,PromanageOeeUploadService>();
builder.Services.AddScoped<IPromanageGunlukUretimUploadService, PromanageGunlukUretimUploadService>();
builder.Services.AddScoped<IUretimDashboardService,UretimDashboardService>();

// UI
builder.Services.AddScoped<IUiMenuService, UiMenuService>();

// Sales Order Servisler
builder.Services.AddScoped<ISalesOrderService, SalesOrderService>();
builder.Services.AddScoped<ISalesOrderLineService, SalesOrderLineService>();
builder.Services.AddScoped<ISalesOrderLineCKConfigurationService,SalesOrderLineCKConfigurationService>();
builder.Services.AddScoped<ISalesOrderLineImageService,SalesOrderLineImageService>();
builder.Services.AddScoped<ISalesOrderLineTechnicalItemService,SalesOrderLineTechnicalItemService>();
builder.Services.AddScoped<ISalesOrderAssemblyCodeRequestService,SalesOrderAssemblyCodeRequestService>();
builder.Services.AddScoped<ISalesDefinitionService, SalesDefinitionService>();
builder.Services.AddScoped<ISalesOrderPackageService, SalesOrderPackageService>();

var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

// ✅ CORS burada olmalı (Authentication'dan önce koymak en sağlıklısı)
app.UseCors("AllowReact");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
