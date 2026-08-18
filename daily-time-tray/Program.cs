namespace DailyTime.Tray;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Root = carpeta DailyTime (padre de daily-time-tray)
        var root = FindProjectRoot();
        if (root is null)
        {
            MessageBox.Show(
                "No encontré la carpeta del proyecto DailyTime.\nEjecuta el tray desde el repo.",
                "DailyTime",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        using var mutex = new Mutex(true, @"Global\DailyTime.Tray", out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "DailyTime ya está en la bandeja del sistema.",
                "DailyTime",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.Run(new TrayAppContext(root));
    }

    private static string? FindProjectRoot()
    {
        // Prefer: exe is in daily-time-tray/bin/... → go up to repo root
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "start-local.bat"))
                && Directory.Exists(Path.Combine(dir.FullName, "dailyTimeApi")))
            {
                return dir.FullName;
            }

            // If we are inside daily-time-tray source folder
            if (dir.Name.Equals("daily-time-tray", StringComparison.OrdinalIgnoreCase)
                && dir.Parent != null
                && File.Exists(Path.Combine(dir.Parent.FullName, "start-local.bat")))
            {
                return dir.Parent.FullName;
            }
        }

        // Fallback: current directory walk
        dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (var i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "start-local.bat"))
                && Directory.Exists(Path.Combine(dir.FullName, "dailyTimeApi")))
            {
                return dir.FullName;
            }
        }

        return null;
    }
}
