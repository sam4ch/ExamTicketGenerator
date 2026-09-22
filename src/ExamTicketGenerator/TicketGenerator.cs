namespace ExamTicketGenerator;

public sealed class TicketGenerator
{
    public const int MinimumTicketNumber = 1;
    public const int MaximumTicketNumber = 20;

    private readonly Random _random;

    public TicketGenerator(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    public int Generate()
    {
        return _random.Next(MinimumTicketNumber, MaximumTicketNumber + 1);
    }
}
