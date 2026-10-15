using System.Numerics;
using Raylib_cs;
using TheFifthAxis.Core;
using TheFifthAxis.Audio;
using TheFifthAxis.Graphics;
using TheFifthAxis.Entities;

namespace TheFifthAxis.Scenes;

public class LaboratoryScene : IScene
{
    private readonly Player _player;
    private readonly SoundSynthesizer _audio;
    private readonly AssetManager _assets;
    private readonly Action<int> _onTriggerTeleport;
    private readonly Action _onGameOver;
    private readonly int _currentEra;

    private readonly List<Cyborg> _cyborgs = new();
    private readonly List<LaserBeam> _lasers = new();
    private readonly List<Elevator> _elevators = new();
    private readonly List<Rectangle> _platforms = new();
    private readonly List<(Vector2 Pos, bool Collected)> _energyCells = new();

    // 5th Axis Chrono-Meter (0 to 100%)
    public float ChronoMeter { get; private set; } = 25f;
    private const float MaxChrono = 100f;

    // Portal state
    private readonly Vector2 _portalPos = new(2400f, 390f);
    private float _portalPulse = 0f;

    // Camera
    private Camera2D _camera;
    private const float WorldWidth = 2800f;

    public LaboratoryScene(Player player, int startingEra, SoundSynthesizer audio, AssetManager assets,
                           Action<int> onTriggerTeleport, Action onGameOver)
    {
        _player = player;
        _currentEra = startingEra;
        _audio = audio;
        _assets = assets;
        _onTriggerTeleport = onTriggerTeleport;
        _onGameOver = onGameOver;

        InitLevel();

        _camera = new Camera2D
        {
            Target = _player.Position,
            Offset = new Vector2(Constants.CanvasWidth / 2f, Constants.CanvasHeight / 2f),
            Rotation = 0f,
            Zoom = 1.0f
        };
    }

    private void InitLevel()
    {
        // Floor Heights:
        // Floor 0 (Ground): Y = 880
        // Floor 1 (Mid):    Y = 640
        // Floor 2 (Top):    Y = 400

        // Main floor platforms
        _platforms.Add(new Rectangle(0, 880, WorldWidth, 80));      // Floor 0
        _platforms.Add(new Rectangle(200, 640, 1000, 24));          // Floor 1 Left
        _platforms.Add(new Rectangle(1400, 640, 1200, 24));         // Floor 1 Right
        _platforms.Add(new Rectangle(400, 400, 2200, 24));          // Floor 2 Top

        // Elevators
        float[] elevatorFloors = [400f, 640f, 880f];
        _elevators.Add(new Elevator(1250f, elevatorFloors, 2)); // Shaft 1
        _elevators.Add(new Elevator(2250f, elevatorFloors, 0)); // Shaft 2

        // Cyborg patrols
        _cyborgs.Add(new Cyborg(new Vector2(600, 880), 250));
        _cyborgs.Add(new Cyborg(new Vector2(1600, 880), 300));
        _cyborgs.Add(new Cyborg(new Vector2(700, 640), 220));
        _cyborgs.Add(new Cyborg(new Vector2(1800, 640), 250));
        _cyborgs.Add(new Cyborg(new Vector2(1000, 400), 260));
        _cyborgs.Add(new Cyborg(new Vector2(1900, 400), 280));

        // Energy Cells to collect
        _energyCells.Add((new Vector2(450, 830), false));
        _energyCells.Add((new Vector2(1100, 830), false));
        _energyCells.Add((new Vector2(500, 590), false));
        _energyCells.Add((new Vector2(1700, 590), false));
        _energyCells.Add((new Vector2(1300, 350), false));
        _energyCells.Add((new Vector2(2100, 350), false));
    }

