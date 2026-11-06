using System.Numerics;
using Raylib_cs;

namespace TuringFactory.src;

sealed class UserInterface {
    const float designWidth = 1920f;
    const float designHeight = 1080f;
    const float basePadding = 20f;
    const float baseButtonHeight = 45f;
    const float baseButtonGap = 10f;
    const float baseSliderWidth = 300f;
    const float baseSliderHeight = 8f;
    const float baseHistoryWidth = 260f;
    const float minimumWorkspaceWidth = 420f;
    const float cornerRadius = 0.35f;
    bool draggingSlider;
    readonly TextBox definitionTextBox = new();
    float speed = 0.5f;
    public float Speed => speed;
    public bool IsEditingDefinition => definitionTextBox.IsEditing;

    public readonly record struct Layout(
        float WindowWidth,
        float Scale,
        Rectangle Workspace,
        Rectangle DefinitionArea,
        Rectangle HistoryPanel,
        Rectangle TapeArea,
        Rectangle PlayButton,
        Rectangle ResetButton,
        Rectangle SpeedSlider
    );

    public readonly record struct Actions(bool TogglePlay, bool Reset, string? DefinitionSubmission);

    public Layout GetLayout(float windowWidth, float windowHeight) {
        float scale = Math.Clamp(MathF.Min(windowWidth / designWidth, windowHeight / designHeight), 0.55f, 1f);
        float padding = basePadding * scale;
        float historyWidth = MathF.Min(baseHistoryWidth * scale, MathF.Max(0f, windowWidth - minimumWorkspaceWidth * scale));
        float workspaceWidth = windowWidth - historyWidth;
        float buttonGap = baseButtonGap * scale;
        float sliderWidth = MathF.Min(baseSliderWidth * scale, MathF.Max(0f, workspaceWidth - padding * 2f));
        float buttonHeight = baseButtonHeight * scale;
        float buttonWidth = MathF.Max(0f, (sliderWidth - buttonGap) / 2f);
        float buttonY = windowHeight - padding - buttonHeight;
        float tapeWidth = MathF.Max(0f, MathF.Min(windowWidth, 2f * (workspaceWidth - windowWidth / 2f)));
        Rectangle playButton = new(padding, buttonY, buttonWidth, buttonHeight);
        return new(
            windowWidth,
            scale,
            new Rectangle(0, 0, workspaceWidth, windowHeight),
            new Rectangle(0, 0, windowWidth, 70f * scale),
            new Rectangle(workspaceWidth, padding, MathF.Max(0f, historyWidth - padding), MathF.Max(0f, windowHeight - padding * 2f)),
            new Rectangle(windowWidth / 2f - tapeWidth / 2f, 0, tapeWidth, MathF.Max(0f, buttonY - padding)),
            playButton,
            new Rectangle(playButton.X + playButton.Width + buttonGap, buttonY, buttonWidth, buttonHeight),
            new Rectangle(padding, buttonY - 25f * scale, sliderWidth, baseSliderHeight * scale)
        );
    }

