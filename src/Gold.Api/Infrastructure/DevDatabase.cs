using System.Security.Cryptography;
using System.Text;
using Gold.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gold.Api.Infrastructure;

/// <summary>
/// Development/Testing only. Creates the database, and if the local SQLite file was made by an older
/// version of the model (new tables/columns since), deletes and recreates it so F5 never fails on a stale schema.
/// Production uses reviewed migration scripts instead — never this.
/// </summary>
public static class DevDatabase
{
    /// <summary>Bump when the demo seed content changes (e.g. new images) so local dev DBs are rebuilt.</summary>
    private const string SeedVersion = "seed-3-owner-installments";

    public static async Task EnsureCurrentAsync(AppDbContext db, ILogger log)
    {
        if (!db.Database.IsSqlite())
        {
            await db.Database.EnsureCreatedAsync();
            return;
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(SeedVersion + db.Database.GenerateCreateScript())));
        var created = await db.Database.EnsureCreatedAsync();

        if (!created && await StoredHashAsync(db) != hash)
        {
            log.LogWarning("Local dev database was created by an older model version — recreating it with fresh demo data.");
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }

        await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS __DevSchema (Hash TEXT NOT NULL)");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM __DevSchema");
        await db.Database.ExecuteSqlAsync($"INSERT INTO __DevSchema (Hash) VALUES ({hash})");
    }

    private static async Task<string?> StoredHashAsync(AppDbContext db)
    {
        var hasMarker = await db.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sqlite_master WHERE type = 'table' AND name = '__DevSchema'")
            .FirstAsync() > 0;
        if (!hasMarker) return null; // older file without the marker table → treat as stale
        return await db.Database.SqlQueryRaw<string>("SELECT Hash AS Value FROM __DevSchema").FirstOrDefaultAsync();
    }
}
