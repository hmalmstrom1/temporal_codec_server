# Temporal Codec Server (.NET)
NOTE: This is a work in progress and is not yet ready for production use.  It was also largely AI generated, so please use with caution.

This project implements a Temporal Codec server in C#/.NET as described in the [Temporal documentation](https://docs.temporal.io/codec-server) and [encryption deployment guide](https://docs.temporal.io/production-deployment/data-encryption#codec-server-setup).

## Features
- Implements the Temporal Codec server API endpoints (`/encode`, `/decode`, `/health`) for data encryption/decryption.
- JWT-based authorization for all endpoints.
- Multiple encryption providers supported (AES, KMS).
- Configurable via environment variables.
- Ready for AWS Secrets Manager integration for key management (see `KeyManagement` interface).

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)

### Run locally
```sh
dotnet run
```

### Encryption Providers

The server supports multiple encryption providers that can be configured via the `ENCRYPTION_PROVIDER` environment variable.

#### AES Provider (Default)
Uses AES-256 encryption with a static key (for development only).

```bash
ENCRYPTION_PROVIDER=AES
```

#### AWS KMS Provider
Uses AWS Key Management Service for encryption/decryption.

```bash
ENCRYPTION_PROVIDER=KMS
AWS_REGION=us-west-2
AWS_ACCESS_KEY_ID=your-access-key
AWS_SECRET_ACCESS_KEY=your-secret-key
# Optional: AWS_SESSION_TOKEN=your-session-token # If using temporary credentials
```

### Authentication

#### JWT Authentication
Required for all endpoints when JWT is configured.

```bash
JWT_AUTHORITY=https://your-auth0-domain/
JWT_AUDIENCE=your-audience
```

#### Development Mode
When neither JWT configuration is provided, the server runs in development mode with no authentication.

### AWS Integration (for KMS)
- `AWS_REGION`: The AWS region where your KMS keys are stored (required for KMS provider)
- `AWS_ACCESS_KEY_ID`: AWS access key (required for KMS provider if not using instance profiles)
- `AWS_SECRET_ACCESS_KEY`: AWS secret key (required for KMS provider if not using instance profiles)
- `AWS_SESSION_TOKEN`: AWS session token (required when using temporary credentials)

## References
- [Temporal Codec Server Protocol](https://docs.temporal.io/codec-server)
- [Temporal Data Encryption](https://docs.temporal.io/production-deployment/data-encryption#codec-server-setup)

---

This project is scaffolded as an ASP.NET Core Web API. See `Program.cs` and `Controllers/CodecController.cs` for main logic.
