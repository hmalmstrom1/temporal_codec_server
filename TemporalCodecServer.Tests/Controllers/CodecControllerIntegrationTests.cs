using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Encodings.Web;
using TemporalCodecServer.Models;
using Xunit;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace TemporalCodecServer.Tests.Controllers
{
    public class CodecControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public CodecControllerIntegrationTests()
        {
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        // Configure test authentication
                        services.AddAuthentication("Test")
                            .AddScheme<TestAuthHandlerOptions, TestAuthHandler>(
                                "Test", options => { });

                        // Override any existing authentication
                        services.AddAuthorization(options =>
                        {
                            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                                .AddAuthenticationSchemes("Test")
                                .RequireAuthenticatedUser()
                                .Build();
                        });
                    });
                });

            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        [Fact]
        public async Task EncodeThenDecode_ReturnsOriginalMessage()
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
            Console.WriteLine($"Sending request:\n{requestJson}");

            // Helper method to log response details
            async Task<string> GetResponseDetails(HttpResponseMessage response)
            {
                var content = await response.Content.ReadAsStringAsync();
                var headers = string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));
                return $"Status: {response.StatusCode}, Headers: {headers}, Content: {content}";
            }

            // Act - Encode
            var encodeResponse = await _client.PostAsJsonAsync("/encode", request);
            var encodeResponseContent = await encodeResponse.Content.ReadAsStringAsync();
            var encodeResponseDetails = await GetResponseDetails(encodeResponse);
            
            if (!encodeResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Encode request failed. Status: {encodeResponse.StatusCode}, Content: {encodeResponseContent}");
                throw new HttpRequestException($"Encode request failed. {encodeResponseDetails}");
            }
            var encodedResult = await encodeResponse.Content.ReadFromJsonAsync<CodecResponse>();
            
            // Assert - Verify we got an encoded response
            Assert.NotNull(encodedResult);
            Assert.Single(encodedResult.Payloads);
            Assert.NotEqual(base64Message, encodedResult.Payloads[0].Data);

            // Act - Decode
            var decodeRequest = new CodecRequest
            {
                Payloads = new List<Payload>
                {
                    new Payload
                    {
                        Data = encodedResult.Payloads[0].Data,
                        Metadata = new Dictionary<string, string> { { "test", "value" } }
                    }
                }
            };

            var decodeResponse = await _client.PostAsJsonAsync("/decode", decodeRequest);
            decodeResponse.EnsureSuccessStatusCode();
            var decodedResult = await decodeResponse.Content.ReadFromJsonAsync<CodecResponse>();

            // Assert - Verify we got back the original message
            Assert.NotNull(decodedResult);
            Assert.Single(decodedResult.Payloads);
            var decodedBytes = Convert.FromBase64String(decodedResult.Payloads[0].Data);
            var decodedMessage = Encoding.UTF8.GetString(decodedBytes);
            
            Assert.Equal(originalMessage, decodedMessage);
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

                public void Dispose()
        {
            _client?.Dispose();
            _factory?.Dispose();
        }
    }

    // Test authentication handler
    public class TestAuthHandler : AuthenticationHandler<TestAuthHandlerOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<TestAuthHandlerOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { new Claim(ClaimTypes.Name, "Test user") };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    public class TestAuthHandlerOptions : AuthenticationSchemeOptions
    {
    }
}
