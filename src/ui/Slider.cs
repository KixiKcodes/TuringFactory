using System.Numerics;
using Raylib_cs;

namespace TuringFactory;

sealed class Slider {
    const float cornerRadius = 0.35f;
    bool dragging;

    public float Update(Vector2 mousePosition, Rectangle bounds, float value, float scale) {
        float handleX = bounds.X + value * bounds.Width;
        Vector2 handlePosition = new(handleX, bounds.Y + bounds.Height / 2f);
        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Vector2.Distance(mousePosition, handlePosition) <= 12f * scale)
            dragging = true;
        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
            dragging = false;
        return dragging ? Math.Clamp((mousePosition.X - bounds.X) / bounds.Width, 0f, 1f) : value;
    }

    public void Draw(Rectangle bounds, float value, float scale) {
        Raylib.DrawRectangleRounded(bounds, cornerRadius, 8, Theme.Panel);
        float handleX = bounds.X + value * bounds.Width;
        Color handleColor = dragging || Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), bounds) ? Theme.MouseHover : Theme.ElementLight;
        Raylib.DrawCircle((int)handleX, (int)(bounds.Y + bounds.Height / 2f), (int)(9f * scale), handleColor);
    }
}
