using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TemporalCodecServer.Encryption;
using TemporalCodecServer.KeyManagement;

// Make the Program class public for testing
public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // JWT Authentication (only enabled if JWT_AUTHORITY is set)
        var jwtAuthority = builder.Configuration["JWT_AUTHORITY"];
        if (!string.IsNullOrEmpty(jwtAuthority))
        {
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = jwtAuthority;
                    options.Audience = builder.Configuration["JWT_AUDIENCE"];
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true
                    };
                });
        }
        else
        {
            // Allow anonymous access in development when JWT is not configured
            builder.Services.AddAuthentication();
        }

        // Register KeyManagement (placeholder implementation for now)
        builder.Services.AddSingleton<IKeyManagement, DummyKeyManagement>();
        // Register AES Encryption Provider
        builder.Services.AddSingleton<TemporalCodecServer.Encryption.IEncryptionProvider, TemporalCodecServer.Encryption.AesEncryptionProvider>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}

// DummyKeyManagement for development/testing
public class DummyKeyManagement : IKeyManagement
{
    private static readonly byte[] _aesKey = new byte[]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
        0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
        0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20
    };

    public Task<byte[]> GetEncryptionKeyAsync(string keyId) => Task.FromResult(_aesKey);
    public Task<byte[]> GetDecryptionKeyAsync(string keyId) => Task.FromResult(_aesKey);
}

// For testing
namespace Microsoft.AspNetCore.Builder
{
    public static class WebApplicationExtensions
    {
        public static WebApplicationBuilder CreateBuilder(string[] args) => WebApplication.CreateBuilder(args);
    }
}
