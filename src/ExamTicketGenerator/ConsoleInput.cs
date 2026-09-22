using System.Text;

namespace ExamTicketGenerator;

public static class ConsoleInput
{
    public static string? ReadRequired(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var buffer = new StringBuilder();

            while (true)
            {
                var key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Escape)
                {
                    return null;
                }

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();

                    if (InputValidator.TryNormalizeName(buffer.ToString(), out var normalizedValue))
                    {
                        return normalizedValue;
                    }

                    Console.WriteLine("Поле не может быть пустым. Повторите ввод.");
                    break;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (buffer.Length > 0)
                    {
                        buffer.Length--;
                        Console.Write("\b \b");
                    }

                    continue;
                }

                if (!char.IsControl(key.KeyChar))
                {
                    buffer.Append(key.KeyChar);
                    Console.Write(key.KeyChar);
                }
            }
        }
    }
}
