using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace ExamTicketGenerator;

public sealed class ExcelJournal
{
    private const string WorksheetPath = "xl/worksheets/sheet1.xml";
    private static readonly XNamespace SpreadsheetNamespace =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private static readonly string[] Headers =
    [
        "Last name",
        "First name",
        "Номер билета",
        "Дата и время"
    ];

    private readonly string _filePath;
    private readonly object _syncRoot = new();

    public ExcelJournal(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = Path.GetFullPath(filePath);
    }

    public void EnsureCreated()
    {
        lock (_syncRoot)
        {
            if (File.Exists(_filePath))
            {
                return;
            }

            ExecuteWithFriendlyError(() => WriteNewWorkbook([]));
        }
    }

    public void Append(StudentRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.TicketNumber is < TicketGenerator.MinimumTicketNumber or > TicketGenerator.MaximumTicketNumber)
        {
            throw new ArgumentOutOfRangeException(
                nameof(record),
                $"Ticket number must be between {TicketGenerator.MinimumTicketNumber} and {TicketGenerator.MaximumTicketNumber}.");
        }

        lock (_syncRoot)
        {
            ExecuteWithFriendlyError(() =>
            {
                if (!File.Exists(_filePath))
                {
                    WriteNewWorkbook([record]);
                    return;
                }

                AppendToExistingWorkbook(record);
            });
        }
    }

    public IReadOnlyList<StudentRecord> ReadAll()
    {
        lock (_syncRoot)
        {
            if (!File.Exists(_filePath))
            {
                return [];
            }

            try
            {
                using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                var worksheetEntry = archive.GetEntry(WorksheetPath)
                    ?? throw new InvalidDataException("В файле journal.xlsx отсутствует рабочий лист.");

                using var worksheetStream = worksheetEntry.Open();
                var worksheet = XDocument.Load(worksheetStream);

                return worksheet
                    .Descendants(SpreadsheetNamespace + "row")
                    .Skip(1)
                    .Select(ReadRecord)
                    .Where(record => record is not null)
                    .Cast<StudentRecord>()
                    .ToArray();
            }
            catch (IOException exception)
            {
                throw new JournalUnavailableException(
                    "Не удалось прочитать journal.xlsx. Возможно, файл открыт в Excel.",
                    exception);
            }
        }
    }

    public int RemoveWhere(Func<StudentRecord, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        lock (_syncRoot)
        {
            var records = ReadAll();
            var remainingRecords = records.Where(record => !predicate(record)).ToArray();
            var removedCount = records.Count - remainingRecords.Length;

            if (removedCount > 0)
            {
                ExecuteWithFriendlyError(() => WriteNewWorkbook(remainingRecords));
            }

            return removedCount;
        }
    }

    private static StudentRecord? ReadRecord(XElement row)
    {
        var values = row
            .Elements(SpreadsheetNamespace + "c")
            .Select(cell => cell.Descendants(SpreadsheetNamespace + "t").FirstOrDefault()?.Value ?? string.Empty)
            .ToArray();

        if (values.Length < 4 ||
            !int.TryParse(values[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticketNumber) ||
            !DateTime.TryParseExact(
                values[3],
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var recordedAt))
        {
            return null;
        }

        return new StudentRecord(values[0], values[1], ticketNumber, recordedAt);
    }

    private void WriteNewWorkbook(IReadOnlyCollection<StudentRecord> records)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var temporaryPath = CreateTemporaryPath();

        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                WriteTextEntry(archive, "[Content_Types].xml", BuildContentTypesXml());
                WriteTextEntry(archive, "_rels/.rels", BuildRootRelationshipsXml());
                WriteTextEntry(archive, "xl/workbook.xml", BuildWorkbookXml());
                WriteTextEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelationshipsXml());
                WriteTextEntry(archive, WorksheetPath, BuildWorksheet(records).ToString(SaveOptions.DisableFormatting));
            }

            if (File.Exists(_filePath))
            {
                File.Replace(temporaryPath, _filePath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(temporaryPath, _filePath);
            }
        }
        finally
        {
            TryDeleteTemporaryFile(temporaryPath);
        }
    }

    private void AppendToExistingWorkbook(StudentRecord record)
    {
        var temporaryPath = CreateTemporaryPath();

        try
        {
            using (var sourceStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            using (var sourceArchive = new ZipArchive(sourceStream, ZipArchiveMode.Read))
            using (var targetStream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var targetArchive = new ZipArchive(targetStream, ZipArchiveMode.Create))
            {
                var worksheetEntry = sourceArchive.GetEntry(WorksheetPath)
                    ?? throw new InvalidDataException("В файле journal.xlsx отсутствует рабочий лист.");

                XDocument worksheet;
                using (var worksheetStream = worksheetEntry.Open())
                {
                    worksheet = XDocument.Load(worksheetStream);
                }

                AppendRow(worksheet, record);

                foreach (var sourceEntry in sourceArchive.Entries)
                {
                    if (string.Equals(sourceEntry.FullName, WorksheetPath, StringComparison.OrdinalIgnoreCase))
                    {
                        WriteTextEntry(targetArchive, WorksheetPath, worksheet.ToString(SaveOptions.DisableFormatting));
                        continue;
                    }

                    var targetEntry = targetArchive.CreateEntry(sourceEntry.FullName, CompressionLevel.Optimal);
                    targetEntry.LastWriteTime = sourceEntry.LastWriteTime;

                    using var sourceEntryStream = sourceEntry.Open();
                    using var targetEntryStream = targetEntry.Open();
                    sourceEntryStream.CopyTo(targetEntryStream);
                }
            }

            File.Replace(temporaryPath, _filePath, destinationBackupFileName: null);
        }
        finally
        {
            TryDeleteTemporaryFile(temporaryPath);
        }
    }

    private static XDocument BuildWorksheet(IEnumerable<StudentRecord> records)
    {
        var sheetData = new XElement(SpreadsheetNamespace + "sheetData");
        sheetData.Add(CreateRow(1, Headers));

        var rowNumber = 2;
        foreach (var record in records)
        {
            sheetData.Add(CreateRecordRow(rowNumber++, record));
        }

        return new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(
                SpreadsheetNamespace + "worksheet",
                new XElement(SpreadsheetNamespace + "dimension", new XAttribute("ref", $"A1:D{rowNumber - 1}")),
                new XElement(
                    SpreadsheetNamespace + "sheetViews",
                    new XElement(SpreadsheetNamespace + "sheetView", new XAttribute("workbookViewId", "0"))),
                new XElement(SpreadsheetNamespace + "sheetFormatPr", new XAttribute("defaultRowHeight", "15")),
                new XElement(
                    SpreadsheetNamespace + "cols",
                    CreateColumn(1, 1, 20),
                    CreateColumn(2, 2, 20),
                    CreateColumn(3, 3, 16),
                    CreateColumn(4, 4, 22)),
                sheetData));
    }

    private static XElement CreateColumn(int minimum, int maximum, int width)
    {
        return new XElement(
            SpreadsheetNamespace + "col",
            new XAttribute("min", minimum),
            new XAttribute("max", maximum),
            new XAttribute("width", width),
            new XAttribute("customWidth", "1"));
    }

    private static void AppendRow(XDocument worksheet, StudentRecord record)
    {
        var sheetData = worksheet.Root?.Element(SpreadsheetNamespace + "sheetData")
            ?? throw new InvalidDataException("В рабочем листе отсутствует таблица данных.");

        var lastRowNumber = sheetData
            .Elements(SpreadsheetNamespace + "row")
            .Select(row => (int?)row.Attribute("r") ?? 0)
            .DefaultIfEmpty(0)
            .Max();

        var newRowNumber = lastRowNumber + 1;
        sheetData.Add(CreateRecordRow(newRowNumber, record));

        var dimension = worksheet.Root?.Element(SpreadsheetNamespace + "dimension");
        dimension?.SetAttributeValue("ref", $"A1:D{newRowNumber}");
    }

    private static XElement CreateRecordRow(int rowNumber, StudentRecord record)
    {
        return CreateRow(
            rowNumber,
            [
                record.LastName,
                record.FirstName,
                record.TicketNumber.ToString(CultureInfo.InvariantCulture),
                record.RecordedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            ]);
    }

    private static XElement CreateRow(int rowNumber, IReadOnlyList<string> values)
    {
        var row = new XElement(SpreadsheetNamespace + "row", new XAttribute("r", rowNumber));

        for (var index = 0; index < values.Count; index++)
        {
            var columnName = ((char)('A' + index)).ToString(CultureInfo.InvariantCulture);
            row.Add(
                new XElement(
                    SpreadsheetNamespace + "c",
                    new XAttribute("r", $"{columnName}{rowNumber}"),
                    new XAttribute("t", "inlineStr"),
                    new XElement(
                        SpreadsheetNamespace + "is",
                        new XElement(SpreadsheetNamespace + "t", values[index]))));
        }

        return row;
    }

    private static void WriteTextEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    private static string BuildContentTypesXml() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
          <Default Extension="xml" ContentType="application/xml"/>
          <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
          <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
        </Types>
        """;

    private static string BuildRootRelationshipsXml() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
        </Relationships>
        """;

    private static string BuildWorkbookXml() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
          <sheets>
            <sheet name="Journal" sheetId="1" r:id="rId1"/>
          </sheets>
        </workbook>
        """;

    private static string BuildWorkbookRelationshipsXml() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
        </Relationships>
        """;

    private string CreateTemporaryPath()
    {
        var directory = Path.GetDirectoryName(_filePath)!;
        var fileName = $".{Path.GetFileName(_filePath)}.{Guid.NewGuid():N}.tmp";
        return Path.Combine(directory, fileName);
    }

    private static void TryDeleteTemporaryFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static void ExecuteWithFriendlyError(Action action)
    {
        try
        {
            action();
        }
        catch (IOException exception)
        {
            throw new JournalUnavailableException(
                "Не удалось записать данные в journal.xlsx. Возможно, файл открыт в Excel. Закройте его и повторите попытку.",
                exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new JournalUnavailableException(
                "Нет доступа для записи в journal.xlsx. Проверьте права на папку и закройте файл в Excel.",
                exception);
        }
    }
}
