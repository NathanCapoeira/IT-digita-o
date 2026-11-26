using System.Collections.Generic;

namespace ITDigitacao.Core.Models
{
    public sealed class Activity
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Difficulty { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public string Question { get; set; }
        public List<string> Options { get; set; }
        public int CorrectOption { get; set; }
        public List<string> References { get; set; }
    }
}
