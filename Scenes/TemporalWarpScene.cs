using System.Numerics;
using Raylib_cs;
using TheFifthAxis.Core;
using TheFifthAxis.Audio;
using TheFifthAxis.Graphics;
using TheFifthAxis.Entities;

namespace TheFifthAxis.Scenes;

public class TemporalHazard
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 Size;
    public Color Color;
    public float MinX;
    public float MaxX;
    public float MinY;
    public float MaxY;
    public bool Vertical;

    public Rectangle BoundingBox => new(Position.X - Size.X / 2f, Position.Y - Size.Y / 2f, Size.X, Size.Y);
}

public class TemporalWarpScene : IScene
{
    private readonly Player _player;
    private readonly int _eraIndex;
    private readonly SoundSynthesizer _audio;
    private readonly AssetManager _assets;
    private readonly Action<int> _onEraCompleted;
    private readonly Action _onGameOver;

    private float _timeRemaining = 55f;
    private readonly Vector2 _anachrononPos = new(2450f, 650f);
    private bool _anachrononCollected = false;
    private float _returnTimer = 0f;

    private readonly List<Rectangle> _platforms = new();
    private readonly List<TemporalHazard> _hazards = new();
    private Camera2D _camera;
    private const float WorldWidth = 2700f;

    // Era metadata extracted from C64 RAM
    private static readonly (string Name, string Date, string Time, string HazardName, Color SkyColor, Color GroundColor)[] Epochs = [
        ("THEBES : NEPHTYS TEMPLE", "JULY 4TH 1555 B.C.", "06H45 AM", "SPEAR TRAPS & FALLING PILLARS", new Color(180, 110, 40, 255), new Color(120, 80, 30, 255)),
        ("ATHENS AGORA", "JUNE 25TH 561 B.C.", "11H29 AM", "ROLLING CHARIOTS & JAVELINS", new Color(40, 120, 190, 255), new Color(160, 150, 130, 255)),
        ("MEMPHIS : AMASIS PALACE", "JULY 2ND 560 B.C.", "03H12 PM", "BURNING OIL & TEMPLE SPIKES", new Color(190, 90, 30, 255), new Color(110, 60, 20, 255)),
        ("LOCHES CLOISTER", "APRIL 12TH 512", "08H32 PM", "PENDULUM BLADES & TRAPDOORS", new Color(30, 35, 60, 255), new Color(60, 65, 80, 255)),
        ("APAMEA : PERGAMONS PALACE", "MAY 21TH 800", "02H30 PM", "GREEK FIRE & SPIKED GATES", new Color(140, 50, 80, 255), new Color(90, 40, 50, 255)),
        ("FLEURY ABBEY", "MARCH 25TH 999", "07H10 PM", "GARGOYLE PROJECTILES & EMBERS", new Color(25, 30, 50, 255), new Color(50, 55, 70, 255)),
        ("PARIS BANLIEUE SUD", "SEPTEMBER 3RD 1998", "00H12 AM", "HIGH-VOLTAGE WIRES & STEAM JETS", new Color(20, 25, 45, 255), new Color(55, 60, 75, 255)),
        ("NEW MARSEILLE", "MARCH 8TH 2038", "11H56 PM", "ACID VENTS & INDUSTRIAL SAWS", new Color(15, 40, 60, 255), new Color(35, 70, 90, 255)),
        ("SUBPACIFIC NORTH CITY", "DECEMBER 24TH 2201", "11H29 PM", "PRESSURIZED WATER & HYDRAULICS", new Color(10, 30, 70, 255), new Color(20, 50, 100, 255)),
        ("MARS : COPERNIC STATION", "OCTOBER 6TH 2320", "08H37 PM", "PLASMA ARCS & GRAVITY SPIKES", new Color(80, 25, 20, 255), new Color(130, 45, 35, 255))
    ];

    public TemporalWarpScene(Player player, int eraIndex, SoundSynthesizer audio, AssetManager assets,
                             Action<int> onEraCompleted, Action onGameOver)
    {
        _player = player;
        _eraIndex = Math.Clamp(eraIndex, 0, Epochs.Length - 1);
        _audio = audio;
        _assets = assets;
        _onEraCompleted = onEraCompleted;
        _onGameOver = onGameOver;

        _player.Position = new Vector2(150f, 750f);
        _player.Velocity = Vector2.Zero;

        InitEraObstacles();

        _camera = new Camera2D
        {
            Target = _player.Position,
            Offset = new Vector2(Constants.CanvasWidth / 2f, Constants.CanvasHeight / 2f),
            Rotation = 0f,
            Zoom = 1.0f
        };
    }

