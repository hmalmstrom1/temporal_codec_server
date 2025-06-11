# Temporal Codec Server - Tests

This directory contains integration tests for the Temporal Codec Server. The tests verify the functionality of the encode and decode endpoints.

## Test Setup

The test project is set up using:
- xUnit as the test framework
- Microsoft's test server for in-memory testing of the ASP.NET Core application
- A custom test authentication handler to bypass JWT authentication during tests
- A test key management implementation for consistent encryption/decryption keys

### Test Authentication

Tests use a custom `TestAuthHandler` that provides a simple authentication mechanism:
- Authentication Scheme: `Test`
- Automatically authenticates all requests with a test user
- User claims include:
  - Name: "TestUser"
  - NameIdentifier: "1"
  - Role: "User"

### Test Key Management

The `TestKeyManagement` class provides deterministic encryption keys for testing:
- Always returns the same 32-byte key for both encryption and decryption
- Ensures consistent test results across test runs

## Running Tests

### Prerequisites

- .NET 8.0 SDK or later
- All NuGet packages restored (run `dotnet restore` if needed)

### Running All Tests

```bash
dotnet test
```

### Running Specific Tests

To run only the integration tests:

```bash
dotnet test --filter "FullyQualifiedName~CodecControllerIntegrationTests"
```

To run tests with detailed output:

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Running Tests in Debug Mode

1. Open the solution in your IDE (like Visual Studio or VS Code)
2. Set breakpoints in the test methods as needed
3. Run the tests using the test explorer or debugger

## Test Cases

### `EncodeThenDecode_ReturnsOriginalMessage`

Verifies that encoding and then decoding a message returns the original content.

1. Sends a test message to the `/encode` endpoint
2. Takes the encoded result and sends it to the `/decode` endpoint
3. Verifies the decoded message matches the original

### `Encode_WithInvalidBase64_ReturnsBadRequest`

Verifies that providing invalid base64 data returns a 400 Bad Request.

## Debugging Tests

If a test fails, the test output includes detailed logging:
- Request payloads
- Response status codes and content
- Any exceptions that occurred

To see detailed logs, run:

```bash
dotnet test --logger "console;verbosity=detailed"
```

## Writing New Tests

1. Add a new test method with the `[Fact]` attribute
2. Use the `_client` field to make HTTP requests to the test server
3. Use assertions to verify the results
4. Add appropriate error handling and logging

Example test method:

```csharp
[Fact]
public async Task MyNewTest()
{
    // Arrange
    var request = new TestRequest { /* ... */ };
    
    // Act
    var response = await _client.PostAsJsonAsync("/api/endpoint", request);
    
    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<ExpectedType>();
    Assert.NotNull(result);
    // More assertions...
}
```

## Dependencies

- `Microsoft.AspNetCore.Mvc.Testing` - For test server and WebApplicationFactory
- `Microsoft.NET.Test.Sdk` - Test SDK
- `xunit` - Test framework
- `xunit.runner.visualstudio` - Test runner
- `Microsoft.Extensions.TimeProvider.Testing` - For testing time-dependent code

## Troubleshooting

### Test Fails with Authentication Errors

- Ensure the test authentication handler is properly registered in the test setup
- Verify the `Authorization` header is being set correctly on test requests

### Test Fails with Serialization Errors

- Check that all required properties are set on request objects
- Ensure the request content type is set to `application/json`
- Verify that the model classes match the expected JSON structure

### Tests Run Slowly

- The test server is started once per test class by default
- Consider using `IClassFixture` to share the server between test classes if needed
- Avoid making real HTTP calls in tests; use the test server instead
