using System.Numerics;
using Raylib_cs;

namespace TuringFactory;

class TransitionDiagramRenderer(TuringMachine machine) {
    readonly record struct GraphEdge(
        char From,
        char To,
        Transition Transition,
        int ParallelIndex,
        int ParallelCount
    );

    readonly record struct SymbolMarker(
        Vector2 Position,
        Vector2 Direction,
        char Symbol
    );

    readonly List<GraphEdge> edges = BuildEdges(machine);
    public Dictionary<char, Vector2> Positions { get; } = BuildStateLayout([.. machine.States]);
    public Vector2 HaltPosition { get; set; } = new(1100, 120);

    public char? FindNodeAtPosition(Vector2 mousePosition, IReadOnlyCollection<char> states) {
        const float nodeRadius = 40f;

        if (Vector2.Distance(mousePosition, HaltPosition) <= nodeRadius)
            return TuringMachine.HaltState;
        foreach (char state in states) {
            if (Vector2.Distance(mousePosition, Positions[state]) <= nodeRadius)
                return state;
        }
        return null;
    }

    public void Draw(IReadOnlyCollection<char> states, char currentState, char? previousState, Transition? activeTransition, float transitionProgress, Font font) {
        List<SymbolMarker> outlineMarkers = [];
        List<SymbolMarker> filledMarkers = [];

        foreach (GraphEdge edge in edges) {
            Vector2 from = Positions[edge.From];
            Vector2 to = edge.To == TuringMachine.HaltState ? HaltPosition : Positions[edge.To];
            bool isActive = previousState == edge.From &&
                activeTransition == edge.Transition;

            DrawTransitionEdge(edge, from, to, isActive, transitionProgress, font, outlineMarkers, filledMarkers);
        }

        foreach (SymbolMarker marker in outlineMarkers)
            DrawSymbolMarker(marker.Position, marker.Direction, marker.Symbol);

        foreach (SymbolMarker marker in filledMarkers)
            DrawSymbolMarker(marker.Position, marker.Direction, marker.Symbol);

        List<char> stateList = [.. states];
        foreach (char state in states) {
            Vector2 pos = Positions[state];
            Color color = state == currentState ? Color.Gold : GetStateColor(state, stateList);

            Raylib.DrawCircle((int)pos.X, (int)pos.Y, 40, color);
            Raylib.DrawTextEx(font, state.ToString(), new Vector2(pos.X - 12.5f, pos.Y - 18), 40, 1, Color.Black);
        }

        Raylib.DrawCircle((int)HaltPosition.X, (int)HaltPosition.Y, 40, currentState == TuringMachine.HaltState ? Color.Gold : Color.Gray);
        Raylib.DrawTextEx(font, "Halt", new Vector2(HaltPosition.X - 36, HaltPosition.Y - 15), 30, 1, Color.Black);
    }

    static List<GraphEdge> BuildEdges(TuringMachine machine) {
        var transitions = machine.Transitions
            .SelectMany(state => state.Value.Values.Select(transition =>
                new { From = state.Key, To = transition.NextState, Transition = transition }))
            .ToList();
        var grouped = transitions
            .GroupBy(edge => edge.From < edge.To ? (edge.From, edge.To) : (edge.To, edge.From))
            .ToDictionary(group => group.Key, group => group.ToList());
        List<GraphEdge> edges = [];

        foreach (var group in grouped) {
            for (int i = 0; i < group.Value.Count; i++) {
                var edge = group.Value[i];
                edges.Add(new GraphEdge(edge.From, edge.To, edge.Transition, i, group.Value.Count));
            }
        }

        return edges;
    }

    static void DrawTransitionEdge(GraphEdge edge, Vector2 from, Vector2 to, bool isActive, float transitionProgress, Font font, List<SymbolMarker> outlineMarkers, List<SymbolMarker> filledMarkers) {
        const float nodeRadius = 40f;

        if (Vector2.Distance(from, to) < 1f) {
            DrawSelfLoop(edge, from, isActive, transitionProgress, font, outlineMarkers, filledMarkers);
            return;
        }

        Vector2 direction = Vector2.Normalize(to - from);
        Vector2 normal = new(-direction.Y, direction.X);
        float separation = 130f;
        float curveOffset = (edge.ParallelIndex - (edge.ParallelCount - 1) / 2f) * separation;
        if (edge.From > edge.To)
            curveOffset = -curveOffset;
        Vector2 start = from + direction * nodeRadius;
        Vector2 end = to - direction * nodeRadius;
        Vector2 midpoint = (start + end) * 0.5f;
        Vector2 control = midpoint + normal * curveOffset;

        DrawQuadraticCurve(start, control, end, edge.Transition, isActive, transitionProgress, font, outlineMarkers, filledMarkers);
    }

