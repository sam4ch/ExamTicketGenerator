using System.Text;
using ExamTicketGenerator;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var journalPath = Path.Combine(Environment.CurrentDirectory, "journal.xlsx");
var journal = new ExcelJournal(journalPath);
var ticketGenerator = new TicketGenerator();

Console.WriteLine("Генератор экзаменационных билетов");
Console.WriteLine("Для выхода нажмите ESC.");
Console.WriteLine($"Журнал: {journalPath}");

try
{
    journal.EnsureCreated();
}
catch (JournalUnavailableException exception)
{
    Console.Error.WriteLine(exception.Message);
    return;
}

while (true)
{
    var lastName = ConsoleInput.ReadRequired("Last name: ");
    if (lastName is null)
    {
        break;
    }

    var firstName = ConsoleInput.ReadRequired("First name: ");
    if (firstName is null)
    {
        break;
    }

    var ticketNumber = ticketGenerator.Generate();
    var record = new StudentRecord(lastName, firstName, ticketNumber, DateTime.Now);

    Console.WriteLine($"Билет № {ticketNumber}.");

    while (true)
    {
        try
        {
            journal.Append(record);
            Console.WriteLine("Запись сохранена в journal.xlsx.");
            Console.WriteLine();
            break;
        }
        catch (JournalUnavailableException exception)
        {
            Console.Error.WriteLine(exception.Message);
            Console.WriteLine("Закройте journal.xlsx в Excel и нажмите Enter для повтора или ESC для выхода.");

            if (Console.ReadKey(intercept: true).Key == ConsoleKey.Escape)
            {
                Console.WriteLine();
                Console.WriteLine("Запись не сохранена. Работа завершена.");
                return;
            }

            Console.WriteLine();
        }
    }
}

Console.WriteLine();
Console.WriteLine("Работа завершена.");
