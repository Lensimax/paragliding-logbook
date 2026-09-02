using ParagLog.Core.Common;

namespace ParagLog.Api.Common;

public static class ResultExtensions
{
    public static IResult ToProblem(this Result result)
    {
        var error = result.Error ?? throw new InvalidOperationException("Successful results have no problem to render.");

        return error.Type switch
        {
            DomainErrorType.Validation => Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [error.Field ?? "_"] = [error.Message],
            }),
            DomainErrorType.Conflict => Results.Conflict(new { error.Message, error.Field }),
            DomainErrorType.Unauthorized => Results.Unauthorized(),
            DomainErrorType.NotFound => Results.NotFound(new { error.Message }),
            _ => Results.Problem(error.Message),
        };
    }
}
