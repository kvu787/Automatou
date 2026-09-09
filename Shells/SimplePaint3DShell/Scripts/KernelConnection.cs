using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace SimplePaint3DShell;

internal sealed class KernelConnection : IDisposable
{
    private readonly Process _process;
    private readonly ConcurrentQueue<string> _responses = new();
    private readonly ConcurrentQueue<string> _errors = new();

    internal KernelConnection(string hostDirectory)
    {
        var application = Path.Combine(hostDirectory,
            OperatingSystem.IsWindows() ? "Automapolis.Kernel.Host.exe" : "Automapolis.Kernel.Host");
        var assembly = Path.Combine(hostDirectory, "Automapolis.Kernel.Host.dll");
        if (!File.Exists(application) && !File.Exists(assembly))
        {
            throw new FileNotFoundException("Run Run.cmd to publish the Kernel host.", assembly);
        }

        var start = new ProcessStartInfo
        {
            FileName = File.Exists(application) ? application : "dotnet",
            WorkingDirectory = hostDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        if (!File.Exists(application))
        {
            start.ArgumentList.Add(assembly);
        }

        _process = new Process { StartInfo = start };
        _process.OutputDataReceived += (_, arguments) =>
        {
            if (arguments.Data is { } line) _responses.Enqueue(line);
        };
        _process.ErrorDataReceived += (_, arguments) =>
        {
            if (arguments.Data is { } line) _errors.Enqueue(line);
        };
        try
        {
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }
        catch
        {
            _process.Dispose();
            throw;
        }
    }

    internal bool HasExited => _process.HasExited;
    internal bool TryRead(out string? line) => _responses.TryDequeue(out line);
    internal bool TryReadError(out string? line) => _errors.TryDequeue(out line);

    internal void Send(object payload)
    {
        _process.StandardInput.WriteLine(JsonSerializer.Serialize(payload, KernelProtocol.JsonOptions));
        _process.StandardInput.Flush();
    }

    public void Dispose()
    {
        try
        {
            if (!_process.HasExited) _process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // The host can exit between checking its state and requesting termination.
        }
        finally
        {
            _process.Dispose();
        }
    }
}
