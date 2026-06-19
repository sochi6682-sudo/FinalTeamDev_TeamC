using NLog.Web;
using Scalar.AspNetCore;
using System.Text.Json;
using FinalAssigenment.Repositories;
using FinalAssigenment.Services;


namespace FinalAssigenment;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddOpenApi();
        builder.Services.AddScoped<SqlRepository>();
        builder.Services.AddScoped<ShelfSystemService>();

        builder.Services.AddControllers()
        .AddJsonOptions(options =>
         {
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
         });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFetch", policy =>
            {
                policy
                    .WithOrigins("")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
        builder.Logging.ClearProviders();
        builder.Host.UseNLog();
        builder.Services.AddHttpClient();
        // Add services to the container.
        builder.Services.AddRazorPages();

        var app = builder.Build();
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(); 
        }
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseCors("AllowFetch");

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllers();

        app.MapStaticAssets();
        app.MapRazorPages()
           .WithStaticAssets();

        app.Run();
    }
}
