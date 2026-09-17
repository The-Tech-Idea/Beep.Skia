// Tests exercise process-wide state (ThemeManager, static caches, the shared typeface cache and
// environment-level WinForms settings). xUnit runs test *classes* in parallel by default, which
// makes assertions on that shared state flaky, so execution is serialized for the whole assembly.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
