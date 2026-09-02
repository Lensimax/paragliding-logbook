using ParagLog.Core.Abstractions;
using ParagLog.Core.Activities;
using ParagLog.Core.Common;

namespace ParagLog.UnitTests.Activities;

public class ActivityServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static CreateActivityCommand ValidCreateCommand(
        string name = "Evening glide", DateTimeOffset? startedAt = null, DateTimeOffset? endedAt = null,
        int? windSpeedKmh = null, int? windDirection = null) =>
        new(
            ActivityType.Flight, name, startedAt ?? DateTimeOffset.UtcNow, endedAt,
            DateOnly.FromDateTime(DateTime.UtcNow), "Europe/Paris", null, null, null,
            windSpeedKmh, windDirection, []);

    [Fact]
    public async Task CreateAsync_rejects_blank_name()
    {
        var service = new ActivityService(new FakeActivityRepository());

        var result = await service.CreateAsync(UserId, ValidCreateCommand(name: "   "), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Validation, result.Error!.Type);
        Assert.Equal("name", result.Error.Field);
    }

    [Fact]
    public async Task CreateAsync_rejects_end_before_start()
    {
        var service = new ActivityService(new FakeActivityRepository());
        var startedAt = DateTimeOffset.UtcNow;

        var result = await service.CreateAsync(
            UserId, ValidCreateCommand(startedAt: startedAt, endedAt: startedAt.AddMinutes(-1)), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("endedAt", result.Error!.Field);
    }

    [Fact]
    public async Task CreateAsync_rejects_end_equal_to_start()
    {
        var service = new ActivityService(new FakeActivityRepository());
        var startedAt = DateTimeOffset.UtcNow;

        var result = await service.CreateAsync(
            UserId, ValidCreateCommand(startedAt: startedAt, endedAt: startedAt), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(360)]
    public async Task CreateAsync_rejects_out_of_range_wind_direction(int windDirection)
    {
        var service = new ActivityService(new FakeActivityRepository());

        var result = await service.CreateAsync(
            UserId, ValidCreateCommand(windDirection: windDirection), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("windDirection", result.Error!.Field);
    }

    [Fact]
    public async Task CreateAsync_rejects_negative_wind_speed()
    {
        var service = new ActivityService(new FakeActivityRepository());

        var result = await service.CreateAsync(UserId, ValidCreateCommand(windSpeedKmh: -5), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("windSpeedKmh", result.Error!.Field);
    }

    [Fact]
    public async Task CreateAsync_accepts_a_valid_command()
    {
        var service = new ActivityService(new FakeActivityRepository());

        var result = await service.CreateAsync(UserId, ValidCreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Evening glide", result.Value.Name);
    }

    [Fact]
    public async Task UpdateAsync_returns_not_found_when_the_repository_finds_nothing()
    {
        var service = new ActivityService(new FakeActivityRepository(updateReturnsNull: true));

        var result = await service.UpdateAsync(UserId, Guid.NewGuid(), new UpdateActivityCommand(
            ActivityType.Flight, "Name", DateTimeOffset.UtcNow, null, DateOnly.FromDateTime(DateTime.UtcNow),
            null, null, null, null, null, null, []), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task DeleteAsync_returns_not_found_when_nothing_was_deleted()
    {
        var service = new ActivityService(new FakeActivityRepository(deleteReturns: false));

        var result = await service.DeleteAsync(UserId, Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.NotFound, result.Error!.Type);
    }

    private sealed class FakeActivityRepository(bool updateReturnsNull = false, bool deleteReturns = true)
        : IActivityRepository
    {
        public Task<Activity?> FindByIdAsync(Guid userId, Guid activityId, CancellationToken ct) =>
            Task.FromResult<Activity?>(null);

        public Task<ActivityPage> ListAsync(Guid userId, ActivityListQuery query, CancellationToken ct) =>
            Task.FromResult(new ActivityPage([], false));

        public Task<Activity> CreateAsync(Guid userId, CreateActivityCommand command, CancellationToken ct) =>
            Task.FromResult(new Activity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = command.Type,
                Name = command.Name,
                StartedAt = command.StartedAt,
                EndedAt = command.EndedAt,
                LocalDate = command.LocalDate,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });

        public Task<Activity?> UpdateAsync(Guid userId, Guid activityId, UpdateActivityCommand command, CancellationToken ct) =>
            Task.FromResult(updateReturnsNull
                ? null
                : new Activity
                {
                    Id = activityId,
                    UserId = userId,
                    Type = command.Type,
                    Name = command.Name,
                    StartedAt = command.StartedAt,
                    EndedAt = command.EndedAt,
                    LocalDate = command.LocalDate,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                });

        public Task<bool> DeleteAsync(Guid userId, Guid activityId, CancellationToken ct) => Task.FromResult(deleteReturns);
    }
}
