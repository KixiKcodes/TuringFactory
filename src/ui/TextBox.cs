using System.Numerics;
using System.Runtime.InteropServices;
using Raylib_cs;

namespace TuringFactory;

sealed class TextBox {
    string text = string.Empty;
    int cursorIndex;

    public string Text => text;
    public bool IsEditing { get; private set; }
    public string? Error { get; private set; }

    public void Sync(string value) {
        if (!IsEditing)
            text = value;
    }

    public void BeginEditing(string value, Vector2 mousePosition, Rectangle textBounds, Font font, float fontSize) {
        text = value;
        cursorIndex = FindCursorIndex(mousePosition.X - textBounds.X, text, font, fontSize);
        IsEditing = true;
        Error = null;
    }

    public void Reject(string error) {
        IsEditing = true;
        Error = error;
        cursorIndex = Math.Clamp(cursorIndex, 0, text.Length);
    }

    public string? Update() {
        if (!IsEditing)
            return null;

        bool controlPressed = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);
        if (controlPressed && Raylib.IsKeyPressed(KeyboardKey.V))
            Insert(FilterText(GetClipboardText()));
        if (Raylib.IsKeyPressed(KeyboardKey.Left))
            cursorIndex = Math.Max(0, cursorIndex - 1);
        if (Raylib.IsKeyPressed(KeyboardKey.Right))
            cursorIndex = Math.Min(text.Length, cursorIndex + 1);
        if (Raylib.IsKeyPressed(KeyboardKey.Home))
            cursorIndex = 0;
        if (Raylib.IsKeyPressed(KeyboardKey.End))
            cursorIndex = text.Length;
        if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && cursorIndex > 0) {
            text = text.Remove(cursorIndex - 1, 1);
            cursorIndex--;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Delete) && cursorIndex < text.Length)
            text = text.Remove(cursorIndex, 1);
        if (Raylib.IsKeyPressed(KeyboardKey.Escape)) {
            IsEditing = false;
            Error = null;
            return null;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Enter) && !string.IsNullOrWhiteSpace(text)) {
            IsEditing = false;
            Error = null;
            return text;
        }

        int pressedCharacter;
        while ((pressedCharacter = Raylib.GetCharPressed()) > 0)
            if (pressedCharacter >= 32 && pressedCharacter <= 126)
                Insert(((char)pressedCharacter).ToString());

        return null;
    }

    public void Draw(Font font, float centerX, float top, float fontSize, Color color, float scale) {
        Vector2 textSize = Raylib.MeasureTextEx(font, text, fontSize, 1);
        Vector2 textPosition = new(centerX - textSize.X / 2f, top);
        if (IsEditing) {
            Rectangle textBox = new(textPosition.X - 8f * scale, textPosition.Y - 4f * scale,
                textSize.X + 16f * scale, textSize.Y + 8f * scale);
            Raylib.DrawRectangleLinesEx(textBox, MathF.Max(1f, 2f * scale), Theme.MouseHover);
        }
        Raylib.DrawTextEx(font, text, textPosition, fontSize, 1, color);
        if (IsEditing) {
            float cursorX = textPosition.X + Raylib.MeasureTextEx(font, text[..cursorIndex], fontSize, 1).X;
            Raylib.DrawLineEx(new(cursorX + 2f * scale, textPosition.Y),
                new(cursorX + 2f * scale, textPosition.Y + textSize.Y), MathF.Max(1f, 2f * scale), Theme.MouseHover);
        }
    }

    static int FindCursorIndex(float localMouseX, string value, Font font, float fontSize) {
        for (int index = 0; index <= value.Length; index++) {
            float left = index == 0 ? 0f : Raylib.MeasureTextEx(font, value[..index], fontSize, 1).X;
            float right = index == value.Length ? left : Raylib.MeasureTextEx(font, value[..(index + 1)], fontSize, 1).X;
            if (localMouseX <= (left + right) / 2f)
                return index;
        }
        return value.Length;
    }

    void Insert(string value) {
        if (value.Length == 0)
            return;
        text = text.Insert(cursorIndex, value);
        cursorIndex += value.Length;
    }

    static string FilterText(string value) => new([.. value.Where(character => character >= 32 && character <= 126)]);

    static unsafe string GetClipboardText() => Marshal.PtrToStringAnsi((IntPtr)Raylib.GetClipboardText()) ?? string.Empty;
}
