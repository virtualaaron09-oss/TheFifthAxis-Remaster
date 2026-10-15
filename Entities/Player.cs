using System.Numerics;
using Raylib_cs;
using TheFifthAxis.Core;
using TheFifthAxis.Audio;
using TheFifthAxis.Graphics;

namespace TheFifthAxis.Entities;

public enum PlayerState
{
    Idle,
    Walking,
    Ducking,
    Jumping,
    Punching,
    Kicking,
    Hurt,
    Teleporting
}

public class Player
{
    // C64 Attributes
    public int Force { get; set; } = 25;
    public int Agility { get; set; } = 25;
    public int Life { get; set; } = 25;
    public int Credit { get; set; } = 25;

    public float Health { get; set; }
    public float MaxHealth => Life * 4f;

    // Physics
    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 Size = new(48, 84);
    public bool FacingRight = true;
    public bool IsGrounded;
    public PlayerState State = PlayerState.Idle;

    private float _actionTimer;
    private float _invulnTimer;
    private float _animTimer;
    private int _animFrame;

    public Rectangle BoundingBox => new(Position.X - Size.X / 2f, Position.Y - Size.Y, Size.X, Size.Y);

    public Player(Vector2 startPos)
    {
        Position = startPos;
        Health = MaxHealth;
    }

    public void ResetHealth()
    {
        Health = MaxHealth;
    }

    public void Update(float dt, SoundSynthesizer audio, AssetManager assets)
    {
        if (_invulnTimer > 0f) _invulnTimer -= dt;
        if (_actionTimer > 0f)
        {
            _actionTimer -= dt;
            if (_actionTimer <= 0f)
            {
                State = IsGrounded ? PlayerState.Idle : PlayerState.Jumping;
            }
        }

        float moveSpeed = Constants.BaseMoveSpeed * (0.8f + (Agility / 50f) * 0.4f);
        float jumpImpulse = Constants.JumpSpeed * (0.85f + (Agility / 50f) * 0.3f);

        // Control input during non-attack states
        if (State != PlayerState.Punching && State != PlayerState.Kicking && State != PlayerState.Teleporting)
        {
            bool left = Raylib.IsKeyDown(KeyboardKey.Left) || Raylib.IsKeyDown(KeyboardKey.A);
            bool right = Raylib.IsKeyDown(KeyboardKey.Right) || Raylib.IsKeyDown(KeyboardKey.D);
            bool down = Raylib.IsKeyDown(KeyboardKey.Down) || Raylib.IsKeyDown(KeyboardKey.S);
            bool jump = Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W) || Raylib.IsKeyPressed(KeyboardKey.Space);

            bool punch = Raylib.IsKeyPressed(KeyboardKey.Z) || Raylib.IsKeyPressed(KeyboardKey.J);
            bool kick = Raylib.IsKeyPressed(KeyboardKey.X) || Raylib.IsKeyPressed(KeyboardKey.K);

            if (punch)
            {
                State = PlayerState.Punching;
                _actionTimer = 0.20f;
                audio.PlayPunch();
                Velocity.X *= 0.3f;
            }
            else if (kick)
            {
                State = PlayerState.Kicking;
                _actionTimer = 0.28f;
                audio.PlayKick();
                Velocity.X *= 0.4f;
            }
            else if (down && IsGrounded)
            {
                State = PlayerState.Ducking;
                Velocity.X = 0f;
            }
            else
            {
                if (left)
                {
                    Velocity.X = -moveSpeed;
                    FacingRight = false;
                    if (IsGrounded) State = PlayerState.Walking;
                }
                else if (right)
                {
                    Velocity.X = moveSpeed;
                    FacingRight = true;
                    if (IsGrounded) State = PlayerState.Walking;
                }
                else
                {
                    Velocity.X = 0f;
                    if (IsGrounded) State = PlayerState.Idle;
                }

                if (jump && IsGrounded)
                {
                    Velocity.Y = -jumpImpulse;
                    IsGrounded = false;
                    State = PlayerState.Jumping;
                }
            }
        }

        // Apply Gravity
        if (!IsGrounded)
        {
            Velocity.Y += Constants.Gravity * dt;
        }

        // Apply Movement
        Position += Velocity * dt;