    private void InitEraObstacles()
    {
        // Ground & Platforms
        _platforms.Add(new Rectangle(0, 800, 500, 100));
        _platforms.Add(new Rectangle(650, 750, 350, 30));
        _platforms.Add(new Rectangle(1150, 680, 380, 30));
        _platforms.Add(new Rectangle(1650, 620, 360, 30));
        _platforms.Add(new Rectangle(2150, 700, 550, 100));

        // Stepping stones
        _platforms.Add(new Rectangle(520, 850, 100, 20));
        _platforms.Add(new Rectangle(1030, 820, 90, 20));
        _platforms.Add(new Rectangle(1550, 780, 80, 20));

        // Era Hazards
        Color hazardColor = Epochs[_eraIndex].HazardName.Contains("FIRE") || Epochs[_eraIndex].HazardName.Contains("PLASMA") ?
            Constants.NeonOrange : Constants.C64LightRed;

        // Moving hazard 1 (Horizontal patrol)
        _hazards.Add(new TemporalHazard
        {
            Position = new Vector2(800, 730),
            Velocity = new Vector2(180, 0),
            Size = new Vector2(36, 36),
            Color = hazardColor,
            MinX = 660,
            MaxX = 980,
            Vertical = false
        });

        // Moving hazard 2 (Vertical pendulum/crusher)
        _hazards.Add(new TemporalHazard
        {
            Position = new Vector2(1340, 500),
            Velocity = new Vector2(0, 220),
            Size = new Vector2(40, 40),
            Color = hazardColor,
            MinY = 460,
            MaxY = 660,
            Vertical = true
        });

        // Moving hazard 3 (Fast horizontal projectile)
        _hazards.Add(new TemporalHazard
        {
            Position = new Vector2(1800, 600),
            Velocity = new Vector2(240, 0),
            Size = new Vector2(44, 30),
            Color = hazardColor,
            MinX = 1660,
            MaxX = 1990,
            Vertical = false
        });

        // Acid/Chasm hazard at bottom
        _hazards.Add(new TemporalHazard
        {
            Position = new Vector2(WorldWidth / 2f, 960),
            Velocity = Vector2.Zero,
            Size = new Vector2(WorldWidth, 80),
            Color = Constants.C64LightRed,
            MinX = 0,
            MaxX = WorldWidth,
            Vertical = false
        });
    }

    public void Update(float dt)
    {
        // Countdown
        if (!_anachrononCollected)
        {
            _timeRemaining -= dt;
            if (_timeRemaining <= 0f)
            {
                _onGameOver();
                return;
            }
        }
        else
        {
            _returnTimer += dt;
            if (_returnTimer >= 3.0f)
            {
                _onEraCompleted(_eraIndex);
                return;
            }
        }

        // Update Player
        _player.Update(dt, _audio, _assets);

        // Platform Collisions
        _player.IsGrounded = false;
        foreach (var plat in _platforms)
        {
            if (_player.Velocity.Y >= 0f &&
                _player.Position.X >= plat.X && _player.Position.X <= plat.X + plat.Width &&
                _player.Position.Y >= plat.Y && _player.Position.Y <= plat.Y + 20f)
            {
                _player.Position.Y = plat.Y;
                _player.Velocity.Y = 0f;
                _player.IsGrounded = true;
            }
        }

        // Update Hazards
        foreach (var h in _hazards)
        {
            h.Position += h.Velocity * dt;
            if (h.Vertical)
            {
                if (h.Position.Y < h.MinY) { h.Position.Y = h.MinY; h.Velocity.Y *= -1; }
                else if (h.Position.Y > h.MaxY) { h.Position.Y = h.MaxY; h.Velocity.Y *= -1; }
            }
            else
            {
                if (h.Position.X < h.MinX) { h.Position.X = h.MinX; h.Velocity.X *= -1; }
                else if (h.Position.X > h.MaxX) { h.Position.X = h.MaxX; h.Velocity.X *= -1; }
            }

            // Hazard collision with player
            if (Raylib.CheckCollisionRecs(_player.BoundingBox, h.BoundingBox))
            {
                float dir = _player.Position.X > h.Position.X ? 1f : -1f;
                _player.TakeDamage(25f, dir, _audio, _assets);
            }
        }

        // Pit death check
        if (_player.Position.Y > 1000f)
        {
            _player.TakeDamage(100f, 0, _audio, _assets);
            if (_player.Health <= 0f)
            {
                _onGameOver();
                return;
            }
            _player.Position = new Vector2(150f, 750f);
        }

        // Collect Anachronon
        if (!_anachrononCollected && Vector2.Distance(_player.Position, _anachrononPos) < 65f)
        {
            _anachrononCollected = true;
            _audio.PlayAnachronon();
            _assets.SpawnBurst(_anachrononPos, Constants.AccentGold, 60, 350f);
        }

        // Camera follow
        float targetCamX = Math.Clamp(_player.Position.X, Constants.CanvasWidth / 2f, WorldWidth - Constants.CanvasWidth / 2f);
        _camera.Target = Vector2.Lerp(_camera.Target, new Vector2(targetCamX, 600f), 8f * dt);

        _assets.UpdateParticles(dt);
    }

