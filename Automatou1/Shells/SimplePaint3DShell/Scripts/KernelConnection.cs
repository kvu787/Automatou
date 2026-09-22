using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace SimplePaint3DShell;

internal sealed class KernelConnection : IDisposable {
    private readonly Process _process;
    private readonly ConcurrentQueue<string> _responses = new();
    private readonly ConcurrentQueue<string> _errors = new();

    internal KernelConnection(string hostDirectory) {
        string application = Path.Combine(hostDirectory, "Automatou.Kernel.Host.exe");
        if (!File.Exists(application)) {
            throw new FileNotFoundException("Run Run.cmd to publish the Kernel host.", application);
        }

        ProcessStartInfo start = new() {
            FileName = application,
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

        this._process = new Process { StartInfo = start };
        this._process.OutputDataReceived += (_, arguments) => {
            if (arguments.Data is { } line) {
                this._responses.Enqueue(line);
            }
        };
        this._process.ErrorDataReceived += (_, arguments) => {
            if (arguments.Data is { } line) {
                this._errors.Enqueue(line);
            }
        };
        try {
            _ = this._process.Start();
            this._process.BeginOutputReadLine();
            this._process.BeginErrorReadLine();
        } catch {
            this._process.Dispose();
            throw;
        }
    }

    internal bool HasExited => this._process.HasExited;
    internal bool TryRead(out string? line) {
        return this._responses.TryDequeue(out line);
    }

    internal bool TryReadError(out string? line) {
        return this._errors.TryDequeue(out line);
    }

    internal void Send(object payload) {
        this._process.StandardInput.WriteLine(JsonSerializer.Serialize(payload, KernelProtocol.JsonOptions));
        this._process.StandardInput.Flush();
    }

    public void Dispose() {
        try {
            if (!this._process.HasExited) {
                this._process.Kill(entireProcessTree: true);
            }
        } catch (InvalidOperationException) {
            // The host can exit between checking its state and requesting termination.
        } finally {
            this._process.Dispose();
        }
    }
}