    public Actions Update(Vector2 mousePosition, Layout layout, string definition) {
        definitionTextBox.Sync(definition);
        Rectangle definitionBounds = GetDefinitionBounds(definition, layout);
        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mousePosition, definitionBounds))
            definitionTextBox.BeginEditing(definition, mousePosition, definitionBounds, Theme.Font, 28f * layout.Scale);
        string? definitionSubmission = definitionTextBox.Update();

        float sliderHandleX = layout.SpeedSlider.X + speed * layout.SpeedSlider.Width;
        Vector2 sliderHandlePosition = new(sliderHandleX, layout.SpeedSlider.Y + layout.SpeedSlider.Height / 2f);

        if (Raylib.IsMouseButtonPressed(MouseButton.Left) &&
            Vector2.Distance(mousePosition, sliderHandlePosition) <= 12f * layout.Scale)
            draggingSlider = true;
        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
            draggingSlider = false;
        if (draggingSlider)
            speed = Math.Clamp((mousePosition.X - layout.SpeedSlider.X) / layout.SpeedSlider.Width, 0f, 1f);

        bool reset = Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mousePosition, layout.ResetButton);
        bool togglePlay = Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mousePosition, layout.PlayButton);

        return new(togglePlay, reset, definitionSubmission);
    }

    public void RejectDefinition(string error) {
        definitionTextBox.Reject(error);
    }

    public void Draw(string definition, MachineValidationResult validation, TuringMachineRuntime runtime, Layout layout) {
        float padding = basePadding * layout.Scale;
        float titleSizeValue = 28f * layout.Scale;
        float bodyTextSize = 30f * layout.Scale;
        definitionTextBox.Sync(definition);
        Color statusColor = validation.IsValid && definitionTextBox.Error is null ? Theme.ElementLight : Theme.Incomplete;
        definitionTextBox.Draw(Theme.Font, layout.WindowWidth / 2f, padding, titleSizeValue, statusColor, layout.Scale);
        Raylib.DrawTextEx(Theme.Font, $"Steps: {runtime.StepCount}", new Vector2(padding, padding), bodyTextSize, 1, Theme.ElementLight);
        string? error = definitionTextBox.Error ?? (!validation.IsValid ? validation.Errors[0] : null);
        if (error is not null)
            Raylib.DrawTextEx(Theme.Font, error, new Vector2(layout.WindowWidth / 2f - 180f * layout.Scale, padding + 34f * layout.Scale), 18f * layout.Scale, 1, Theme.Incomplete);

        bool playEnabled = validation.IsValid;
        bool playHovered = playEnabled && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), layout.PlayButton);
        bool resetHovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), layout.ResetButton);
        bool sliderHovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), layout.SpeedSlider);

        DrawButton(layout.PlayButton, runtime.IsPlaying ? "Pause" : "Play", Theme.Font, playEnabled, playHovered);
        DrawButton(layout.ResetButton, "Reset", Theme.Font, true, resetHovered);

        Raylib.DrawTextEx(Theme.Font, "Step Speed", new Vector2(layout.SpeedSlider.X, layout.SpeedSlider.Y - 30f * layout.Scale), 25f * layout.Scale, 1, Theme.ElementLight);
        Raylib.DrawRectangleRounded(layout.SpeedSlider, cornerRadius, 8, Theme.Panel);
        float sliderX = layout.SpeedSlider.X + speed * layout.SpeedSlider.Width;
        Color handleColor = sliderHovered || draggingSlider ? Theme.MouseHover : Theme.ElementLight;
        Raylib.DrawCircle((int)sliderX, (int)(layout.SpeedSlider.Y + layout.SpeedSlider.Height / 2f), (int)(9f * layout.Scale), handleColor);
    }

    static Rectangle GetDefinitionBounds(string definition, Layout layout) {
        float titleSize = 28f * layout.Scale;
        Vector2 size = Raylib.MeasureTextEx(Theme.Font, definition, titleSize, 1);
        return new(layout.WindowWidth / 2f - size.X / 2f - 8f * layout.Scale, 0f,
            size.X + 16f * layout.Scale, 70f * layout.Scale);
    }

    static void DrawButton(Rectangle button, string text, Font font, bool enabled, bool hovered) {
        Color background = !enabled ? Theme.Innactive : hovered ? Theme.MouseHover : Theme.ElementLight;
        Color textColor = enabled ? Theme.ElementDark : Theme.Panel;

        Raylib.DrawRectangleRounded(button, cornerRadius, 8, background);
        float textSizeValue = MathF.Max(1f, button.Height * 0.67f);
        Vector2 textSize = Raylib.MeasureTextEx(font, text, textSizeValue, 1);
        Raylib.DrawTextEx(font, text, new Vector2(button.X + button.Width / 2f - textSize.X / 2f, button.Y + button.Height / 2f - textSize.Y / 2f), textSizeValue, 1, textColor);
    }
}
