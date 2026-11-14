using Xunit;

// Host configuration uses process environment variables; isolate the SQL fixtures.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
