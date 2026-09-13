using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace dailyTimeWorker.Services.Scheduling;

/// <summary>
/// Temporizadores de Windows por hora absoluta (despiertan el equipo de la suspensión) y
/// solicitudes para que no se suspenda durante una captura.
/// </summary>
internal static class WindowsPower
{
    private const uint TimerAllAccess = 0x1F0003;
    private const int ErrorNotSupported = 50;

    /// <summary>
    /// Espera hasta <paramref name="utc"/> sin consumir CPU. A diferencia de <see cref="Task.Delay(TimeSpan)"/>,
    /// apunta a la hora del reloj, así que no se retrasa si el equipo estuvo suspendido.
    /// </summary>
    public static async Task WaitUntilAsync(
        DateTime utc, bool wakeSystem, ILogger logger, CancellationToken cancellationToken)
    {
        utc = utc.Kind == DateTimeKind.Local ? utc.ToUniversalTime() : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        if (utc <= DateTime.UtcNow)
            return;

        if (!OperatingSystem.IsWindows())
        {
            await Task.Delay(utc - DateTime.UtcNow, cancellationToken);
            return;
        }

        var timer = CreateWaitableTimerExW(IntPtr.Zero, null, 0, TimerAllAccess);
        if (timer.IsInvalid)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "No se pudo crear el temporizador de Windows.");

        using var waitHandle = new TimerWaitHandle(timer);
        var dueTime = utc.ToFileTimeUtc(); // valor positivo = hora absoluta
        if (!SetWaitableTimer(timer, ref dueTime, 0, IntPtr.Zero, IntPtr.Zero, wakeSystem))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "No se pudo programar el temporizador de Windows.");

        if (wakeSystem && Marshal.GetLastWin32Error() == ErrorNotSupported)
        {
            logger.LogWarning(
                "Este equipo no permite despertar con temporizadores: si está suspendido a la hora programada, la captura se omitirá.");
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var registration = ThreadPool.RegisterWaitForSingleObject(
            waitHandle, (_, _) => tcs.TrySetResult(), null, Timeout.Infinite, executeOnlyOnce: true);
        try
        {
            await using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
                await tcs.Task;
        }
        finally
        {
            registration.Unregister(null);
        }
    }

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

    private sealed class TimerWaitHandle : WaitHandle
    {
        public TimerWaitHandle(SafeWaitHandle handle) => SafeWaitHandle = handle;
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

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeWaitHandle CreateWaitableTimerExW(
        IntPtr lpTimerAttributes, string? lpTimerName, uint dwFlags, uint dwDesiredAccess);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWaitableTimer(
        SafeWaitHandle hTimer,
        ref long pDueTime,
        int lPeriod,
        IntPtr pfnCompletionRoutine,
        IntPtr lpArgToCompletionRoutine,
        [MarshalAs(UnmanagedType.Bool)] bool fResume);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeFileHandle PowerCreateRequest(ref ReasonContext context);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PowerSetRequest(SafeFileHandle powerRequest, PowerRequestType requestType);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PowerClearRequest(SafeFileHandle powerRequest, PowerRequestType requestType);
}
