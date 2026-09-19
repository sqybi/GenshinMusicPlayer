using GenshinMusicPlayer;
using System;
using System.Threading;
using System.Threading.Tasks;

// Standalone regression runner: no game, native input, or test packages required.
internal static class PlaybackTargetTests
{
    private sealed class WindowState
    {
        internal bool Exists = true;
        internal bool Foreground = true;
        internal int ActivationRequests;
        internal int Sent;
        internal readonly PlaybackTarget Target;

        internal WindowState()
        {
            Target = new PlaybackTarget(() => Exists, () => Foreground, () => ActivationRequests++);
        }

        internal void Send(CancellationToken token = default(CancellationToken))
        {
            Target.Send(() => Sent++, token);
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static async Task Expect<T>(Func<Task> action) where T : Exception
    {
        try { await action(); }
        catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }

    private static Task Send(WindowState state)
    {
        state.Send();
        return Task.CompletedTask;
    }

    private static async Task Run()
    {
        var missing = new WindowState { Exists = false };
        await Expect<PlaybackTargetException>(() => missing.Target.ActivateAsync(CancellationToken.None));
        await Expect<PlaybackTargetException>(() => Send(missing));
        Check(missing.ActivationRequests == 0 && missing.Sent == 0, "Missing window must fail before activation/input.");
        Console.WriteLine("PASS missing/invalid window");

        var denied = new WindowState { Foreground = false };
        await Expect<PlaybackTargetException>(() => denied.Target.ActivateAsync(CancellationToken.None));
        denied.Foreground = true;
        await Expect<PlaybackTargetException>(() => Send(denied));
        Check(denied.ActivationRequests == 1 && denied.Sent == 0, "Failed activation must end the session.");
        Console.WriteLine("PASS activation timeout and no automatic restart");

        bool requested = false;
        int checks = 0;
        var delayedActivation = new PlaybackTarget(() => true,
            () => requested && ++checks >= 3, () => requested = true);
        await delayedActivation.ActivateAsync(CancellationToken.None);
        Check(requested && checks >= 3, "Must wait for actual foreground acquisition.");
        Console.WriteLine("PASS asynchronous activation");

        foreach (int seconds in new[] { 1, 30 })
        {
            int foregroundChecks = 0;
            var lostDuringWait = new PlaybackTarget(() => true, () => ++foregroundChecks < 3, () => { });
            using (var deadline = new CancellationTokenSource(2000))
            {
                await Expect<PlaybackTargetException>(() => lostDuringWait.DelayAsync(TimeSpan.FromSeconds(seconds), deadline.Token));
            }
            Console.WriteLine("PASS focus loss during " + seconds + " second wait");
        }

        var lostBeforeSend = new WindowState();
        await lostBeforeSend.Target.ActivateAsync(CancellationToken.None);
        await lostBeforeSend.Target.DelayAsync(TimeSpan.Zero, CancellationToken.None);
        lostBeforeSend.Foreground = false;
        await Expect<PlaybackTargetException>(() => Send(lostBeforeSend));
        lostBeforeSend.Foreground = true;
        await Expect<PlaybackTargetException>(() => Send(lostBeforeSend));
        Check(lostBeforeSend.Sent == 0, "Focus change immediately before injection must suppress input permanently.");
        Console.WriteLine("PASS final input check and stopped-session latch");

        var closed = new WindowState();
        await closed.Target.ActivateAsync(CancellationToken.None);
        closed.Exists = false;
        await Expect<PlaybackTargetException>(() => Send(closed));
        Check(closed.Sent == 0, "Closed/replaced window must suppress input even if foreground still matches.");
        Console.WriteLine("PASS window closure/identity loss");

        using (var cancellation = new CancellationTokenSource())
        {
            var canceled = new WindowState();
            cancellation.Cancel();
            await Expect<OperationCanceledException>(() => canceled.Target.ActivateAsync(cancellation.Token));
            await Expect<OperationCanceledException>(() =>
            {
                canceled.Send(cancellation.Token);
                return Task.CompletedTask;
            });
            Check(canceled.ActivationRequests == 0 && canceled.Sent == 0, "Cancellation must suppress activation/input.");
        }
        Console.WriteLine("PASS cancellation before activation/input");

        using (var cancellation = new CancellationTokenSource(50))
        {
            var canceled = new WindowState();
            await Expect<OperationCanceledException>(() => canceled.Target.DelayAsync(TimeSpan.FromSeconds(30), cancellation.Token));
        }
        Console.WriteLine("PASS cancellation during a long rest");

        var normal = new WindowState();
        await normal.Target.ActivateAsync(CancellationToken.None);
        await normal.Target.DelayAsync(TimeSpan.FromMilliseconds(50), CancellationToken.None);
        normal.Send();
        normal.Send();
        Check(normal.ActivationRequests == 0 && normal.Sent == 2, "Valid foreground target must allow playback.");
        Console.WriteLine("PASS normal playback");
    }

    private static int Main()
    {
        try { Run().GetAwaiter().GetResult(); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
