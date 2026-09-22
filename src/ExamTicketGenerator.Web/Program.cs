using ExamTicketGenerator;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
var dataProtectionDirectory = Directory.CreateDirectory(
    Path.Combine(Path.GetTempPath(), "ExamTicketGenerator", "DataProtection-Keys"));
builder.Services
    .AddDataProtection()
    .SetApplicationName("ExamTicketGenerator.Web")
    .PersistKeysToFileSystem(dataProtectionDirectory);
builder.Services.AddRazorPages();
builder.Services.AddSingleton<TicketGenerator>();
builder.Services.AddSingleton(_ =>
{
    var journalPath = builder.Configuration["JournalPath"];
    return new ExcelJournal(
        string.IsNullOrWhiteSpace(journalPath)
            ? Path.Combine(builder.Environment.ContentRootPath, "journal.xlsx")
            : journalPath);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

app.Run();

public partial class Program;
