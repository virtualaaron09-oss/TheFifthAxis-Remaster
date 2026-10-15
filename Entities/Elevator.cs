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
    public float Width = 110f;
    public float Height = 120f;
    public readonly float[] FloorHeights;
    public int CurrentFloorIndex = 0;
    public bool IsMoving => MathF.Abs(CurrentY - TargetY) > 1f;

    public Rectangle CabinBox => new(X - Width / 2f, CurrentY - Height, Width, Height);

    public Elevator(float x, float[] floorHeights, int initialFloor = 0)
    {
        X = x;
        FloorHeights = floorHeights;
        CurrentFloorIndex = initialFloor;
        CurrentY = FloorHeights[initialFloor];
        TargetY = CurrentY;
    }

    public bool IsPlayerInside(Vector2 playerPos)
    {
        bool withinX = MathF.Abs(playerPos.X - X) <= Width / 2f;
        bool withinY = playerPos.Y >= CurrentY - Height && playerPos.Y <= CurrentY + 18f;
        return withinX && withinY;
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

    public void CallToFloor(int floorIndex, SoundSynthesizer audio)
    {
        if (IsMoving) return;
        if (floorIndex >= 0 && floorIndex < FloorHeights.Length && floorIndex != CurrentFloorIndex)
        {
            CurrentFloorIndex = floorIndex;
            TargetY = FloorHeights[floorIndex];
            audio.PlayElevator();
        }
    }

    public bool IsInShaft(Vector2 pos)
    {
        return MathF.Abs(pos.X - X) <= Width / 2f + 10f;
    }

    public int GetNearestFloor(float y)
    {
        for (int i = 0; i < FloorHeights.Length; i++)
        {
            if (MathF.Abs(y - FloorHeights[i]) < 45f)
            {
                return i;
            }
        }
        return -1;
    }

    public void Update(float dt, Player player)
    {
        float speed = 280f;
        if (CurrentY < TargetY)
        {
            CurrentY = MathF.Min(CurrentY + speed * dt, TargetY);
        }
        else if (CurrentY > TargetY)
        {
            CurrentY = MathF.Max(CurrentY - speed * dt, TargetY);
        }

        // Lock player position to elevator cabin floor
        if (IsPlayerInside(player.Position))
        {
            player.InElevator = true;
            player.Position.Y = CurrentY;
            player.Velocity.Y = 0f;
            player.IsGrounded = true;

            // Prevent falling off while moving between floors
            if (IsMoving)
            {
                player.Position.X = Math.Clamp(player.Position.X, X - Width / 2f + 16f, X + Width / 2f - 16f);
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

        // Indicator Light (Green when idle, Pulsing Orange when moving)
        Color lightColor = IsMoving ? Constants.NeonOrange : Constants.NeonGreen;
        Raylib.DrawCircle((int)X, (int)(CurrentY - Height + 14), 6, lightColor);
    }
}
