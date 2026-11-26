using System;

namespace ITDigitacao.Core.Metrics
{
    public static class MetricsCalculator
    {
        public static double WordsPerMinute(int correctCharacters, TimeSpan elapsed)
        {
            return elapsed.TotalSeconds < 5 ? 0 : correctCharacters / 5.0 / elapsed.TotalMinutes;
        }
        public static int Stars(int attempts, double accuracy)
        {
            if (attempts < 20) return 0;
            if (accuracy >= 95) return 3;
            if (accuracy >= 80) return 2;
            return 1;
        }
    }
}