    static void DrawSelfLoop(GraphEdge edge, Vector2 center, bool isActive, float transitionProgress, Font font, List<SymbolMarker> outlineMarkers, List<SymbolMarker> filledMarkers) {
        const float nodeRadius = 40f;
        const float loopRadius = 40f;
        float horizontalOffset = (edge.ParallelIndex - (edge.ParallelCount - 1) / 2f) * 10f;
        Vector2 loopCenter = center + new Vector2(horizontalOffset, -nodeRadius - loopRadius);
        Vector2 start = center + new Vector2(horizontalOffset - nodeRadius * 0.55f, -nodeRadius * 0.65f);
        Vector2 end = center + new Vector2(horizontalOffset + nodeRadius * 0.55f, -nodeRadius * 0.65f);
        const int segments = 32;
        var points = new Vector2[segments + 1];

        for (int i = 0; i <= segments; i++) {
            float angle = MathF.PI + MathF.PI * i / segments;
            points[i] = loopCenter + new Vector2(MathF.Cos(angle) * loopRadius, MathF.Sin(angle) * loopRadius);
        }

        points[0] = start;
        points[^1] = end;
        for (int i = 0; i < points.Length - 1; i++)
            Raylib.DrawLineEx(points[i], points[i + 1], 3f, isActive ? Color.Gold : Color.Black);

        if (isActive)
            Raylib.DrawCircleV(points[(int)(transitionProgress * (points.Length - 1))], 10f, Color.Gold);

        Vector2 startDirection = Vector2.Normalize(points[1] - points[0]);
        Vector2 endDirection = Vector2.Normalize(points[^1] - points[^2]);
        Vector2 startMarker = Vector2.Lerp(points[0], points[1], 0.35f);
        Vector2 endMarker = Vector2.Lerp(points[^1], points[^2], 0.35f);

        AddMarker(new SymbolMarker(startMarker, startDirection, edge.Transition.ReadValue), outlineMarkers, filledMarkers);
        AddMarker(new SymbolMarker(endMarker, endDirection, edge.Transition.WriteValue), outlineMarkers, filledMarkers);

        Vector2 labelPosition = loopCenter + new Vector2(0, -loopRadius);
        string directionLabel = edge.Transition.Direction == MoveDirection.Left ? "L" : "R";

        DrawCenteredLabel(font, directionLabel, labelPosition);
    }

    static void DrawQuadraticCurve(Vector2 start, Vector2 control, Vector2 end, Transition transition, bool isActive, float transitionProgress, Font font, List<SymbolMarker> outlineMarkers, List<SymbolMarker> filledMarkers) {
        const int segments = 48;
        var points = new Vector2[segments + 1];

        for (int i = 0; i <= segments; i++) {
            float t = i / (float)segments;
            points[i] = QuadraticBezier(start, control, end, t);
        }

        for (int i = 0; i < segments; i++)
            Raylib.DrawLineEx(points[i], points[i + 1], 3f, isActive ? Color.Gold : Color.Black);

        if (isActive)
            Raylib.DrawCircleV(QuadraticBezier(start, control, end, transitionProgress), 10f, Color.Gold);

        const float headDistance = 32f;
        (Vector2 startPosition, Vector2 startTangent) = GetCurvePointAtDistance(points, headDistance, false);
        (Vector2 endPosition, Vector2 endTangent) = GetCurvePointAtDistance(points, headDistance, true);

        AddMarker(new SymbolMarker(startPosition, startTangent, transition.ReadValue), outlineMarkers, filledMarkers);
        AddMarker(new SymbolMarker(endPosition, endTangent, transition.WriteValue), outlineMarkers, filledMarkers);

        Vector2 labelPoint = QuadraticBezier(start, control, end, 0.5f);
        string directionLabel = transition.Direction == MoveDirection.Left ? "L" : "R";

        DrawCenteredLabel(font, directionLabel, labelPoint);
    }

