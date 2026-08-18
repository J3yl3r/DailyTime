using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace DailyTime.Tray;

internal sealed class TrayAppContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly ServiceManager _services;
    private readonly string _root;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _startItem;
    private readonly ToolStripMenuItem _stopItem;
    private readonly System.Windows.Forms.Timer _statusTimer;

    public TrayAppContext(string root)
    {
        _root = root;
        _services = new ServiceManager(root);

        _statusItem = new ToolStripMenuItem("Estado: …") { Enabled = false };
        _startItem = new ToolStripMenuItem("Iniciar servicios", null, async (_, _) => await StartAsync());
        _stopItem = new ToolStripMenuItem("Detener servicios", null, (_, _) => StopServices());
        var openItem = new ToolStripMenuItem("Abrir DailyTime", null, (_, _) => OpenApp());
        var logsItem = new ToolStripMenuItem("Ver logs", null, (_, _) => OpenLogs());
        var exitItem = new ToolStripMenuItem("Salir", null, (_, _) => ExitApp());

        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(openItem);
        menu.Items.Add(_startItem);
        menu.Items.Add(_stopItem);
        menu.Items.Add(logsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _tray = new NotifyIcon
        {
            Icon = CreateAppIcon(),
            Text = "DailyTime",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _tray.DoubleClick += (_, _) => OpenApp();

        _statusTimer = new System.Windows.Forms.Timer { Interval = 3000 };
        _statusTimer.Tick += (_, _) => RefreshStatus();
        _statusTimer.Start();

        RefreshStatus();

        // Auto-start if web is not already up
        if (!_services.WebReady)
        {
            _ = StartAsync(showBalloon: true);
        }
        else
        {
            _tray.ShowBalloonTip(
                2500,
                "DailyTime",
                "Servicios ya activos. Doble clic para abrir.",
                ToolTipIcon.Info);
        }
    }

    private async Task StartAsync(bool showBalloon = true)
    {
        try
        {
            _startItem.Enabled = false;
            _tray.Text = "DailyTime — iniciando…";
            await Task.Run(() => _services.StartLocalServices());

            // Wait a bit for ports
            for (var i = 0; i < 20 && !_services.WebReady; i++)
                await Task.Delay(500);

            RefreshStatus();
            if (showBalloon)
            {
                _tray.ShowBalloonTip(
                    3000,
                    "DailyTime",
                    _services.WebReady
                        ? "Servicios listos. Doble clic para abrir."
                        : "Servicios iniciados. Si no abre, revisa logs.",
                    ToolTipIcon.Info);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudieron iniciar los servicios:\n{ex.Message}",
                "DailyTime",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            RefreshStatus();
        }
    }

    private void StopServices()
    {
        try
        {
            _services.Stop();
            RefreshStatus();
            _tray.ShowBalloonTip(2000, "DailyTime", "Servicios detenidos.", ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "DailyTime", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenApp()
    {
        const string url = "http://localhost:4010/workspace";
        try
        {
            var browsers = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Microsoft", "Edge", "Application", "msedge.exe"),
            };

            foreach (var browser in browsers)
            {
                if (!File.Exists(browser))
                    continue;
                Process.Start(new ProcessStartInfo
                {
                    FileName = browser,
                    Arguments = $"--app={url}",
                    UseShellExecute = false,
                });
                return;
            }

            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "DailyTime", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenLogs()
    {
        Directory.CreateDirectory(_services.LogsDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = _services.LogsDirectory,
            UseShellExecute = true,
        });
    }

    private void ExitApp()
    {
        var result = MessageBox.Show(
            "¿Detener también API / Web / Voz al salir?",
            "DailyTime",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        if (result == DialogResult.Cancel)
            return;

        if (result == DialogResult.Yes)
            _services.Stop();

        _statusTimer.Stop();
        _tray.Visible = false;
        _tray.Dispose();
        _services.Dispose();
        ExitThread();
    }

    private void RefreshStatus()
    {
        var status = _services.StatusText;
        _statusItem.Text = status;
        _tray.Text = $"DailyTime — {status}";
        _startItem.Enabled = !_services.IsManagedRunning;
        _stopItem.Enabled = _services.IsManagedRunning || _services.WebReady;
    }

    private static Icon CreateAppIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(37, 99, 235));
        using var pen = new Pen(Color.White, 2.4f);
        g.DrawEllipse(pen, 5, 5, 22, 22);
        g.DrawLine(pen, 16, 10, 16, 16);
        g.DrawLine(pen, 16, 16, 21, 19);
        return Icon.FromHandle(bmp.GetHicon());
    }
}
