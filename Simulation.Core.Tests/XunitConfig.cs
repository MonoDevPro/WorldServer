using Xunit;

// Arch ECS world operations are not thread-safe across tests; disable parallel test execution.
[assembly: CollectionBehavior(DisableTestParallelization = true, MaxParallelThreads = 1)]
