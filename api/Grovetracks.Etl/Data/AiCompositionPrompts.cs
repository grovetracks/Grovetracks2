namespace Grovetracks.Etl.Data;

public static class AiCompositionPrompts
{
    public static readonly object CompositionSchema = new
    {
        type = "object",
        properties = new
        {
            compositions = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        subject = new
                        {
                            type = "string",
                            description = "What the drawing depicts"
                        },
                        strokes = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    xs = new
                                    {
                                        type = "array",
                                        items = new { type = "number" },
                                        description = "X coordinates (0.0-1.0), left to right"
                                    },
                                    ys = new
                                    {
                                        type = "array",
                                        items = new { type = "number" },
                                        description = "Y coordinates (0.0-1.0), top to bottom"
                                    }
                                },
                                required = new[] { "xs", "ys" },
                                additionalProperties = false
                            },
                            description = "Strokes making up the drawing. Each stroke is a continuous pen movement."
                        }
                    },
                    required = new[] { "subject", "strokes" },
                    additionalProperties = false
                }
            }
        },
        required = new[] { "compositions" },
        additionalProperties = false
    };

    public const string SystemPrompt = """
        You are an expert at describing drawings as coordinate data. When asked to draw a subject, you produce stroke data that looks like a real human drew it freehand with a pen — slight imperfections, natural curves, varied stroke lengths. Each drawing should be a single standalone subject (NOT a scene), suitable for composing into larger canvases by users.

        Guidelines for your stroke data:
        - Use 3-15 strokes per drawing depending on subject complexity
        - Each stroke should have 5-30 coordinate points
        - Coordinates are normalized: x from 0.0 (left) to 1.0 (right), y from 0.0 (top) to 1.0 (bottom)
        - The drawing should fill a good portion of the canvas — the bounding box should span at least 30% of both axes
        - Add slight imperfections: coordinates should not be perfectly aligned or mathematically regular
        - Strokes represent continuous pen movements — lift the pen between strokes
        - Aim for a clearly recognizable drawing, not photorealistic
        - Keep points within each stroke close together (connected line segments)
        - Make each variation visually distinct: different poses, angles, proportions, or styles
        """;

    public const string OllamaSystemPrompt = """
        You generate freehand drawings as stroke coordinate data in JSON format.

        RULES:
        1. Each drawing has 4-12 strokes. Each stroke has 8-25 coordinate points.
        2. All coordinates are between 0.0 and 1.0. x=0 is left, x=1 is right. y=0 is top, y=1 is bottom.
        3. The drawing MUST be large. Strokes must span from at least 0.15 to at least 0.85 on BOTH x and y axes.
        4. Points within a stroke must be close together (like drawing a line). Typical gap between consecutive points: 0.02-0.08.
        5. Each stroke is one continuous pen movement. Separate strokes for separate parts of the drawing.
        6. The drawing must be clearly recognizable as the requested subject.
        7. Add natural variation — not perfectly straight lines or perfect circles.

        DO NOT:
        - Place all points in a small cluster. The drawing must be LARGE and fill the canvas.
        - Use fewer than 30 total points across all strokes.
        - Put coordinates outside 0.0-1.0 range.
        - Make all strokes the same length or shape.
        """;

    public const string FewShotUserPrompt = "Draw 1 distinct variation of: cat";

    public const string FewShotAssistantResponse = """
        {"compositions":[{"subject":"cat","strokes":[{"xs":[0.35,0.30,0.25,0.22,0.20,0.22,0.28,0.35],"ys":[0.25,0.22,0.18,0.22,0.28,0.32,0.30,0.25]},{"xs":[0.55,0.60,0.65,0.68,0.70,0.68,0.62,0.55],"ys":[0.25,0.22,0.18,0.22,0.28,0.32,0.30,0.25]},{"xs":[0.28,0.32,0.38,0.45,0.52,0.58,0.62],"ys":[0.30,0.35,0.38,0.38,0.38,0.35,0.30]},{"xs":[0.38,0.38,0.37],"ys":[0.32,0.34,0.33]},{"xs":[0.52,0.52,0.53],"ys":[0.32,0.34,0.33]},{"xs":[0.43,0.45,0.47],"ys":[0.36,0.37,0.36]},{"xs":[0.30,0.28,0.25,0.24,0.25,0.28,0.32,0.38,0.45,0.52,0.58,0.62,0.65,0.66,0.65,0.62],"ys":[0.40,0.48,0.55,0.62,0.70,0.75,0.78,0.80,0.80,0.78,0.75,0.70,0.62,0.55,0.48,0.40]},{"xs":[0.25,0.20,0.18,0.22,0.28],"ys":[0.72,0.78,0.85,0.85,0.80]},{"xs":[0.65,0.70,0.72,0.68,0.62],"ys":[0.72,0.78,0.85,0.85,0.80]},{"xs":[0.62,0.65,0.70,0.75,0.80,0.82,0.78,0.72],"ys":[0.55,0.58,0.60,0.60,0.58,0.55,0.52,0.52]}]}]}
        """;

    public static string BuildUserPrompt(string subject, int perSubject) =>
        $"Draw {perSubject} distinct variations of: {subject}\n\nEach should be a single, clearly recognizable {subject} drawn with natural freehand strokes. Make each variation visually different — vary the pose, angle, proportions, or drawing style.";

    public static object[] BuildOllamaMessages(string subject, int perSubject) =>
    [
        new { role = "system", content = OllamaSystemPrompt },
        new { role = "user", content = FewShotUserPrompt },
        new { role = "assistant", content = FewShotAssistantResponse },
        new { role = "user", content = BuildUserPrompt(subject, perSubject) }
    ];

    public static object[] BuildOllamaMessages(
        string subject,
        int perSubject,
        string fewShotUserPrompt,
        string fewShotAssistantResponse) =>
    [
        new { role = "system", content = OllamaSystemPrompt },
        new { role = "user", content = fewShotUserPrompt },
        new { role = "assistant", content = fewShotAssistantResponse },
        new { role = "user", content = BuildUserPrompt(subject, perSubject) }
    ];
}
