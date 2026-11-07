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
