using System.Numerics;
using Raylib_cs;
using TheFifthAxis.Core;
using TheFifthAxis.Audio;
using TheFifthAxis.Graphics;
using TheFifthAxis.Entities;

namespace TheFifthAxis.Scenes;

public class MenuScene : IScene
{
    private readonly Action<Player, int> _onStartGame;
    private readonly SoundSynthesizer _audio;
    private readonly AssetManager _assets;

    private int _selectedOption = 0;
    private bool _inCharacterDefine = false;
    private int _selectedStat = 0;

    // Player stats
    public int Force = 25;
    public int Agility = 25;
    public int Life = 25;
    public int Credit = 25;
    public int SelectedEra = 0;

    private const int TotalQuota = 100;
    private float _titlePulse = 0f;

    public MenuScene(Action<Player, int> onStartGame, SoundSynthesizer audio, AssetManager assets)
    {
        _onStartGame = onStartGame;
        _audio = audio;
        _assets = assets;
    }

    public void Update(float dt)
    {
        _titlePulse += dt * 2.5f;

        if (_inCharacterDefine)
        {
            UpdateCharacterDefine();
        }
        else
        {
            UpdateMainMenu();
        }
    }

    private void UpdateMainMenu()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
        {
            _selectedOption = (_selectedOption + 3) % 4;
            _audio.PlayPunch();
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S))
        {
            _selectedOption = (_selectedOption + 1) % 4;
            _audio.PlayPunch();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Z))
        {
            _audio.PlayKick();
            switch (_selectedOption)
            {
                case 0: // Start Game
                    var player = new Player(new Vector2(200, 780))
                    {
                        Force = Force,
                        Agility = Agility,
                        Life = Life,
                        Credit = Credit
                    };
                    player.ResetHealth();
                    _onStartGame(player, SelectedEra);
                    break;

                case 1: // Define Character
                    _inCharacterDefine = true;
                    _selectedStat = 0;
                    break;

                case 2: // Change starting epoch (0-9)
                    SelectedEra = (SelectedEra + 1) % 10;
                    break;

                case 3: // Toggle CRT Filter
                    _assets.CrtFilterEnabled = !_assets.CrtFilterEnabled;
                    break;
            }
        }

        // Quick CRT toggle on F1
        if (Raylib.IsKeyPressed(KeyboardKey.F1))
        {
            _assets.CrtFilterEnabled = !_assets.CrtFilterEnabled;
        }

        // Quick Music toggle on M
        if (Raylib.IsKeyPressed(KeyboardKey.M))
        {
            _audio.MusicEnabled = !_audio.MusicEnabled;
        }
    }

    private void UpdateCharacterDefine()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
        {
            _selectedStat = (_selectedStat + 3) % 4;
            _audio.PlayPunch();
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S))
        {
            _selectedStat = (_selectedStat + 1) % 4;
            _audio.PlayPunch();
        }

        int currentTotal = Force + Agility + Life + Credit;

        if (Raylib.IsKeyPressed(KeyboardKey.Left) || Raylib.IsKeyPressed(KeyboardKey.A))
        {
            ModifyStat(_selectedStat, -5);
            _audio.PlayPunch();
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Right) || Raylib.IsKeyPressed(KeyboardKey.D))
        {
            if (currentTotal < TotalQuota)
            {
                ModifyStat(_selectedStat, Math.Min(5, TotalQuota - currentTotal));
                _audio.PlayPunch();
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Z) || Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            _inCharacterDefine = false;
            _audio.PlayKick();
        }
    }

    private void ModifyStat(int statIndex, int delta)
    {
        switch (statIndex)
        {
            case 0: Force = Math.Clamp(Force + delta, 5, 50); break;
            case 1: Agility = Math.Clamp(Agility + delta, 5, 50); break;
            case 2: Life = Math.Clamp(Life + delta, 5, 50); break;
            case 3: Credit = Math.Clamp(Credit + delta, 5, 50); break;
        }
    }

    public void Draw()
    {
        // Dark futuristic laboratory background
        Raylib.DrawRectangleGradientV(0, 0, Constants.CanvasWidth, Constants.CanvasHeight,
            new Color(10, 12, 22, 255), new Color(4, 6, 12, 255));

        // Decorative background grid
        for (int x = 0; x < Constants.CanvasWidth; x += 80)
        {
            Raylib.DrawLine(x, 0, x, Constants.CanvasHeight, new Color(30, 40, 60, 40));
        }
        for (int y = 0; y < Constants.CanvasHeight; y += 80)
        {
            Raylib.DrawLine(0, y, Constants.CanvasWidth, y, new Color(30, 40, 60, 40));
        }

        // Title Header
        float pulse = MathF.Sin(_titlePulse) * 4f;
        Raylib.DrawText("THE FIFTH AXIS", (int)(480 + pulse), 110, 84, Constants.NeonCyan);
        Raylib.DrawText("LE 5EME AXE - C64 REMASTER 2026", 620, 205, 26, Constants.NeonOrange);

        // Historical Credits from C64 RAM
        Raylib.DrawText("(C) 1986 LORICIELS", 780, 250, 22, Constants.C64LightGrey);
        Raylib.DrawText("Original Concept by Didier & Olivier Guillion", 640, 280, 20, Constants.C64Grey);
        Raylib.DrawText("C64 Adaptation & Sound by Gilles Soulet  |  Participation by Philippe Guegan", 480, 305, 20, Constants.C64Grey);

        if (_inCharacterDefine)
        {
            DrawCharacterDefine();
        }
        else
        {
            DrawMainMenu();
        }

        // Control instructions bar at bottom
        Raylib.DrawRectangle(0, Constants.CanvasHeight - 60, Constants.CanvasWidth, 60, Constants.DarkPanel);
        Raylib.DrawText("CONTROLS: [Arrows/WASD] Move & Navigate   [Z/J] Punch   [X/K] Kick   [Space/Up] Jump   [F1] CRT Filter   [M] Mute",
            320, Constants.CanvasHeight - 40, 22, Constants.C64LightGreen);
    }

    private void DrawMainMenu()
    {
        int panelX = 560;
        int panelY = 380;
        int panelWidth = 800;
        int panelHeight = 460;

        Raylib.DrawRectangle(panelX, panelY, panelWidth, panelHeight, Constants.DarkPanel);
        Raylib.DrawRectangleLinesEx(new Rectangle(panelX, panelY, panelWidth, panelHeight), 3, Constants.NeonCyan);

        string[] eraNames = [
            "Thebes (1555 B.C.)", "Athens (561 B.C.)", "Memphis (560 B.C.)", "Loches (512 A.D.)",
            "Apamea (800 A.D.)", "Fleury (999 A.D.)", "Paris (1998 A.D.)", "New Marseille (2038 A.D.)",
            "Subpacific (2201 A.D.)", "Mars Copernic (2320 A.D.)"
        ];

        string[] options = [
            "1.  START MISSION (ENTER FACILITY)",
            $"2.  CUSTOMIZE CHARACTER [F:{Force} A:{Agility} L:{Life} C:{Credit}]",
            $"3.  RECALL ADVENTURE ERA : < {eraNames[SelectedEra]} >",
            $"4.  DISPLAY MODE : < {(_assets.CrtFilterEnabled ? "RETRO CRT SCANLINES" : "MODERN 1080P HD")} >"
        ];

        for (int i = 0; i < options.Length; i++)
        {
            bool isSelected = i == _selectedOption;
            int itemY = panelY + 60 + i * 85;

            if (isSelected)
            {
                Raylib.DrawRectangle(panelX + 30, itemY - 10, panelWidth - 60, 56, new Color(30, 230, 255, 45));
                Raylib.DrawRectangleLinesEx(new Rectangle(panelX + 30, itemY - 10, panelWidth - 60, 56), 2, Constants.NeonCyan);
                Raylib.DrawText(">", panelX + 45, itemY, 32, Constants.NeonOrange);
            }

            Color textColor = isSelected ? Constants.C64White : Constants.C64LightGrey;
            Raylib.DrawText(options[i], panelX + 80, itemY, 28, textColor);
        }

        Raylib.DrawText("PRESS ENTER / SPACE / Z TO SELECT", panelX + 180, panelY + panelHeight - 45, 22, Constants.AccentGold);
    }

    private void DrawCharacterDefine()
    {
        int panelX = 560;
        int panelY = 370;
        int panelWidth = 800;
        int panelHeight = 490;

        Raylib.DrawRectangle(panelX, panelY, panelWidth, panelHeight, Constants.DarkPanel);
        Raylib.DrawRectangleLinesEx(new Rectangle(panelX, panelY, panelWidth, panelHeight), 3, Constants.NeonOrange);

        Raylib.DrawText("CHARACTER DEFINITION - QUOTA ALLOCATION", panelX + 90, panelY + 30, 28, Constants.NeonOrange);
        int remQuota = TotalQuota - (Force + Agility + Life + Credit);
        Raylib.DrawText($"REMAINING POINTS: {remQuota}", panelX + 280, panelY + 70, 24, remQuota == 0 ? Constants.NeonGreen : Constants.NeonOrange);

        (string name, int val, string desc)[] stats = [
            ("FORCE", Force, "Increases punch & kick damage to cyborgs"),
            ("AGILITY", Agility, "Enhances running speed and jump reach"),
            ("LIFE", Life, "Expands maximum health durability pool"),
            ("CREDIT", Credit, "Emergency timeline energy reserve")
        ];

        for (int i = 0; i < stats.Length; i++)
        {
            bool isSelected = i == _selectedStat;
            int itemY = panelY + 120 + i * 75;

            if (isSelected)
            {
                Raylib.DrawRectangle(panelX + 30, itemY - 6, panelWidth - 60, 64, new Color(255, 120, 30, 45));
                Raylib.DrawRectangleLinesEx(new Rectangle(panelX + 30, itemY - 6, panelWidth - 60, 64), 2, Constants.NeonOrange);
            }

            Raylib.DrawText($"{stats[i].name,-8} :", panelX + 50, itemY, 26, isSelected ? Constants.C64White : Constants.C64LightGrey);
            Raylib.DrawText($"< {stats[i].val:D2} >", panelX + 220, itemY, 26, Constants.NeonCyan);

            // Bar indicator
            int barWidth = (int)(stats[i].val * 4.5f);
            Raylib.DrawRectangle(panelX + 330, itemY + 4, barWidth, 18, Constants.NeonCyan);
            Raylib.DrawRectangleLines(panelX + 330, itemY + 4, 230, 18, Constants.C64Grey);

            Raylib.DrawText(stats[i].desc, panelX + 50, itemY + 32, 17, Constants.C64Grey);
        }

        Raylib.DrawText("USE LEFT/RIGHT TO ADJUST  |  ENTER TO VALIDATE", panelX + 130, panelY + panelHeight - 45, 22, Constants.AccentGold);
    }
}
