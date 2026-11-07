using System.Numerics;
using Raylib_cs;

namespace TuringFactory.src;

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

sealed class UserInterface(SoundMode soundMode = SoundMode.Notes) {
    const float designWidth = 1920f;
    const float designHeight = 1080f;
    const float basePadding = 20f;
    const float baseButtonHeight = 45f;
    const float baseButtonGap = 10f;
    const float baseSliderWidth = 300f;
    const float baseSliderHeight = 8f;
    const float baseHistoryWidth = 260f;
    const float minimumWorkspaceWidth = 420f;

    readonly TextBox definitionTextBox = new();
    readonly Button settingsButton = new();
    readonly Button playButton = new();
    readonly Button resetButton = new();
    readonly Button modeButton = new();
    readonly Slider speedSlider = new();
    readonly Slider volumeSlider = new();

    bool settingsOpen;
    SoundMode soundMode = soundMode;
    float speed = 0.5f;
    float volume = 1f;
    public float Speed => speed;
    public float Volume => volume;
    public SoundMode SoundMode => soundMode;
    public bool IsEditingDefinition => definitionTextBox.IsEditing;

    public readonly record struct Layout(
        float WindowWidth,
        float Scale,
        Rectangle Workspace,
        Rectangle DefinitionArea,
        Rectangle HistoryPanel,
        Rectangle TapeArea,
        Rectangle SettingsButton,
        Rectangle SettingsPanel,
        Rectangle PlayButton,
        Rectangle ResetButton,
        Rectangle SpeedSlider
        ,Rectangle VolumeSlider,
        Rectangle ModeButton,
        Rectangle StepCountArea
    );

    public readonly record struct Actions(bool TogglePlay, bool Reset, bool SoundModeChanged, string? DefinitionSubmission);

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
        Rectangle settingsButton = new(padding, padding, 100f * scale, buttonHeight);
        Rectangle settingsPanel = new(padding, settingsButton.Y + settingsButton.Height + buttonGap, sliderWidth, 130f * scale);
        Rectangle volumeSlider = new(settingsPanel.X + padding, settingsPanel.Y + 50f * scale, MathF.Max(0f, settingsPanel.Width - padding * 2f), baseSliderHeight * scale);
        Rectangle modeButton = new(settingsPanel.X + padding, settingsPanel.Y + 85f * scale, MathF.Max(0f, settingsPanel.Width - padding * 2f), buttonHeight);
        Rectangle playButton = new(padding, buttonY, buttonWidth, buttonHeight);
        return new(
            windowWidth,
            scale,
            new Rectangle(0, 0, workspaceWidth, windowHeight),
            new Rectangle(0, 0, windowWidth, 70f * scale),
            new Rectangle(workspaceWidth, padding, MathF.Max(0f, historyWidth - padding), MathF.Max(0f, windowHeight - padding * 2f)),
            new Rectangle(windowWidth / 2f - tapeWidth / 2f, 0, tapeWidth, MathF.Max(0f, buttonY - padding)),
            settingsButton,
            settingsPanel,
            playButton,
            new Rectangle(playButton.X + playButton.Width + buttonGap, buttonY, buttonWidth, buttonHeight),
            new Rectangle(padding, buttonY - 25f * scale, sliderWidth, baseSliderHeight * scale),
            volumeSlider,
            modeButton,
            new Rectangle(windowWidth / 2f + 100f * scale, buttonY, 250f * scale, buttonHeight)
        );
    }

    public Actions Update(Vector2 mousePosition, Layout layout, string definition) {
        definitionTextBox.Sync(definition);
        Rectangle definitionBounds = GetDefinitionBounds(definition, layout);
        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mousePosition, definitionBounds))
            definitionTextBox.BeginEditing(definition, mousePosition, definitionBounds, Theme.Font, 28f * layout.Scale);
        string? definitionSubmission = definitionTextBox.Update();

        bool soundModeChanged = false;
        if (settingsButton.Update(mousePosition, layout.SettingsButton, true))
            settingsOpen = !settingsOpen;
        if (settingsOpen) {
            volume = volumeSlider.Update(mousePosition, layout.VolumeSlider, volume, layout.Scale);
            if (modeButton.Update(mousePosition, layout.ModeButton, true)) {
                soundMode = soundMode == SoundMode.Notes ? SoundMode.Drums : SoundMode.Notes;
                soundModeChanged = true;
            }
        }
        speed = speedSlider.Update(mousePosition, layout.SpeedSlider, speed, layout.Scale);

        bool reset = resetButton.Update(mousePosition, layout.ResetButton, true);
        bool togglePlay = playButton.Update(mousePosition, layout.PlayButton, true);
        if (!definitionTextBox.IsEditing && Raylib.IsKeyPressed(KeyboardKey.Space))
            togglePlay = true;

        return new(togglePlay, reset, soundModeChanged, definitionSubmission);
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
        string stepText = $"Steps: {runtime.StepCount}";
        Vector2 stepTextSize = Raylib.MeasureTextEx(Theme.Font, stepText, bodyTextSize, 1);
        Raylib.DrawTextEx(Theme.Font, stepText, new Vector2(layout.StepCountArea.X + (layout.StepCountArea.Width - stepTextSize.X) / 2f, layout.StepCountArea.Y + (layout.StepCountArea.Height - stepTextSize.Y) / 2f), bodyTextSize, 1, Theme.ElementLight);
        string? error = definitionTextBox.Error ?? (!validation.IsValid ? validation.Errors[0] : null);
        if (error is not null)
            Raylib.DrawTextEx(Theme.Font, error, new Vector2(layout.WindowWidth / 2f - 180f * layout.Scale, padding + 34f * layout.Scale), 18f * layout.Scale, 1, Theme.Incomplete);

        bool playEnabled = validation.IsValid;
        playButton.Draw(layout.PlayButton, runtime.IsPlaying ? "Pause" : "Play", Theme.Font, playEnabled);
        resetButton.Draw(layout.ResetButton, "Reset", Theme.Font, true);
        settingsButton.Draw(layout.SettingsButton, "Settings", Theme.Font, true);
        if (settingsOpen) {
            Raylib.DrawTextEx(Theme.Font, "Volume", new Vector2(layout.VolumeSlider.X, layout.VolumeSlider.Y - 30f * layout.Scale), 25f * layout.Scale, 1, Theme.ElementLight);
            volumeSlider.Draw(layout.VolumeSlider, volume, layout.Scale);
            modeButton.Draw(layout.ModeButton, soundMode == SoundMode.Notes ? "Notes" : "Drums", Theme.Font, true);
        }

        Raylib.DrawTextEx(Theme.Font, "Step Speed", new Vector2(layout.SpeedSlider.X, layout.SpeedSlider.Y - 30f * layout.Scale), 25f * layout.Scale, 1, Theme.ElementLight);
        speedSlider.Draw(layout.SpeedSlider, speed, layout.Scale);
    }

    static Rectangle GetDefinitionBounds(string definition, Layout layout) {
        float titleSize = 28f * layout.Scale;
        Vector2 size = Raylib.MeasureTextEx(Theme.Font, definition, titleSize, 1);
        return new(layout.WindowWidth / 2f - size.X / 2f - 8f * layout.Scale, 0f,
            size.X + 16f * layout.Scale, 70f * layout.Scale);
    }
}
