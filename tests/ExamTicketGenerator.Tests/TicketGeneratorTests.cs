namespace ExamTicketGenerator.Tests;

internal static class TicketGeneratorTests
{
    public static void GenerateReturnsNumberFromOneToTwenty()
    {
        var generator = new TicketGenerator(new Random(42));

        for (var index = 0; index < 1_000; index++)
        {
            var number = generator.Generate();
            TestAssert.True(number is >= 1 and <= 20, $"Ticket {number} is outside the allowed range.");
        }
    }
}
