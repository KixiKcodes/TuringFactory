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

        Raylib.DrawRectangleRec(tapeView, Theme.ElementLight);
        Raylib.BeginScissorMode((int)tapeView.X, (int)tapeView.Y, (int)tapeView.Width, (int)tapeView.Height);

        int firstCell = tapeHead - visibleCells / 2 - 1;
        int lastCell = tapeHead + visibleCells / 2 + 1;
        for (int tapePosition = firstCell; tapePosition <= lastCell; tapePosition++) {
            float x = viewX + (tapePosition - tapeHead + visibleCells / 2) * cellSize + movementOffset;
            bool isFilled = tape.TryGetValue(tapePosition, out char value) && value == '1';
            Rectangle cell = new(x, viewY, cellSize, cellSize);

            Raylib.DrawRectangleRec(cell, isFilled ? Theme.ElementLight : Theme.ElementDark);
        }

        for (int tapePosition = firstCell + 1; tapePosition <= lastCell; tapePosition++) {
            bool leftFilled = tape.TryGetValue(tapePosition - 1, out char leftValue) && leftValue == '1';
            bool rightFilled = tape.TryGetValue(tapePosition, out char rightValue) && rightValue == '1';
            float x = viewX + (tapePosition - tapeHead + visibleCells / 2) * cellSize + movementOffset;
            Color dividerColor = leftFilled && rightFilled ? Theme.ElementDark : Theme.ElementLight;
            Raylib.DrawLineEx(new System.Numerics.Vector2(x, viewY), new System.Numerics.Vector2(x, viewY + cellSize), 2f, dividerColor);
        }

        Rectangle headCell = new(viewX + visibleCells / 2 * cellSize + movementOffset, viewY, cellSize, cellSize);
        Raylib.DrawRectangleLinesEx(headCell, 5f, Theme.Active);

        Raylib.EndScissorMode();
        Raylib.DrawRectangleLinesEx(tapeView, 2f, Theme.ElementLight);
    }
}
