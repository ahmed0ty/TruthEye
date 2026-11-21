namespace TruthEye.Security
{
    public static class RiskCalculator
    {
        // لازم تفضل مطابقة لـ src/lib/proctoring/events.ts
        public static readonly Dictionary<string, int> Weights = new(StringComparer.OrdinalIgnoreCase)
        {
            ["MultipleFaces"] = 3, ["NoFace"] = 3, ["PhoneDetected"] = 3, ["PaperDetected"] = 3,
            ["TabSwitch"] = 2, ["FullscreenExit"] = 2, ["ConnectionLost"] = 2,
            ["WindowBlur"] = 1, ["CopyPaste"] = 1, ["ContextMenu"] = 1,["DuplicateLogin"] = 3,["ResumedAfterDisconnect"] = 1,
        };

        public static int WeightOf(string? type) =>
            type != null && Weights.TryGetValue(type, out var w) ? w : 1;

        public static int Score(IEnumerable<(string? Type, int Count)> events) =>
            events.Sum(e => WeightOf(e.Type) * e.Count);

        public const int HighFrom = 10;
        public const int MediumFrom = 5;

        public static string Level(int score) =>
            score >= HighFrom ? "High" : score >= MediumFrom ? "Medium" : score > 0 ? "Low" : "None";
    }
}