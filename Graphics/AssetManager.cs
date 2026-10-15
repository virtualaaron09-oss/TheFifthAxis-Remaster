using System.Numerics;
using Raylib_cs;
using TheFifthAxis.Core;

namespace TheFifthAxis.Graphics;

public class Particle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Color Color;
    public float Size;
    public float Lifetime;
    public float MaxLifetime;
}

public class AssetManager : IDisposable
{
    private readonly Dictionary<string, Texture2D> _textures = new();
    private readonly List<Particle> _particles = new();
    public bool CrtFilterEnabled { get; set; } = false;

    public void Init()
    {
        string assetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sprites");
        if (!Directory.Exists(assetPath))
        {
            assetPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Sprites");
        }

        if (Directory.Exists(assetPath))
        {
            var files = Directory.GetFiles(assetPath, "*.png");
            foreach (var file in files)
            {
                string key = Path.GetFileNameWithoutExtension(file);
                var tex = Raylib.LoadTexture(file);
                if (tex.Id > 0)
                {
                    _textures[key] = tex;
                }
            }
        }
    }

    public Texture2D? GetTexture(string key)
    {
        return _textures.TryGetValue(key, out var tex) ? tex : null;
    }

    public void SpawnParticle(Vector2 pos, Vector2 vel, Color color, float size, float lifetime)
    {
        _particles.Add(new Particle
        {
            Position = pos,
            Velocity = vel,
            Color = color,
            Size = size,
            Lifetime = 0f,
            MaxLifetime = lifetime
        });
    }

    public void SpawnBurst(Vector2 pos, Color color, int count, float speed)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = (float)(Random.Shared.NextDouble() * Math.PI * 2.0);
            float spd = speed * (0.3f + 0.7f * (float)Random.Shared.NextDouble());
            Vector2 vel = new(MathF.Cos(angle) * spd, MathF.Sin(angle) * spd);
            SpawnParticle(pos, vel, color, Random.Shared.Next(3, 8), (float)(0.3 + Random.Shared.NextDouble() * 0.4));
        }
    }

    public void UpdateParticles(float dt)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Lifetime += dt;
            if (p.Lifetime >= p.MaxLifetime)
            {
                _particles.RemoveAt(i);
                continue;
            }

            p.Position += p.Velocity * dt;
            p.Velocity.Y += 500f * dt; // Particle gravity
        }
    }

    public void DrawParticles()
    {
        foreach (var p in _particles)
        {
            float alpha = 1f - (p.Lifetime / p.MaxLifetime);
            Color c = new(p.Color.R, p.Color.G, p.Color.B, (byte)(p.Color.A * alpha));
            Raylib.DrawCircleV(p.Position, p.Size * alpha, c);
        }
    }

    public void DrawCrtOverlay()
    {
        if (!CrtFilterEnabled) return;

        // Subtle horizontal scanlines
        for (int y = 0; y < Constants.CanvasHeight; y += 4)
        {
            Raylib.DrawRectangle(0, y, Constants.CanvasWidth, 2, new Color(0, 0, 0, 45));
        }

        // Vignette shadow around borders
        Raylib.DrawRectangleGradientV(0, 0, Constants.CanvasWidth, 80, new Color(0, 0, 0, 160), new Color(0, 0, 0, 0));
        Raylib.DrawRectangleGradientV(0, Constants.CanvasHeight - 80, Constants.CanvasWidth, 80, new Color(0, 0, 0, 0), new Color(0, 0, 0, 160));
        Raylib.DrawRectangleGradientH(0, 0, 80, Constants.CanvasHeight, new Color(0, 0, 0, 160), new Color(0, 0, 0, 0));
        Raylib.DrawRectangleGradientH(Constants.CanvasWidth - 80, 0, 80, Constants.CanvasHeight, new Color(0, 0, 0, 0), new Color(0, 0, 0, 160));
    }

    public void Dispose()
    {
        foreach (var tex in _textures.Values)
        {
            Raylib.UnloadTexture(tex);
        }
        _textures.Clear();
        GC.SuppressFinalize(this);
    }
}
