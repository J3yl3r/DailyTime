using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace dailyTimeApi.Services;

/// <summary>
/// Pide a Windows que no suspenda el equipo mientras dura un trabajo en segundo plano. Si una captura
/// programada despertó el equipo, sin esto Windows lo vuelve a suspender a los pocos minutos. En otros
/// sistemas (Docker) no hace nada.
/// </summary>
internal static class WindowsPower
{
    /// <summary>Evita que Windows suspenda el equipo mientras el objeto no se libere.</summary>
    public static IDisposable KeepSystemAwake(string reason, ILogger logger)
    {
        if (!OperatingSystem.IsWindows())
            return NoopDisposable.Instance;

        var context = new ReasonContext
        {
            Version = 0,
            Flags = 1, // POWER_REQUEST_CONTEXT_SIMPLE_STRING
            SimpleReasonString = reason
        };
        var request = PowerCreateRequest(ref context);
        if (request.IsInvalid || !PowerSetRequest(request, PowerRequestType.SystemRequired))
        {
            logger.LogWarning("No se pudo pedir a Windows que no suspenda el equipo (error {Error}).",
                Marshal.GetLastWin32Error());
            request.Dispose();
            return NoopDisposable.Instance;
        }

        return new PowerRequestScope(request);
    }

    private sealed class PowerRequestScope(SafeFileHandle request) : IDisposable
    {
        public void Dispose()
        {
            if (request.IsClosed)
                return;
            PowerClearRequest(request, PowerRequestType.SystemRequired);
            request.Dispose();
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();
        public void Dispose() { }
    }

    private enum PowerRequestType
    {
        DisplayRequired = 0,
        SystemRequired = 1,
        AwayModeRequired = 2,
        ExecutionRequired = 3
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ReasonContext
    {
        public uint Version;
        public uint Flags;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string SimpleReasonString;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeFileHandle PowerCreateRequest(ref ReasonContext context);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PowerSetRequest(SafeFileHandle powerRequest, PowerRequestType requestType);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PowerClearRequest(SafeFileHandle powerRequest, PowerRequestType requestType);
}
