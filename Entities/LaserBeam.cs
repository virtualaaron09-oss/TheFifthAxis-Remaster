using System.Numerics;
using Raylib_cs;
using TheFifthAxis.Core;
using TheFifthAxis.Graphics;

namespace TheFifthAxis.Entities;

public class LaserBeam
{
    public Vector2 Position;
    public Vector2 Velocity;
    public bool FromPlayer;
    public bool Destroyed;
    public float Lifetime;
    private const float MaxLifetime = 3.0f;

    public Rectangle BoundingBox => new(Position.X - 14, Position.Y - 5, 28, 10);

    public LaserBeam(Vector2 pos, Vector2 vel, bool fromPlayer)
    {
        Position = pos;
        Velocity = vel;
        FromPlayer = fromPlayer;
    }

    public void Update(float dt, AssetManager assets)
    {
        Lifetime += dt;
        if (Lifetime >= MaxLifetime)
        {
            Destroyed = true;
            return;
        }

        Position += Velocity * dt;

        // Trail particles
        Color trailColor = FromPlayer ? Constants.NeonCyan : Constants.C64LightRed;
        assets.SpawnParticle(Position, -Velocity * 0.05f, trailColor, 3f, 0.15f);
    }

    public void Draw()
    {
        Color coreColor = Constants.C64White;
        Color glowColor = FromPlayer ? Constants.NeonCyan : Constants.NeonOrange;

        // Outer glow
        Raylib.DrawRectangle((int)(Position.X - 16), (int)(Position.Y - 6), 32, 12, new Color((byte)glowColor.R, (byte)glowColor.G, (byte)glowColor.B, (byte)120));
        // Inner core
        Raylib.DrawRectangle((int)(Position.X - 12), (int)(Position.Y - 3), 24, 6, coreColor);
    }
}
