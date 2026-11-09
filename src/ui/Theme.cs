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

using Raylib_cs;

namespace TuringFactory;

static class Theme {
    public static readonly Color Background = new(24, 25, 38);
    public static readonly Color Panel = new(30, 32, 48);
    public static readonly Color ElementDark = new(36, 39, 58);
    public static readonly Color ElementLight = new(202, 211, 245);
    public static readonly Color Innactive = new(110, 115, 141);
    public static readonly Color Selected = new(145, 215, 227);
    public static readonly Color Active = new(244, 219, 214);
    public static readonly Color Incomplete = new(237, 135, 150);
    public static readonly Color MouseHover = new(165, 173, 203);
    public static readonly Color[] StateColors = [
        new (238, 153, 160), // red
        new (138, 173, 244), // blue
        new (166, 218, 149), // green
        new (238, 212, 159), // yellow
        new (245, 169, 127), // orange
        new (139, 213, 202), // teal
        new (245, 189, 230), // pink
        new (125, 196, 228), // cyan
        new (198, 160, 246), // magenta
        new (240, 198, 198), // beige
        new (183, 189, 248)  // purple
    ];

    public static Font Font { get; private set; }

    public static void LoadFont() {
        byte[] fontData = ResourceLoader.ReadBytes("resources/font/Nunito-Medium.ttf");
        Font = Raylib.LoadFontFromMemory(".ttf", fontData, 144, null, 0);
        Raylib.SetTextureFilter(Font.Texture, TextureFilter.Bilinear);
    }

    public static void UnloadFont() {
        Raylib.UnloadFont(Font);
    }
}
