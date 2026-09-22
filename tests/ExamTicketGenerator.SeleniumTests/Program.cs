using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using ExamTicketGenerator;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;

var repositoryRoot = FindRepositoryRoot();
var webProjectPath = Path.Combine(
    repositoryRoot,
    "src",
    "ExamTicketGenerator.Web",
    "ExamTicketGenerator.Web.csproj");
var temporaryDirectory = Path.Combine(
    Path.GetTempPath(),
    $"ExamTicketGenerator-Selenium-{Guid.NewGuid():N}");
var journalPath = Path.Combine(temporaryDirectory, "journal.xlsx");
var port = FindFreePort();
var baseUrl = $"http://127.0.0.1:{port}";

Directory.CreateDirectory(temporaryDirectory);

using var webProcess = StartWebApplication(webProjectPath, baseUrl, journalPath);

try
{
    await WaitForApplicationAsync(baseUrl, webProcess);
    RunSeleniumScenario(baseUrl, journalPath);
    Console.WriteLine("SELENIUM PASS: студент введён через UI, билет создан, запись появилась в таблице и Excel-журнале.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine("SELENIUM FAIL: автоматизированный сценарий завершился с ошибкой.");
    Console.Error.WriteLine(exception);
    return 1;
}
finally
{
    if (!webProcess.HasExited)
    {
        webProcess.Kill(entireProcessTree: true);
        webProcess.WaitForExit(5_000);
    }

    Directory.Delete(temporaryDirectory, recursive: true);
}

static void RunSeleniumScenario(string baseUrl, string journalPath)
{
    using var driver = CreateWebDriver(journalPath);
    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
    Thread.Sleep(3_000);
    driver.SwitchTo().NewWindow(WindowType.Tab);
    driver.Navigate().GoToUrl(baseUrl);

    WaitUntil(
        () =>
        {
            if (!driver.Url.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
            {
                driver.Navigate().GoToUrl(baseUrl);
                return false;
            }

            return driver.Title.Contains("Генератор билетов", StringComparison.Ordinal);
        },
        "Страница приложения не загрузилась в браузере.");

    ClickElement(driver, By.Id("addStudentButton"));
    WaitUntil(
        () => (driver.FindElement(By.Id("lastNameError")).GetAttribute("textContent") ?? string.Empty).Length > 0,
        "Валидация пустой формы не сработала.");

    var suffix = DateTime.UtcNow.ToString("HHmmss");
    var lastName = $"Selenium{suffix}";
    const string firstName = "Student";

    EnterText(driver, By.Id("lastName"), lastName);
    EnterText(driver, By.Id("firstName"), firstName);
    ClickElement(driver, By.Id("addStudentButton"));

    WaitUntil(
        () => driver.FindElements(By.Id("ticketResult")).Count == 1,
        "Результат генерации билета не появился.");

    var result = driver.FindElement(By.Id("ticketResult"));
    var ticketValue = result.GetAttribute("data-ticket");
    Assert(int.TryParse(ticketValue, out var ticketNumber), "Номер билета отсутствует в результате.");
    Assert(ticketNumber is >= 1 and <= 20, "Номер билета находится вне диапазона 1–20.");

    var matchingRows = driver.FindElements(By.CssSelector(
        $"[data-student-row][data-last-name='{lastName}'][data-first-name='{firstName}']"));
    Assert(matchingRows.Count == 1, "Добавленный студент не появился в таблице.");

    var records = new ExcelJournal(journalPath).ReadAll();
    Assert(
        records.Any(record =>
            record.LastName == lastName &&
            record.FirstName == firstName &&
            record.TicketNumber == ticketNumber),
        "Добавленный через Selenium студент отсутствует в journal.xlsx.");
}

static void EnterText(IWebDriver driver, By locator, string value)
{
    var element = driver.FindElement(locator);
    if (element.Displayed)
    {
        element.Clear();
        element.SendKeys(value);
        return;
    }

    ((IJavaScriptExecutor)driver).ExecuteScript("""
        const element = arguments[0];
        const value = arguments[1];
        const setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set;
        setter.call(element, value);
        element.dispatchEvent(new Event('input', { bubbles: true }));
        element.dispatchEvent(new Event('change', { bubbles: true }));
        """, element, value);
}

static void ClickElement(IWebDriver driver, By locator)
{
    var element = driver.FindElement(locator);
    if (element.Displayed)
    {
        element.Click();
        return;
    }

    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", element);
}

static IWebDriver CreateWebDriver(string journalPath)
{
    var operaPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Programs",
        "Opera GX",
        "opera.exe");
    var profilePath = Path.Combine(Path.GetDirectoryName(journalPath)!, "browser-profile");

    if (File.Exists(operaPath))
    {
        var options = new ChromeOptions
        {
            BinaryLocation = operaPath
        };
        AddBrowserArguments(options.AddArgument, profilePath, headless: false);
        Console.WriteLine("Browser: Opera GX");

        var driverPath = Environment.GetEnvironmentVariable("CHROMEDRIVER_PATH");
        if (string.IsNullOrWhiteSpace(driverPath))
        {
            driverPath = Path.Combine(AppContext.BaseDirectory, "chromedriver.exe");
        }

        if (!string.IsNullOrWhiteSpace(driverPath) && File.Exists(driverPath))
        {
            var service = ChromeDriverService.CreateDefaultService(
                Path.GetDirectoryName(driverPath)!,
                Path.GetFileName(driverPath));
            return new ChromeDriver(service, options);
        }

        return new ChromeDriver(options);
    }

    var edgeOptions = new EdgeOptions();
    AddBrowserArguments(edgeOptions.AddArgument, profilePath, headless: true);
    Console.WriteLine("Browser: Microsoft Edge");
    return new EdgeDriver(edgeOptions);
}

static void AddBrowserArguments(Action<string> addArgument, string profilePath, bool headless)
{
    if (headless)
    {
        addArgument("--headless=new");
    }

    addArgument("--window-size=1440,1000");
    addArgument("--disable-gpu");
    addArgument("--no-sandbox");
    addArgument("--disable-dev-shm-usage");
    addArgument("--no-first-run");
    addArgument("--no-default-browser-check");
    addArgument($"--user-data-dir={profilePath}");
}

static Process StartWebApplication(string projectPath, string baseUrl, string journalPath)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = "dotnet",
        UseShellExecute = false,
        CreateNoWindow = true
    };

    startInfo.ArgumentList.Add("run");
    startInfo.ArgumentList.Add("--no-build");
    startInfo.ArgumentList.Add("--project");
    startInfo.ArgumentList.Add(projectPath);
    startInfo.ArgumentList.Add("--urls");
    startInfo.ArgumentList.Add(baseUrl);
    startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
    startInfo.Environment["JournalPath"] = journalPath;

    return Process.Start(startInfo)
        ?? throw new InvalidOperationException("Не удалось запустить веб-приложение.");
}

