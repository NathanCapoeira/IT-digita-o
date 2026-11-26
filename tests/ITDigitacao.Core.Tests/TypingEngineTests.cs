using ITDigitacao.Core.Typing;
using ITDigitacao.Core.Metrics;
using ITDigitacao.Core.Sessions;
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

        [Fact]
        public void DeleteAtRemovesCharacterWithoutErasingAttemptHistory()
        {
            var engine = new TypingEngine("abc");
            Assert.True(engine.Type('a'));
            Assert.False(engine.Type('x'));
            Assert.True(engine.Type('c'));

            Assert.True(engine.DeleteAt(1));

            Assert.Equal("ac", engine.EnteredText);
            Assert.Equal(1, engine.Errors);
            Assert.Equal(3, engine.Attempts);
            Assert.Equal(2, engine.CorrectAttempts);
            Assert.Equal(66.667, engine.Accuracy, 2);
        }

        [Fact]
        public void EditingAtCaretKeepsVisibleTextAndHistoricalAccuracyConsistent()
        {
            var engine = new TypingEngine("abc");
            engine.Type('a');
            engine.Type('x');

            Assert.True(engine.Edit(1, 1, "b"));

            Assert.Equal("ab", engine.EnteredText);
            Assert.Equal(0, engine.Errors);
            Assert.Equal(3, engine.Attempts);
            Assert.Equal(66.667, engine.Accuracy, 2);
        }

        [Fact]
        public void EmptyReferenceCannotStartAnActivity()
        {
            Assert.Throws<ArgumentException>(() => new TypingEngine(""));
        }

        [Fact]
        public void SessionClockStartsOnlyWhenFirstCharacterIsRequested()
        {
            var clock = new SessionClock(5);

            Assert.False(clock.HasStarted);
            Assert.Equal(TimeSpan.FromMinutes(5), clock.Remaining);

            clock.StartOnFirstCharacter();

            Assert.True(clock.HasStarted);
            Assert.True(clock.Remaining <= TimeSpan.FromMinutes(5));
            Assert.False(clock.IsExpired);
        }

        [Fact]
        public void SessionClockRejectsDurationsOutsideTheProductLimit()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SessionClock(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SessionClock(31));
        }

        [Fact]
        public void StarsRequirePracticeAndUseAccuracyThresholds()
        {
            Assert.Equal(0, MetricsCalculator.Stars(19, 100));
            Assert.Equal(1, MetricsCalculator.Stars(20, 79.9));
            Assert.Equal(2, MetricsCalculator.Stars(20, 80));
            Assert.Equal(3, MetricsCalculator.Stars(20, 95));
        }

        [Fact]
        public void PunctuationSpacesAndLineBreaksAreTypedAsReferenceCharacters()
        {
            var engine = new TypingEngine("Olá, mundo!\nTudo bem?");

            foreach (char character in "Olá, mundo!\nTudo bem?")
                Assert.True(engine.Type(character));

            Assert.True(engine.IsComplete);
            Assert.Equal(100, engine.Accuracy);
        }

        [Fact]
        public void BackspaceAtStartDoesNotChangeProgressOrAttemptCount()
        {
            var engine = new TypingEngine("á");

            Assert.False(engine.Backspace());
            Assert.Equal(0, engine.Attempts);
            Assert.Equal(0, engine.ProgressPercent);
            Assert.Equal("", engine.EnteredText);
        }
    }
}
