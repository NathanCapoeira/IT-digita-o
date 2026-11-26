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
        public int CorrectCharacters { get { return correctness.FindAll(value => value).Count; } }
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
            Edit(entered.Length, 0, value.ToString());
            return correct;
        }
        public bool Backspace()
        {
            if (entered.Length == 0) return false;
            return Edit(entered.Length - 1, 1, String.Empty);
        }
        public bool DeleteAt(int index)
        {
            if (index < 0 || index >= entered.Length) return false;
            return Edit(index, 1, String.Empty);
        }
        public bool Edit(int index, int removeLength, string insertedText)
        {
            if (index < 0 || index > entered.Length || removeLength < 0 || index + removeLength > entered.Length)
                throw new ArgumentOutOfRangeException("index");

            insertedText = (insertedText ?? String.Empty).Normalize(NormalizationForm.FormC);
            int available = reference.Length - (entered.Length - removeLength);
            if (insertedText.Length > available) insertedText = insertedText.Substring(0, available);
            if (removeLength == 0 && insertedText.Length == 0) return false;

            entered.Remove(index, removeLength);
            entered.Insert(index, insertedText);
            for (int i = 0; i < insertedText.Length; i++)
            {
                if (index + i < reference.Length && CharEquals(reference[index + i], insertedText[i])) attemptsCorrect++;
                else attemptsIncorrect++;
            }
            RecalculateCorrectness();
            return true;
        }
        private void RecalculateCorrectness()
        {
            correctness.Clear();
            errors = 0;
            for (int i = 0; i < entered.Length; i++)
            {
                bool correct = i < reference.Length && CharEquals(reference[i], entered[i]);
                correctness.Add(correct);
                if (!correct) errors++;
            }
        }
        private static bool CharEquals(char left, char right)
        {
            return left.ToString().Normalize(NormalizationForm.FormC) == right.ToString().Normalize(NormalizationForm.FormC);
        }
        public double Accuracy { get { return Attempts == 0 ? 0.0 : 100.0 * attemptsCorrect / Attempts; } }
    }
}
