using System;
using System.Diagnostics;

namespace ITDigitacao.Core.Sessions
{
    public sealed class SessionClock
    {
        private readonly Stopwatch watch = new Stopwatch();
        private readonly TimeSpan duration;
        public SessionClock(int minutes)
        {
            if (minutes < 1 || minutes > 30) throw new ArgumentOutOfRangeException("minutes");
            duration = TimeSpan.FromMinutes(minutes);
        }
        public void StartOnFirstCharacter() { if (!watch.IsRunning && !watch.IsStopped) watch.Start(); }
        public TimeSpan Elapsed { get { return watch.Elapsed; } }
        public TimeSpan Remaining { get { return duration - watch.Elapsed < TimeSpan.Zero ? TimeSpan.Zero : duration - watch.Elapsed; } }
        public bool IsExpired { get { return watch.Elapsed >= duration; } }
        public bool HasStarted { get { return watch.IsRunning || watch.IsFinished(); } }
        public void Stop() { watch.Stop(); }
    }
    internal static class StopwatchExtensions
    {
        public static bool IsFinished(this Stopwatch watch) { return watch.ElapsedTicks > 0 && !watch.IsRunning; }
    }
}
