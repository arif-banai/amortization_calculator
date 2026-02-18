# Amortization Calculator

A Windows desktop amortization calculator for fixed-rate home loans with support for extra payments. Built with .NET 10 and WPF.

## Run in development

```bash
dotnet run --project src/Amortization.App
```

## Publish single EXE

To build a self-contained **single-file** executable for Windows x64 (native WPF DLLs are bundled into the exe):

```bash
dotnet publish src/Amortization.App -c Release
```

The output will be under `src/Amortization.App/bin/Release/net10.0-windows/win-x64/publish/` — you should see only **Amortization.App.exe** in that folder.

## For end-users (published app)

1. Open the `publish` folder (see path above, or the folder where you copied the published app).
2. Double-click **Amortization.App.exe** to start the calculator.

No separate .NET install is needed when published as self-contained. You can copy **Amortization.App.exe** to another Windows PC and run it the same way.

**If nothing happens when you double-click:** Republish the app (the latest build shows error messages instead of failing silently). Then run the EXE from a command prompt: open Command Prompt, `cd` to the folder containing `Amortization.App.exe`, and run `Amortization.App.exe`. Any error message will appear in a popup or in the console. Antivirus can sometimes block single-file apps when they extract; temporarily allow the EXE or add an exception if needed.

## Assumptions

- **StartDate = first payment date.** Payment #1 is on `StartDate`; payment #n is `StartDate.AddMonths(n - 1)`.
- **Extra payments apply to principal only.** The scheduled payment amount stays the same (KEEP_PAYMENT mode); the loan pays off earlier.
- **Rounding:** All currency values are rounded to two decimal places (cents) with midpoint rounding away from zero. The final schedule line is adjusted so the ending balance is exactly 0.00.

## Solution layout

- **Amortization.Core** — Class library: domain models, schedule engine, CSV export (string only; no file I/O). No WPF or IO dependencies.
- **Amortization.App** — WPF application: MVVM UI, inputs, schedule grid, base vs with-extras comparison, CSV export via Save File dialog.
- **Amortization.Tests** — xUnit tests for the engine (payment formula, base schedule, extra payments, lump sums).

## Tests

```bash
dotnet test tests/Amortization.Tests
```
