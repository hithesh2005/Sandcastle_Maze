# 🐾 3D Colorful Maze Runner (5 Levels Edition)

[![Unity 6](https://img.shields.io/badge/Unity-6000.5.9f1-black.svg?style=flat&logo=unity)](https://unity.com/)
[![C#](https://img.shields.io/badge/Language-C%23-blue.svg?style=flat&logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20PC-brightgreen.svg?style=flat&logo=windows)](https://microsoft.com/windows)
[![License](https://img.shields.io/badge/License-Academic%20Capstone-orange.svg)](#)

A complete, verified 3D maze adventure game built from first principles in **Unity 6** and **C#**. Features procedural labyrinth generation across **5 progressive levels**, real-time avatar switching between an animated quadruped **Dog (Buddy)** and a **Human Runner**, elevated isometric follow camera controls, a **2-stage Golden Key quest**, and a seamless single-collider floor architecture eliminating character-controller seam snags.

---

## 👨‍💻 Developer & Academic Information

- **Developer:** K. Naga Chaitanya
- **Hall Ticket / Roll No:** `2303A51292`
- **Degree:** Bachelor of Technology in Computer Science & Artificial Intelligence
- **Supervisor:** Dr. Vijaya Chandra Jadala (Assistant Professor, School of CS&AI)
- **Institution:** SR University, Warangal, Telangana, India (Academic Year: 2026–2027)

---

## ✨ Key Features

- **5 Progressive Difficulty Levels:**
  - Labyrinth dimensions scale dynamically using the formula:  
    $$\text{Dimension} = 13 + (\text{Level} \times 4)$$
  - **Level 1:** Casual $17 \times 17$ (12 Coins, 1 Key, 1 Speed Pad)
  - **Level 2:** Normal $21 \times 21$ (16 Coins, 1 Key, 2 Speed Pads)
  - **Level 3:** Intermediate $25 \times 25$ (20 Coins, 1 Key, 3 Speed Pads)
  - **Level 4:** Expert $29 \times 29$ (24 Coins, 1 Key, 4 Speed Pads)
  - **Level 5:** Grand Labyrinth $33 \times 33$ (28 Coins, 1 Key, 5 Speed Pads)
- **Dual Character Model Toggle (`C` Key):**
  - **Dog Runner ("Buddy"):** Quadruped model with real-time procedural diagonal trot gait, tail wagging, and head bob.
  - **Human Runner:** Compact bipedal character model with stride animations.
- **2-Stage Objective Loop:**
  1. Navigate the maze and retrieve the hidden, spinning **Golden Key** before the 180s timer expires.
  2. The key triggers the northern **Portcullis Exit Gate** to slide open and turns beacon lights from red to green. Follow the dynamic compass arrow to escape onto the outdoor meadow.
- **Seam-Free Continuous Floor Architecture:**
  - Uses a single continuous physical `BoxCollider` beneath the arena, while decorative checkered floor tile colliders are stripped with `SafeDestroy()`. Eliminates micro-seam character freezes.
- **Elevated 3/4 Isometric Camera System:**
  - 3 switchable angle presets via <kbd>V</kbd> (Isometric, Overhead Drone, Action View).
  - Quick 45° step orbit via <kbd>Q</kbd> and <kbd>E</kbd>.
  - Smooth scroll-wheel zoom adjustment (0.65× to 1.55×).
- **Interactive Pickups & Boosts:**
  - 🟡 **Gold Coins:** +10 points each.
  - 🔑 **Golden Key:** +300 points and unlocks the Exit Gate.
  - ⚡ **Cyan Speed Boost Pads:** Grants a 1.6× speed multiplier for 4.0 seconds.
- **Clean, Non-Overlapping HUD Canvas:**
  - Top-left card displaying Level, Key status, and Coins.
  - Top-right card displaying Countdown Timer.
  - Bottom bar displaying controls reference and objective alerts.
  - Working **NEXT LEVEL** and **PLAY AGAIN** victory dialog buttons.

---

## 🎮 Controls Reference

| Key / Input | Action | Description |
| :--- | :--- | :--- |
| <kbd>W</kbd> <kbd>A</kbd> <kbd>S</kbd> <kbd>D</kbd> / Arrows | **Move Runner** | Camera-relative forward, back, strafe, and turn |
| <kbd>Left Shift</kbd> (Hold) | **Sprint** | Accelerates speed from 6.5 m/s to 9.5 m/s |
| <kbd>Space</kbd> | **Jump / Hop** | Hop over low obstacles (active when grounded) |
| <kbd>C</kbd> | **Toggle Avatar** | Switch between Dog Runner (Buddy) and Human Model |
| <kbd>V</kbd> | **Camera Presets** | Cycle: 0 (Elevated 3/4) $\rightarrow$ 1 (Overhead) $\rightarrow$ 2 (Action) |
| <kbd>Q</kbd> / <kbd>E</kbd> | **Rotate Camera** | Rotate camera yaw angle 45° left or right |
| <kbd>Mouse Scroll</kbd> | **Zoom** | Smoothly zoom camera distance in/out |
| <kbd>R</kbd> | **Restart Level** | Reloads current level with fresh randomized positions |
| <kbd>Enter</kbd> / <kbd>Space</kbd> | **Advance / Retry** | Trigger "NEXT LEVEL" or "PLAY AGAIN" on end screen |

---

## 📂 Project Architecture

```
Colourful_Maze_5Levels/
├── Assets/
│   ├── Editor/
│   │   └── NeonMazeBuilder.cs      # Menu-driven Editor scene generator & build registrar
│   ├── Scenes/
│   │   └── NeonMaze.unity          # Main playable scene
│   └── Scripts/
│       ├── NeonPlayer.cs           # CharacterController locomotion, avatar swap, compass
│       ├── NeonMazeGame.cs         # 5-level procedural builder, scoring, HUD, progression
│       ├── NeonCoin.cs             # Collectible spin/bob animation & pickup triggers
│       ├── NeonExit.cs             # Portcullis sliding door physics & beacon lamps
│       └── NeonCameraFollow.cs     # Multi-preset tracking camera with Q/E rotation
├── Packages/
│   └── manifest.json               # Package dependencies (UGUI, Physics, Core)
├── ProjectSettings/
│   └── ProjectVersion.txt          # Target Unity 6 version declaration
└── README.md
```

---

## 🚀 Getting Started

### Prerequisites
- **Unity Hub**
- **Unity 6 (6000.0+)** or any modern Unity LTS version (2022.3 LTS+)
- Visual Studio 2022 or VS Code with C# Dev Kit (optional, for code inspection)

### Installation & Execution
1. **Clone or Download:**
   ```bash
   git clone https://github.com/<your-username>/3D-Colorful-Maze-Runner.git
   ```
2. **Open in Unity Hub:**
   - Launch Unity Hub.
   - Click **Add** $\rightarrow$ **Add project from disk**.
   - Select the repository root folder.
3. **Generate & Open Scene:**
   - Allow Unity to import packages and compile C# scripts.
   - From the top menu bar, select:
     ```
     Neon Maze Runner > Generate Playable Maze Scene
     ```
   - Open the generated scene located at:
     ```
     Assets/Scenes/NeonMaze.unity
     ```
4. **Play:**
   - Click the **Play (▶)** button in the Unity Editor toolbar.
   - Guide your runner to retrieve the Golden Key and escape through the Exit Gate to progress through all 5 levels!

---

## 📊 Scoring & Rules

$$\text{Final Level Score} = (\text{Coins Collected} \times 10) + (\text{Has Key} \times 300) + \lfloor\text{Remaining Seconds} \times 3\rfloor$$

- **Win Condition:** Retrieve the Golden Key and step into the Exit Gate archway before time expires.
- **Defeat Condition:** Countdown timer reaches `00:00` before reaching the gate.
- Cumulative score is tracked across all 5 levels.

---

## 📜 Academic Capstone Notice

This repository represents the official Capstone Project implementation for the degree of **Bachelor of Technology in Computer Science & Artificial Intelligence** at **SR University, Warangal**.
