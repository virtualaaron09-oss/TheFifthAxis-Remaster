using System.Numerics;
using Raylib_cs;
using TheFifthAxis.Core;
using TheFifthAxis.Audio;
using TheFifthAxis.Graphics;

namespace TheFifthAxis.Entities;

public enum CyborgState
{
    Patrol,
    Alert,
    Attack,
    Stunned,
    Dead
}

public class Cyborg
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 Size = new(48, 86);
    public bool FacingRight;
    public CyborgState State = CyborgState.Patrol;

    public float Health = 45f;
    public float MaxHealth = 45f;
    public bool IsDead => State == CyborgState.Dead;

    private readonly float _leftBound;
    private readonly float _rightBound;
    private float _attackCooldown;
    private float _stunTimer;
    private float _animTimer;
    private int _animFrame;

    public Rectangle BoundingBox => new(Position.X - Size.X / 2f, Position.Y - Size.Y, Size.X, Size.Y);

    public Cyborg(Vector2 pos, float patrolRange)
    {
        Position = pos;
        _leftBound = pos.X - patrolRange;
        _rightBound = pos.X + patrolRange;
        FacingRight = Random.Shared.Next(2) == 0;
    }

    public void Update(float dt, Player player, SoundSynthesizer audio, AssetManager assets, List<LaserBeam> lasers)
    {
        if (State == CyborgState.Dead) return;

        if (_stunTimer > 0f)
        {
            _stunTimer -= dt;
            if (_stunTimer <= 0f) State = CyborgState.Patrol;
            return;
        }

        if (_attackCooldown > 0f) _attackCooldown -= dt;

        float distToPlayer = Vector2.Distance(Position, player.Position);
        bool sameLevel = MathF.Abs(Position.Y - player.Position.Y) < 60f;

        // AI Logic
        if (sameLevel && distToPlayer < 400f)
        {
            State = CyborgState.Alert;
            FacingRight = player.Position.X > Position.X;

            // Attack if in range
            if (distToPlayer < 75f && _attackCooldown <= 0f)
            {
                // Melee strike
                _attackCooldown = 1.2f;
                float knockback = FacingRight ? 1f : -1f;
                player.TakeDamage(14f, knockback, audio, assets);
                audio.PlayHit();
            }
            else if (distToPlayer > 120f && distToPlayer < 350f && _attackCooldown <= 0f)
            {
                // Ranged laser attack
                _attackCooldown = 2.0f;
                Vector2 spawnPos = new(Position.X + (FacingRight ? 26f : -26f), Position.Y - 50f);
                Vector2 laserVel = new((FacingRight ? 1f : -1f) * 650f, 0f);
                lasers.Add(new LaserBeam(spawnPos, laserVel, false));
                audio.PlayLaser();
            }
            else if (distToPlayer >= 75f)
            {
                // Pursue player
                float speed = 140f;
                Position.X += (FacingRight ? 1f : -1f) * speed * dt;
            }
        }
        else
        {
            // Standard Patrol
            State = CyborgState.Patrol;
            float patrolSpeed = 90f;
            Position.X += (FacingRight ? 1f : -1f) * patrolSpeed * dt;

            if (Position.X < _leftBound)
            {
                Position.X = _leftBound;
                FacingRight = true;
            }
            else if (Position.X > _rightBound)
            {
                Position.X = _rightBound;
                FacingRight = false;
            }
        }

        _animTimer += dt;
        if (_animTimer >= 0.15f)
        {
            _animTimer = 0f;
            _animFrame = (_animFrame + 1) % 4;
        }
    }

    public void TakeDamage(float amount, float knockbackDir, SoundSynthesizer audio, AssetManager assets)
    {
        if (State == CyborgState.Dead) return;

        Health -= amount;
        _stunTimer = 0.25f;
        State = CyborgState.Stunned;
        Position.X += knockbackDir * 18f;

        audio.PlayHit();
        assets.SpawnBurst(Position - new Vector2(0, Size.Y / 2f), Constants.NeonOrange, 10, 160f);

        if (Health <= 0f)
        {
            State = CyborgState.Dead;
            audio.PlayExplosion();
            assets.SpawnBurst(Position - new Vector2(0, Size.Y / 2f), Constants.C64LightRed, 25, 260f);
            assets.SpawnBurst(Position - new Vector2(0, Size.Y / 2f), Constants.C64Yellow, 15, 200f);
        }
    }

    public void Draw(AssetManager assets)
    {
        if (State == CyborgState.Dead) return;

        float x = Position.X;
        float y = Position.Y;
        int dir = FacingRight ? 1 : -1;

        // Shadow
        Raylib.DrawEllipse((int)x, (int)y, 22, 6, new Color(0, 0, 0, 90));

        // Chrome & Red Metallic Body
        Color bodyColor = State == CyborgState.Stunned ? Constants.C64LightRed : Constants.C64DarkGrey;
        Color armorColor = State == CyborgState.Stunned ? Constants.C64White : Constants.C64LightGrey;
        Color eyeColor = State == CyborgState.Alert ? Constants.C64LightRed : Constants.NeonOrange;

        float torsoY = y - 76;

        // Head (Angular robot skull)
        Raylib.DrawRectangle((int)(x - 12), (int)(torsoY - 26), 24, 22, bodyColor);
        // Glowing Visor / Red Eye
        Raylib.DrawRectangle((int)(x + (dir > 0 ? 0 : -10)), (int)(torsoY - 18), 10, 5, eyeColor);

        // Torso with armor plate
        Raylib.DrawRectangle((int)(x - 16), (int)torsoY, 32, 34, bodyColor);
        Raylib.DrawRectangle((int)(x - 12), (int)(torsoY + 4), 24, 24, armorColor);

        // Red Power Core in chest
        Raylib.DrawCircle((int)x, (int)(torsoY + 16), 5, eyeColor);

        // Arms (Cannon or hydraulic fist)
        float armSwing = MathF.Sin(_animFrame * MathF.PI / 2f) * 8f;
        Raylib.DrawRectangle((int)(x - 20), (int)(torsoY + 4 + armSwing), 8, 28, armorColor);
        Raylib.DrawRectangle((int)(x + 12), (int)(torsoY + 4 - armSwing), 8, 28, armorColor);

        // Legs
        float legSwing = MathF.Cos(_animFrame * MathF.PI / 2f) * 10f;
        Raylib.DrawRectangle((int)(x - 14), (int)(torsoY + 34 + legSwing), 10, 38, bodyColor);
        Raylib.DrawRectangle((int)(x + 4), (int)(torsoY + 34 - legSwing), 10, 38, bodyColor);

        // Health Bar above Cyborg
        float barWidth = 44f;
        float hpPercent = Math.Clamp(Health / MaxHealth, 0f, 1f);
        Raylib.DrawRectangle((int)(x - barWidth / 2f), (int)(torsoY - 36), (int)barWidth, 5, Constants.C64DarkGrey);
        Raylib.DrawRectangle((int)(x - barWidth / 2f), (int)(torsoY - 36), (int)(barWidth * hpPercent), 5, Constants.C64LightRed);
    }
}
