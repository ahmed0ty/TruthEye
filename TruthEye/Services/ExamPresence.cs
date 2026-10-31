using System.Collections.Concurrent;

namespace TruthEye.Services
{
    /// What the student had on the screen (but had not saved yet) when they left the exam
    public record LeaveAnswer(int QuestionId, int? SelectedOptionId, string? AnswerText);

    /// Remembers, in memory only, when each running exam page was last seen.
    public static class ExamPresence
    {
        private static readonly ConcurrentDictionary<int, DateTime> Seen = new();

        /// Opening the exam again after the page was silent for longer than this counts as "left the exam"
        public static readonly TimeSpan ReturnGrace = TimeSpan.FromSeconds(20);

        /// A running exam whose page has been silent for this long is closed by the background sweep
        public static readonly TimeSpan AbandonAfter = TimeSpan.FromSeconds(90);

        public static void Touch(int sessionId) => Seen[sessionId] = DateTime.UtcNow;

        public static DateTime? LastSeen(int sessionId) =>
            Seen.TryGetValue(sessionId, out var t) ? t : (DateTime?)null;

        public static void Forget(int sessionId) => Seen.TryRemove(sessionId, out _);
    }
}