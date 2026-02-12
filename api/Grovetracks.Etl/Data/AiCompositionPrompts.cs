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

    public static string BuildUserPrompt(string subject, int perSubject) =>
        $"Draw {perSubject} distinct variations of: {subject}\n\nEach should be a single, clearly recognizable {subject} drawn with natural freehand strokes. Make each variation visually different — vary the pose, angle, proportions, or drawing style.";
}
