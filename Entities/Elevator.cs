using System.Numerics;
using Raylib_cs;
using TheFifthAxis.Core;
using TheFifthAxis.Audio;

namespace TheFifthAxis.Entities;

public class Elevator
{
    public float X;
    public float CurrentY;
    public float TargetY;
    public float Width = 96f;
    public float Height = 110f;
    public readonly float[] FloorHeights;
    public int CurrentFloorIndex = 0;
    public bool IsMoving => MathF.Abs(CurrentY - TargetY) > 2f;

    public Rectangle CabinBox => new(X - Width / 2f, CurrentY - Height, Width, Height);

    public Elevator(float x, float[] floorHeights, int initialFloor = 0)
    {
        X = x;
        FloorHeights = floorHeights;
        CurrentFloorIndex = initialFloor;
        CurrentY = FloorHeights[initialFloor];
        TargetY = CurrentY;
    }

    public void MoveUp(SoundSynthesizer audio)
    {
        if (IsMoving) return;
        if (CurrentFloorIndex > 0)
        {
            CurrentFloorIndex--;
            TargetY = FloorHeights[CurrentFloorIndex];
            audio.PlayElevator();
        }
    }

    public void MoveDown(SoundSynthesizer audio)
    {
        if (IsMoving) return;
        if (CurrentFloorIndex < FloorHeights.Length - 1)
        {
            CurrentFloorIndex++;
            TargetY = FloorHeights[CurrentFloorIndex];
            audio.PlayElevator();
        }
    }

    public void Update(float dt, Player player)
    {
        float speed = 260f;
        if (CurrentY < TargetY)
        {
            CurrentY = MathF.Min(CurrentY + speed * dt, TargetY);
            if (Raylib.CheckCollisionPointRec(player.Position, CabinBox))
            {
                player.Position.Y = CurrentY;
                player.Velocity.Y = 0f;
                player.IsGrounded = true;
            }
        }
        else if (CurrentY > TargetY)
        {
            CurrentY = MathF.Max(CurrentY - speed * dt, TargetY);
            if (Raylib.CheckCollisionPointRec(player.Position, CabinBox))
            {
                player.Position.Y = CurrentY;
                player.Velocity.Y = 0f;
                player.IsGrounded = true;
            }
        }
    }

    public void Draw()
    {
        // Shaft Rails
        float topY = FloorHeights[0] - Height - 20f;
        float bottomY = FloorHeights[^1] + 20f;
        Raylib.DrawRectangle((int)(X - Width / 2f - 4), (int)topY, 4, (int)(bottomY - topY), Constants.C64Grey);
        Raylib.DrawRectangle((int)(X + Width / 2f), (int)topY, 4, (int)(bottomY - topY), Constants.C64Grey);

        // Cabin background
        Raylib.DrawRectangleRec(CabinBox, Constants.DarkPanel);

        // Platform floor
        Raylib.DrawRectangle((int)(X - Width / 2f), (int)(CurrentY - 6), (int)Width, 6, Constants.NeonCyan);

        // Cabin frame
        Raylib.DrawRectangleLinesEx(CabinBox, 3, Constants.C64LightBlue);

        // Indicator Light
        Color lightColor = IsMoving ? Constants.NeonOrange : Constants.NeonGreen;
        Raylib.DrawCircle((int)X, (int)(CurrentY - Height + 14), 5, lightColor);
    }
}
