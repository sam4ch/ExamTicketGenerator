<div align="center">

# 🎫 Exam Ticket Generator

Генератор экзаменационных билетов с консольным и веб-интерфейсом, Excel-журналом и Selenium-автоматизацией.

![C#](https://img.shields.io/badge/C%23-12-512BD4?logo=csharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Razor_Pages-512BD4?logo=dotnet&logoColor=white)
![Selenium](https://img.shields.io/badge/Selenium-4.49-43B02A?logo=selenium&logoColor=white)
![Tests](https://img.shields.io/badge/unit_tests-8%2F8-brightgreen)
![Labs](https://img.shields.io/badge/labs-1_%26_2-completed-success)

</div>

## О проекте

Учебный проект по разработке с использованием искусственного интеллекта. В лабораторной № 1 создана консольная программа, в лабораторной № 2 — адаптивный веб-интерфейс на ASP.NET Core и сквозной Selenium-тест.

Пользователь вводит фамилию и имя студента, приложение генерирует билет от **1 до 20**, показывает результат и добавляет запись в `journal.xlsx`. Старые строки сохраняются между запусками.

## Реализовано

| Возможность | Лабораторная | Реализация |
|---|---:|---|
| Ввод и проверка ФИО | № 1 | Пустые значения отклоняются, внешние пробелы удаляются |
| Генерация билета | № 1 | Случайное целое число от 1 до 20 включительно |
| Excel-журнал | № 1 | Настоящий `.xlsx`, запись после каждого студента |
| Сохранение истории | № 1 | Новые строки не перезаписывают предыдущие |
| Консольный выход | № 1 | `ESC` при ожидании фамилии или имени |
| Адаптивный UI | № 2 | Razor Pages: форма, результат, ошибки и таблица журнала |
| Selenium | № 2 | Автоматический ввод студента и отправка формы |
| E2E-проверка Excel | № 2 | Тест подтверждает запись студента в `.xlsx` |
| Браузеры | № 2 | Opera GX, при её отсутствии — Microsoft Edge |

## Быстрый старт

Откройте в VS Code папку с файлами `ExamTicketGenerator.sln`, `global.json` и каталогами `src`, `tests`.

Если терминал открыт в папке `console app`, сначала перейдите в проект:

```powershell
cd ".\ExamTicketGenerator"
```

Восстановите зависимости и соберите решение:

```powershell
dotnet restore
dotnet build
```

Проект использует .NET 8; совместимая версия SDK закреплена в `global.json`.

## Лабораторная № 2: веб-интерфейс

```powershell
dotnet run --project ".\src\ExamTicketGenerator.Web\ExamTicketGenerator.Web.csproj"
```

Откройте адрес из терминала, например `http://localhost:5129`. В интерфейсе можно ввести студента, получить билет, увидеть последние записи и сообщения валидации.

Веб-журнал создаётся в `src/ExamTicketGenerator.Web/journal.xlsx`. Другой путь можно передать через переменную окружения `JournalPath`.

## Selenium: автоматический ввод студента

```powershell
dotnet run --project ".\tests\ExamTicketGenerator.SeleniumTests\ExamTicketGenerator.SeleniumTests.csproj"
```

Тест самостоятельно:

1. запускает веб-приложение на свободном локальном порту;
2. открывает Opera GX, а при её отсутствии — Edge;
3. проверяет валидацию пустой формы;
4. последовательно вводит трёх демонстрационных студентов: Popescu Ana, Ionescu Mihai и Rusu Elena;
5. отправляет каждую форму и проверяет билеты в диапазоне 1–20;
6. находит всех студентов в таблице UI;
7. читает постоянный Excel-журнал и подтверждает сохранение;
8. закрывает браузер и сервер, сохраняя добавленные записи.

Успешный результат:

```text
Browser: Opera GX
SELENIUM PASS: 3 студента добавлены через UI и сохранены в постоянном Excel-журнале.
Journal: C:\...\src\ExamTicketGenerator.Web\journal.xlsx
```

Совместимый с текущей Opera GX `ChromeDriver` устанавливается NuGet-пакетом вместе с тестовым проектом.

Повторный запуск Selenium добавляет ещё три строки. По умолчанию записи остаются в `src/ExamTicketGenerator.Web/journal.xlsx`; другой файл можно выбрать переменной `SELENIUM_JOURNAL_PATH`.

## Лабораторная № 1: консоль

```powershell
dotnet run --project ".\src\ExamTicketGenerator\ExamTicketGenerator.csproj"
```

```text
Генератор экзаменационных билетов
Для выхода нажмите ESC.
Last name: Popescu
First name: Ana
Билет № 7.
Запись сохранена в journal.xlsx.
```

## Excel-журнал

| Last name | First name | Номер билета | Дата и время |
|---|---|---:|---|
| Popescu | Ana | 7 | 2026-09-22 15:32:28 |
| Ionescu | Mihai | 12 | 2026-09-22 15:35:10 |

`journal.xlsx` находится в `.gitignore`, поэтому данные студентов не отправляются в GitHub.

## Тестирование

Unit-тесты:

```powershell
dotnet run --project ".\tests\ExamTicketGenerator.Tests\ExamTicketGenerator.Tests.csproj"
```

```text
Tests: 8, Passed: 8, Failed: 0
```

Полная проверка:

```powershell
dotnet build
dotnet run --project ".\tests\ExamTicketGenerator.Tests\ExamTicketGenerator.Tests.csproj"
dotnet run --project ".\tests\ExamTicketGenerator.SeleniumTests\ExamTicketGenerator.SeleniumTests.csproj"
```

## Структура решения

```text
ExamTicketGenerator/
├── src/
│   ├── ExamTicketGenerator/                  # логика и консоль
│   │   ├── InputValidator.cs
│   │   ├── TicketGenerator.cs
│   │   ├── StudentRecord.cs
│   │   └── ExcelJournal.cs
│   └── ExamTicketGenerator.Web/              # ASP.NET Core UI
│       ├── Pages/Index.cshtml
│       ├── Pages/Index.cshtml.cs
│       └── wwwroot/css/site.css
├── tests/
│   ├── ExamTicketGenerator.Tests/            # unit-тесты
│   └── ExamTicketGenerator.SeleniumTests/    # браузерный E2E-тест
├── AI_PROMPTS.md                              # журнал работы с ИИ
├── ExamTicketGenerator.sln
└── README.md
```

## Архитектура

| Компонент | Ответственность |
|---|---|
| `InputValidator` | Очистка и проверка имени и фамилии |
| `TicketGenerator` | Генерация билета от 1 до 20 |
| `StudentRecord` | Модель строки журнала |
| `ExcelJournal` | Создание, чтение и безопасное обновление `.xlsx` |
| `ExamTicketGenerator.Web` | Веб-интерфейс лабораторной № 2 |
| `ExamTicketGenerator.SeleniumTests` | Сквозная автоматизация действий пользователя |

Консоль и веб-интерфейс используют одну бизнес-логику и один формат журнала.

## Использование искусственного интеллекта

ИИ применялся для анализа задания, выбора стека, проектирования, реализации C#-кода, UI, Excel-формата, unit- и Selenium-тестов, диагностики браузеров и подготовки документации.

Подробный журнал: [`AI_PROMPTS.md`](AI_PROMPTS.md).

## Email-уведомления о коммитах

Workflow `.github/workflows/email-on-push.yml` запускается после каждого `push` в любую ветку и отправляет письмо со списком коммитов, автором, веткой и ссылкой на изменения. Локальный `git commit` без `git push` письмо не вызывает, потому что GitHub ещё не получил изменения.

Для настройки откройте репозиторий на GitHub и перейдите в **Settings → Secrets and variables → Actions → New repository secret**. Добавьте:

| Secret | Значение |
|---|---|
| `SMTP_HOST` | SMTP-сервер почтового провайдера, например `smtp.gmail.com` |
| `SMTP_PORT` | Обычно `587`; для SMTP SSL может использоваться `465` |
| `SMTP_USERNAME` | Полный адрес почты отправителя |
| `SMTP_PASSWORD` | Пароль приложения, а не пароль от основного аккаунта |
| `MAIL_TO` | Адрес получателя; необязательно, по умолчанию `SMTP_USERNAME` |
| `MAIL_FROM` | Адрес отправителя; необязательно, по умолчанию `SMTP_USERNAME` |

После добавления секретов откройте вкладку **Actions**, выберите **Email notification on push** и нажмите **Run workflow** для тестового письма. Секреты не записываются в репозиторий и не выводятся в логах.

## Возможные проблемы

### `Указанный путь к файлу не существует`

Перейдите в папку с `ExamTicketGenerator.sln` и повторите команду.

### `journal.xlsx` открыт в Excel

Закройте файл и повторите сохранение: Excel блокирует открытую книгу.

### Несовместимая версия ChromeDriver

Opera GX обновила встроенный Chromium. Обновите пакет:

```powershell
dotnet add ".\tests\ExamTicketGenerator.SeleniumTests\ExamTicketGenerator.SeleniumTests.csproj" package Selenium.WebDriver.ChromeDriver
dotnet restore
```

### Edge запрещает remote debugging

На некоторых учебных компьютерах политика Windows запрещает WebDriver для Edge. При наличии Opera GX тест автоматически использует её.

---

<div align="center">

Учебный проект · C# · .NET 8 · ASP.NET Core · Selenium · AI-assisted development

</div>
