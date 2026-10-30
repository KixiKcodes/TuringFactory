using System.Numerics;
using Raylib_cs;

namespace BusyBeaver;

class Program {
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

    public static void Main(string[] args) {
        string definition = args.Length > 0 ? string.Join(" ", args) : "1RB1RD_1LC0RC_1RA1LD_0RE0LB_0RZ1RC"; // Default.
        TuringMachine machine = TuringMachine.Parse(definition);
        List<GraphEdge> edges = BuildEdges(machine);
        Dictionary<char, Vector2> positions = BuildStateLayout([.. machine.States]);

        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1280, 720, "Busy Beaver");
        Raylib.SetTargetFPS(60);

        Vector2 haltPosition = new(1100, 120);
        Font font = Raylib.LoadFontEx("resources/Sono-Regular.ttf", 72, null, 0);
        char? draggedNode = null;
        Vector2 dragOffset = Vector2.Zero;
        const float nodeRadius = 40f;
        Dictionary<int, char> tape = [];
        int tapeHead = 0;
        char currentState = 'A';
        Transition? activeTransition = null;
        char? previousState = null;
        bool isPlaying = false;
        float stepTimer = 0f;
        const float stepInterval = 0.2f;
        float transitionProgress = 1f;
        const float transitionDuration = 0.35f;

        while (!Raylib.WindowShouldClose()) {
            Vector2 mousePosition = Raylib.GetMousePosition();
            Rectangle playButton = new(20, 630, 110, 45);

            if (Raylib.IsMouseButtonPressed(MouseButton.Left)) {
                draggedNode = FindNodeAtPosition(mousePosition, machine.States, positions, haltPosition, nodeRadius);
                if (draggedNode.HasValue) {
                    Vector2 nodePosition = draggedNode.Value == TuringMachine.HaltState ? haltPosition : positions[draggedNode.Value];
                    dragOffset = nodePosition - mousePosition;
                }
            }

            if (draggedNode.HasValue && Raylib.IsMouseButtonDown(MouseButton.Left)) {
                Vector2 newPosition = mousePosition + dragOffset;

                if (draggedNode.Value == TuringMachine.HaltState)
                    haltPosition = newPosition;
                else
                    positions[draggedNode.Value] = newPosition;
            }

            if (Raylib.IsMouseButtonReleased(MouseButton.Left))
                draggedNode = null;

            if (Raylib.IsMouseButtonPressed(MouseButton.Left) &&
                Raylib.CheckCollisionPointRec(mousePosition, playButton)) {
                if (currentState == TuringMachine.HaltState) {
                    tape.Clear();
                    tapeHead = 0;
                    currentState = machine.StartState;
                    activeTransition = null;
                    previousState = null;
                }
                isPlaying = !isPlaying;
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Space))
                isPlaying = !isPlaying;

            if (Raylib.IsKeyPressed(KeyboardKey.R)) {
                tape.Clear();
                tapeHead = 0;
                currentState = machine.StartState;
                activeTransition = null;
                previousState = null;
                isPlaying = false;
            }

            if (isPlaying && currentState != TuringMachine.HaltState) {
                stepTimer += Raylib.GetFrameTime();

                if (stepTimer >= stepInterval) {
                    stepTimer = 0f;
                    StepMachine(machine, tape, ref tapeHead, ref currentState, ref activeTransition, ref previousState);
                    transitionProgress = 0f;
                }
            }

