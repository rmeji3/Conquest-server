namespace Ping.Dtos.Auth;

public record AppleLoginDto(string IdToken, string? FirstName, string? LastName);

