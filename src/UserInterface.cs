using System.Numerics;
using System.Runtime.InteropServices;
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
    bool editingDefinition;
    string definitionText = string.Empty;
    string? definitionError;
    float speed = 0.5f;
    public float Speed => speed;

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
        if (!editingDefinition)
            definitionText = definition;

        string? definitionSubmission = null;
        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mousePosition, layout.DefinitionArea)) {
            editingDefinition = true;
            definitionText = definition;
            definitionError = null;
        }

        if (editingDefinition) {
            bool controlPressed = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);
            if (controlPressed && Raylib.IsKeyPressed(KeyboardKey.V))
                definitionText += FilterDefinitionText(GetClipboardText());
            if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && definitionText.Length > 0)
                definitionText = definitionText[..^1];
            if (Raylib.IsKeyPressed(KeyboardKey.Escape)) {
                editingDefinition = false;
                definitionError = null;
                definitionText = definition;
            } else if (Raylib.IsKeyPressed(KeyboardKey.Enter) && !string.IsNullOrWhiteSpace(definitionText)) {
                definitionSubmission = definitionText;
                editingDefinition = false;
                definitionError = null;
            }

            int pressedCharacter;
            while ((pressedCharacter = Raylib.GetCharPressed()) > 0)
                if (pressedCharacter >= 32 && pressedCharacter <= 126)
                    definitionText += (char)pressedCharacter;
        }

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
        editingDefinition = true;
        definitionError = error;
    }

    public void Draw(string definition, MachineValidationResult validation, TuringMachineRuntime runtime, Layout layout) {
        float padding = basePadding * layout.Scale;
        float titleSizeValue = 28f * layout.Scale;
        float bodyTextSize = 30f * layout.Scale;
        string displayedDefinition = editingDefinition ? definitionText : definition;
        Vector2 titleSize = Raylib.MeasureTextEx(Theme.Font, displayedDefinition, titleSizeValue, 1);
        Color statusColor = validation.IsValid && definitionError is null ? Theme.ElementLight : Theme.Incomplete;
        Vector2 titlePosition = new(layout.WindowWidth / 2f - titleSize.X / 2f, padding);
        if (editingDefinition) {
            Rectangle titleBox = new(titlePosition.X - 8f * layout.Scale, titlePosition.Y - 4f * layout.Scale,
                titleSize.X + 16f * layout.Scale, titleSize.Y + 8f * layout.Scale);
            Raylib.DrawRectangleLinesEx(titleBox, MathF.Max(1f, 2f * layout.Scale), Theme.MouseHover);
        }
        Raylib.DrawTextEx(Theme.Font, displayedDefinition, titlePosition, titleSizeValue, 1, statusColor);
        if (editingDefinition)
            Raylib.DrawLineEx(new(titlePosition.X + titleSize.X + 2f * layout.Scale, titlePosition.Y), new(titlePosition.X + titleSize.X + 2f * layout.Scale, titlePosition.Y + titleSize.Y), MathF.Max(1f, 2f * layout.Scale), Theme.MouseHover);
        Raylib.DrawTextEx(Theme.Font, $"Steps: {runtime.StepCount}", new Vector2(padding, padding), bodyTextSize, 1, Theme.ElementLight);
        string? error = definitionError ?? (!validation.IsValid ? validation.Errors[0] : null);
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

    static string FilterDefinitionText(string text) => new([.. text.Where(character => character >= 32 && character <= 126)]);

    static unsafe string GetClipboardText() => Marshal.PtrToStringAnsi((IntPtr)Raylib.GetClipboardText()) ?? string.Empty;

    static void DrawButton(Rectangle button, string text, Font font, bool enabled, bool hovered) {
        Color background = !enabled ? Theme.Innactive : hovered ? Theme.MouseHover : Theme.ElementLight;
        Color textColor = enabled ? Theme.ElementDark : Theme.Panel;

        Raylib.DrawRectangleRounded(button, cornerRadius, 8, background);
        float textSizeValue = MathF.Max(1f, button.Height * 0.67f);
        Vector2 textSize = Raylib.MeasureTextEx(font, text, textSizeValue, 1);
        Raylib.DrawTextEx(font, text, new Vector2(button.X + button.Width / 2f - textSize.X / 2f, button.Y + button.Height / 2f - textSize.Y / 2f), textSizeValue, 1, textColor);
    }
}
