namespace ExamTicketGenerator;

public sealed class JournalUnavailableException : Exception
{
    public JournalUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
