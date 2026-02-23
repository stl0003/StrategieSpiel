using System;

namespace StrategieGameServer.Models
{
    public class Question
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;          // NOT NULL in DB
        public string QuestionText { get; set; } = string.Empty;  // NOT NULL in DB
        public string? OptionsJson { get; set; }                  // Kann NULL sein
        public string? CorrectAnswerJson { get; set; }            // Kann NULL sein
        public string? Placeholder { get; set; }                  // Kann NULL sein
        public string? SentenceTemplate { get; set; }             // Kann NULL sein
        public string? Placeholders { get; set; }                 // Kann NULL sein
        public string? OptionsMapping { get; set; }               // Kann NULL sein
        public DateTime? CreatedAt { get; set; }                  // In der Tabelle vorhanden, aber NULL erlaubt – daher nullable!
    }
}