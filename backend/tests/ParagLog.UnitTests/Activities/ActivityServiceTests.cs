using ParagLog.Core.Abstractions;
using ParagLog.Core.Activities;
using ParagLog.Core.Common;
using ParagLog.Core.Equipment;
using ParagLog.Core.Export;

namespace ParagLog.UnitTests.Activities;

public class ActivityServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private const string UserPublicId = "bob-ab12x";

    private static CreateActivityCommand ValidCreateCommand(
        string name = "Evening glide", DateTimeOffset? startedAt = null, DateTimeOffset? endedAt = null,
        int? windSpeedKmh = null, int? windDirection = null, IReadOnlyList<Guid>? equipmentIds = null,
        ActivityType type = ActivityType.Flight, int? maxAltitudeM = null, int? altitudeGainM = null, double? distanceKm = null) =>
        new(
            type, name, startedAt ?? DateTimeOffset.UtcNow, endedAt,
            DateOnly.FromDateTime(DateTime.UtcNow), "Europe/Paris", null, null, null,
            windSpeedKmh, windDirection, equipmentIds ?? [],
            MaxAltitudeM: maxAltitudeM, AltitudeGainM: altitudeGainM, DistanceKm: distanceKm);

    private static ActivityService CreateService(
        bool updateReturnsNull = false, bool deleteReturns = true, int ownedEquipmentCount = int.MaxValue,
        FakeActivityRepository? activityRepository = null, FakeBlobStore? blobStore = null) =>
        new(
            activityRepository ?? new FakeActivityRepository(updateReturnsNull, deleteReturns),
            new FakeEquipmentRepository(ownedEquipmentCount),
            blobStore ?? new FakeBlobStore());

    [Fact]
    public async Task CreateAsync_rejects_blank_name()
    {
        var service = CreateService();

        var result = await service.CreateAsync(UserId, ValidCreateCommand(name: "   "), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Validation, result.Error!.Type);
        Assert.Equal("name", result.Error.Field);
    }

    [Fact]
    public async Task CreateAsync_rejects_end_before_start()
    {
        var service = CreateService();
        var startedAt = DateTimeOffset.UtcNow;

        var result = await service.CreateAsync(
            UserId, ValidCreateCommand(startedAt: startedAt, endedAt: startedAt.AddMinutes(-1)), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("endedAt", result.Error!.Field);
    }

    [Fact]
    public async Task CreateAsync_rejects_end_equal_to_start()
    {
        var service = CreateService();
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
        var service = CreateService();

        var result = await service.CreateAsync(
            UserId, ValidCreateCommand(windDirection: windDirection), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("windDirection", result.Error!.Field);
    }

    [Fact]
    public async Task CreateAsync_rejects_negative_wind_speed()
    {
        var service = CreateService();

        var result = await service.CreateAsync(UserId, ValidCreateCommand(windSpeedKmh: -5), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("windSpeedKmh", result.Error!.Field);
    }

    [Fact]
    public async Task CreateAsync_accepts_a_valid_command()
    {
        var service = CreateService();

        var result = await service.CreateAsync(UserId, ValidCreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Evening glide", result.Value.Name);
    }

    [Fact]
    public async Task CreateAsync_rejects_equipment_the_user_does_not_own()
    {
        var service = CreateService(ownedEquipmentCount: 0);

        var result = await service.CreateAsync(
            UserId, ValidCreateCommand(equipmentIds: [Guid.NewGuid()]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("equipmentIds", result.Error!.Field);
    }

    [Fact]
    public async Task CreateAsync_rejects_flight_stats_on_a_ground_handling_activity()
    {
        var service = CreateService();

        var result = await service.CreateAsync(
            UserId, ValidCreateCommand(type: ActivityType.GroundHandling, maxAltitudeM: 1500), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public async Task UpdateAsync_returns_not_found_when_the_repository_finds_nothing()
    {
        var service = CreateService(updateReturnsNull: true);

        var result = await service.UpdateAsync(UserId, Guid.NewGuid(), new UpdateActivityCommand(
            ActivityType.Flight, "Name", DateTimeOffset.UtcNow, null, DateOnly.FromDateTime(DateTime.UtcNow),
            null, null, null, null, null, null, []), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task DeleteAsync_returns_not_found_when_nothing_was_deleted()
    {
        var service = CreateService(deleteReturns: false);

        var result = await service.DeleteAsync(UserId, UserPublicId, Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task DeleteAsync_deletes_the_blob_folder_after_the_row_is_gone()
    {
        var blobStore = new FakeBlobStore();
        var service = CreateService(blobStore: blobStore);
        var activityId = Guid.NewGuid();

        var result = await service.DeleteAsync(UserId, UserPublicId, activityId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal((UserPublicId, activityId), blobStore.DeletedFolder);
    }

    [Fact]
    public async Task UploadTrackAsync_rejects_a_ground_handling_activity()
    {
        var repository = new FakeActivityRepository(activityType: ActivityType.GroundHandling);
        var blobStore = new FakeBlobStore();
        var service = CreateService(activityRepository: repository, blobStore: blobStore);

        var track = new TrackReference { Filename = "track.gpx", Format = TrackFormat.Gpx, SizeBytes = 100, Sha256 = "abc" };
        var result = await service.UploadTrackAsync(
            UserId, UserPublicId, Guid.NewGuid(), track, new MemoryStream(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Validation, result.Error!.Type);
        Assert.False(blobStore.SaveWasCalled);
    }

    [Fact]
    public async Task UploadTrackAsync_saves_the_blob_before_updating_the_row()
    {
        var repository = new FakeActivityRepository(activityType: ActivityType.Flight);
        var blobStore = new FakeBlobStore();
        var service = CreateService(activityRepository: repository, blobStore: blobStore);

        var track = new TrackReference { Filename = "track.gpx", Format = TrackFormat.Gpx, SizeBytes = 100, Sha256 = "abc" };
        var result = await service.UploadTrackAsync(
            UserId, UserPublicId, Guid.NewGuid(), track, new MemoryStream(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(blobStore.SaveWasCalled);
        Assert.True(repository.SetTrackWasCalled);
    }

    [Fact]
    public async Task DeleteTrackAsync_returns_not_found_when_the_activity_has_no_track()
    {
        var repository = new FakeActivityRepository(track: null);
        var service = CreateService(activityRepository: repository);

        var result = await service.DeleteTrackAsync(UserId, UserPublicId, Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.NotFound, result.Error!.Type);
    }

    private sealed class FakeActivityRepository(
        bool updateReturnsNull = false,
        bool deleteReturns = true,
        ActivityType activityType = ActivityType.Flight,
        TrackReference? track = null) : IActivityRepository
    {
        public bool SetTrackWasCalled { get; private set; }

        public Task<Activity?> FindByIdAsync(Guid userId, Guid activityId, CancellationToken ct) =>
            Task.FromResult<Activity?>(new Activity
            {
                Id = activityId,
                UserId = userId,
                Type = activityType,
                Name = "Existing",
                StartedAt = DateTimeOffset.UtcNow,
                LocalDate = DateOnly.FromDateTime(DateTime.UtcNow),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Track = track,
            });

        public Task<ActivityPage> ListAsync(Guid userId, ActivityListQuery query, CancellationToken ct) =>
            Task.FromResult(new ActivityPage([], false));

        public Task<IReadOnlyList<ActivityExportRow>> ListAllForExportAsync(Guid userId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ActivityExportRow>>([]);

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

        public Task<Activity?> SetTrackAsync(Guid userId, Guid activityId, TrackReference? newTrack, CancellationToken ct)
        {
            SetTrackWasCalled = true;
            return Task.FromResult<Activity?>(new Activity
            {
                Id = activityId,
                UserId = userId,
                Type = activityType,
                Name = "Existing",
                StartedAt = DateTimeOffset.UtcNow,
                LocalDate = DateOnly.FromDateTime(DateTime.UtcNow),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Track = newTrack,
            });
        }

        public Task<Activity?> SetHasElevationAsync(Guid userId, Guid activityId, bool hasElevation, CancellationToken ct) =>
            Task.FromResult<Activity?>(new Activity
            {
                Id = activityId,
                UserId = userId,
                Type = activityType,
                Name = "Existing",
                StartedAt = DateTimeOffset.UtcNow,
                LocalDate = DateOnly.FromDateTime(DateTime.UtcNow),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Track = track,
                HasElevation = hasElevation,
            });
    }

    private sealed class FakeBlobStore : IBlobStore
    {
        public bool SaveWasCalled { get; private set; }
        public (string UserPublicId, Guid ActivityId)? DeletedFolder { get; private set; }

        public Task SaveTrackAsync(string userPublicId, Guid activityId, TrackFormat format, Stream content, CancellationToken ct)
        {
            SaveWasCalled = true;
            return Task.CompletedTask;
        }

        public Task<Stream?> OpenTrackAsync(string userPublicId, Guid activityId, TrackFormat format, CancellationToken ct) =>
            Task.FromResult<Stream?>(null);

        public Task DeleteTrackAsync(string userPublicId, Guid activityId, TrackFormat format, CancellationToken ct) =>
            Task.CompletedTask;

        public Task SaveElevationAsync(string userPublicId, Guid activityId, Stream content, CancellationToken ct) =>
            Task.CompletedTask;

        public Task<Stream?> OpenElevationAsync(string userPublicId, Guid activityId, CancellationToken ct) =>
            Task.FromResult<Stream?>(null);

        public Task DeleteActivityFolderAsync(string userPublicId, Guid activityId, CancellationToken ct)
        {
            DeletedFolder = (userPublicId, activityId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEquipmentRepository(int ownedCount) : IEquipmentRepository
    {
        public Task<IReadOnlyList<ParagLog.Core.Equipment.Equipment>> ListAsync(Guid userId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ParagLog.Core.Equipment.Equipment>>([]);

        public Task<ParagLog.Core.Equipment.Equipment?> FindByIdAsync(Guid userId, Guid equipmentId, CancellationToken ct) =>
            Task.FromResult<ParagLog.Core.Equipment.Equipment?>(null);

        public Task<EquipmentUsage> GetUsageAsync(Guid userId, Guid equipmentId, CancellationToken ct) =>
            Task.FromResult(new EquipmentUsage(0, 0));

        public Task<bool> DisplayNameExistsAsync(Guid userId, string displayName, Guid? excludingId, CancellationToken ct) =>
            Task.FromResult(false);

        public Task<int> CountOwnedAsync(Guid userId, IReadOnlyList<Guid> equipmentIds, CancellationToken ct) =>
            Task.FromResult(Math.Min(ownedCount, equipmentIds.Count));

        public Task<ParagLog.Core.Equipment.Equipment> CreateAsync(Guid userId, CreateEquipmentCommand command, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<ParagLog.Core.Equipment.Equipment?> UpdateAsync(
            Guid userId, Guid equipmentId, UpdateEquipmentCommand command, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<ParagLog.Core.Equipment.Equipment?> RetireAsync(Guid userId, Guid equipmentId, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<bool> DeleteAsync(Guid userId, Guid equipmentId, CancellationToken ct) =>
            throw new NotSupportedException();
    }
}
