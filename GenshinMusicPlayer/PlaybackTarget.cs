using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace GenshinMusicPlayer
{
    internal sealed class PlaybackTargetException : InvalidOperationException
    {
        internal PlaybackTargetException(string message) : base(message) { }
    }

    // One target per session. Once a check fails, that session cannot resume sending input.
    internal sealed class PlaybackTarget
    {
        private const int CheckIntervalMilliseconds = 25;
        private readonly Func<bool> isValid;
        private readonly Func<bool> isForeground;
        private readonly Action activate;
        private bool stopped;

        internal PlaybackTarget(Func<bool> isValid, Func<bool> isForeground, Action activate)
        {
            this.isValid = isValid;
            this.isForeground = isForeground;
            this.activate = activate;
        }

        internal static PlaybackTarget FindGameWindow()
        {
            var processes = Process.GetProcessesByName("YuanShen");
            try
            {
                foreach (var process in processes)
                {
                    if (process.HasExited) continue;
                    var window = process.MainWindowHandle;
                    var processId = (uint)process.Id;
                    if (!IsTargetWindow(window, processId)) continue;

                    return new PlaybackTarget(
                        () => IsTargetWindow(window, processId),
                        () => !IsIconic(window) && GetForegroundWindow() == window,
                        () =>
                        {
                            if (IsIconic(window)) ShowWindowAsync(window, 9); // SW_RESTORE
                            SetForegroundWindow(window);
                        });
                }
            }
            finally
            {
                foreach (var process in processes) process.Dispose();
            }
            throw new PlaybackTargetException("未找到原神游戏窗口，已取消演奏。请先启动游戏。");
        }

        internal async Task ActivateAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (stopped || !isValid()) Stop("原神游戏窗口已关闭，已取消演奏。");
            if (!isForeground()) activate();

            // Foreground activation can complete asynchronously. Do not start the countdown
            // until the actual foreground window matches, regardless of the API return value.
            var timeout = Stopwatch.StartNew();
            while (!isForeground())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!isValid() || timeout.ElapsedMilliseconds >= 1000)
                    Stop("无法将原神窗口切换到前台，已取消演奏。请切回游戏后重试。");
                await Task.Delay(CheckIntervalMilliseconds, cancellationToken).ConfigureAwait(false);
            }
            EnsureForeground(cancellationToken);
        }

        internal void EnsureForeground(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (stopped || !isValid() || !isForeground())
                Stop("原神窗口已关闭或不在前台，已停止演奏。请切回游戏后重新开始。");
        }

        internal async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            var timer = Stopwatch.StartNew();
            EnsureForeground(cancellationToken);
            while (timer.Elapsed < delay)
            {
                var remaining = delay - timer.Elapsed;
                if (remaining <= TimeSpan.Zero) break;
                await Task.Delay(remaining < TimeSpan.FromMilliseconds(CheckIntervalMilliseconds)
                    ? remaining : TimeSpan.FromMilliseconds(CheckIntervalMilliseconds), cancellationToken).ConfigureAwait(false);
                EnsureForeground(cancellationToken);
            }
            EnsureForeground(cancellationToken);
        }

        internal void Send(Action sendInput, CancellationToken cancellationToken)
        {
            // Keep this check adjacent to input injection, after any note generation or wait.
            // Windows does not provide an atomic foreground-check-and-SendInput operation.
            EnsureForeground(cancellationToken);
            sendInput();
        }

        private void Stop(string message)
        {
            stopped = true;
            throw new PlaybackTargetException(message);
        }

        private static bool IsTargetWindow(IntPtr window, uint processId)
        {
            return window != IntPtr.Zero && IsWindow(window)
                && GetWindowThreadProcessId(window, out uint ownerId) != 0 && ownerId == processId;
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr window);
        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr window, int command);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    }
}
