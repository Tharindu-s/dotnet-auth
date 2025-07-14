using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace auth.webapi.Helpers
{
    public class AppException : Exception
    {
        public AppException() { }
        public AppException(string message) : base(message) { }
        public AppException(string message, Exception inner) : base(message, inner) { }
    }

    public class UserNotFoundException : AppException
    {
        public UserNotFoundException() : base("User not found.") { }
        public UserNotFoundException(string message) : base(message) { }
    }

    public class InvalidCredentialsException : AppException
    {
        public InvalidCredentialsException() : base("Invalid username or password.") { }
        public InvalidCredentialsException(string message) : base(message) { }
    }

    public class AppUnauthorizedException : AppException
    {
        public AppUnauthorizedException() : base("Unauthorized access.") { }
        public AppUnauthorizedException(string message) : base(message) { }
    }

    public class RefreshTokenExpiredException : AppException
    {
        public RefreshTokenExpiredException() : base("Refresh token has expired.") { }
        public RefreshTokenExpiredException(string message) : base(message) { }
    }

    public class EmailAlreadyExistsException : AppException
    {
        public EmailAlreadyExistsException() : base("Email already exists.") { }
        public EmailAlreadyExistsException(string message) : base(message) { }
    }

    public class UserCreationFailedException : AppException
    {
        public UserCreationFailedException() : base("User creation failed.") { }
        public UserCreationFailedException(string message) : base(message) { }
        public UserCreationFailedException(string message, Exception inner) : base(message, inner) { }
    }

}