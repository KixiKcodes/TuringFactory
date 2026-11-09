/*
Turing Factory
Copyright (C) 2026 KixiK

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Numerics;
using Raylib_cs;

namespace TuringFactory;

static class TapeRenderer {
    public static void Draw(IReadOnlyDictionary<int, char> tape, int tapeHead, int previousTapeHead, float transitionProgress, UserInterface.Layout layout) {
        const int visibleCells = 15;
        if (layout.TapeArea.Width <= 0f || layout.TapeArea.Height <= 0f)
            return;
        float cellSize = MathF.Min(55f * layout.Scale, layout.TapeArea.Width / visibleCells);
        float viewWidth = visibleCells * cellSize;
        float viewX = layout.TapeArea.X + (layout.TapeArea.Width - viewWidth) / 2f;
        float viewY = layout.TapeArea.Y + layout.TapeArea.Height - cellSize;
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

            string symbol = isFilled ? "1" : "0";
            Vector2 textSize = Raylib.MeasureTextEx(Theme.Font, symbol, 32f * layout.Scale, 1f);
            Vector2 textPosition = new(cell.X + (cell.Width - textSize.X) / 2f, cell.Y + (cell.Height - textSize.Y) / 2f);
            Raylib.DrawTextEx(Theme.Font, symbol, textPosition, 32f * layout.Scale, 1f, isFilled ? Theme.ElementDark : Theme.ElementLight);
        }

        for (int tapePosition = firstCell + 1; tapePosition <= lastCell; tapePosition++) {
            bool leftFilled = tape.TryGetValue(tapePosition - 1, out char leftValue) && leftValue == '1';
            bool rightFilled = tape.TryGetValue(tapePosition, out char rightValue) && rightValue == '1';
            float x = viewX + (tapePosition - tapeHead + visibleCells / 2) * cellSize + movementOffset;
            Color dividerColor = leftFilled && rightFilled ? Theme.ElementDark : Theme.ElementLight;
            Raylib.DrawLineEx(new Vector2(x, viewY), new Vector2(x, viewY + cellSize), MathF.Max(1f, 2f * layout.Scale), dividerColor);
        }

        Rectangle headCell = new(viewX + visibleCells / 2 * cellSize + movementOffset, viewY, cellSize, cellSize);
        Raylib.DrawRectangleLinesEx(headCell, MathF.Max(1f, 5f * layout.Scale), Theme.Active);

        Raylib.EndScissorMode();
        Raylib.DrawRectangleLinesEx(tapeView, MathF.Max(1f, 2f * layout.Scale), Theme.ElementLight);
    }
}
