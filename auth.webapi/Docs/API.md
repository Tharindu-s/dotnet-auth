# Authentication Web API Documentation

## Overview

This is a comprehensive authentication and authorization API built with ASP.NET Core. The API provides user registration, login, token management, session tracking, and application client management capabilities.

**Base URL**: `https://localhost:5183` (Development)  
**API Version**: 1.0  
**Content-Type**: `application/json`

## Table of Contents

1. [Authentication](#authentication)
2. [Application Client Management](#application-client-management)
3. [User Authentication](#user-authentication)
4. [Session Management](#session-management)
5. [Error Handling](#error-handling)
6. [Rate Limiting](#rate-limiting)
7. [Security](#security)

---

## Authentication

### Application Client Authentication

Most endpoints require application client authentication using headers:

- `X-App-Id`: Application client GUID
- `X-Api-Key`: API key for the application client

### JWT Token Authentication

Protected endpoints require a valid JWT access token in the Authorization header:

```
Authorization: Bearer <access_token>
```

### Refresh Token

Refresh tokens are automatically set as HTTP-only cookies and are used for token refresh operations.

---

## Application Client Management

### Create Application Client

Creates a new application client and returns the API credentials.

**Endpoint**: `POST /api/application-client`

**Headers**: None required

**Request Body**:

```json
{
  "name": "string"
}
```

**Response**: `200 OK`

```json
{
  "id": "uuid",
  "rawApiKey": "string"
}
```

**Example**:

```bash
curl -X POST "https://localhost:5183/api/application-client" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "My Application"
  }'
```

**Notes**:

- The `rawApiKey` is only returned once and should be stored securely
- Use the returned `id` and `rawApiKey` for subsequent API calls

---

## User Authentication

### Register User

Registers a new user account.

**Endpoint**: `POST /api/auth/register`

**Headers**:

- `X-App-Id`: Application client GUID
- `X-Api-Key`: API key
- `User-Agent`: Client user agent (optional)

**Request Body**:

```json
{
  "fullName": "string",
  "email": "string",
  "city": "string",
  "password": "string"
}
```

**Response**: `200 OK`

```json
{
  "id": "string",
  "fullName": "string",
  "userName": "string",
  "email": "string",
  "city": "string",
  "accessToken": "string"
}
```

**Example**:

```bash
curl -X POST "https://localhost:5183/api/auth/register" \
  -H "Content-Type: application/json" \
  -H "X-App-Id: your-app-id" \
  -H "X-Api-Key: your-api-key" \
  -H "User-Agent: MyApp/1.0" \
  -d '{
    "fullName": "John Doe",
    "email": "john.doe@example.com",
    "city": "New York",
    "password": "SecurePassword123!"
  }'
```

**Validation Rules**:

- `fullName`: Required, non-empty string
- `email`: Required, valid email format
- `city`: Required, non-empty string
- `password`: Required, minimum security requirements

### Login User

Authenticates a user and returns access credentials.

**Endpoint**: `POST /api/auth/login`

**Headers**:

- `X-App-Id`: Application client GUID
- `X-Api-Key`: API key
- `User-Agent`: Client user agent (optional)

**Request Body**:

```json
{
  "email": "string",
  "password": "string"
}
```

**Response**: `200 OK`

```json
{
  "id": "string",
  "fullName": "string",
  "userName": "string",
  "email": "string",
  "city": "string",
  "accessToken": "string"
}
```

**Example**:

```bash
curl -X POST "https://localhost:5183/api/auth/login" \
  -H "Content-Type: application/json" \
  -H "X-App-Id: your-app-id" \
  -H "X-Api-Key: your-api-key" \
  -H "User-Agent: MyApp/1.0" \
  -d '{
    "email": "john.doe@example.com",
    "password": "SecurePassword123!"
  }'
```

### Refresh Token

Refreshes an expired access token using the refresh token.

**Endpoint**: `POST /api/auth/refresh`

**Headers**:

- `Authorization`: Bearer token (expired access token)
- `User-Agent`: Client user agent (optional)

**Cookies**:

- `refreshToken`: HTTP-only cookie containing the refresh token

**Response**: `200 OK`

```json
{
  "accessToken": "string"
}
```

**Example**:

```bash
curl -X POST "https://localhost:5183/api/auth/refresh" \
  -H "Authorization: Bearer expired-access-token" \
  -H "User-Agent: MyApp/1.0" \
  --cookie "refreshToken=your-refresh-token"
```

### Logout User

Logs out the current user and invalidates the refresh token.

**Endpoint**: `POST /api/auth/logout`

**Headers**:

- `Authorization`: Bearer token (valid access token)

**Cookies**:

- `refreshToken`: HTTP-only cookie containing the refresh token

**Response**: `200 OK`

```json
"Logged out successfully"
```

**Example**:

```bash
curl -X POST "https://localhost:5183/api/auth/logout" \
  -H "Authorization: Bearer valid-access-token" \
  --cookie "refreshToken=your-refresh-token"
```

---

## Session Management

### Get User Sessions

Retrieves all active sessions for the authenticated user.

**Endpoint**: `GET /api/auth/sessions`

**Headers**:

- `Authorization`: Bearer token (valid access token)

**Response**: `200 OK`

```json
[
  {
    "tokenId": 1,
    "device": "string",
    "ipAddress": "string",
    "issuedAt": "2024-01-01T00:00:00Z",
    "expiresAt": "2024-01-08T00:00:00Z"
  }
]
```

**Example**:

```bash
curl -X GET "https://localhost:5183/api/auth/sessions" \
  -H "Authorization: Bearer valid-access-token"
```

---

## Error Handling

The API uses standard HTTP status codes and returns detailed error messages.

### Common Error Responses

**400 Bad Request**

```json
{
  "message": "Validation error message",
  "statusCode": 400
}
```

**401 Unauthorized**

```json
{
  "message": "Authentication failed",
  "statusCode": 401
}
```

**403 Forbidden**

```json
{
  "message": "Access denied",
  "statusCode": 403
}
```

**404 Not Found**

```json
{
  "message": "Resource not found",
  "statusCode": 404
}
```

**500 Internal Server Error**

```json
{
  "message": "Internal server error",
  "statusCode": 500
}
```

### Specific Error Types

- `ApplicationClientAuthenticationException`: Invalid or missing application client credentials
- `RefreshTokenExpiredException`: Refresh token has expired
- `AppUnauthorizedException`: Missing or invalid access token

---

## Rate Limiting

Currently, no explicit rate limiting is implemented. Consider implementing rate limiting for production use.

---

## Security

### JWT Configuration

The API uses JWT tokens with the following configuration:

- **Issuer**: `http://localhost:5183`
- **Audience**: `http://localhost:5183`
- **Signing Key**: Configured in `appsettings.json`
- **Token Expiration**: Configurable (typically 15-60 minutes)
- **Refresh Token Expiration**: 7 days

### Security Headers

The API includes security headers:

- `Secure` cookies for HTTPS
- `HttpOnly` cookies for refresh tokens
- `SameSite=Strict` for CSRF protection

### CORS Configuration

CORS is configured to allow all origins in development. Configure appropriate origins for production.

---

## Development Setup

### Prerequisites

- .NET 8.0 or later
- PostgreSQL database
- SMTP server for email functionality

### Configuration

Update `appsettings.json` with your configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your-database-connection-string"
  },
  "JWT": {
    "Issuer": "your-issuer",
    "Audience": "your-audience",
    "SigningKey": "your-secure-signing-key"
  },
  "EmailSettings": {
    "From": "your-email@domain.com",
    "SmtpServer": "smtp.gmail.com",
    "Username": "your-email@domain.com",
  }

# Navigate to project directory

cd auth.webapi




# Start the API

dotnet run

```

The API will be available at `https://localhost:5183`

### Swagger Documentation

Interactive API documentation is available at:

- Swagger UI: `https://localhost:5183/swagger`
- OpenAPI JSON: `https://localhost:5183/swagger/v1/swagger.json`

---

## Best Practices

### Client Implementation

3. **Secure cookie handling**: Ensure refresh tokens are handled securely
4. **Error handling**: Implement proper error handling for all API responses
5. **User agent**: Include meaningful user agent strings for session tracking

### Security Considerations

1. **HTTPS only**: Always use HTTPS in production
2. **Token storage**: Store access tokens in memory, not persistent storage
3. **Logout**: Always call logout endpoint when user signs out
4. **Session monitoring**: Regularly check user sessions for security
5. **Input validation**: Validate all user inputs on client side

---

## Support

For technical support or questions about the API, please refer to the project documentation or contact the development team.

---

**Last Updated**: January 2024  
**API Version**: 1.0
