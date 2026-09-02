namespace ParagLog.Core.Common;

public enum DomainErrorType
{
    Validation,
    Conflict,
    Unauthorized,
    NotFound,
}

public sealed record DomainError(DomainErrorType Type, string Message, string? Field = null)
{
    public static DomainError Validation(string message, string? field = null) =>
        new(DomainErrorType.Validation, message, field);

    public static DomainError Conflict(string message, string? field = null) =>
        new(DomainErrorType.Conflict, message, field);

    public static DomainError Unauthorized(string message) =>
        new(DomainErrorType.Unauthorized, message);

    public static DomainError NotFound(string message) =>
        new(DomainErrorType.NotFound, message);
}
