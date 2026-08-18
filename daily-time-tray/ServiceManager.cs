using System.Diagnostics;
using System.Net.Sockets;

namespace DailyTime.Tray;

internal sealed class ServiceManager : IDisposable
{
    private readonly string _root;
    private readonly string _logsDir;
    private readonly List<ManagedProcess> _processes = new();
    private readonly object _gate = new();

    public ServiceManager(string root)
    {
        _root = root;
        _logsDir = Path.Combine(root, "logs", "tray");
        Directory.CreateDirectory(_logsDir);
    }

    public string LogsDirectory => _logsDir;

    public bool IsManagedRunning
    {
        get
        {
            lock (_gate)
            {
                return _processes.Any(p => p.Process is { HasExited: false });
            }
        }
    }

    public bool WebReady => IsPortOpen(4010);

    public string StatusText
    {
        get
        {
            var api = IsPortOpen(5110) || IsPortOpen(5100);
            var web = IsPortOpen(4010);
            var voice = IsPortOpen(5400);
            var worker = IsPortOpen(5510) || IsPortOpen(5500);
            return $"API: {(api ? "OK" : "off")} · Web: {(web ? "OK" : "off")} · Voz: {(voice ? "OK" : "off")} · Worker: {(worker ? "OK" : "off")}";
        }
    }

    public void StartLocalServices()
    {
        lock (_gate)
        {
            if (IsManagedRunning)
                return;

            StopInternal();

            StartHidden(
                "api",
                "dotnet",
                "run --launch-profile https",
                Path.Combine(_root, "dailyTimeApi"));

            StartHidden(
                "worker",
                "dotnet",
                "run --launch-profile https",
                Path.Combine(_root, "dailyTimeWorker"));

            StartHidden(
                "web",
                "npm",
                "run dev",
                Path.Combine(_root, "daily-time-web"));

            var voiceDir = Path.Combine(_root, "daily-time-voice");
            var venvPython = Path.Combine(voiceDir, ".venv", "Scripts", "python.exe");
            if (File.Exists(venvPython))
                StartHidden("voice", venvPython, "-m app", voiceDir);
            else
                StartHidden("voice", "python", "-m app", voiceDir);
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            StopInternal();
        }
    }

    public void Dispose() => Stop();

    private void StopInternal()
    {
        foreach (var item in _processes.ToList())
        {
            TryKillTree(item.Process);
            item.Dispose();
        }
        _processes.Clear();
    }

    private void StartHidden(string name, string fileName, string arguments, string workingDirectory)
    {
        var logPath = Path.Combine(_logsDir, $"{name}.log");
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };

        if (fileName.Equals("npm", StringComparison.OrdinalIgnoreCase))
        {
            psi.FileName = "cmd.exe";
            psi.Arguments = $"/c npm {arguments}";
        }

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var writer = new StreamWriter(
            new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
        {
            AutoFlush = true
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                lock (writer)
                    writer.WriteLine(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                lock (writer)
                    writer.WriteLine(e.Data);
            }
        };

        if (!process.Start())
            throw new InvalidOperationException($"No se pudo iniciar {name}.");

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        _processes.Add(new ManagedProcess(name, process, writer));
    }

    private static void TryKillTree(Process? process)
    {
        if (process is null)
            return;

        try
        {
            if (process.HasExited)
                return;

            using var killer = Process.Start(new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = $"/PID {process.Id} /T /F",
                CreateNoWindow = true,
                UseShellExecute = false,
            });
            killer?.WaitForExit(5000);
        }
        catch
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
        }
    }

    private static bool IsPortOpen(int port)
    {
        try
        {
            using var client = new TcpClient();
            var task = client.ConnectAsync("127.0.0.1", port);
            return task.Wait(TimeSpan.FromMilliseconds(250)) && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private sealed class ManagedProcess : IDisposable
    {
        public Process Process { get; }
        private readonly StreamWriter _writer;

        public ManagedProcess(string name, Process process, StreamWriter writer)
        {
            Process = process;
            _writer = writer;
        }

        public void Dispose()
        {
            try { _writer.Dispose(); } catch { /* ignore */ }
            try { Process.Dispose(); } catch { /* ignore */ }
        }
    }
}
