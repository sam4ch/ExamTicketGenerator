namespace ExamTicketGenerator.Tests;

internal static class InputValidatorTests
{
    public static void NormalizeNameTrimsOuterWhitespace()
    {
        var isValid = InputValidator.TryNormalizeName("  Popescu  ", out var value);

        TestAssert.True(isValid, "A non-empty name must be valid.");
        TestAssert.Equal("Popescu", value, "Outer whitespace was not removed.");
    }

    public static void NormalizeNameRejectsEmptyValues()
    {
        string?[] values = [null, string.Empty, "   "];

        foreach (var input in values)
        {
            var isValid = InputValidator.TryNormalizeName(input, out var value);
            TestAssert.True(!isValid, "An empty value was accepted.");
            TestAssert.Equal(string.Empty, value, "An invalid value was not normalized to an empty string.");
        }
    }
}
