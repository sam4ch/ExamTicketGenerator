using System.IO.Compression;
using System.Xml.Linq;

namespace ExamTicketGenerator.Tests;

internal static class ExcelJournalTests
{
    private static readonly XNamespace SpreadsheetNamespace =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    public static void EnsureCreatedCreatesWorkbookWithHeader()
    {
        RunInTemporaryDirectory(filePath =>
        {
            new ExcelJournal(filePath).EnsureCreated();

            TestAssert.True(File.Exists(filePath), "journal.xlsx was not created.");
            var rows = ReadRows(filePath);
            TestAssert.Equal(1, rows.Count, "A new workbook must contain one header row.");
            TestAssert.SequenceEqual(
                ["Last name", "First name", "Номер билета", "Дата и время"],
                rows[0],
                "Workbook headers do not match the specification.");
        });
    }

    public static void AppendAddsRowsWithoutChangingExistingRows()
    {
        RunInTemporaryDirectory(filePath =>
        {
            var journal = new ExcelJournal(filePath);
            journal.Append(new StudentRecord("Popescu", "Ana", 7, new DateTime(2026, 9, 22, 10, 15, 30)));
            journal.Append(new StudentRecord("Ionescu", "Mihai", 12, new DateTime(2026, 9, 22, 10, 20, 45)));

            var rows = ReadRows(filePath);
            TestAssert.Equal(3, rows.Count, "Two records must produce three rows including the header.");
            TestAssert.SequenceEqual(
                ["Popescu", "Ana", "7", "2026-09-22 10:15:30"],
                rows[1],
                "The first record was changed.");
            TestAssert.SequenceEqual(
                ["Ionescu", "Mihai", "12", "2026-09-22 10:20:45"],
                rows[2],
                "The second record was not appended correctly.");
        });
    }

    public static void NewJournalInstanceAppendsAfterExistingData()
    {
        RunInTemporaryDirectory(filePath =>
        {
            new ExcelJournal(filePath).Append(new StudentRecord("One", "Student", 1, DateTime.Now));
            new ExcelJournal(filePath).Append(new StudentRecord("Two", "Student", 2, DateTime.Now));

            TestAssert.Equal(3, ReadRows(filePath).Count, "A second application run did not append a row.");
        });
    }

    public static void LockedWorkbookReturnsFriendlyError()
    {
        RunInTemporaryDirectory(filePath =>
        {
            var journal = new ExcelJournal(filePath);
            journal.EnsureCreated();

            using var lockStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            var exception = TestAssert.Throws<JournalUnavailableException>(
                () => journal.Append(new StudentRecord("Locked", "File", 5, DateTime.Now)),
                "A locked workbook did not produce JournalUnavailableException.");

            TestAssert.True(exception.Message.Contains("Закройте", StringComparison.Ordinal), "The error is not user-friendly.");
        });
    }

    private static void RunInTemporaryDirectory(Action<string> test)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ExamTicketGeneratorTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            test(Path.Combine(directory, "journal.xlsx"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static List<string[]> ReadRows(string filePath)
    {
        using var archive = ZipFile.OpenRead(filePath);
        var entry = archive.GetEntry("xl/worksheets/sheet1.xml")
            ?? throw new InvalidDataException("Worksheet entry is missing.");
        using var stream = entry.Open();
        var document = XDocument.Load(stream);

        return document
            .Descendants(SpreadsheetNamespace + "row")
            .Select(row => row
                .Elements(SpreadsheetNamespace + "c")
                .Select(cell => cell.Descendants(SpreadsheetNamespace + "t").Single().Value)
                .ToArray())
            .ToList();
    }
}
