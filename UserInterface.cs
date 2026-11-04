using System.Numerics;
using Raylib_cs;

namespace TuringFactory;

sealed class UserInterface {
    const float padding = 20f;
    const float buttonHeight = 45f;
    const float buttonWidth = 110f;
    const float buttonGap = 10f;
    const float sliderWidth = 300f;
    const float sliderHeight = 8f;

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

    public void Draw(string definition, MachineValidationResult validation, TuringMachineRuntime runtime, Layout layout, Font font) {
        Vector2 titleSize = Raylib.MeasureTextEx(font, definition, 28, 1);
        Color statusColor = validation.IsValid ? Color.DarkGreen : Color.Red;
        Raylib.DrawTextEx(font, definition, new Vector2(layout.WindowWidth / 2f - titleSize.X / 2f, padding), 28, 1, statusColor);
        Raylib.DrawTextEx(font, $"Steps: {runtime.StepCount}", new Vector2(padding, padding), 24, 1, Color.Black);
        if (!validation.IsValid)
            Raylib.DrawTextEx(font, validation.Errors[0], new Vector2(layout.WindowWidth / 2f - 180, padding + 34), 18, 1, Color.Red);

        DrawButton(layout.PlayButton, runtime.IsPlaying ? "Pause" : "Play", font);
        DrawButton(layout.ResetButton, "Reset", font);

        Raylib.DrawRectangleRec(layout.SpeedSlider, Color.LightGray);
        Raylib.DrawRectangleLinesEx(layout.SpeedSlider, 2f, Color.DarkGray);
        float sliderX = layout.SpeedSlider.X + speed * layout.SpeedSlider.Width;
        Raylib.DrawCircle((int)sliderX, (int)(layout.SpeedSlider.Y + layout.SpeedSlider.Height / 2f), 9, Color.DarkGray);

    }

    static void DrawButton(Rectangle button, string text, Font font) {
        Raylib.DrawRectangleRec(button, Color.LightGray);
        Raylib.DrawRectangleLinesEx(button, 2f, Color.DarkGray);
        Vector2 textSize = Raylib.MeasureTextEx(font, text, 24, 1);
        Raylib.DrawTextEx(font, text, new Vector2(button.X + button.Width / 2f - textSize.X / 2f, button.Y + button.Height / 2f - textSize.Y / 2f), 24, 1, Color.Black);
    }
}
