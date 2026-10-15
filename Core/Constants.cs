using Raylib_cs;

namespace TheFifthAxis.Core;

public static class Constants
{
    public const int CanvasWidth = 1920;
    public const int CanvasHeight = 1080;
    public const int TargetFps = 60;

    // Commodore 64 Palette (Pepto RGB standard)
    public static readonly Color C64Black = new(0, 0, 0, 255);
    public static readonly Color C64White = new(255, 255, 255, 255);
    public static readonly Color C64Red = new(136, 0, 0, 255);
    public static readonly Color C64Cyan = new(170, 255, 238, 255);
    public static readonly Color C64Purple = new(204, 68, 204, 255);
    public static readonly Color C64Green = new(0, 204, 85, 255);
    public static readonly Color C64Blue = new(0, 0, 170, 255);
    public static readonly Color C64Yellow = new(238, 238, 119, 255);
    public static readonly Color C64Orange = new(221, 136, 85, 255);
    public static readonly Color C64Brown = new(102, 68, 0, 255);
    public static readonly Color C64LightRed = new(255, 119, 119, 255);
    public static readonly Color C64DarkGrey = new(51, 51, 51, 255);
    public static readonly Color C64Grey = new(119, 119, 119, 255);
    public static readonly Color C64LightGreen = new(170, 255, 102, 255);
    public static readonly Color C64LightBlue = new(0, 136, 255, 255);
    public static readonly Color C64LightGrey = new(187, 187, 187, 255);

    // Modern Remaster Palette Enhancements
    public static readonly Color NeonCyan = new(30, 230, 255, 255);
    public static readonly Color NeonOrange = new(255, 120, 30, 255);
    public static readonly Color NeonPurple = new(190, 60, 255, 255);
    public static readonly Color NeonGreen = new(50, 255, 130, 255);
    public static readonly Color DarkBackground = new(14, 16, 24, 255);
    public static readonly Color DarkPanel = new(22, 26, 38, 240);
    public static readonly Color AccentGold = new(255, 215, 0, 255);

    // Physics
    public const float Gravity = 1800f;
    public const float JumpSpeed = 700f;
    public const float BaseMoveSpeed = 350f;
}
