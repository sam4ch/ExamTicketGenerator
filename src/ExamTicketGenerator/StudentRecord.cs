namespace ExamTicketGenerator;

public sealed record StudentRecord(
    string LastName,
    string FirstName,
    int TicketNumber,
    DateTime RecordedAt);
