using ExamTicketGenerator.Tests;

var tests = new (string Name, Action Run)[]
{
    ("Ticket number is always between 1 and 20", TicketGeneratorTests.GenerateReturnsNumberFromOneToTwenty),
    ("Names are trimmed", InputValidatorTests.NormalizeNameTrimsOuterWhitespace),
    ("Empty names are rejected", InputValidatorTests.NormalizeNameRejectsEmptyValues),
    ("Workbook is created with headers", ExcelJournalTests.EnsureCreatedCreatesWorkbookWithHeader),
    ("Rows are appended without overwriting", ExcelJournalTests.AppendAddsRowsWithoutChangingExistingRows),
    ("A new run continues after existing rows", ExcelJournalTests.NewJournalInstanceAppendsAfterExistingData),
    ("Selected journal rows can be removed", ExcelJournalTests.RemoveWhereRemovesOnlyMatchingRows),
    ("A locked workbook returns a friendly error", ExcelJournalTests.LockedWorkbookReturnsFriendlyError)
};

var failed = 0;

foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS: {test.Name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.Error.WriteLine($"FAIL: {test.Name}");
        Console.Error.WriteLine($"      {exception.Message}");
    }
}

Console.WriteLine();
Console.WriteLine($"Tests: {tests.Length}, Passed: {tests.Length - failed}, Failed: {failed}");
return failed == 0 ? 0 : 1;