static async Task WaitForApplicationAsync(string baseUrl, Process process)
{
    using var client = new HttpClient();
    var timeoutAt = DateTime.UtcNow.AddSeconds(30);

    while (DateTime.UtcNow < timeoutAt)
    {
        if (process.HasExited)
        {
            throw new InvalidOperationException(
                $"Веб-приложение завершилось до запуска с кодом {process.ExitCode}.");
        }

        try
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            using var response = await client.GetAsync(baseUrl, cancellation.Token);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return;
            }
        }
        catch (Exception exception) when (
            exception is HttpRequestException or OperationCanceledException)
        {
            // The application is still starting.
        }

        await Task.Delay(250);
    }

    throw new TimeoutException("Веб-приложение не запустилось за 30 секунд.");
}

static void WaitUntil(Func<bool> condition, string failureMessage)
{
    var timeoutAt = DateTime.UtcNow.AddSeconds(10);

    while (DateTime.UtcNow < timeoutAt)
    {
        try
        {
            if (condition())
            {
                return;
            }
        }
        catch (NoSuchElementException)
        {
            // The page is still updating.
        }
        catch (StaleElementReferenceException)
        {
            // The form was re-rendered; retry with a fresh element lookup.
        }

        Thread.Sleep(100);
    }

    throw new TimeoutException(failureMessage);
}

static int FindFreePort()
{
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}

static string FindRepositoryRoot()
{
    foreach (var startingDirectory in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        var directory = new DirectoryInfo(startingDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ExamTicketGenerator.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }
    }

    throw new DirectoryNotFoundException("Не найден корень репозитория ExamTicketGenerator.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
