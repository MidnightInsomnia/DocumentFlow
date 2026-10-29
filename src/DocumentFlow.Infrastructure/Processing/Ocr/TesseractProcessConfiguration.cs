using System.Diagnostics;
using System.Text;

namespace DocumentFlow.Infrastructure.Processing.Ocr;

// The health probe and worker must resolve the same binary and the same language data.
public static class TesseractProcessConfiguration
{
    public static string ResolveExecutablePath(string configured)
    {
        if (!OperatingSystem.IsWindows() ||
            !(configured.Equals("tesseract", StringComparison.OrdinalIgnoreCase) ||
              configured.Equals("tesseract.exe", StringComparison.OrdinalIgnoreCase))) return configured;
        var candidates = new List<string>();
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
            if (!string.IsNullOrWhiteSpace(directory)) candidates.Add(Path.Combine(directory.Trim('"'), "tesseract.exe"));
        var runtime = Environment.GetEnvironmentVariable("DOCUMENTFLOW_RUNTIME");
        if (!string.IsNullOrWhiteSpace(runtime))
        {
            candidates.Add(Path.Combine(runtime, "tesseract", "tesseract.exe"));
            candidates.Add(Path.Combine(runtime, "Tesseract-OCR", "tesseract.exe"));
            candidates.Add(Path.Combine(runtime, "tesseract", "bin", "tesseract.exe"));
        }
        foreach (var variable in new[] { "ProgramW6432", "ProgramFiles", "ProgramFiles(x86)", "LOCALAPPDATA" })
        {
            var directory = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrWhiteSpace(directory)) continue;
            candidates.Add(Path.Combine(directory, "Tesseract-OCR", "tesseract.exe"));
            if (variable == "LOCALAPPDATA") candidates.Add(Path.Combine(directory, "Programs", "Tesseract-OCR", "tesseract.exe"));
        }
        // An explicit custom path is never silently replaced. No PATH changes or downloads.
        return candidates.FirstOrDefault(File.Exists) ?? configured;
    }

    public static ProcessStartInfo Create(TesseractOptions options, bool redirectInput = false)
    {
        var start = new ProcessStartInfo
        {
            FileName = ResolveExecutablePath(options.ExecutablePath),
            UseShellExecute = false,
            RedirectStandardInput = redirectInput,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true
        };
        if (!string.IsNullOrWhiteSpace(options.TessdataPath))
        {
            start.ArgumentList.Add("--tessdata-dir");
            start.ArgumentList.Add(options.TessdataPath);
        }
        return start;
    }

    public static bool LanguageLoadingFailed(string stderr) =>
        stderr.Contains("Failed loading language", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("Error opening data file", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("couldn't load any languages", StringComparison.OrdinalIgnoreCase);

    public static void Kill(Process process)
    {
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
    }
}
