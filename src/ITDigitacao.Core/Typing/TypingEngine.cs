using System;
using System.Globalization;
using System.Text;

namespace ITDigitacao.Core.Typing
{
    public sealed class TypingEngine
    {
        private readonly string reference;
        private readonly StringBuilder entered = new StringBuilder();
        private int attempts;
        private int errors;

        public TypingEngine(string text)
        {
            if (String.IsNullOrEmpty(text)) throw new ArgumentException("O texto de referência é obrigatório.", "text");
            reference = text.Normalize(NormalizationForm.FormC);
        }

        public string EnteredText { get { return entered.ToString(); } }
        public int Attempts { get { return attempts; } }
        public int Errors { get { return errors; } }
        public int CorrectCharacters { get { return Math.Max(0, entered.Length - errors); } }
        public int Position { get { return entered.Length; } }
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
            attempts++;
            bool correct = CharEquals(reference[entered.Length], value);
            if (!correct) errors++;
            entered.Append(value);
            return correct;
        }
        public bool Backspace()
        {
            if (entered.Length == 0) return false;
            entered.Length--;
            return true;
        }
        private static bool CharEquals(char left, char right)
        {
            return left.ToString().Normalize(NormalizationForm.FormC) == right.ToString().Normalize(NormalizationForm.FormC);
        }
        public double Accuracy { get { return attempts == 0 ? 100.0 : 100.0 * (attempts - errors) / attempts; } }
    }
}
