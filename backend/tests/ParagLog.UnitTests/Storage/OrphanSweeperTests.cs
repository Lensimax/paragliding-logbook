using ParagLog.Infrastructure.Storage;

namespace ParagLog.UnitTests.Storage;

public class OrphanSweeperTests
{
    [Fact]
    public void FindOrphans_returns_blob_ids_with_no_matching_activity_row()
    {
        var known = Guid.NewGuid();
        var orphan = Guid.NewGuid();

        var result = OrphanSweeper.FindOrphans(
            blobActivityIds: [known, orphan],
            dbActivityIds: [known]);

        Assert.Equal([orphan], result);
    }

    [Fact]
    public void FindOrphans_returns_empty_when_every_blob_folder_has_a_row()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        var result = OrphanSweeper.FindOrphans(
            blobActivityIds: [a, b],
            dbActivityIds: [a, b, Guid.NewGuid()]);

        Assert.Empty(result);
    }
}
