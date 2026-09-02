using Xunit;

// Test classes share a single external Postgres instance (via ConnectionStrings:Default) and
// each boots its own WebApplicationFactory, which runs migrations independently. Running them
// in parallel races on first-time schema creation (DbUp's journal table).
[assembly: CollectionBehavior(DisableTestParallelization = true)]
