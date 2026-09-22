using ExamTicketGenerator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExamTicketGenerator.Web.Pages;

public sealed class IndexModel : PageModel
{
    private readonly ExcelJournal _journal;
    private readonly TicketGenerator _ticketGenerator;

    public IndexModel(ExcelJournal journal, TicketGenerator ticketGenerator)
    {
        _journal = journal;
        _ticketGenerator = ticketGenerator;
    }

    [BindProperty]
    public StudentInput Input { get; set; } = new();

    public IReadOnlyList<StudentRecord> Records { get; private set; } = [];

    public int? GeneratedTicketNumber { get; private set; }

    public string? JournalError { get; private set; }

    public void OnGet()
    {
        LoadRecords();
    }

    public IActionResult OnPost()
    {
        if (!InputValidator.TryNormalizeName(Input.LastName, out var lastName))
        {
            ModelState.AddModelError("Input.LastName", "Введите фамилию студента.");
        }

        if (!InputValidator.TryNormalizeName(Input.FirstName, out var firstName))
        {
            ModelState.AddModelError("Input.FirstName", "Введите имя студента.");
        }

        if (!ModelState.IsValid)
        {
            LoadRecords();
            return Page();
        }

        var ticketNumber = _ticketGenerator.Generate();
        var record = new StudentRecord(lastName, firstName, ticketNumber, DateTime.Now);

        try
        {
            _journal.Append(record);
            GeneratedTicketNumber = ticketNumber;
            Input = new StudentInput();
        }
        catch (JournalUnavailableException exception)
        {
            JournalError = exception.Message;
        }

        LoadRecords();
        return Page();
    }

    private void LoadRecords()
    {
        try
        {
            Records = _journal.ReadAll().Reverse().ToArray();
        }
        catch (JournalUnavailableException exception)
        {
            JournalError = exception.Message;
            Records = [];
        }
    }

    public sealed class StudentInput
    {
        public string? LastName { get; set; }

        public string? FirstName { get; set; }
    }
}
