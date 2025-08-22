# Auth WebAPI

A .NET 9 Web API for authentication and authorization, featuring JWT-based authentication, user registration, session management, and application client support. This project is designed for extensibility and security, suitable for modern web and mobile applications.

## Features

- JWT authentication and refresh tokens
- User registration and login
- Application client management
- Global exception handling middleware
- Email service integration
- Extensible architecture for custom services
- Entity Framework Core for data access
- Swagger/OpenAPI documentation

## Project Structure

```
├── Controllers/                # API endpoints (Account, ApplicationClient)
├── Data/                       # Entity Framework DbContext
├── DTO/                        # Data Transfer Objects
│   ├── ApplicationClient/
│   └── Auth/
├── Extentions/                 # Service and Swagger extensions
├── Helpers/                    # Exception handling and utility classes
├── Interfaces/                 # Service interfaces
├── Middleware/                 # Global exception middleware
├── Migrations/                 # EF Core migrations
├── Models/                     # Entity models (AppUser, ApplicationClient, RefreshToken)
├── Services/                   # Business logic (AuthService, TokenService, etc.)
├── Properties/                 # Launch settings
├── appsettings.json            # Configuration
├── auth.webapi.csproj          # Project file
├── Program.cs                  # Entry point
└── Docs/                       # API documentation
```

## Getting Started

### Prerequisites

- .NET 9 SDK
- Postgres Database (update connection string in `appsettings.json`)

### Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/tharindu-s/dotnet-auth.git
   cd dotnet-auth/auth.webapi
   ```
2. Restore dependencies:
   ```bash
   dotnet restore
   ```
3. Update `appsettings.json` with your database connection string and email settings.
4. Apply migrations:
   ```bash
   dotnet ef database update
   ```
5. Run the API:
   ```bash
   dotnet run
   ```
6. Swagger is configured but do not use swagger as swagger does not support API headers. Use an API tester like Postman to test the APIs with cutom headers like X-APP-ID (headers are used for identifying apps)

## Usage

- Register a new user via `/api/auth/register`
- Login via `/api/auth/login` to receive JWT and refresh token
- Manage application clients via `/api/applicationclient/*` endpoints
- Use Swagger UI for API exploration

## Architecture Overview

- **Controllers**: Handle HTTP requests and responses
- **Interfaces**: Abstractions for services
- **Services**: Business logic, authentication, token generation, email
- **Models**: Entity definitions for EF Core
- **DTOs**: Data transfer between layers
- **Middleware**: Global exception handling
- **Extensions**: Service registration and Swagger setup


## Contributing

Pull requests are available at the moment and will be available in near future.

## License

MIT

## Authentication Flow

This project implements a secure authentication flow using JWTs and refresh tokens. Below is an overview of how authentication, token storage, handling, and rotation work:

### 1. User Login

- The user submits credentials to `/api/account/login`.
- If valid, the API issues:
  - **Access Token (JWT):** Short-lived, used for API requests.
  - **Refresh Token:** Long-lived, used to obtain new access tokens.
- Both tokens are returned in the response. The client should store:
  - Access token: In memory or secure storage (never in localStorage for web apps).
  - Refresh token: In secure, httpOnly cookie or secure storage.

### 2. Accessing Protected Resources

- The client sends the access token in the `Authorization: Bearer <token>` header.
- The API validates the token and grants access if valid.

### 3. Token Expiry & Rotation

- When the access token expires, the client calls `/api/account/refresh-token` with the refresh token.
- The API validates the refresh token:
  - If valid, issues a new access token and a new refresh token (rotating the refresh token).
  - The old refresh token is invalidated and replaced in the database.
- The client updates its stored tokens.

### 4. Logout & Token Revocation

- On logout, the client calls `/api/account/logout`.
- The API deletes the refresh token from the database, invalidating future use.

### 5. Security Considerations

- Refresh tokens are stored securely in the database and mapped to users.
- Token rotation ensures that stolen refresh tokens cannot be reused.
- All sensitive operations are protected by middleware and exception handling.

### 6. Example Flow

1. **Login:** User receives access and refresh tokens.
2. **API Request:** User sends access token for protected endpoints.
3. **Token Expiry:** Client requests new tokens using refresh token.
4. **Rotation:** API issues new tokens, invalidates old refresh token.
5. **Logout:** API deletes refresh token, ending session.