        // Animation ticker
        _animTimer += dt;
        if (_animTimer >= 0.12f)
        {
            _animTimer = 0f;
            _animFrame = (_animFrame + 1) % 4;
        }
    }

    public void TakeDamage(float amount, float knockbackDir, SoundSynthesizer audio, AssetManager assets)
    {
        if (_invulnTimer > 0f) return;
        Health -= amount;
        _invulnTimer = 0.6f;
        Velocity.X = knockbackDir * 250f;
        Velocity.Y = -200f;
        IsGrounded = false;
        audio.PlayHit();
        assets.SpawnBurst(Position - new Vector2(0, Size.Y / 2f), Constants.C64LightRed, 12, 180f);
    }

    public Rectangle? GetAttackHitbox()
    {
        if (State == PlayerState.Punching)
        {
            float hitWidth = 36f;
            float hitX = FacingRight ? Position.X : Position.X - hitWidth;
            return new Rectangle(hitX, Position.Y - Size.Y * 0.75f, hitWidth, 28f);
        }
        if (State == PlayerState.Kicking)
        {
            float hitWidth = 48f;
            float hitX = FacingRight ? Position.X : Position.X - hitWidth;
            return new Rectangle(hitX, Position.Y - Size.Y * 0.50f, hitWidth, 32f);
        }
        return null;
    }

    public float GetAttackDamage()
    {
        float baseDmg = State == PlayerState.Kicking ? 18f : 12f;
        return baseDmg * (0.8f + (Force / 50f) * 0.6f);
    }

    public void Draw(AssetManager assets)
    {
        // Flash when invulnerable
        if (_invulnTimer > 0f && ((int)(_invulnTimer * 20f) % 2 == 0))
        {
            return;
        }

        // Draw Player Character (Modernized clean hero art with cyberpunk suit)
        float x = Position.X;
        float y = Position.Y;
        int dir = FacingRight ? 1 : -1;

        // Shadow
        Raylib.DrawEllipse((int)x, (int)y, 22, 6, new Color(0, 0, 0, 90));

        // Suit Colors
        Color suitColor = Constants.NeonCyan;
        Color armorColor = Constants.C64White;
        Color skinColor = new(245, 195, 160, 255);
        Color bootsColor = Constants.C64Blue;

        if (State == PlayerState.Ducking)
        {
            // Ducking pose
            Raylib.DrawRectangle((int)(x - 18), (int)(y - 48), 36, 44, suitColor);
            Raylib.DrawRectangle((int)(x - 14), (int)(y - 44), 28, 20, armorColor);
            Raylib.DrawCircle((int)(x + dir * 6), (int)(y - 54), 14, skinColor);
            Raylib.DrawRectangle((int)(x - 18), (int)(y - 12), 36, 12, bootsColor);
        }
        else
        {
            // Torso & Armor
            float bob = State == PlayerState.Walking ? MathF.Sin(_animFrame * MathF.PI / 2f) * 3f : 0f;
            float torsoY = y - 76 + bob;

            // Head
            Raylib.DrawCircle((int)x, (int)(torsoY - 14), 14, skinColor);
            // Visor / Hair
            Raylib.DrawRectangle((int)(x - 10), (int)(torsoY - 26), 20, 8, Constants.C64DarkGrey);
            Raylib.DrawRectangle((int)(x + (dir > 0 ? 0 : -8)), (int)(torsoY - 16), 8, 4, Constants.NeonOrange);

            // Torso
            Raylib.DrawRectangle((int)(x - 16), (int)torsoY, 32, 34, suitColor);
            Raylib.DrawRectangle((int)(x - 12), (int)(torsoY + 4), 24, 22, armorColor);

            // Arms / Punching pose
            if (State == PlayerState.Punching)
            {
                Raylib.DrawRectangle((int)(x + (dir > 0 ? 8 : -38)), (int)(torsoY + 8), 30, 10, suitColor);
                Raylib.DrawCircle((int)(x + (dir > 0 ? 38 : -38)), (int)(torsoY + 13), 8, skinColor);
            }
            else
            {
                float armSwing = State == PlayerState.Walking ? MathF.Cos(_animFrame * MathF.PI / 2f) * 12f : 0f;
                Raylib.DrawRectangle((int)(x - 20), (int)(torsoY + 6 + armSwing), 8, 24, suitColor);
                Raylib.DrawRectangle((int)(x + 12), (int)(torsoY + 6 - armSwing), 8, 24, suitColor);
            }

            // Legs / Kicking pose
            if (State == PlayerState.Kicking)
            {
                // Standing leg
                Raylib.DrawRectangle((int)(x - 8), (int)(torsoY + 34), 12, 36, suitColor);
                Raylib.DrawRectangle((int)(x - 10), (int)(torsoY + 62), 16, 14, bootsColor);
                // Kicking leg outstretched
                Raylib.DrawRectangle((int)(x + (dir > 0 ? 4 : -44)), (int)(torsoY + 28), 40, 12, suitColor);
                Raylib.DrawRectangle((int)(x + (dir > 0 ? 40 : -48)), (int)(torsoY + 26), 14, 16, bootsColor);
            }
            else
            {
                float legSwing = State == PlayerState.Walking ? MathF.Sin(_animFrame * MathF.PI / 2f) * 14f : 0f;
                Raylib.DrawRectangle((int)(x - 14), (int)(torsoY + 34 + legSwing), 12, 32, suitColor);
                Raylib.DrawRectangle((int)(x + 2), (int)(torsoY + 34 - legSwing), 12, 32, suitColor);
                Raylib.DrawRectangle((int)(x - 16), (int)(torsoY + 60 + legSwing), 14, 14, bootsColor);
                Raylib.DrawRectangle((int)(x), (int)(torsoY + 60 - legSwing), 14, 14, bootsColor);
            }
        }
    }
}