    public void Draw()
    {
        Raylib.BeginMode2D(_camera);

        var era = Epochs[_eraIndex];

        // Atmosphere Backdrop
        Raylib.DrawRectangle(0, 0, (int)WorldWidth, 1200, era.SkyColor);

        // Distant Parallax Silhouettes (Pillars, Pyramids, Cathedrals, Domes, or Mars Craters)
        for (int i = 0; i < 8; i++)
        {
            int bgX = i * 360;
            Raylib.DrawTriangle(
                new Vector2(bgX + 180, 380),
                new Vector2(bgX, 800),
                new Vector2(bgX + 360, 800),
                new Color((byte)era.GroundColor.R, (byte)era.GroundColor.G, (byte)era.GroundColor.B, (byte)110)
            );
        }

        // Platforms & Stepping Stones
        foreach (var plat in _platforms)
        {
            Raylib.DrawRectangleRec(plat, era.GroundColor);
            Raylib.DrawRectangle((int)plat.X, (int)plat.Y, (int)plat.Width, 8, Constants.AccentGold);
            Raylib.DrawRectangleLinesEx(plat, 2, Constants.C64Black);
        }

        // Hazards
        foreach (var h in _hazards)
        {
            Raylib.DrawRectangleRec(h.BoundingBox, h.Color);
            Raylib.DrawRectangleLinesEx(h.BoundingBox, 2, Constants.C64White);
            Raylib.DrawCircle((int)h.Position.X, (int)h.Position.Y, 6, Constants.C64Yellow);
        }

        // The Anachronon Fragment (Golden Glowing Time Relic)
        if (!_anachrononCollected)
        {
            float bob = MathF.Sin((float)Raylib.GetTime() * 5f) * 10f;
            Vector2 aPos = _anachrononPos + new Vector2(0, bob);
            Raylib.DrawCircleV(aPos, 22, Constants.AccentGold);
            Raylib.DrawCircleV(aPos, 14, Constants.C64White);
            Raylib.DrawCircleLines((int)aPos.X, (int)aPos.Y, 28, Constants.NeonCyan);
            Raylib.DrawText("ANACHRONON", (int)(aPos.X - 60), (int)(aPos.Y - 45), 18, Constants.AccentGold);
        }

        // Player
        _player.Draw(_assets);

        // Particles
        _assets.DrawParticles();

        Raylib.EndMode2D();

        // HUD
        DrawHUD();

        // Era banner on entry / success
        if (_anachrononCollected)
        {
            int modalW = 860;
            int modalH = 260;
            int mx = (Constants.CanvasWidth - modalW) / 2;
            int my = (Constants.CanvasHeight - modalH) / 2;

            Raylib.DrawRectangle(mx, my, modalW, modalH, Constants.DarkPanel);
            Raylib.DrawRectangleLinesEx(new Rectangle(mx, my, modalW, modalH), 4, Constants.AccentGold);

            Raylib.DrawText("ANACHRONON RETRIEVED!", mx + 190, my + 45, 38, Constants.AccentGold);
            Raylib.DrawText($"TIME COORDINATE STABILIZED: {Epochs[_eraIndex].Name}", mx + 90, my + 110, 24, Constants.NeonCyan);
            Raylib.DrawText("DISCONNECTING IN 3 SECONDS...", mx + 240, my + 165, 22, Constants.C64White);
        }
    }

    private void DrawHUD()
    {
        Raylib.DrawRectangle(0, 0, Constants.CanvasWidth, 75, Constants.DarkPanel);
        Raylib.DrawLine(0, 75, Constants.CanvasWidth, 75, Constants.AccentGold);

        var era = Epochs[_eraIndex];
        Raylib.DrawText($"TEMPORAL ERA: {era.Name}", 40, 15, 24, Constants.AccentGold);
        Raylib.DrawText($"COORDINATES: {era.Date} {era.Time}", 40, 42, 18, Constants.C64LightGrey);

        // Countdown Timer
        Color timerColor = _timeRemaining < 15f ? Constants.C64LightRed : Constants.NeonGreen;
        Raylib.DrawText($"CHRONO TIME LIMIT: {_timeRemaining:F1}s", 900, 24, 28, timerColor);

        // Health
        Raylib.DrawText("HEALTH:", 1450, 26, 20, Constants.C64LightGrey);
        float hpPercent = Math.Clamp(_player.Health / _player.MaxHealth, 0f, 1f);
        Raylib.DrawRectangle(1540, 24, 180, 26, Constants.C64DarkGrey);
        Raylib.DrawRectangle(1540, 24, (int)(180 * hpPercent), 26, hpPercent > 0.3f ? Constants.NeonGreen : Constants.C64LightRed);
        Raylib.DrawRectangleLines(1540, 24, 180, 26, Constants.C64White);
    }
}