            if (transitionProgress < 1f)
                transitionProgress = MathF.Min(transitionProgress + Raylib.GetFrameTime() / transitionDuration, 1f);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RayWhite);
            List<SymbolMarker> outlineMarkers = [];
            List<SymbolMarker> filledMarkers = [];

            Raylib.DrawRectangleRec(playButton, Color.LightGray);
            Raylib.DrawRectangleLinesEx(playButton, 2f, Color.DarkGray);

            string buttonText = isPlaying ? "Pause" : "Play";
            Vector2 buttonSize = Raylib.MeasureTextEx(font, buttonText, 24, 1);
            Raylib.DrawTextEx(font, buttonText, new Vector2(
                playButton.X + playButton.Width / 2f - buttonSize.X / 2f,
                playButton.Y + playButton.Height / 2f - buttonSize.Y / 2f
            ), 24, 1, Color.Black);

            foreach (GraphEdge edge in edges) {
                Vector2 from = positions[edge.From];
                Vector2 to = edge.To == TuringMachine.HaltState ? haltPosition : positions[edge.To];
                bool isActive = previousState == edge.From &&
                    activeTransition == edge.Transition;

                DrawTransitionEdge(from, to, edge.From, edge.To, edge.Transition, edge.ParallelIndex, edge.ParallelCount, isActive, transitionProgress, font, outlineMarkers, filledMarkers);
            }

            foreach (SymbolMarker marker in outlineMarkers)
                DrawSymbolMarker(marker.Position, marker.Direction, marker.Symbol);

            foreach (SymbolMarker marker in filledMarkers)
                DrawSymbolMarker(marker.Position, marker.Direction, marker.Symbol);

            foreach (char state in machine.States) {
                Vector2 pos = positions[state];
                List<char> states = [.. machine.States];
                Color color = state == currentState ? Color.Gold : GetStateColor(state, states);

                Raylib.DrawCircle((int)pos.X, (int)pos.Y, 40, color);
                Raylib.DrawTextEx(font, state.ToString(), new Vector2(pos.X - 12.5f, pos.Y - 18), 40, 1, Color.Black);
            }

            Raylib.DrawCircle((int)haltPosition.X, (int)haltPosition.Y, 40, currentState == TuringMachine.HaltState ? Color.Gold : Color.Gray);
            Raylib.DrawTextEx(font, "Halt", new Vector2(haltPosition.X - 36, haltPosition.Y - 15), 30, 1, Color.Black);

            DrawTape(tape, tapeHead, font, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
            Raylib.EndDrawing();
        }
        Raylib.UnloadFont(font);
        Raylib.CloseWindow();
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

    static void DrawTransitionEdge(Vector2 from, Vector2 to, char fromState, char toState, Transition transition, int parallelIndex, int parallelCount, bool isActive, float transitionProgress, Font font, List<SymbolMarker> outlineMarkers, List<SymbolMarker> filledMarkers) {
        const float nodeRadius = 40f;

        if (Vector2.Distance(from, to) < 1f) {
            DrawSelfLoop(from, transition, parallelIndex, parallelCount, isActive, transitionProgress, font, outlineMarkers, filledMarkers);
            return;
        }

        Vector2 direction = Vector2.Normalize(to - from);
        Vector2 normal = new(-direction.Y, direction.X);
        float separation = 130f;
        float curveOffset = (parallelIndex - (parallelCount - 1) / 2f) * separation;
        if (fromState > toState)
            curveOffset = -curveOffset;
        Vector2 start = from + direction * nodeRadius;
        Vector2 end = to - direction * nodeRadius;
        Vector2 midpoint = (start + end) * 0.5f;
        Vector2 control = midpoint + normal * curveOffset;

        DrawQuadraticCurve(start, control, end, transition, isActive, transitionProgress, font, outlineMarkers, filledMarkers);
    }

    static void DrawSelfLoop(Vector2 center, Transition transition, int parallelIndex, int parallelCount, bool isActive, float transitionProgress, Font font, List<SymbolMarker> outlineMarkers, List<SymbolMarker> filledMarkers) {
        const float nodeRadius = 40f;
        const float loopRadius = 40f;
        float horizontalOffset = (parallelIndex - (parallelCount - 1) / 2f) * 10f;
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
            Raylib.DrawLineEx(points[i], points[i + 1], 3f, isActive ? Color.Red : Color.Black);

        if (isActive)
            Raylib.DrawCircleV(points[(int)(transitionProgress * (points.Length - 1))], 10f, Color.Red);

        Vector2 startDirection = Vector2.Normalize(points[1] - points[0]);
        Vector2 endDirection = Vector2.Normalize(points[^1] - points[^2]);
        Vector2 startMarker = Vector2.Lerp(points[0], points[1], 0.35f);
        Vector2 endMarker = Vector2.Lerp(points[^1], points[^2], 0.35f);

        AddMarker(new SymbolMarker(startMarker, startDirection, transition.ReadValue), outlineMarkers, filledMarkers);
        AddMarker(new SymbolMarker(endMarker, endDirection, transition.WriteValue), outlineMarkers, filledMarkers);

        Vector2 labelPosition = loopCenter + new Vector2(0, -loopRadius);
        string directionLabel = transition.Direction == MoveDirection.Left ? "L" : "R";

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
            Raylib.DrawLineEx(points[i], points[i + 1], 3f, isActive ? Color.Red : Color.Black);

        if (isActive)
            Raylib.DrawCircleV(QuadraticBezier(start, control, end, transitionProgress), 10f, Color.Red);

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

    static char? FindNodeAtPosition(Vector2 mousePosition, IReadOnlyCollection<char> states, Dictionary<char, Vector2> positions, Vector2 haltPosition, float nodeRadius) {
        if (Vector2.Distance(mousePosition, haltPosition) <= nodeRadius)
            return TuringMachine.HaltState;
        foreach (char state in states) {
            if (Vector2.Distance(mousePosition, positions[state]) <= nodeRadius)
                return state;
        }
        return null;
    }

    static Color GetStateColor(char state, List<char> states) {
        int index = states.IndexOf(state);
        float hue = index * 360f / Math.Max(states.Count, 1);
        return Raylib.ColorFromHSV(hue, 0.65f, 0.95f);
    }

    static void StepMachine(TuringMachine machine, Dictionary<int, char> tape, ref int tapeHead, ref char currentState, ref Transition? activeTransition, ref char? previousState) {
        if (currentState == TuringMachine.HaltState)
            return;
        char readValue = tape.TryGetValue(tapeHead, out char value) ? value : '0';
        if (!machine.TryGetTransition(currentState, readValue, out Transition? transition) || transition is null)
            return;

        previousState = currentState;
        activeTransition = transition;
        if (transition.WriteValue == '1')
            tape[tapeHead] = '1';
        else
            tape.Remove(tapeHead);
        tapeHead += transition.Direction == MoveDirection.Right ? 1 : -1;
        currentState = transition.NextState;
    }

    static void DrawTape(Dictionary<int, char> tape, int tapeHead, Font font, int windowWidth, int windowHeight) {
        const int cellSize = 55;
        const int visibleCells = 15;
        float startX = (windowWidth - visibleCells * cellSize) / 2f;
        float y = windowHeight - 100;
        int firstCell = tapeHead - visibleCells / 2;

        for (int i = 0; i < visibleCells; i++) {
            int tapePosition = firstCell + i;
            float x = startX + i * cellSize;
            char value = tape.TryGetValue(tapePosition, out char storedValue) ? storedValue : '0';

            Raylib.DrawRectangle((int)x, (int)y, cellSize, cellSize, Color.RayWhite);
            Raylib.DrawRectangleLinesEx(new Rectangle(x, y, cellSize, cellSize), tapePosition == tapeHead ? 5f : 2f, tapePosition == tapeHead ? Color.Red : Color.DarkGray);
            Vector2 textSize = Raylib.MeasureTextEx(font, value.ToString(), 28, 1);
            Raylib.DrawTextEx(font, value.ToString(), new Vector2(x + cellSize / 2f - textSize.X / 2f, y + cellSize / 2f - textSize.Y / 2f), 28, 1, Color.Black);
        }
    }
}
