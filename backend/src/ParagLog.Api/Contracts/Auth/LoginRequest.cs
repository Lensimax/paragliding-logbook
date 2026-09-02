namespace ParagLog.Api.Contracts.Auth;

public sealed record LoginRequest(string Email, string Password, bool StayConnected);
