using System.Numerics;
using Raylib_cs;

namespace TuringFactory.src;

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
        char Symbol,
        bool IsActive,
        bool IsSelected
    );

    readonly record struct TransitionLabel(
        Vector2 Position,
        string Text,
        bool IsActive,
        bool IsSelected
    );

    public enum TransitionControl {
        Read,
        Write,
        Direction
    }

    public readonly record struct TransitionHit(char State, char ReadSymbol, TransitionControl Control);

    List<GraphEdge> edges = BuildEdges(machine);
    readonly Dictionary<(char State, char ReadSymbol), float> selfLoopAngles = [];
    public Dictionary<char, Vector2> Positions { get; } = BuildStateLayout([.. machine.States]);
    public Vector2 HaltPosition { get; set; } = new(1100, 120);

    public void Refresh() {
        edges = BuildEdges(machine);
        foreach (char state in machine.States)
            if (!Positions.ContainsKey(state))
                Positions[state] = FindNewStatePosition();
        foreach (char state in Positions.Keys.ToList())
            if (!machine.States.Contains(state))
                Positions.Remove(state);
    }

    public void SetStatePosition(char state, Vector2 position) {
        Positions[state] = position;
    }

    public void RemapStatePositions(IReadOnlyDictionary<char, char> renames) {
        foreach (KeyValuePair<char, char> rename in renames) {
            if (Positions.TryGetValue(rename.Key, out Vector2 position)) {
                Positions.Remove(rename.Key);
                Positions[rename.Value] = position;
            }
        }
    }

    public void SwapSelfLoopAngles(char state) {
        bool hasZeroAngle = selfLoopAngles.TryGetValue((state, '0'), out float zeroAngle);
        bool hasOneAngle = selfLoopAngles.TryGetValue((state, '1'), out float oneAngle);
        if (hasZeroAngle)
            selfLoopAngles[(state, '1')] = zeroAngle;
        else
            selfLoopAngles.Remove((state, '1'));
        if (hasOneAngle)
            selfLoopAngles[(state, '0')] = oneAngle;
        else
            selfLoopAngles.Remove((state, '0'));
    }

    public TransitionHit? FindTransitionControlAtPosition(Vector2 mousePosition) {
        const float markerHitDistance = 12f;
        const float labelHitDistance = 25f;
        foreach (GraphEdge edge in edges) {
            Vector2 from = Positions[edge.From];
            Vector2 to = edge.To == TuringMachine.HaltState || !Positions.TryGetValue(edge.To, out Vector2 targetPosition) ? HaltPosition : targetPosition;
            if (Vector2.Distance(from, to) < 1f) {
                const float loopRadius = 40f;
                GetSelfLoopGeometry(edge, from, out Vector2 loopCenter, out Vector2 loopStart, out Vector2 loopEnd, out Vector2 radial, out _);
                Vector2[] loopPoints = GetSelfLoopPoints(loopCenter, loopStart, loopEnd, radial, loopRadius);
                Vector2 firstMarker = Vector2.Lerp(loopPoints[0], loopPoints[1], 0.35f);
                Vector2 lastMarker = Vector2.Lerp(loopPoints[^1], loopPoints[^2], 0.35f);
                if (Vector2.Distance(mousePosition, firstMarker) <= markerHitDistance)
                    return new(edge.From, edge.Transition.ReadValue, TransitionControl.Read);
                if (Vector2.Distance(mousePosition, lastMarker) <= markerHitDistance)
                    return new(edge.From, edge.Transition.ReadValue, TransitionControl.Write);
                Vector2 selfLoopLabelPosition = loopCenter + radial * loopRadius;
                if (Vector2.Distance(mousePosition, selfLoopLabelPosition) <= labelHitDistance)
                    return new(edge.From, edge.Transition.ReadValue, TransitionControl.Direction);
                continue;
            }

            Vector2 direction = Vector2.Normalize(to - from);
            Vector2 normal = new(-direction.Y, direction.X);
            float curveOffset = (edge.ParallelIndex - (edge.ParallelCount - 1) / 2f) * 130f;
            if (edge.From > edge.To)
                curveOffset = -curveOffset;
            Vector2 start = from + direction * 40f;
            Vector2 end = to - direction * 40f;
            Vector2 control = (start + end) * 0.5f + normal * curveOffset;
            Vector2[] points = [.. Enumerable.Range(0, 49).Select(index => QuadraticBezier(start, control, end, index / 48f))];
            (Vector2 readPosition, Vector2 readDirection) = GetCurvePointAtDistance(points, 32f, false);
            (Vector2 writePosition, Vector2 writeDirection) = GetCurvePointAtDistance(points, 32f, true);
            Vector2 labelPosition = QuadraticBezier(start, control, end, 0.5f);

            if (Vector2.Distance(mousePosition, readPosition) <= markerHitDistance)
                return new(edge.From, edge.Transition.ReadValue, TransitionControl.Read);
            if (Vector2.Distance(mousePosition, writePosition) <= markerHitDistance)
                return new(edge.From, edge.Transition.ReadValue, TransitionControl.Write);
            if (Vector2.Distance(mousePosition, labelPosition) <= labelHitDistance)
                return new(edge.From, edge.Transition.ReadValue, TransitionControl.Direction);
        }
        return null;
    }

    public (char State, char ReadSymbol)? FindTransitionAtPosition(Vector2 mousePosition) {
        const float hitDistance = 18f;
        foreach (GraphEdge edge in edges) {
            Vector2 from = Positions[edge.From];
            Vector2 to = edge.To == TuringMachine.HaltState || !Positions.TryGetValue(edge.To, out Vector2 targetPosition) ? HaltPosition : targetPosition;
            if (Vector2.Distance(from, to) < 1f) {
                GetSelfLoopGeometry(edge, from, out Vector2 loopCenter, out Vector2 loopStart, out Vector2 loopEnd, out Vector2 radial, out _);
                Vector2[] points = GetSelfLoopPoints(loopCenter, loopStart, loopEnd, radial, 40f);
                for (int i = 1; i < points.Length; i++)
                    if (DistanceToSegment(mousePosition, points[i - 1], points[i]) <= hitDistance)
                        return (edge.From, edge.Transition.ReadValue);
                continue;
            }

            Vector2 direction = Vector2.Normalize(to - from);
            Vector2 normal = new(-direction.Y, direction.X);
            float separation = 130f;
            float curveOffset = (edge.ParallelIndex - (edge.ParallelCount - 1) / 2f) * separation;
            if (edge.From > edge.To)
                curveOffset = -curveOffset;
            Vector2 start = from + direction * 40f;
            Vector2 end = to - direction * 40f;
            Vector2 control = (start + end) * 0.5f + normal * curveOffset;
            Vector2 previous = start;
            for (int i = 1; i <= 24; i++) {
                Vector2 current = QuadraticBezier(start, control, end, i / 24f);
                if (DistanceToSegment(mousePosition, previous, current) <= hitDistance)
                    return (edge.From, edge.Transition.ReadValue);
                previous = current;
            }
        }
        return null;
    }

    public (char State, char ReadSymbol)? FindSelfLoopAtPosition(Vector2 mousePosition) {
        const float hitDistance = 10f;
        const float markerHitDistance = 12f;
        const float labelHitDistance = 25f;
        foreach (GraphEdge edge in edges) {
            Vector2 center = Positions[edge.From];
            Vector2 to = edge.To == TuringMachine.HaltState || !Positions.TryGetValue(edge.To, out Vector2 targetPosition) ? HaltPosition : targetPosition;
            if (Vector2.Distance(center, to) >= 1f)
                continue;

            GetSelfLoopGeometry(edge, center, out Vector2 loopCenter, out Vector2 loopStart, out Vector2 loopEnd, out Vector2 radial, out _);
            Vector2[] points = GetSelfLoopPoints(loopCenter, loopStart, loopEnd, radial, 40f);
            Vector2 firstMarker = Vector2.Lerp(points[0], points[1], 0.35f);
            Vector2 lastMarker = Vector2.Lerp(points[^1], points[^2], 0.35f);
            Vector2 labelPosition = loopCenter + radial * 40f;
            if (Vector2.Distance(mousePosition, firstMarker) <= markerHitDistance ||
                Vector2.Distance(mousePosition, lastMarker) <= markerHitDistance ||
                Vector2.Distance(mousePosition, labelPosition) <= labelHitDistance)
                continue;
            for (int i = 1; i < points.Length; i++)
                if (DistanceToSegment(mousePosition, points[i - 1], points[i]) <= hitDistance)
                    return (edge.From, edge.Transition.ReadValue);
        }
        return null;
    }

    public void RotateSelfLoop((char State, char ReadSymbol) loop, Vector2 mousePosition) {
        if (!Positions.TryGetValue(loop.State, out Vector2 center))
            return;
        Vector2 direction = mousePosition - center;
        if (direction.LengthSquared() > 0f)
            selfLoopAngles[loop] = MathF.Atan2(direction.Y, direction.X);
    }

    void GetSelfLoopGeometry(GraphEdge edge, Vector2 center, out Vector2 loopCenter, out Vector2 start, out Vector2 end, out Vector2 radial, out float angle) {
        bool bottom = edge.ParallelCount == 2 && edge.ParallelIndex == 1;
        angle = selfLoopAngles.GetValueOrDefault((edge.From, edge.Transition.ReadValue), bottom ? MathF.PI / 2f : -MathF.PI / 2f);
        radial = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 tangent = new(-radial.Y, radial.X);
        loopCenter = center + radial * 80f;
        start = center - tangent * 22f + radial * 26f;
        end = center + tangent * 22f + radial * 26f;
    }

    static Vector2[] GetSelfLoopPoints(Vector2 loopCenter, Vector2 start, Vector2 end, Vector2 radial, float loopRadius) {
        const int segments = 32;
        Vector2 tangent = Vector2.Normalize(end - start);
        Vector2[] points = new Vector2[segments + 1];
        for (int i = 0; i <= segments; i++) {
            float theta = MathF.PI - MathF.PI * i / segments;
            points[i] = loopCenter + tangent * (MathF.Cos(theta) * loopRadius) + radial * (MathF.Sin(theta) * loopRadius);
        }
        points[0] = start;
        points[^1] = end;
        return points;
    }

    Vector2 FindNewStatePosition() {
        int index = Positions.Count;
        return new(300 + index % 4 * 220, 180 + index / 4 * 180);
    }

    static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end) {
        Vector2 segment = end - start;
        float lengthSquared = segment.LengthSquared();
        if (lengthSquared <= 0f)
            return Vector2.Distance(point, start);
        float projection = Math.Clamp(Vector2.Dot(point - start, segment) / lengthSquared, 0f, 1f);
        return Vector2.Distance(point, start + segment * projection);
    }

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

    public void Draw(IReadOnlyCollection<char> states, char currentState, int stepCount, char? previousState, Transition? activeTransition, float transitionProgress,
                     char? selectedState = null, (char State, char ReadSymbol)? selectedTransition = null) {
        List<SymbolMarker> outlineMarkers = [];
        List<SymbolMarker> filledMarkers = [];
        List<TransitionLabel> labels = [];

        foreach (GraphEdge edge in edges) {
            Vector2 from = Positions[edge.From];
            Vector2 to = edge.To == TuringMachine.HaltState || !Positions.TryGetValue(edge.To, out Vector2 targetPosition) ? HaltPosition : targetPosition;
            bool isActive = previousState == edge.From &&
                activeTransition == edge.Transition;
            bool isSelected = selectedTransition is (char state, char readSymbol) &&
                state == edge.From && readSymbol == edge.Transition.ReadValue;

            DrawTransitionEdge(edge, from, to, isActive, isSelected, transitionProgress, outlineMarkers, filledMarkers, labels);
        }

        foreach (SymbolMarker marker in outlineMarkers)
            DrawSymbolMarker(marker.Position, marker.Direction, marker.Symbol, marker.IsActive, marker.IsSelected);

        foreach (SymbolMarker marker in filledMarkers)
            DrawSymbolMarker(marker.Position, marker.Direction, marker.Symbol, marker.IsActive, marker.IsSelected);

        foreach (TransitionLabel label in labels)
            DrawCenteredLabel(label.Text, label.Position, label.IsActive, label.IsSelected);

        List<char> stateList = [.. states];
        char? highlightedState = transitionProgress < 1f && previousState.HasValue ? previousState : currentState;
        foreach (char state in states) {
            const float circleRadius = 40;
            Vector2 pos = Positions[state];
            Color color = stepCount > 0 && highlightedState == state ? Theme.Active : GetStateColor(state, stateList);

            Raylib.DrawCircle((int)pos.X, (int)pos.Y, circleRadius, color);
            if (selectedState == state)
                Raylib.DrawCircleLines((int)pos.X, (int)pos.Y, circleRadius + 6, Theme.Selected);

            float textSizeValue = MathF.Max(1f, circleRadius * 2 * 0.66f);
            Vector2 textSize = Raylib.MeasureTextEx(Theme.Font, state.ToString(), textSizeValue, 1);
            Vector2 textPosition = new(pos.X - textSize.X / 2f - 1f, pos.Y - textSize.Y / 2f + 1f);
            Raylib.DrawTextEx(Theme.Font, state.ToString(), textPosition, textSizeValue, 1, Theme.ElementDark);
        }

        bool haltIsHighlighted = stepCount > 0 && highlightedState == TuringMachine.HaltState;
        Raylib.DrawCircle((int)HaltPosition.X, (int)HaltPosition.Y, 40, haltIsHighlighted ? Theme.Active : Theme.Innactive);
        if (selectedState == TuringMachine.HaltState)
            Raylib.DrawCircleLines((int)HaltPosition.X, (int)HaltPosition.Y, 46, Theme.Selected);
        Raylib.DrawTextEx(Theme.Font, "Halt", new Vector2(HaltPosition.X - 32, HaltPosition.Y - 19), 40, 1, haltIsHighlighted ? Theme.ElementDark : Theme.ElementLight);
    }

    public void DrawLinkPreview(char source, Vector2 mousePosition) {
        if (!Positions.TryGetValue(source, out Vector2 start))
            return;
        Vector2 direction = Vector2.Normalize(mousePosition - start);
        if (float.IsNaN(direction.X))
            return;
        Raylib.DrawLineEx(start + direction * 40f, mousePosition, 3f, Theme.Selected);
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

    void DrawTransitionEdge(GraphEdge edge, Vector2 from, Vector2 to, bool isActive, bool isSelected, float transitionProgress, List<SymbolMarker> outlineMarkers, List<SymbolMarker> filledMarkers, List<TransitionLabel> labels) {
        const float nodeRadius = 40f;

        if (Vector2.Distance(from, to) < 1f) {
            DrawSelfLoop(edge, from, isActive, isSelected, transitionProgress, outlineMarkers, filledMarkers, labels);
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

        DrawQuadraticCurve(start, control, end, edge.Transition, isActive, isSelected, transitionProgress, outlineMarkers, filledMarkers, labels);
    }

    void DrawSelfLoop(GraphEdge edge, Vector2 center, bool isActive, bool isSelected, float transitionProgress, List<SymbolMarker> outlineMarkers, List<SymbolMarker> filledMarkers, List<TransitionLabel> labels) {
        const float loopRadius = 40f;
        GetSelfLoopGeometry(edge, center, out Vector2 loopCenter, out Vector2 start, out Vector2 end, out Vector2 radial, out _);
        Vector2[] points = GetSelfLoopPoints(loopCenter, start, end, radial, loopRadius);
        for (int i = 0; i < points.Length - 1; i++)
            Raylib.DrawLineEx(points[i], points[i + 1], 3f, isActive ? Theme.Active : isSelected ? Theme.Selected : Theme.Innactive);

        if (isActive && transitionProgress < 1f)
            Raylib.DrawCircleV(GetPointAtPathProgress(points, transitionProgress), 10f, Theme.Active);

        Vector2 startDirection = Vector2.Normalize(points[1] - points[0]);
        Vector2 endDirection = Vector2.Normalize(points[^1] - points[^2]);
        Vector2 startMarker = Vector2.Lerp(points[0], points[1], 0.35f);
        Vector2 endMarker = Vector2.Lerp(points[^1], points[^2], 0.35f);

        AddMarker(new SymbolMarker(startMarker, startDirection, edge.Transition.ReadValue, isActive, isSelected), outlineMarkers, filledMarkers);
        AddMarker(new SymbolMarker(endMarker, endDirection, edge.Transition.WriteValue, isActive, isSelected), outlineMarkers, filledMarkers);

        Vector2 labelPosition = loopCenter + radial * loopRadius;
        string directionLabel = edge.Transition.Direction == MoveDirection.Left ? "L" : "R";

        labels.Add(new TransitionLabel(labelPosition, directionLabel, isActive, isSelected));
    }

    static Vector2 GetPointAtPathProgress(Vector2[] points, float progress) {
        float totalLength = 0f;
        for (int i = 1; i < points.Length; i++)
            totalLength += Vector2.Distance(points[i - 1], points[i]);

        float targetLength = totalLength * Math.Clamp(progress, 0f, 1f);
        for (int i = 1; i < points.Length; i++) {
            Vector2 start = points[i - 1];
            Vector2 end = points[i];
            float segmentLength = Vector2.Distance(start, end);
            if (targetLength <= segmentLength)
                return Vector2.Lerp(start, end, segmentLength > 0f ? targetLength / segmentLength : 0f);
            targetLength -= segmentLength;
        }

        return points[^1];
    }

    static void DrawQuadraticCurve(Vector2 start, Vector2 control, Vector2 end, Transition transition, bool isActive, bool isSelected, float transitionProgress,
                                   List<SymbolMarker> outlineMarkers, List<SymbolMarker> filledMarkers, List<TransitionLabel> labels) {
        const int segments = 48;
        Vector2[] points = new Vector2[segments + 1];

        for (int i = 0; i <= segments; i++) {
            float t = i / (float)segments;
            points[i] = QuadraticBezier(start, control, end, t);
        }

        for (int i = 0; i < segments; i++)
            Raylib.DrawLineEx(points[i], points[i + 1], 3f, isActive ? Theme.Active : isSelected ? Theme.Selected : Theme.Innactive);

        if (isActive && transitionProgress < 1f)
            Raylib.DrawCircleV(QuadraticBezier(start, control, end, transitionProgress), 10f, Theme.Active);

        const float headDistance = 32f;
        (Vector2 startPosition, Vector2 startTangent) = GetCurvePointAtDistance(points, headDistance, false);
        (Vector2 endPosition, Vector2 endTangent) = GetCurvePointAtDistance(points, headDistance, true);

        AddMarker(new SymbolMarker(startPosition, startTangent, transition.ReadValue, isActive, isSelected), outlineMarkers, filledMarkers);
        AddMarker(new SymbolMarker(endPosition, endTangent, transition.WriteValue, isActive, isSelected), outlineMarkers, filledMarkers);

        Vector2 labelPoint = QuadraticBezier(start, control, end, 0.5f);
        string directionLabel = transition.Direction == MoveDirection.Left ? "L" : "R";

        labels.Add(new TransitionLabel(labelPoint, directionLabel, isActive, isSelected));
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

    static void DrawSymbolMarker(Vector2 position, Vector2 direction, char symbol, bool isActive, bool isSelected) {
        const float length = 18f;
        const float width = 10f;

        direction = Vector2.Normalize(direction);
        Vector2 normal = new(-direction.Y, direction.X);
        Vector2 tip = position + direction * length;
        Vector2 baseCenter = position - direction * length * 0.35f;
        Vector2 left = baseCenter + normal * width;
        Vector2 right = baseCenter - normal * width;

        Color highlightColor = isActive ? Theme.Active : isSelected ? Theme.Selected : Theme.Innactive;
        if (symbol == '1') {
            DrawFilledTriangle(tip, left, right, highlightColor);
            return;
        } else {
            DrawFilledTriangle(tip, left, right, Theme.ElementDark);
            Raylib.DrawTriangleLines(tip, left, right, highlightColor);
        }
    }

    static void DrawFilledTriangle(Vector2 tip, Vector2 left, Vector2 right, Color color) {
        float winding = (left.X - tip.X) * (right.Y - tip.Y) - (left.Y - tip.Y) * (right.X - tip.X);
        if (winding < 0f)
            Raylib.DrawTriangle(tip, left, right, color);
        else
            Raylib.DrawTriangle(tip, right, left, color);
    }

    static void DrawCenteredLabel(string text, Vector2 center, bool isActive, bool isSelected) {
        const float fontSize = 28f;
        const float padding = 8f;
        const float labelWidth = 30f;
        Vector2 size = Raylib.MeasureTextEx(Theme.Font, text, fontSize, 1f);
        Rectangle background = new(center.X - labelWidth / 2f, center.Y - size.Y / 2f - padding, labelWidth, size.Y + padding * 2f);

        Color panelColor = isActive ? Theme.Active : isSelected ? Theme.Selected : Theme.Panel;
        Color textColor = isActive || isSelected ? Theme.ElementDark : Theme.ElementLight;
        Raylib.DrawRectangleRounded(background, 0.25f, 8, panelColor);
        Raylib.DrawTextEx(Theme.Font, text, new Vector2(center.X - size.X / 2f, center.Y - size.Y / 2f), fontSize, 1f, textColor);
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
        return Theme.StateColors[index % Theme.StateColors.Length];
    }
}
