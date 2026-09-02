# Exam Ticket Generator

Console application for generating exam ticket numbers and recording student data in an Excel journal.

## Technology

- C# and .NET 8
- ClosedXML for `.xlsx` files
- xUnit for automated tests

## Planned behavior

The application will request a student's last name and first name, generate a ticket number from 1 to 20, append the result with the current date and time to `journal.xlsx`, and return to the input loop. Pressing `ESC` while waiting for input will close the application.

## Project structure

- `src/ExamTicketGenerator` - console application
- `tests/ExamTicketGenerator.Tests` - automated tests
- `AI_PROMPTS.md` - record of AI-assisted development decisions and prompts

## Status

Initial project structure created. Application logic will be implemented in subsequent commits.
