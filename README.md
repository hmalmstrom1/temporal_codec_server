# Temporal Codec Server (.NET)

This project implements a Temporal Codec server in C#/.NET as described in the [Temporal documentation](https://docs.temporal.io/codec-server) and [encryption deployment guide](https://docs.temporal.io/production-deployment/data-encryption#codec-server-setup).

## Features
- Implements the Temporal Codec server API endpoints (`/encode`, `/decode`, `/health`) for data encryption/decryption.
- JWT-based authorization for all endpoints.
- Ready for AWS Secrets Manager integration for key management (see `KeyManagement` interface).

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)

### Run locally
```sh
dotnet run
```

### Environment Variables
- `AWS_REGION` (when deployed)
- `AWS_SECRET_ID` (when deployed)
- `JWT_AUTHORITY` (for JWT validation)
- `JWT_AUDIENCE` (for JWT validation)

## References
- [Temporal Codec Server Protocol](https://docs.temporal.io/codec-server)
- [Temporal Data Encryption](https://docs.temporal.io/production-deployment/data-encryption#codec-server-setup)

---

This project is scaffolded as an ASP.NET Core Web API. See `Program.cs` and `Controllers/CodecController.cs` for main logic.
