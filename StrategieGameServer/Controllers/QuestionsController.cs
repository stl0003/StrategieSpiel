using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using StrategieGameServer.Data;
using StrategieGameServer.Models;

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
        [HttpGet]
        public async Task<ActionResult> GetRandomQuestion([FromQuery] string type)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(type))
                    return BadRequest(new { error = "Der Parameter 'type' ist erforderlich." });

                var question = await _context.Questions
                    .Where(q => q.Type == type)
                    .OrderBy(q => Guid.NewGuid())
                    .FirstOrDefaultAsync();

                if (question == null)
                    return NotFound(new { error = $"Keine Frage vom Typ '{type}' gefunden." });

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
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Interner Serverfehler: {ex.Message}" });
            }
        }

        /// <summary>
        /// Prüft, ob die vom Client gesendete Antwort korrekt ist.
        /// </summary>
        [HttpPost("answer")]
        public async Task<ActionResult> CheckAnswer([FromBody] AnswerRequest request)
        {
            try
            {
                if (request == null || request.QuestionId <= 0)
                    return BadRequest(new { error = "Ungültige Anfrage. 'QuestionId' ist erforderlich." });

                var question = await _context.Questions.FindAsync(request.QuestionId);
                if (question == null)
                    return NotFound(new { error = "Frage nicht gefunden." });

                bool isCorrect;

                switch (question.Type)
                {
                    case "singleChoice":
                        isCorrect = CheckSingleChoice(question, request.Answer);
                        break;
                    case "multipleChoice":
                        isCorrect = CheckMultipleChoice(question, request.Answer);
                        break;
                    case "freeText":
                        isCorrect = true;
                        break;
                    case "dropdown":
                        isCorrect = CheckDropdown(question, request.Answer);
                        break;
                    case "dragDrop":
                        isCorrect = CheckDragDrop(question, request.Answer);
                        break;
                    default:
                        return BadRequest(new { error = $"Unbekannter Fragetyp: {question.Type}" });
                }

                return Ok(new AnswerResponse { Correct = isCorrect });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Interner Serverfehler: {ex.Message}" });
            }
        }

        // ----- Hilfsmethoden für die Antwortprüfung (mit Validierung) -----

        private bool CheckSingleChoice(Question question, JsonElement answer)
        {
            if (answer.ValueKind != JsonValueKind.Number)
                throw new ArgumentException("Bei 'singleChoice' muss die Antwort eine Zahl (Index) sein.");

            if (string.IsNullOrEmpty(question.CorrectAnswerJson))
                throw new InvalidOperationException("Keine korrekte Antwort für diese Frage in der Datenbank.");

            int selectedIndex = answer.GetInt32();
            int correctIndex = JsonSerializer.Deserialize<int>(question.CorrectAnswerJson);
            return selectedIndex == correctIndex;
        }

        private bool CheckMultipleChoice(Question question, JsonElement answer)
        {
            if (answer.ValueKind != JsonValueKind.Array)
                throw new ArgumentException("Bei 'multipleChoice' muss die Antwort ein Array von Zahlen (Indizes) sein.");

            if (string.IsNullOrEmpty(question.CorrectAnswerJson))
                throw new InvalidOperationException("Keine korrekte Antwort für diese Frage in der Datenbank.");

            var selectedIndices = JsonSerializer.Deserialize<List<int>>(answer.GetRawText());
            var correctIndices = JsonSerializer.Deserialize<List<int>>(question.CorrectAnswerJson);

            if (selectedIndices == null || correctIndices == null)
                throw new InvalidOperationException("Fehler beim Deserialisieren der Antwortdaten.");

            return selectedIndices.Count == correctIndices.Count &&
                   !selectedIndices.Except(correctIndices).Any();
        }

        private bool CheckDropdown(Question question, JsonElement answer)
        {
            if (answer.ValueKind != JsonValueKind.Number)
                throw new ArgumentException("Bei 'dropdown' muss die Antwort eine Zahl (Index) sein.");

            if (string.IsNullOrEmpty(question.CorrectAnswerJson))
                throw new InvalidOperationException("Keine korrekte Antwort für diese Frage in der Datenbank.");

            int selectedIndex = answer.GetInt32();
            int correctIndex = JsonSerializer.Deserialize<int>(question.CorrectAnswerJson);
            return selectedIndex == correctIndex;
        }

        private bool CheckDragDrop(Question question, JsonElement answer)
        {
            if (answer.ValueKind != JsonValueKind.Object)
                throw new ArgumentException("Bei 'dragDrop' muss die Antwort ein JSON-Objekt mit Platzhalter-Zuordnungen sein.");

            if (string.IsNullOrEmpty(question.OptionsMapping))
                throw new InvalidOperationException("Keine korrekte Zuordnung für diese Frage in der Datenbank.");

            var userMapping = JsonSerializer.Deserialize<Dictionary<string, string>>(answer.GetRawText());
            var correctMapping = JsonSerializer.Deserialize<Dictionary<string, string>>(question.OptionsMapping);

            if (userMapping == null || correctMapping == null)
                throw new InvalidOperationException("Fehler beim Deserialisieren der Zuordnungsdaten.");

            return correctMapping.All(kv =>
                userMapping.ContainsKey(kv.Key) && userMapping[kv.Key] == kv.Value
            );
        }
    }

    // ----- DTOs (unverändert) -----
    public class QuestionDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public List<string>? Options { get; set; }
        public string? Placeholder { get; set; }
        public string? SentenceTemplate { get; set; }
        public List<string>? Placeholders { get; set; }
        public Dictionary<string, string>? OptionsMapping { get; set; }
    }

    public class AnswerRequest
    {
        public int QuestionId { get; set; }
        public JsonElement Answer { get; set; }
    }

    public class AnswerResponse
    {
        public bool Correct { get; set; }
    }
}