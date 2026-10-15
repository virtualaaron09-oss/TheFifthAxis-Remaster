# The 5th Axis (Le 5ème Axe) - C# .NET 10 Remaster

A complete reverse-engineered modern source port and remaster of the 1986 Commodore 64 classic **The 5th Axis** (*Le 5ème Axe*, Loriciels), converted from MOS 6502 machine code into C# (.NET 10) with hardware-accelerated high-resolution rendering, smooth 60+ FPS sub-pixel animation, and remastered polyphonic audio.

---

## 🕹️ Background & Reverse Engineering

The original game was reverse engineered directly from the VICE snapshot file `5thaxisSNAP.vsf`:
* **Memory & State Extraction**:
  * Unpacked the 64 KB C64 RAM dump from the snapshot `C64MEM` module.
  * Extracted VIC-II Bank 3 display matrices, multicolor player & cyborg sprite sheets, and projectile patterns.
  * Disassembled the raster interrupt handler at `$4B17` (line `$64`) and secondary IRQ multiplexer at `$57F3`.
  * Reverse engineered Gilles Soulet's custom SID sound driver and pattern tables at `$0900`-`$0E00`.
  * Extracted the 10 historical space-time coordinate locations from memory (`$4C90`-`$4E80`).

---

## ✨ Modern Remaster Features

| Feature | Original Commodore 64 (1986) | Modern C# Remaster (2026) |
| :--- | :--- | :--- |
| **Resolution** | 320×200 (160×200 multicolor) | **Native 1920×1080 Full HD** with virtual letterboxed scaling |
| **Frame Rate** | 50 Hz PAL / 60 Hz NTSC (1px stepped) | **Smooth 60+ FPS sub-pixel floating-point physics** |
| **Display Modes** | Fixed CRT output | **Modern HD Cyberpunk** vs. **Retro CRT Scanlines [F1]** |
| **Audio** | 3-voice 4-bit mono MOS 6581/8580 SID | **44.1 kHz stereo polyphonic synthesizer & dynamic SFX** |
| **Combat** | Integer grid hitboxes | **Sub-pixel combat** (Punch, Kick, Duck, Jump Kick) |
| **Architecture** | 6502 assembly language | **Clean, modular C# (.NET 10) & Raylib-cs** |

---

## 🎮 Controls

| Action | Primary Keyboard | Secondary Keyboard |
| :--- | :--- | :--- |
| **Move Left / Right** | `Left Arrow` / `Right Arrow` | `A` / `D` |
| **Duck** | `Down Arrow` | `S` |
| **Jump** | `Up Arrow` / `Space` | `W` |
| **Punch** (Fast Attack) | `Z` | `J` |
| **Kick** (Heavy Knockback) | `X` | `K` |
| **Use Elevator** | `Up` / `Down` inside shaft | `W` / `S` |
| **Toggle CRT Scanlines**| `F1` | - |
| **Toggle Music Mute** | `M` | - |

---

## 🗺️ Game Phases

### Phase 1: The Time Laboratory (Facility Infiltration)
* Explore the multi-tiered research complex using functional elevators and platforms.
* Defeat malfunctioning cyborgs and collect scattered energy cells.
* Charge your **Fifth Axis / Chrono-Meter** to 100% to activate the **Chrono-5 Temporal Teleportation Portal**.

### Phase 2: Temporal Teleportation (The Anachronon Hunt)
Teleport across 10 historical time coordinates to retrieve scattered Anachronon pieces:
1. **Thebes : Nephtys Temple** (July 4th 1555 B.C.)
2. **Athens Agora** (June 25th 561 B.C.)
3. **Memphis : Amasis Palace** (July 2nd 560 B.C.)
4. **Loches Cloister** (April 12th 512 A.D.)
5. **Apamea : Pergamons Palace** (May 21st 800 A.D.)
6. **Fleury Abbey** (March 25th 999 A.D.)
7. **Paris Banlieue Sud** (September 3rd 1998 A.D.)
8. **New Marseille** (March 8th 2038 A.D.)
9. **Subpacific North City** (December 24th 2201 A.D.)
10. **Mars : Copernic Station** (October 6th 2320 A.D.)

Collect all 10 Anachronons to stabilize reality and save the universe!

---

## 🚀 How to Run

### Option 1: From the Terminal
```powershell
cd C:\Users\fancy\.gemini\antigravity\scratch\TheFifthAxis
dotnet run
```

### Option 2: Direct Binary Execution
Double-click:
`C:\Users\fancy\.gemini\antigravity\scratch\TheFifthAxis\bin\Debug\net10.0\TheFifthAxis.exe`
