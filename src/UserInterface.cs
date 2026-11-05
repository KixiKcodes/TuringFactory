using System.Numerics;
using Raylib_cs;

namespace TuringFactory.src;

sealed class UserInterface {
    const float padding = 20f;
    const float buttonHeight = 45f;
    const float buttonGap = 10f;
    const float sliderWidth = 300f;
    const float sliderHeight = 8f;
    const float buttonWidth = (sliderWidth - buttonGap) / 2f;
    const float cornerRadius = 0.35f;
    bool draggingSlider;
    float speed = 0.5f;
    public float Speed => speed;

    public readonly record struct Layout(
        float WindowWidth,
        Rectangle Workspace,
        Rectangle PlayButton,
        Rectangle ResetButton,
        Rectangle SpeedSlider
    );

    public readonly record struct Actions(bool TogglePlay, bool Reset);

    public Layout GetLayout(float windowWidth, float windowHeight) {
        float buttonY = windowHeight - padding - buttonHeight;
        Rectangle playButton = new(padding, buttonY, buttonWidth, buttonHeight);
        return new(
            windowWidth,
            new Rectangle(0, 0, windowWidth - 300f, windowHeight),
            playButton,
            new Rectangle(playButton.X + playButton.Width + buttonGap, buttonY, buttonWidth, buttonHeight),
            new Rectangle(padding, buttonY - 25f, sliderWidth, sliderHeight)
        );
    }

    public Actions Update(Vector2 mousePosition, Layout layout) {
        float sliderHandleX = layout.SpeedSlider.X + speed * layout.SpeedSlider.Width;
        Vector2 sliderHandlePosition = new(sliderHandleX, layout.SpeedSlider.Y + layout.SpeedSlider.Height / 2f);

        if (Raylib.IsMouseButtonPressed(MouseButton.Left) &&
            Vector2.Distance(mousePosition, sliderHandlePosition) <= 12f)
            draggingSlider = true;
        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
            draggingSlider = false;
        if (draggingSlider)
            speed = Math.Clamp((mousePosition.X - layout.SpeedSlider.X) / layout.SpeedSlider.Width, 0f, 1f);

        bool reset = Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mousePosition, layout.ResetButton);
        bool togglePlay = Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mousePosition, layout.PlayButton);

        return new(togglePlay, reset);
    }

    public void Draw(string definition, MachineValidationResult validation, TuringMachineRuntime runtime, Layout layout) {
        Vector2 titleSize = Raylib.MeasureTextEx(Theme.Font, definition, 36, 1);
        Color statusColor = validation.IsValid ? Theme.ElementLight : Theme.Incomplete;
        Raylib.DrawTextEx(Theme.Font, definition, new Vector2(layout.WindowWidth / 2f - titleSize.X / 2f, padding), 28, 1, statusColor);
        Raylib.DrawTextEx(Theme.Font, $"Steps: {runtime.StepCount}", new Vector2(padding, padding), 30, 1, Theme.ElementLight);
        if (!validation.IsValid)
            Raylib.DrawTextEx(Theme.Font, validation.Errors[0], new Vector2(layout.WindowWidth / 2f - 180, padding + 34), 18, 1, Theme.Incomplete);

        bool playEnabled = validation.IsValid;
        bool playHovered = playEnabled && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), layout.PlayButton);
        bool resetHovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), layout.ResetButton);
        bool sliderHovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), layout.SpeedSlider);

        DrawButton(layout.PlayButton, runtime.IsPlaying ? "Pause" : "Play", Theme.Font, playEnabled, playHovered);
        DrawButton(layout.ResetButton, "Reset", Theme.Font, true, resetHovered);

        Raylib.DrawTextEx(Theme.Font, "Step Speed", new Vector2(layout.SpeedSlider.X, layout.SpeedSlider.Y - 30f), 25, 1, Theme.ElementLight);
        Raylib.DrawRectangleRounded(layout.SpeedSlider, cornerRadius, 8, Theme.Panel);
        float sliderX = layout.SpeedSlider.X + speed * layout.SpeedSlider.Width;
        Color handleColor = sliderHovered || draggingSlider ? Theme.MouseHover : Theme.ElementLight;
        Raylib.DrawCircle((int)sliderX, (int)(layout.SpeedSlider.Y + layout.SpeedSlider.Height / 2f), 9, handleColor);

    }

    static void DrawButton(Rectangle button, string text, Font font, bool enabled, bool hovered) {
        Color background = !enabled ? Theme.Innactive : hovered ? Theme.MouseHover : Theme.ElementLight;
        Color textColor = enabled ? Theme.ElementDark : Theme.Panel;

        Raylib.DrawRectangleRounded(button, cornerRadius, 8, background);
        Vector2 textSize = Raylib.MeasureTextEx(font, text, 30, 1);
        Raylib.DrawTextEx(font, text, new Vector2(button.X + button.Width / 2f - textSize.X / 2f, button.Y + button.Height / 2f - textSize.Y / 2f), 30, 1, textColor);
    }
}
