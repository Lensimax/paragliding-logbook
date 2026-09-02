namespace ParagLog.Api.Contracts.Auth;

public sealed record RegisterRequest(string Username, string Email, string Password, string PasswordConfirmation);
