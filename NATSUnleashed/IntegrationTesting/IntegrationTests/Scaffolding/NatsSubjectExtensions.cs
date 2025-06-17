namespace NATSUnleashed.MyNatsApp.IntegrationTests.Scaffolding;

public static class NatsSubjectExtensions
{
    public static string TestClassName(
        this ITestContext context,
        string? fallback = default)
        => context.TestClass?.TestClassSimpleName ?? fallback ?? nameof(TestClassName);

    public static string TestMethodName(
        this ITestContext context,
        string? fallback = default)
        => context.TestMethod?.MethodName ?? fallback ?? nameof(TestMethodName);
}
