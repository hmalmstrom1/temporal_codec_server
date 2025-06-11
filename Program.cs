using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TemporalCodecServer.KeyManagement;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["JWT_AUTHORITY"];
        options.Audience = builder.Configuration["JWT_AUDIENCE"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

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

// DummyKeyManagement for development/testing
public class DummyKeyManagement : IKeyManagement
{
    public Task<byte[]> GetEncryptionKeyAsync(string keyId) => Task.FromResult(System.Text.Encoding.UTF8.GetBytes("dummy-encryption-key"));
    public Task<byte[]> GetDecryptionKeyAsync(string keyId) => Task.FromResult(System.Text.Encoding.UTF8.GetBytes("dummy-decryption-key"));
}
