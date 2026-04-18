# Accounting System Docs for Codex

This package contains a structured set of markdown documents for building a general business accounting and sales system using:
- C# ASP.NET Core MVC
- Microsoft SQL Server
- Entity Framework Core

Recommended usage order:
1. Read `BUSINESS_REQUIREMENTS.md`
2. Read `IMPLEMENTATION_SPEC.md`
3. Give Codex one phase task file at a time:
   - `PHASE_1_TASK.md`
   - `PHASE_2_TASK.md`
   - `PHASE_3_TASK.md`
4. Keep `PROMPT_TEMPLATE_FOR_CODEX.md` nearby when prompting Codex.

Important security note:
- Do not place real database passwords in these documents.
- Use placeholders for connection strings.
- Put the real connection string later in `appsettings.json` or environment-specific configuration.
