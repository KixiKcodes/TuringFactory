using Raylib_cs;

namespace TuringFactory.src;

static class TapeHistoryRenderer {
    public readonly record struct TapeHistorySnapshot(
        Dictionary<int, char> Tape,
        Dictionary<int, char> FillStates,
        int TapeHead
    );

    public static void Draw(IReadOnlyList<TapeHistorySnapshot> history, IReadOnlyCollection<char> states, UserInterface.Layout layout) {
        const int initialVisibleCells = 15;

        Rectangle panel = layout.HistoryPanel;
        float viewX = panel.X;
        float viewY = panel.Y;
        float viewWidth = panel.Width;
        float viewHeight = panel.Height;
        if (history.Count == 0 || viewWidth <= 0f || viewHeight <= 0f)
            return;
        int minimumPosition = history.Min(snapshot =>
            Math.Min(snapshot.TapeHead, snapshot.Tape.Keys.DefaultIfEmpty(snapshot.TapeHead).Min())
        );
        int maximumPosition = history.Max(snapshot =>
            Math.Max(snapshot.TapeHead, snapshot.Tape.Keys.DefaultIfEmpty(snapshot.TapeHead).Max())
        );
        int usedColumnCount = maximumPosition - minimumPosition + 1;
        float cellSize = viewWidth / Math.Max(usedColumnCount, initialVisibleCells);
        float rowHeight = MathF.Min(cellSize, viewHeight / history.Count);
        float contentX = viewX - minimumPosition * cellSize;

        Raylib.DrawRectangle((int)viewX, (int)viewY, (int)viewWidth, (int)viewHeight, Theme.Panel);
        Raylib.BeginScissorMode((int)viewX, (int)viewY, (int)viewWidth, (int)viewHeight);

        for (int row = 0; row < history.Count; row++) {
            TapeHistorySnapshot snapshot = history[row];
            float y = viewY + row * rowHeight;

            foreach (KeyValuePair<int, char> cell in snapshot.Tape) {
                float x = contentX + cell.Key * cellSize;
                Color color = GetStateColor(snapshot.FillStates[cell.Key], states);
                Rectangle cellRectangle = new(x, y, cellSize, rowHeight);

                if (cell.Value == '1')
                    Raylib.DrawRectangleRec(cellRectangle, color);
                else {
                    color.A = 80; // half opacity
                    Raylib.DrawRectangleRec(cellRectangle, color);
                }
            }
        }

        Raylib.EndScissorMode();
        Raylib.DrawRectangleLines((int)viewX, (int)viewY, (int)viewWidth, (int)viewHeight, Theme.ElementDark);
    }

    static Color GetStateColor(char state, IReadOnlyCollection<char> states) {
        int index = states.ToList().IndexOf(state);
        return Theme.StateColors[index % Theme.StateColors.Length];
    }
}
