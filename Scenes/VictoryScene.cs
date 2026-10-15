using System.Numerics;
using Raylib_cs;
using TheFifthAxis.Core;
using TheFifthAxis.Audio;

namespace TheFifthAxis.Scenes;

public class VictoryScene : IScene
{
    private readonly Action _onReturnToMenu;
    private readonly SoundSynthesizer _audio;
    private float _timer;

    public VictoryScene(Action onReturnToMenu, SoundSynthesizer audio)
    {
        _onReturnToMenu = onReturnToMenu;
        _audio = audio;
        _audio.PlayAnachronon();
    }

    public void Update(float dt)
    {
        _timer += dt;
        if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Z))
        {
            _onReturnToMenu();
        }
    }

    public void Draw()
    {
        // Dark space background with stars
        Raylib.DrawRectangle(0, 0, Constants.CanvasWidth, Constants.CanvasHeight, new Color(8, 10, 18, 255));

        // Pulsing Anachronon Relic at Center
        float pulse = MathF.Sin(_timer * 3f) * 12f;
        Raylib.DrawCircle(Constants.CanvasWidth / 2, 280, 70 + pulse, Constants.AccentGold);
        Raylib.DrawCircle(Constants.CanvasWidth / 2, 280, 50, Constants.C64White);
        Raylib.DrawCircleLines(Constants.CanvasWidth / 2, 280, 95 + pulse, Constants.NeonCyan);

        // Victory Text from C64 RAM ($5041)
        Raylib.DrawText("WELL DONE CITIZEN !", 630, 420, 64, Constants.AccentGold);
        Raylib.DrawText("YOU HAVE ACCOMPLISHED YOUR MISSION", 450, 510, 44, Constants.NeonCyan);
        Raylib.DrawText("AND SAVED THE UNIVERSE !", 580, 580, 46, Constants.NeonGreen);

        Raylib.DrawText("THE 5TH AXIS HAS BEEN STABILIZED ACROSS ALL 10 HISTORICAL EPOCHS.", 420, 670, 26, Constants.C64LightGrey);
        Raylib.DrawText("PROFESSOR CHRONOS THANKS YOU FOR RETRIEVING ALL ANACHRONONS.", 440, 710, 24, Constants.C64Grey);

        // Tribute
        Raylib.DrawText("ORIGINAL GAME (C) 1986 LORICIELS", 740, 800, 22, Constants.C64Grey);
        Raylib.DrawText("DIDIER & OLIVIER GUILLION  *  GILLES SOULET  *  PHILIPPE GUEGAN", 530, 830, 22, Constants.C64Grey);

        Raylib.DrawText("PRESS ENTER OR SPACE TO RETURN TO MAIN MENU", 610, 930, 28, Constants.AccentGold);
    }
}
