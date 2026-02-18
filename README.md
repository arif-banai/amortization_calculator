# Amortization Calculator

A simple Windows desktop amortization calculator for fixed-rate home loans with support for extra payments. Built with .NET 10 and WPF.

## Getting Started

1. Install the [.NET 10+ runtime](https://dotnet.microsoft.com/download/dotnet/10.0) if you don't already have it.
2. Download the latest release from the [Releases](../../releases) page.
3. Run **Amortization.App.exe**.

## Development

### Run

```bash
dotnet run --project src/Amortization.App
```

### Test

```bash
dotnet test tests/Amortization.Tests
```

### Publish

```bash
dotnet publish src/Amortization.App -c Release
```

The self-contained single-file executable will be at `src/Amortization.App/bin/Release/net10.0-windows/win-x64/publish/Amortization.App.exe`.

## License

[Apache License 2.0](LICENSE)
