using System.Numerics;
using Raylib_cs;
using TheFifthAxis.Core;
using TheFifthAxis.Audio;
using TheFifthAxis.Graphics;
using TheFifthAxis.Entities;
using TheFifthAxis.Scenes;

namespace TheFifthAxis;

public static class Program
{
    private static SoundSynthesizer _audio = null!;
    private static AssetManager _assets = null!;
    private static IScene _currentScene = null!;
    private static Player _activePlayer = null!;
    private static int _activeEra = 0;
    private static int _completedEras = 0;

    public static void Main()
    {
        // Configure Raylib window
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.Msaa4xHint);
        Raylib.InitWindow(1280, 720, "The 5th Axis (Le 5eme Axe) - C64 Remaster (1986-2026)");
        Raylib.SetWindowMinSize(960, 540);
        Raylib.SetTargetFPS(Constants.TargetFps);

        // Initialize Audio & Assets
        _audio = new SoundSynthesizer();
        _audio.Init();

        _assets = new AssetManager();
        _assets.Init();

        // Render target for virtual 1080p resolution
        RenderTexture2D target = Raylib.LoadRenderTexture(Constants.CanvasWidth, Constants.CanvasHeight);
        Raylib.SetTextureFilter(target.Texture, TextureFilter.Bilinear);

        // Start at Title Menu
        TransitionToMenu();

        // Main Game Loop
        while (!Raylib.WindowShouldClose())
        {
            float dt = Math.Clamp(Raylib.GetFrameTime(), 0.001f, 0.05f);

            // Update Audio Stream
            _audio.Update();

            // Toggle CRT Filter on F1
            if (Raylib.IsKeyPressed(KeyboardKey.F1))
            {
                _assets.CrtFilterEnabled = !_assets.CrtFilterEnabled;
            }

            // Toggle Audio Mute on M
            if (Raylib.IsKeyPressed(KeyboardKey.M))
            {
                _audio.MusicEnabled = !_audio.MusicEnabled;
            }

            // Update active scene
            _currentScene.Update(dt);

            // Render to virtual 1080p target
            Raylib.BeginTextureMode(target);
            Raylib.ClearBackground(Constants.DarkBackground);
            _currentScene.Draw();
            _assets.DrawCrtOverlay();
            Raylib.EndTextureMode();

            // Draw scaled target to actual window with letterboxing
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Constants.C64Black);

            float scale = Math.Min((float)Raylib.GetScreenWidth() / Constants.CanvasWidth,
                                   (float)Raylib.GetScreenHeight() / Constants.CanvasHeight);

            float renderWidth = Constants.CanvasWidth * scale;
            float renderHeight = Constants.CanvasHeight * scale;
            float offsetX = (Raylib.GetScreenWidth() - renderWidth) / 2f;
            float offsetY = (Raylib.GetScreenHeight() - renderHeight) / 2f;

            Rectangle srcRec = new(0, 0, target.Texture.Width, -target.Texture.Height);
            Rectangle destRec = new(offsetX, offsetY, renderWidth, renderHeight);

            Raylib.DrawTexturePro(target.Texture, srcRec, destRec, Vector2.Zero, 0f, Color.White);

            Raylib.EndDrawing();
        }

        // Cleanup
        Raylib.UnloadRenderTexture(target);
        _assets.Dispose();
        _audio.Dispose();
        Raylib.CloseWindow();
    }

    private static void TransitionToMenu()
    {
        _currentScene = new MenuScene(StartGame, _audio, _assets);
    }

    private static void StartGame(Player player, int startingEra)
    {
        _activePlayer = player;
        _activeEra = startingEra;
        _completedEras = 0;
        _currentScene = new LaboratoryScene(_activePlayer, _activeEra, _audio, _assets, TriggerTeleport, GameOver);
    }

    private static void TriggerTeleport(int era)
    {
        _currentScene = new TemporalWarpScene(_activePlayer, era, _audio, _assets, EraCompleted, GameOver);
    }

    private static void EraCompleted(int completedEra)
    {
        _completedEras++;
        if (_completedEras >= 10)
        {
            _currentScene = new VictoryScene(TransitionToMenu, _audio);
        }
        else
        {
            // Advance to next era and return to lab
            _activeEra = (completedEra + 1) % 10;
            _activePlayer.ResetHealth();
            _currentScene = new LaboratoryScene(_activePlayer, _activeEra, _audio, _assets, TriggerTeleport, GameOver);
        }
    }

    private static void GameOver()
    {
        _currentScene = new GameOverScene(
            () => {
                _activePlayer.ResetHealth();
                _currentScene = new LaboratoryScene(_activePlayer, _activeEra, _audio, _assets, TriggerTeleport, GameOver);
            },
            TransitionToMenu,
            _audio
        );
    }
}
