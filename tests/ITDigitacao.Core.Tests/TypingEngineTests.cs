using ITDigitacao.Core.Typing;
using ITDigitacao.Core.Metrics;
using System;
using Xunit;

namespace ITDigitacao.Core.Tests
{
    public sealed class TypingEngineTests
    {
        [Fact]
        public void CountsWrongAttemptEvenAfterCorrection()
        {
            var engine = new TypingEngine("áB");
            Assert.False(engine.Type('a'));
            Assert.Equal(1, engine.Errors);
            Assert.True(engine.Backspace());
            Assert.Equal(0, engine.Errors);
            Assert.True(engine.Type('á'));
            Assert.True(engine.Type('B'));
            Assert.True(engine.IsComplete);
            Assert.Equal(66.667, engine.Accuracy, 2);
        }

        [Fact]
        public void NormalizesComposedAccentsAndTracksPositions()
        {
            var engine = new TypingEngine("ç");
            Assert.True(engine.Type('ç'));
            Assert.True(engine.IsCorrectAt(0));
            Assert.Equal(100, engine.ProgressPercent);
        }

        [Fact]
        public void MetricsAvoidUnrealisticFirstSeconds()
        {
            Assert.Equal(0, MetricsCalculator.WordsPerMinute(10, TimeSpan.FromSeconds(4)));
            Assert.Equal(12, MetricsCalculator.WordsPerMinute(10, TimeSpan.FromSeconds(10)), 3);
        }
    }
}
