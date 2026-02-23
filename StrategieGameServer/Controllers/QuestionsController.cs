using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using StrategieGameServer.Data;      // Für GameDbContext
using StrategieGameServer.Models;    // Für Question

namespace StrategieGameServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionsController : ControllerBase
    {
        private readonly GameDbContext _context;

        public QuestionsController(GameDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gibt eine zufällige Frage des angegebenen Typs zurück (ohne korrekte Antwort).
        /// </summary>
        /// <param name="type">Fragetyp: singleChoice, multipleChoice, freeText, dropdown, dragDrop</param>
        [HttpGet]
        public async Task<ActionResult<QuestionDto>> GetRandomQuestion([FromQuery] string type)
        {
            var question = await _context.Questions
                .Where(q => q.Type == type)
                .OrderBy(q => Guid.NewGuid()) // Zufällige Sortierung (für SQL Server)
                .FirstOrDefaultAsync();

            if (question == null)
                return NotFound($"Keine Frage vom Typ '{type}' gefunden.");

            var dto = new QuestionDto
            {
                Id = question.Id,
                Type = question.Type,
                QuestionText = question.QuestionText,
                Options = !string.IsNullOrEmpty(question.OptionsJson)
                    ? JsonSerializer.Deserialize<List<string>>(question.OptionsJson)
                    : null,
                Placeholder = question.Placeholder,
                SentenceTemplate = question.SentenceTemplate,
                Placeholders = !string.IsNullOrEmpty(question.Placeholders)
                    ? JsonSerializer.Deserialize<List<string>>(question.Placeholders)
                    : null,
                OptionsMapping = !string.IsNullOrEmpty(question.OptionsMapping)
                    ? JsonSerializer.Deserialize<Dictionary<string, string>>(question.OptionsMapping)
                    : null
            };

            return Ok(dto);
        }

        /// <summary>
        /// Prüft, ob die vom Client gesendete Antwort korrekt ist.
        /// </summary>
        [HttpPost("answer")]
        public async Task<ActionResult<AnswerResponse>> CheckAnswer([FromBody] AnswerRequest request)
        {
            var question = await _context.Questions.FindAsync(request.QuestionId);
            if (question == null)
                return NotFound("Frage nicht gefunden.");

            bool isCorrect = false;

            switch (question.Type)
            {
                case "singleChoice":
                    isCorrect = CheckSingleChoice(question, request.Answer);
                    break;
                case "multipleChoice":
                    isCorrect = CheckMultipleChoice(question, request.Answer);
                    break;
                case "freeText":
                    // Freitext kann nicht automatisch korrigiert werden – hier immer true.
                    // Optional: Antwort speichern und später manuell bewerten lassen.
                    isCorrect = true;
                    break;
                case "dropdown":
                    isCorrect = CheckDropdown(question, request.Answer);
                    break;
                case "dragDrop":
                    isCorrect = CheckDragDrop(question, request.Answer);
                    break;
                default:
                    return BadRequest("Unbekannter Fragetyp.");
            }

            return Ok(new AnswerResponse { Correct = isCorrect });
        }

        // ----- Hilfsmethoden für die Antwortprüfung -----

        private bool CheckSingleChoice(Question question, JsonElement answer)
        {
            // Erwartet wird ein Integer (Index der gewählten Antwort)
            if (answer.ValueKind != JsonValueKind.Number)
                return false;

            int selectedIndex = answer.GetInt32();
            int correctIndex = JsonSerializer.Deserialize<int>(question.CorrectAnswerJson);
            return selectedIndex == correctIndex;
        }

        private bool CheckMultipleChoice(Question question, JsonElement answer)
        {
            // Erwartet wird ein Array von Integern (Indizes der gewählten Antworten)
            if (answer.ValueKind != JsonValueKind.Array)
                return false;

            var selectedIndices = JsonSerializer.Deserialize<List<int>>(answer.GetRawText());
            var correctIndices = JsonSerializer.Deserialize<List<int>>(question.CorrectAnswerJson);

            if (selectedIndices == null || correctIndices == null)
                return false;

            // Prüfen auf gleiche Länge und gleiche Elemente (Reihenfolge egal)
            return selectedIndices.Count == correctIndices.Count &&
                   !selectedIndices.Except(correctIndices).Any();
        }

        private bool CheckDropdown(Question question, JsonElement answer)
        {
            // Erwartet wird ein Integer (Index der gewählten Option)
            if (answer.ValueKind != JsonValueKind.Number)
                return false;

            int selectedIndex = answer.GetInt32();
            int correctIndex = JsonSerializer.Deserialize<int>(question.CorrectAnswerJson);
            return selectedIndex == correctIndex;
        }

        private bool CheckDragDrop(Question question, JsonElement answer)
        {
            // Erwartet wird ein JSON-Objekt, das die Zuordnung der Platzhalter zu den gewählten Optionen enthält.
            // Beispiel: {"___1___": "Elefant", "___2___": "tanzt"}
            if (answer.ValueKind != JsonValueKind.Object)
                return false;

            var userMapping = JsonSerializer.Deserialize<Dictionary<string, string>>(answer.GetRawText());
            var correctMapping = JsonSerializer.Deserialize<Dictionary<string, string>>(question.OptionsMapping);

            if (userMapping == null || correctMapping == null)
                return false;

            // Prüfen, ob alle Platzhalter korrekt zugeordnet wurden
            return correctMapping.All(kv =>
                userMapping.ContainsKey(kv.Key) && userMapping[kv.Key] == kv.Value
            );
        }
    }

    // ----- DTOs -----

    public class QuestionDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public List<string>? Options { get; set; }          // Für Single/Multiple Choice, Dropdown
        public string? Placeholder { get; set; }            // Für Freitext
        public string? SentenceTemplate { get; set; }       // Für Drag&Drop
        public List<string>? Placeholders { get; set; }     // Für Drag&Drop (Liste der Platzhalter)
        public Dictionary<string, string>? OptionsMapping { get; set; } // Für Drag&Drop (korrekte Zuordnung)
    }

    public class AnswerRequest
    {
        public int QuestionId { get; set; }
        public JsonElement Answer { get; set; } // Kann Zahl, Array, Objekt oder String sein
    }

    public class AnswerResponse
    {
        public bool Correct { get; set; }
    }
}