    public void Update(float dt)
    {
        _portalPulse += dt * 3.5f;

        // Player Death check
        if (_player.Health <= 0f)
        {
            _onGameOver();
            return;
        }

        // Elevator controls
        bool up = Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W);
        bool down = Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S);

        _player.InElevator = false;
        foreach (var el in _elevators)
        {
            bool inCabin = Raylib.CheckCollisionPointRec(_player.Position, el.CabinBox);
            if (inCabin)
            {
                _player.InElevator = true;
                if (up) el.MoveUp(_audio);
                else if (down) el.MoveDown(_audio);
            }
            else if (el.IsInShaft(_player.Position))
            {
                int nearestFloor = el.GetNearestFloor(_player.Position.Y);
                if (nearestFloor >= 0 && (up || down))
                {
                    el.CallToFloor(nearestFloor, _audio);
                }
            }
            el.Update(dt, _player);
        }

        // Update Player
        _player.Update(dt, _audio, _assets);

        // Platform Collisions
        CheckPlatformCollisions();

        // Player Attacks vs Cyborgs
        var hitBox = _player.GetAttackHitbox();
        if (hitBox.HasValue)
        {
            foreach (var cyborg in _cyborgs)
            {
                if (!cyborg.IsDead && Raylib.CheckCollisionRecs(hitBox.Value, cyborg.BoundingBox))
                {
                    float dir = _player.FacingRight ? 1f : -1f;
                    cyborg.TakeDamage(_player.GetAttackDamage(), dir, _audio, _assets);
                    ChronoMeter = Math.Min(MaxChrono, ChronoMeter + 6f);
                }
            }
        }

        // Update Cyborgs
        foreach (var cyborg in _cyborgs)
        {
            cyborg.Update(dt, _player, _audio, _assets, _lasers);
        }

        // Update Lasers
        for (int i = _lasers.Count - 1; i >= 0; i--)
        {
            var laser = _lasers[i];
            laser.Update(dt, _assets);

            // Laser vs Player
            if (!laser.FromPlayer && Raylib.CheckCollisionRecs(laser.BoundingBox, _player.BoundingBox))
            {
                float dir = laser.Velocity.X > 0 ? 1f : -1f;
                _player.TakeDamage(18f, dir, _audio, _assets);
                laser.Destroyed = true;
            }

            if (laser.Destroyed || laser.Position.X < 0 || laser.Position.X > WorldWidth)
            {
                _lasers.RemoveAt(i);
            }
        }

        // Collect Energy Cells
        for (int i = 0; i < _energyCells.Count; i++)
        {
            var cell = _energyCells[i];
            if (!cell.Collected && Vector2.Distance(_player.Position - new Vector2(0, 40), cell.Pos) < 45f)
            {
                _energyCells[i] = (cell.Pos, true);
                ChronoMeter = Math.Min(MaxChrono, ChronoMeter + 15f);
                _player.Health = Math.Min(_player.MaxHealth, _player.Health + 20f);
                _audio.PlayAnachronon();
                _assets.SpawnBurst(cell.Pos, Constants.NeonCyan, 16, 200f);
            }
        }

        // Portal Activation & Transition to Phase 2
        if (ChronoMeter >= MaxChrono)
        {
            float distToPortal = Vector2.Distance(_player.Position, _portalPos);
            if (distToPortal < 60f)
            {
                _audio.PlayTeleport();
                _assets.SpawnBurst(_portalPos, Constants.NeonCyan, 40, 320f);
                _onTriggerTeleport(_currentEra);
            }
        }

        // Update Camera
        float targetCamX = Math.Clamp(_player.Position.X, Constants.CanvasWidth / 2f, WorldWidth - Constants.CanvasWidth / 2f);
        float targetCamY = Math.Clamp(_player.Position.Y - 100f, Constants.CanvasHeight / 2f, 700f);
        _camera.Target = Vector2.Lerp(_camera.Target, new Vector2(targetCamX, targetCamY), 8f * dt);

        // Update Particles
        _assets.UpdateParticles(dt);
    }

    private void CheckPlatformCollisions()
    {
        _player.IsGrounded = false;

        foreach (var plat in _platforms)
        {
            // Landing on top of platform
            if (_player.Velocity.Y >= 0f &&
                _player.Position.X >= plat.X && _player.Position.X <= plat.X + plat.Width &&
                _player.Position.Y >= plat.Y && _player.Position.Y <= plat.Y + 20f)
            {
                _player.Position.Y = plat.Y;
                _player.Velocity.Y = 0f;
                _player.IsGrounded = true;
            }
        }

        // World boundaries
        if (_player.Position.X < 30f) _player.Position.X = 30f;
        if (_player.Position.X > WorldWidth - 30f) _player.Position.X = WorldWidth - 30f;
    }

    public void Draw()
    {
        Raylib.BeginMode2D(_camera);

        // Futuristic Laboratory Wall Backdrop
        Raylib.DrawRectangle(0, 0, (int)WorldWidth, Constants.CanvasHeight + 200, new Color(14, 18, 28, 255));

        // High-tech architectural beams & wall panels
        for (int x = 0; x < WorldWidth; x += 180)
        {
            Raylib.DrawRectangle(x, 100, 16, 850, new Color(22, 28, 44, 255));
            Raylib.DrawRectangle(x + 4, 100, 8, 850, new Color(35, 45, 68, 255));
        }

        // Computer Terminals & Lore Displays
        DrawComputerTerminal(350, 880, "2066: MR.Z DETECTED");
        DrawComputerTerminal(1500, 880, "CHRONO-5 DAMAGED");
        DrawComputerTerminal(800, 640, "ANACHRONONS LOST");
        DrawComputerTerminal(1650, 400, "WARP PORTAL ONLINE");

        // Elevators
        foreach (var el in _elevators)
        {
            el.Draw();
            if (Raylib.CheckCollisionPointRec(_player.Position, el.CabinBox))
            {
                Raylib.DrawText("[UP / DOWN] RIDE LIFT", (int)(el.X - 90), (int)(el.CurrentY - el.Height - 28), 18, Constants.NeonGreen);
            }
            else if (el.IsInShaft(_player.Position))
            {
                int nearestFloor = el.GetNearestFloor(_player.Position.Y);
                if (nearestFloor >= 0 && nearestFloor != el.CurrentFloorIndex)
                {
                    Raylib.DrawText("[UP / DOWN] CALL LIFT", (int)(el.X - 90), (int)(_player.Position.Y - 110), 18, Constants.NeonOrange);
                }
            }
        }

        // Platforms & Steel Walkways
        foreach (var plat in _platforms)
        {
            Raylib.DrawRectangleRec(plat, new Color(30, 38, 56, 255));
            Raylib.DrawRectangle((int)plat.X, (int)plat.Y, (int)plat.Width, 6, Constants.NeonCyan);
            // Girders
            for (int gx = (int)plat.X; gx < plat.X + plat.Width; gx += 40)
            {
                Raylib.DrawLine(gx, (int)(plat.Y + 6), gx + 20, (int)(plat.Y + plat.Height), new Color(45, 58, 84, 255));
            }
        }

        // Energy Cells
        foreach (var cell in _energyCells)
        {
            if (!cell.Collected)
            {
                float floatBob = MathF.Sin((float)Raylib.GetTime() * 4f) * 6f;
                Vector2 drawPos = cell.Pos + new Vector2(0, floatBob);
                Raylib.DrawCircleV(drawPos, 14, Constants.NeonCyan);
                Raylib.DrawCircleV(drawPos, 8, Constants.C64White);
            }
        }

        // Temporal Portal
        DrawTemporalPortal();

        // Cyborgs
        foreach (var cyborg in _cyborgs)
        {
            cyborg.Draw(_assets);
        }

        // Player
        _player.Draw(_assets);

        // Lasers
        foreach (var laser in _lasers)
        {
            laser.Draw();
        }

        // Particles
        _assets.DrawParticles();

        Raylib.EndMode2D();

        // Draw HUD overlay (Fixed screen coordinates)
        DrawHUD();
    }

    private void DrawComputerTerminal(int x, int y, string label)
    {
        Raylib.DrawRectangle(x - 30, y - 65, 60, 65, Constants.DarkPanel);
        Raylib.DrawRectangleLines(x - 30, y - 65, 60, 65, Constants.NeonCyan);
        // Terminal screen
        Raylib.DrawRectangle(x - 24, y - 58, 48, 30, new Color(10, 40, 50, 255));
        Raylib.DrawText("SYS", x - 14, y - 50, 14, Constants.NeonCyan);
        Raylib.DrawText(label, x - 55, y - 82, 13, Constants.C64Yellow);
    }

    private void DrawTemporalPortal()
    {
        bool ready = ChronoMeter >= MaxChrono;
        Color ringColor = ready ? Constants.NeonCyan : Constants.C64DarkGrey;
        Color innerColor = ready ? Constants.NeonOrange : Constants.C64Black;

        float pulse = MathF.Sin(_portalPulse) * 8f;
        Raylib.DrawEllipse((int)_portalPos.X, (int)_portalPos.Y - 45, 34 + (int)pulse, 64 + (int)pulse, ringColor);
        Raylib.DrawEllipse((int)_portalPos.X, (int)_portalPos.Y - 45, 24, 52, innerColor);

        string prompt = ready ? "TEMPORAL WARP ACTIVE! STEP IN!" : $"CHRONO CHARGE: {(int)ChronoMeter}%";
        Color promptColor = ready ? Constants.NeonCyan : Constants.C64Grey;
        Raylib.DrawText(prompt, (int)(_portalPos.X - 120), (int)(_portalPos.Y - 140), 18, promptColor);
    }

    private void DrawHUD()
    {
        // Top status banner (Homage to C64 HUD: "2066 MR.Z - THE 5TH AXIS")
        Raylib.DrawRectangle(0, 0, Constants.CanvasWidth, 75, Constants.DarkPanel);
        Raylib.DrawLine(0, 75, Constants.CanvasWidth, 75, Constants.NeonCyan);

        Raylib.DrawText("YEAR 2066: MR.Z", 40, 22, 28, Constants.NeonOrange);

        // Player Health Bar
        Raylib.DrawText("HEALTH:", 360, 25, 22, Constants.C64LightGrey);
        float hpPercent = Math.Clamp(_player.Health / _player.MaxHealth, 0f, 1f);
        Raylib.DrawRectangle(460, 24, 240, 26, Constants.C64DarkGrey);
        Raylib.DrawRectangle(460, 24, (int)(240 * hpPercent), 26, hpPercent > 0.3f ? Constants.NeonGreen : Constants.C64LightRed);
        Raylib.DrawRectangleLines(460, 24, 240, 26, Constants.C64White);

        // 5th Axis Chrono-Meter
        Raylib.DrawText("5TH AXIS METER:", 760, 25, 22, Constants.C64LightGrey);
        float meterPercent = Math.Clamp(ChronoMeter / MaxChrono, 0f, 1f);
        Color meterColor = ChronoMeter >= MaxChrono ? Constants.NeonCyan : Constants.NeonOrange;
        Raylib.DrawRectangle(950, 24, 300, 26, Constants.C64DarkGrey);
        Raylib.DrawRectangle(950, 24, (int)(300 * meterPercent), 26, meterColor);
        Raylib.DrawRectangleLines(950, 24, 300, 26, Constants.C64White);
        Raylib.DrawText($"{(int)ChronoMeter}%", 1080, 27, 20, Constants.C64White);

        // Stats Display (F, A, L, C)
        Raylib.DrawText($"FORCE:{_player.Force}  AGILITY:{_player.Agility}  LIFE:{_player.Life}  CREDIT:{_player.Credit}",
            1320, 25, 20, Constants.AccentGold);
    }
}
