using System;
using System.Globalization;
using System.Text;

namespace ITDigitacao.Core.Typing
{
    public sealed class TypingEngine
    {
        private readonly string reference;
        private readonly StringBuilder entered = new StringBuilder();
        private readonly System.Collections.Generic.List<bool> correctness = new System.Collections.Generic.List<bool>();
        private int attemptsCorrect;
        private int attemptsIncorrect;
        private int errors;

        public TypingEngine(string text)
        {
            if (String.IsNullOrEmpty(text)) throw new ArgumentException("O texto de referência é obrigatório.", "text");
            reference = text.Normalize(NormalizationForm.FormC);
        }

        public string EnteredText { get { return entered.ToString(); } }
        public int Attempts { get { return attemptsCorrect + attemptsIncorrect; } }
        public int CorrectAttempts { get { return attemptsCorrect; } }
        public int IncorrectAttempts { get { return attemptsIncorrect; } }
        public int Errors { get { return errors; } }
        public int CorrectCharacters { get { return Math.Max(0, entered.Length - errors); } }
        public int Position { get { return entered.Length; } }
        public int ReferenceLength { get { return reference.Length; } }
        public int ErrorsAt(int index) { return index >= 0 && index < correctness.Count && !correctness[index] ? 1 : 0; }
        public bool IsCorrectAt(int index) { return index >= 0 && index < correctness.Count && correctness[index]; }
        public bool IsComplete { get { return entered.ToString() == reference; } }
        public int ProgressPercent { get { return Math.Min(100, (int)(100L * CorrectCharacters / reference.Length)); } }
        public bool IsNextCorrect(char value)
        {
            return entered.Length < reference.Length && CharEquals(reference[entered.Length], value);
        }
        public bool Type(char value)
        {
            if (entered.Length >= reference.Length) return false;
            value = value.ToString().Normalize(NormalizationForm.FormC)[0];
            bool correct = CharEquals(reference[entered.Length], value);
            if (correct) attemptsCorrect++; else { errors++; attemptsIncorrect++; }
            entered.Append(value);
            correctness.Add(correct);
            return correct;
        }
        public bool Backspace()
        {
            if (entered.Length == 0) return false;
            if (!correctness[correctness.Count - 1]) errors--;
            correctness.RemoveAt(correctness.Count - 1);
            entered.Length--;
            return true;
        }
        private static bool CharEquals(char left, char right)
        {
            return left.ToString().Normalize(NormalizationForm.FormC) == right.ToString().Normalize(NormalizationForm.FormC);
        }
        public double Accuracy { get { return Attempts == 0 ? 0.0 : 100.0 * attemptsCorrect / Attempts; } }
    }
}
