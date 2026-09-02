using ParagLog.Api.Auth;
using ParagLog.Api.Common;
using ParagLog.Api.Contracts.Equipment;
using ParagLog.Core.Equipment;

namespace ParagLog.Api.Endpoints;

public static class EquipmentEndpoints
{
    public static void MapEquipmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/equipment").RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapPost("/", CreateAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);
        group.MapPost("/{id:guid}/retire", RetireAsync);
    }

    private static async Task<IResult> ListAsync(ICurrentUser user, EquipmentService equipment, CancellationToken ct)
    {
        var items = await equipment.ListAsync(user.Id, ct);
        return Results.Ok(items.Select(EquipmentSummaryResponse.From).ToList());
    }

    private static async Task<IResult> CreateAsync(
        CreateEquipmentRequest request, ICurrentUser user, EquipmentService equipment, CancellationToken ct)
    {
        var result = await equipment.CreateAsync(user.Id, request.ToCommand(), ct);
        return result.IsSuccess
            ? Results.Created($"/api/equipment/{result.Value.Id}", EquipmentResponse.FromWithoutUsage(result.Value))
            : result.ToProblem();
    }

    private static async Task<IResult> GetAsync(Guid id, ICurrentUser user, EquipmentService equipment, CancellationToken ct)
    {
        var detail = await equipment.GetAsync(user.Id, id, ct);
        return detail is null ? Results.NotFound() : Results.Ok(EquipmentResponse.From(detail));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateEquipmentRequest request, ICurrentUser user, EquipmentService equipment, CancellationToken ct)
    {
        var result = await equipment.UpdateAsync(user.Id, id, request.ToCommand(), ct);
        return result.IsSuccess ? Results.Ok(EquipmentResponse.FromWithoutUsage(result.Value)) : result.ToProblem();
    }

    private static async Task<IResult> DeleteAsync(Guid id, ICurrentUser user, EquipmentService equipment, CancellationToken ct)
    {
        var result = await equipment.DeleteAsync(user.Id, id, ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    private static async Task<IResult> RetireAsync(Guid id, ICurrentUser user, EquipmentService equipment, CancellationToken ct)
    {
        var result = await equipment.RetireAsync(user.Id, id, ct);
        return result.IsSuccess ? Results.Ok(EquipmentResponse.FromWithoutUsage(result.Value)) : result.ToProblem();
    }
}
