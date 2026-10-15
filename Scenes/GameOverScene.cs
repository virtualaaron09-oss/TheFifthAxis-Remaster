using Raylib_cs;
using TheFifthAxis.Core;
using TheFifthAxis.Audio;

namespace TheFifthAxis.Scenes;

public class GameOverScene : IScene
{
    private readonly Action _onRetry;
    private readonly Action _onReturnToMenu;
    private readonly SoundSynthesizer _audio;
    private int _selectedOption = 0;

    public GameOverScene(Action onRetry, Action onReturnToMenu, SoundSynthesizer audio)
    {
        _onRetry = onRetry;
        _onReturnToMenu = onReturnToMenu;
        _audio = audio;
    }

    public void Update(float dt)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W) ||
            Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S))
        {
            _selectedOption = (_selectedOption + 1) % 2;
            _audio.PlayPunch();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Z))
        {
            _audio.PlayKick();
            if (_selectedOption == 0) _onRetry();
            else _onReturnToMenu();
        }
    }

    public void Draw()
    {
        Raylib.DrawRectangle(0, 0, Constants.CanvasWidth, Constants.CanvasHeight, new Color(12, 10, 14, 255));

        // Original quote from C64 RAM at $4C38
        Raylib.DrawText("YOU FAILED.", 760, 280, 72, Constants.C64LightRed);
        Raylib.DrawText("FIFTH AXIS OUT OF PHASE", 580, 380, 56, Constants.NeonOrange);
        Raylib.DrawText("THE TIMELINE HAS COLLAPSED INTO CHAOS.", 660, 470, 26, Constants.C64Grey);

        // Options
        int panelX = 720;
        int panelY = 580;

        string[] options = [
            "RETRY MISSION (RE-ENTER FACILITY)",
            "RETURN TO TITLE MENU"
        ];

        for (int i = 0; i < options.Length; i++)
        {
            bool isSelected = i == _selectedOption;
            int itemY = panelY + i * 80;

            if (isSelected)
            {
                Raylib.DrawRectangle(panelX - 40, itemY - 8, 560, 52, new Color(255, 120, 30, 45));
                Raylib.DrawRectangleLines(panelX - 40, itemY - 8, 560, 52, Constants.NeonOrange);
                Raylib.DrawText(">", panelX - 20, itemY, 32, Constants.NeonOrange);
            }

            Color textColor = isSelected ? Constants.C64White : Constants.C64LightGrey;
            Raylib.DrawText(options[i], panelX + 20, itemY, 28, textColor);
        }

        Raylib.DrawText("PRESS ENTER OR SPACE TO CONFIRM", 740, 780, 24, Constants.AccentGold);
    }
}
