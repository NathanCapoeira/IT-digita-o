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
        public void StartOnFirstCharacter() { if (!watch.IsRunning && !hasStarted) { hasStarted = true; watch.Start(); } }
        public TimeSpan Elapsed { get { return watch.Elapsed; } }
        public TimeSpan Remaining { get { return duration - watch.Elapsed < TimeSpan.Zero ? TimeSpan.Zero : duration - watch.Elapsed; } }
        public bool IsExpired { get { return watch.Elapsed >= duration; } }
        private bool hasStarted;
        public bool HasStarted { get { return hasStarted; } }
        public void Stop() { watch.Stop(); }
    }
}
