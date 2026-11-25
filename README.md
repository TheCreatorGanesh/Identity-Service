# Identity Service (Authentication & User Session Engine)

Decoupled, configuration-driven Authentication & Identity Service built with **.NET 10.0**, Clean Architecture, and provider-agnostic identity abstractions.

---

## 🏗️ Architecture Overview

The Identity Service isolates all external identity provider communications behind an abstraction (`IExternalIdentityProvider`). Business logic, user role lookups against the PostgreSQL database, and ASP.NET Core JWT minting are independent of any specific authentication vendor.

```mermaid
graph TD
    Client["Client / UI / Consumer"]

    subgraph API ["IdentityService.API"]
        Controller["AuthController<br/>(api/v1/auth/*)"]
        OpenApi["OpenAPI + Scalar UI (DeepSpace)"]
        Health["/health Endpoint"]
    end

    subgraph App ["IdentityService.Application"]
        AuthSvc["IAuthService / AuthService<br/>(Business Orchestration)"]
        JwtGen["IJwtTokenGenerator<br/>(ASP.NET Core JWT Minting)"]
    end

    subgraph Core ["IdentityService.Core"]
        IIdP["«interface» IExternalIdentityProvider"]
        FirebaseIdP["FirebaseIdentityProvider<br/>(Typed HttpClient + REST API)"]
        StubIdP["LocalStubIdentityProvider<br/>(In-Memory Testing)"]
        IUserRepo["«interface» IIdentityUserRepository"]
        DTOs["DTOs & Enums"]
    end

    subgraph Infra ["IdentityService.Infrastructure (DB & Persistence Only)"]
        PGRepo["PostgresIdentityUserRepository<br/>(Queries users table by Email)"]
    end

    subgraph External ["External Services"]
        FirebaseAuth["Firebase Auth REST API<br/>(Google Identity Toolkit v1)"]
        Postgres[("PostgreSQL DB<br/>users table")]
    end

    Client -->|HTTP POST| Controller
    Controller --> AuthSvc
    AuthSvc --> IIdP
    AuthSvc --> IUserRepo
    AuthSvc --> JwtGen

    IIdP -.->|Implemented by| FirebaseIdP
    IIdP -.->|Implemented by| StubIdP
    IUserRepo -.->|Implemented by| PGRepo

    FirebaseIdP --> FirebaseAuth
    PGRepo --> Postgres
```

---

## 🚀 Key Features

1. **Provider-Agnostic Abstraction (`IExternalIdentityProvider`)**:
   - `AuthenticateAsync(email, password)`
   - `SendPasswordResetAsync(email)`
   - `ConfirmPasswordResetAsync(resetCode, newPassword)`
   - `ChangePasswordAsync(email, currentPassword, newPassword)`
   - Normalized `AuthFailureReason` enums (`InvalidCredentials`, `UserDisabled`, `TooManyAttempts`, `InvalidOrExpiredResetCode`, `WeakPassword`, `RequiresRecentLogin`, `ProviderUnavailable`).

2. **Firebase Auth REST Implementation (`FirebaseIdentityProvider`)**:
   - Direct HTTP calls to Google Identity Toolkit v1 REST APIs (no frontend Firebase SDK required).
   - Re-authentication handshake for safe, verified password changes.
   - Anti-enumeration protection: `EMAIL_NOT_FOUND` on password reset returns generic success.

3. **Config-Driven Provider Switching**:
   - Set `"IdentityProvider:ActiveProvider": "Firebase"` or `"LocalStub"` in `appsettings.json` or environment variables.
   - `LocalStubIdentityProvider` provides zero-network, local test accounts (`test@rest.com` / `Password123!`).

4. **Identity Database Role Lookup & JWT Minting**:
   - Queries the PostgreSQL `users` table by lowercase email.
   - Mints ASP.NET Core JWTs containing standard and application claims: `sub`, `email`, `name`, `employeeId`, `ClaimTypes.Role`, and `"userrole"`.

5. **API Versioning**:
   - Enforces `/api/v1/auth/*` versioned routes across all endpoints.

---

## 🛠️ Endpoints

| Route                          | Method | Authorization | Description                                                    |
| :----------------------------- | :----- | :------------ | :------------------------------------------------------------- |
| `/api/v1/auth/login`           | `POST` | Public        | Authenticates credentials, verifies DB provisioning, mints JWT |
| `/api/v1/auth/forgot-password` | `POST` | Public        | Dispatches reset email; always returns generic 200 OK          |
| `/api/v1/auth/reset-password`  | `POST` | Public        | Confirms reset using resetCode / out-of-band token             |
| `/api/v1/auth/change-password` | `POST` | `[Authorize]` | Updates password; reads email strictly from JWT claims         |
| `/api/v1/auth/logout`          | `POST` | `[Authorize]` | Invalidate / discard session                                   |
| `/health`                   | `GET`  | Public        | Health check verifying DB and Identity Provider status         |

---

## 🧪 Running Tests & Build

```bash
# Build the solution
dotnet build IdentityService.slnx

# Run unit tests (42 test cases)
dotnet test tests/IdentityService.UnitTests/IdentityService.UnitTests.csproj
```

---

## 🐳 Docker Orchestration

```bash
# Start full containerized stack (Auth API on port 8081, PostgreSQL on 5432)
docker compose -f docker/docker-compose.yml up -d

# Start only PostgreSQL in Docker for local API debugging
docker compose -f docker/docker-compose.local.yml up -d
```
