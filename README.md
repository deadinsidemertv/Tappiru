# 🎵 Tappiru — Rhythm Typing Game

> A rhythm game where you type to the beat. Built from scratch in C# + OpenTK with a full online backend.

![Platform](https://img.shields.io/badge/platform-Windows-blue)
![Language](https://img.shields.io/badge/language-C%23-purple)
![Framework](https://img.shields.io/badge/framework-OpenTK-orange)
![Backend](https://img.shields.io/badge/backend-ASP.NET%20Core-green)
![Database](https://img.shields.io/badge/database-PostgreSQL-blue)

---

## 🎮 What is Tappiru?

Tappiru is a **keyboard rhythm game** — hit keys in time with the music, type words in Russian or English, and compete for scores on a global leaderboard. Everything — the game engine, the server, the map editor — was built from scratch over 3 months.

**[⬇️ Download and play](https://github.com/deadinsidemertv/Tappiru/releases)**

---

## ✨ Features

### Core Gameplay
- 🎹 **Russian & English typing** — hit notes by typing the displayed characters
- 📊 **Score / Combo / Accuracy** system with real-time feedback
- 🌊 **Slider mechanics** — speed-typing sequences with color selection
- 🌐 **Unicode + Japanese language** support via FreeType rendering

### Online & Profiles
- 👤 **Registration & profiles** — avatar upload, rating, game statistics
- 🏆 **Global leaderboard** — Top 100 scores across all maps
- 📈 **Profile stats** — total games played, total keystrokes, best scores per map
- 🔑 **JWT authentication** — login inside the game, persistent session

### Map System
- 🗺️ **Map library** — browse, download and upload community maps
- 🛠️ **Built-in map editor** — create maps without leaving the game
- 📤 **Upload your own maps** to the server and share with others

### Engine & Framework
- 🏗️ **Custom 2D engine** built on OpenTK with:
  - `child/parent` scene hierarchy
  - `Scene` system with transitions
  - `AutoScale` — UI adapts to any resolution
  - Sprites, Text, Lists, Checkboxes, InputFields, Containers, modular windows
- 🎨 **Smooth animations** throughout all UI
- ⚙️ **Settings** with persistence

---

## 🏗️ Architecture

```
Tappiru/
├── TappiruCS/              # Game client (C# + OpenTK)
│   ├── Engine/             # Custom 2D framework (scenes, nodes, UI)
│   ├── Game/               # Gameplay logic, scoring, input
│   ├── UI/                 # All screens and menus
│   ├── Network/            # HTTP client, JWT auth, score upload
│   └── Rendering/          # FreeType text, sprites, shaders
│
└── TappiruServer/          # Backend (ASP.NET Core + PostgreSQL)
    ├── Controllers/        # REST API endpoints
    ├── Models/             # User, Map, Score, Rating
    ├── Services/           # Rating calculation, file storage
    └── Database/           # EF Core migrations
```

---

## 🔧 Tech Stack

| Layer | Technology |
|---|---|
| Game client | C# / OpenTK |
| Text rendering | FreeType (via SharpFont) |
| Backend | ASP.NET Core |
| Database | PostgreSQL + Entity Framework Core |
| Auth | JWT tokens |
| Avatar / map storage | Server file storage |

---

## 🚀 Getting Started

1. Download the latest release from [Releases](https://github.com/deadinsidemertv/Tappiru/releases)
2. Extract and run `Tappiru.exe`
3. Register an account or play as guest
4. Download a map from the library and play

**To build from source:**
```bash
git clone https://github.com/deadinsidemertv/Tappiru
cd Tappiru
# Open TappiruCS.slnx in Visual Studio 2022+
# Build and run
```

---

## 🗺️ Map Format

Maps live in `Songs/<SongName>/` and contain:
- `audio.mp3` — the track
- `map.tmap` — note timing, lanes, sliders (custom text format)
- `meta.json` — title, artist, BPM, difficulty

The in-game editor lets you place notes visually and export directly to this format.

---

## 📌 Status

The project is actively in development. Core gameplay, online features and the map editor are complete. Upcoming: more map content, difficulty rating system, improved UI polish.

---

## 👤 Author

**neroqwe** — [@deadinsidemertv](https://github.com/deadinsidemertv)

> Built entirely solo. Engine, backend, UI, gameplay — everything from scratch.
