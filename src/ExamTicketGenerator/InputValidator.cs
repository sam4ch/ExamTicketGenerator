namespace ExamTicketGenerator;

public static class InputValidator
{
    public static bool TryNormalizeName(string? value, out string normalizedValue)
    {
        normalizedValue = value?.Trim() ?? string.Empty;
        return normalizedValue.Length > 0;
    }
}
