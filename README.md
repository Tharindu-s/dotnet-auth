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
- SQL Server (or update connection string in `appsettings.json`)

### Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/<your-username>/dotnet-auth.git
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
6. Swagger is configured but do not use swagger as swagger does not support API headers. Use an API tester like Postman to test the APIs with cutom headers like X-APP-ID

## Usage

- Register a new user via `/api/account/register`
- Login via `/api/account/login` to receive JWT and refresh token
- Manage application clients via `/api/applicationclient/*` endpoints
- Use Swagger UI for API exploration

## Architecture Overview

- **Controllers**: Handle HTTP requests and responses
- **Services**: Business logic, authentication, token generation, email
- **Models**: Entity definitions for EF Core
- **DTOs**: Data transfer between layers
- **Middleware**: Global exception handling
- **Extensions**: Service registration and Swagger setup
- **Interfaces**: Abstractions for services

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## License

MIT