    static (Vector2 Position, Vector2 Direction) GetCurvePointAtDistance(Vector2[] points, float distance, bool fromEnd) {
        if (fromEnd) {
            for (int i = points.Length - 1; i > 0; i--) {
                Vector2 segment = points[i - 1] - points[i];
                float segmentLength = segment.Length();
                if (distance <= segmentLength)
                    return (points[i] + Vector2.Normalize(segment) * distance, Vector2.Normalize(-segment));
                distance -= segmentLength;
            }
        } else {
            for (int i = 0; i < points.Length - 1; i++) {
                Vector2 segment = points[i + 1] - points[i];
                float segmentLength = segment.Length();
                if (distance <= segmentLength)
                    return (points[i] + Vector2.Normalize(segment) * distance, Vector2.Normalize(segment));
                distance -= segmentLength;
            }
        }
        Vector2 fallbackDirection = fromEnd ? Vector2.Normalize(points[^1] - points[^2]) : Vector2.Normalize(points[1] - points[0]);

        return (fromEnd ? points[^1] : points[0], fallbackDirection);
    }

    static void AddMarker(SymbolMarker marker, List<SymbolMarker> outlineMarkers, List<SymbolMarker> filledMarkers) {
        if (marker.Symbol == '1')
            filledMarkers.Add(marker);
        else
            outlineMarkers.Add(marker);
    }

    static Vector2 QuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t) {
        float inverse = 1f - t;
        return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
    }

    static void DrawSymbolMarker(Vector2 position, Vector2 direction, char symbol) {
        const float length = 18f;
        const float width = 10f;

        direction = Vector2.Normalize(direction);
        Vector2 normal = new(-direction.Y, direction.X);
        Vector2 tip = position + direction * length;
        Vector2 baseCenter = position - direction * length * 0.35f;
        Vector2 left = baseCenter + normal * width;
        Vector2 right = baseCenter - normal * width;

        if (symbol == '1') {
            DrawFilledTriangle(tip, left, right, Color.Black);
            return;
        } else {
            DrawFilledTriangle(tip, left, right, Color.RayWhite);
            Raylib.DrawTriangleLines(tip, left, right, Color.Black);
        }
    }

    static void DrawFilledTriangle(Vector2 tip, Vector2 left, Vector2 right, Color color) {
        float winding = (left.X - tip.X) * (right.Y - tip.Y) - (left.Y - tip.Y) * (right.X - tip.X);
        if (winding < 0f)
            Raylib.DrawTriangle(tip, left, right, color);
        else
            Raylib.DrawTriangle(tip, right, left, color);
    }

    static void DrawCenteredLabel(Font font, string text, Vector2 center) {
        const float fontSize = 28f;
        const float padding = 8f;
        Vector2 size = Raylib.MeasureTextEx(font, text, fontSize, 1f);
        Rectangle background = new(center.X - size.X / 2f - padding, center.Y - size.Y / 2f - padding, size.X + padding * 2f, size.Y + padding * 2f);

        Raylib.DrawRectangleRounded(background, 0.25f, 8, Color.RayWhite);
        Raylib.DrawRectangleRoundedLines(background, 0.25f, 8, Color.LightGray);
        Raylib.DrawTextEx(font, text, new Vector2(center.X - size.X / 2f, center.Y - size.Y / 2f), fontSize, 1f, Color.DarkGray);
    }

    static Dictionary<char, Vector2> BuildStateLayout(List<char> states) {
        Vector2 center = new(640, 360);
        float radius = 220f;
        Dictionary<char, Vector2> result = [];

        for (int i = 0; i < states.Count; i++) {
            float angle = (MathF.PI * 2f * i / states.Count) - (MathF.PI / 2f);
            float x = center.X + MathF.Cos(angle) * radius;
            float y = center.Y + MathF.Sin(angle) * radius;

            result[states[i]] = new Vector2(x, y);
        }

        return result;
    }

    static Color GetStateColor(char state, List<char> states) {
        int index = states.IndexOf(state);
        float hue = index * 360f / Math.Max(states.Count, 1);
        return Raylib.ColorFromHSV(hue, 0.65f, 0.95f);
    }
}
