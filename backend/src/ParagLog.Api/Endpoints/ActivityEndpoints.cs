using ParagLog.Api.Auth;
using ParagLog.Api.Common;
using ParagLog.Api.Contracts.Activities;
using ParagLog.Core.Activities;

namespace ParagLog.Api.Endpoints;

public static class ActivityEndpoints
{
    public static void MapActivityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/activities").RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapPost("/", CreateAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);
    }

    private static async Task<IResult> ListAsync(
        ICurrentUser user, ActivityService activities, ActivityType? type, string? cursor, int? limit, CancellationToken ct)
    {
        var position = ActivityCursor.Decode(cursor);
        var page = await activities.ListAsync(user.Id, type, position?.StartedAt, position?.Id, limit, ct);

        var items = page.Items.Select(ActivitySummaryResponse.From).ToList();
        var nextCursor = page.HasMore && page.Items.Count > 0
            ? ActivityCursor.Encode(page.Items[^1].StartedAt, page.Items[^1].Id)
            : null;

        return Results.Ok(new ActivityListResponse(items, nextCursor));
    }

    private static async Task<IResult> CreateAsync(
        CreateActivityRequest request, ICurrentUser user, ActivityService activities, CancellationToken ct)
    {
        var result = await activities.CreateAsync(user.Id, request.ToCommand(), ct);
        return result.IsSuccess
            ? Results.Created($"/api/activities/{result.Value.Id}", ActivityResponse.From(result.Value))
            : result.ToProblem();
    }

    private static async Task<IResult> GetAsync(Guid id, ICurrentUser user, ActivityService activities, CancellationToken ct)
    {
        var activity = await activities.GetAsync(user.Id, id, ct);
        return activity is null ? Results.NotFound() : Results.Ok(ActivityResponse.From(activity));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateActivityRequest request, ICurrentUser user, ActivityService activities, CancellationToken ct)
    {
        var result = await activities.UpdateAsync(user.Id, id, request.ToCommand(), ct);
        return result.IsSuccess ? Results.Ok(ActivityResponse.From(result.Value)) : result.ToProblem();
    }

    private static async Task<IResult> DeleteAsync(Guid id, ICurrentUser user, ActivityService activities, CancellationToken ct)
    {
        var result = await activities.DeleteAsync(user.Id, id, ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }
}
