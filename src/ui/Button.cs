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

sealed class Button {
    const float cornerRadius = 0.35f;

    public bool Update(Vector2 mousePosition, Rectangle bounds, bool enabled) {
        return enabled && Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mousePosition, bounds);
    }

    public void Draw(Rectangle bounds, string text, Font font, bool enabled) {
        bool hovered = enabled && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), bounds);
        Color background = !enabled ? Theme.Innactive : hovered ? Theme.MouseHover : Theme.ElementLight;
        Color textColor = enabled ? Theme.ElementDark : Theme.Panel;
        Raylib.DrawRectangleRounded(bounds, cornerRadius, 8, background);
        float textSizeValue = MathF.Max(1f, bounds.Height * 0.67f);
        Vector2 textSize = Raylib.MeasureTextEx(font, text, textSizeValue, 1);
        Raylib.DrawTextEx(font, text, new Vector2(bounds.X + bounds.Width / 2f - textSize.X / 2f, bounds.Y + bounds.Height / 2f - textSize.Y / 2f), textSizeValue, 1, textColor);
    }
}
