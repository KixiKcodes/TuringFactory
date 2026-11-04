using Raylib_cs;

namespace TuringFactory;

static class TapeRenderer {
    public static void Draw(IReadOnlyDictionary<int, char> tape, int tapeHead, int previousTapeHead, float transitionProgress, int windowWidth, int windowHeight) {
        const int cellSize = 55;
        const int visibleCells = 15;
        float viewWidth = visibleCells * cellSize;
        float viewX = (windowWidth - viewWidth) / 2f;
        float viewY = windowHeight - 100;
        Rectangle tapeView = new(viewX, viewY, viewWidth, cellSize);
        float movementOffset = (tapeHead - previousTapeHead) * cellSize * (1f - transitionProgress);

        Raylib.DrawRectangleRec(tapeView, Color.RayWhite);
        Raylib.BeginScissorMode((int)tapeView.X, (int)tapeView.Y, (int)tapeView.Width, (int)tapeView.Height);

        int firstCell = tapeHead - visibleCells / 2 - 1;
        int lastCell = tapeHead + visibleCells / 2 + 1;
        for (int tapePosition = firstCell; tapePosition <= lastCell; tapePosition++) {
            float x = viewX + (tapePosition - tapeHead + visibleCells / 2) * cellSize + movementOffset;
            bool isFilled = tape.TryGetValue(tapePosition, out char value) && value == '1';
            Rectangle cell = new(x, viewY, cellSize, cellSize);

            Raylib.DrawRectangleRec(cell, isFilled ? Color.Black : Color.RayWhite);
            Raylib.DrawRectangleLinesEx(cell, tapePosition == tapeHead ? 5f : 2f, tapePosition == tapeHead ? Color.Gold : Color.DarkGray);
        }

        Raylib.EndScissorMode();
        Raylib.DrawRectangleLinesEx(tapeView, 2f, Color.DarkGray);
    }
}
