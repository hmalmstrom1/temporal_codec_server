using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using TemporalCodecServer.Encryption;
using TemporalCodecServer.KeyManagement;
using TemporalCodecServer.Models;
using Xunit;

// Test Authentication Handler using TimeProvider
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "Test";

    // Primary constructor using TimeProvider
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    // Backward compatibility constructor marked as obsolete
    [Obsolete("Use the constructor without ISystemClock parameter. The recommended approach is to use TimeProvider from the base class.")]
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder)
    {
        // The clock parameter is ignored as we're using TimeProvider from the base class
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] {
            new Claim(ClaimTypes.Name, "TestUser"),
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "User"),
        };
        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

// Test Key Management
public class TestKeyManagement : IKeyManagement
{
    public Task<byte[]> GetEncryptionKeyAsync(string keyId)
    {
        // Return a fixed 32-byte key for AES-256
        var key = new byte[32];
        for (int i = 0; i < key.Length; i++)
        {
            key[i] = (byte)(i % 256);
        }
        return Task.FromResult(key);
    }

    public Task<byte[]> GetDecryptionKeyAsync(string keyId)
    {
        // Use the same key for encryption and decryption in tests
        return GetEncryptionKeyAsync(keyId);
    }
}

namespace TemporalCodecServer.Tests.Controllers
{

    public class CodecControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public CodecControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Remove any existing authentication handlers
                    var authDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IAuthenticationHandlerProvider));
                    if (authDescriptor != null)
                    {
                        services.Remove(authDescriptor);
                    }

                    // Add test authentication
                    services.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                            TestAuthHandler.AuthenticationScheme, options => { });

                    // Replace the IKeyManagement with our test implementation
                    var keyManagementDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IKeyManagement));
                    if (keyManagementDescriptor != null)
                    {
                        services.Remove(keyManagementDescriptor);
                    }
                    services.AddSingleton<IKeyManagement, TestKeyManagement>();

                    // Set the encryption provider to AES for tests using environment variables
                    Environment.SetEnvironmentVariable("ENCRYPTION_PROVIDER", "AES");
                    
                    // Replace any existing encryption provider
                    var encryptionDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IEncryptionProvider));
                    if (encryptionDescriptor != null)
                    {
                        services.Remove(encryptionDescriptor);
                    }
                    services.AddSingleton<IEncryptionProvider, AesEncryptionProvider>();
                });
            });

            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });

            // Set default request headers for authentication
            _client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "test-token");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        [Fact]
        public async Task EncodeThenDecode_ReturnsOriginalMessage()
        {
            try 
            {
                // Arrange
                var originalMessage = "Hello, Temporal!";
                var base64Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalMessage));
                
                var request = new CodecRequest
                {
                    Payloads = new List<Payload>
                    {
                        new Payload
                        {
                            Data = base64Message,
                            Metadata = new Dictionary<string, string> { { "test", "value" } }
                        }
                    },
                    Metadata = new Dictionary<string, string> { { "requestId", "test-request-123" } }
                };

                // Log the request being sent
                var requestJson = System.Text.Json.JsonSerializer.Serialize(request, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine($"[TEST] Sending request to /encode:\n{requestJson}");
                
                // Print request headers for debugging
                Console.WriteLine($"[TEST] Request headers:\n{string.Join("\n", _client.DefaultRequestHeaders.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");

            // Helper method to log response details
            async Task<string> GetResponseDetails(HttpResponseMessage response)
            {
                var content = await response.Content.ReadAsStringAsync();
                var headers = string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));
                return $"Status: {response.StatusCode}, Headers: {headers}, Content: {content}";
            }

                // Act - Encode
                Console.WriteLine("[TEST] Sending encode request...");
                var encodeResponse = await _client.PostAsJsonAsync("/encode", request);
                var encodeResponseContent = await encodeResponse.Content.ReadAsStringAsync();
                var encodeResponseDetails = await GetResponseDetails(encodeResponse);
                
                Console.WriteLine($"[TEST] Encode response status: {encodeResponse.StatusCode}");
                Console.WriteLine($"[TEST] Encode response content: {encodeResponseContent}");
                
                if (!encodeResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Encode request failed. Status: {encodeResponse.StatusCode}, Content: {encodeResponseContent}");
                }
                
                var encodedResult = await encodeResponse.Content.ReadFromJsonAsync<CodecResponse>();
                if (encodedResult?.Payloads == null)
                {
                    throw new InvalidOperationException("Encoded result or its Payloads is null");
                }
                Console.WriteLine($"[TEST] Successfully encoded. Payloads count: {encodedResult.Payloads.Count}");
                
                // Act - Decode
                Console.WriteLine("[TEST] Sending decode request...");
                var decodeRequest = new CodecRequest
                {
                    Payloads = encodedResult.Payloads,
                    Metadata = request.Metadata ?? new Dictionary<string, string>()
                };
                
                var decodeResponse = await _client.PostAsJsonAsync("/decode", decodeRequest);
                var decodeResponseContent = await decodeResponse.Content.ReadAsStringAsync();
                
                Console.WriteLine($"[TEST] Decode response status: {decodeResponse.StatusCode}");
                Console.WriteLine($"[TEST] Decode response content: {decodeResponseContent}");
                
                if (!decodeResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Decode request failed. Status: {decodeResponse.StatusCode}, Content: {decodeResponseContent}");
                }
                
                var decodedResult = await decodeResponse.Content.ReadFromJsonAsync<CodecResponse>();
                
                // Assert
                Console.WriteLine("[TEST] Running assertions...");
                Assert.NotNull(decodedResult);
                Assert.NotNull(decodedResult.Payloads);
                Assert.NotEmpty(decodedResult.Payloads);
                
                var payload = decodedResult.Payloads[0];
                Assert.NotNull(payload);
                Assert.NotNull(payload.Data);
                
                var decodedBytes = Convert.FromBase64String(payload.Data);
                var decodedMessage = Encoding.UTF8.GetString(decodedBytes);
                
                Console.WriteLine($"[TEST] Original: '{originalMessage}', Decoded: '{decodedMessage}'");
                Assert.Equal(originalMessage, decodedMessage);
                Console.WriteLine("[TEST] Test completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST ERROR] {ex}");
                throw;
            }
        }

        [Fact]
        public async Task Encode_WithInvalidBase64_ReturnsBadRequest()
        {
            // Arrange
            var request = new CodecRequest
            {
                Payloads = new List<Payload>
                {
                    new Payload
                    {
                        Data = "not-valid-base64",
                        Metadata = new Dictionary<string, string>()
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/encode", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client?.Dispose();
                _factory?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }


}
