using DAL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Services.Helpers;
using Services.Interfaces;
using Services.Services;
using Transactions.API.Extensions;
using Transactions.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);
var dbConnectionString = builder.Configuration.GetConnectionString("DBConnectionString");
var connectionOptions = new ConnectionOptions
{
    ConnectionString = dbConnectionString
};
var authOptions = new AuthOptions
{
    Audience = builder.Configuration.GetSection("AuthOptions")["Audience"],
    Issuer = builder.Configuration.GetSection("AuthOptions")["Issuer"],
    Key = builder.Configuration.GetSection("AuthOptions")["Key"],
    Lifetime = Convert.ToInt32(builder.Configuration.GetSection("AuthOptions")["Lifetime"])

};

builder.Services.AddDbContext<ApplicationContext>(x =>
    x.UseLazyLoadingProxies().UseSqlServer(dbConnectionString, options => options.EnableRetryOnFailure()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = authOptions.Audience,
            ValidateLifetime = true,
            IssuerSigningKey = authOptions.GetSymmetricSecurityKey(),
            ValidateIssuerSigningKey = true,
        };
    });

builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<AuthHeaderOperationHeader>();

    c.SwaggerDoc("v1", new OpenApiInfo());

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token",
    });
});

builder.Services.AddIdentity<IdentityUser<int>, IdentityRole<int>>()
                .AddEntityFrameworkStores<ApplicationContext>()
                .AddSignInManager<SignInManager<IdentityUser<int>>>()
                .AddUserStore<UserStore<IdentityUser<int>, IdentityRole<int>, ApplicationContext, int, IdentityUserClaim<int>,
                        IdentityUserRole<int>, IdentityUserLogin<int>, IdentityUserToken<int>, IdentityRoleClaim<int>>>()
                .AddRoleStore<RoleStore<IdentityRole<int>, ApplicationContext, int, IdentityUserRole<int>, IdentityRoleClaim<int>>>();

builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IFileService, FileService>();
builder.Services.AddTransient<ITransactionService, TransactionService>();
builder.Services.AddSingleton(_ => authOptions);
builder.Services.AddSingleton(_ => connectionOptions);

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Features.Get<IHttpMaxRequestBodySizeFeature>()!.MaxRequestBodySize = 5_000_000;
    await next.Invoke();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API");
    });
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();