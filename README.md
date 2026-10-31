# Basky Game
Basky Game is a 2D arcade game built with Unity. The objective is simple: move a basket to catch falling coins while avoiding bombs. Catching coins increases your score, but hitting a bomb ends the game.
The game also includes bomb rain, where multiple bombs fall at once, making it harder to avoid them and collect coins safely.

## Gameplay
The player controls a basket near the bottom of the screen. Coins and bombs fall from above, and the player has to react quickly to catch the coins without touching the bombs.

### Features
- **Basket movement:** Move the basket horizontally to catch falling coins.
- **Coin collection:** Catch coins to increase your score.
- **Bombs:** Avoid bombs, as colliding with one ends the game.
- **Bomb rain:** A special event where multiple bombs fall together, increasing the difficulty.
- **2D gameplay:** Built in Unity using 2D game objects and collision detection.

## Built Wit
- **Unity** — Game engine
- **C#** — Gameplay scripting
- **Unity 2D** — 2D game objects, sprites, and physics

## Technical Overview
The game revolves around three main parts: player movement, falling objects, and collision handling.
The basket is controlled by the player and moves horizontally across the play area. Its position determines which falling objects can be collected or avoided.
Coins and bombs spawn above the play area and move downward. Their movement and spawning determine the pace of the game.
Collisions determine what happens when the basket touches an object:
- **Coin collision:** The coin is collected and the score increases.
- **Bomb collision:** The game ends.
- **Bomb rain:** Multiple bombs fall during the same event, making it more difficult to find a safe path to the coins.

### Run the Project Locally
Clone the repository:

```bash
git clone https://github.com/GT-Snix/Basky-Game.git
```

Open Unity Hub, select **Add project from disk**, and choose the cloned `Basky-Game` folder.
Open the project in Unity, load the main game scene, and press the Play button in the Editor.
The first launch may take some time while Unity imports the project assets.

## Project Structure
The repository contains the following main directories:

```text
Basky-Game/
├── Assets/
│   └── Game assets, scenes, and scripts
├── Packages/
│   └── Unity package dependencies
├── ProjectSettings/
│   └── Unity project configuration
└── .gitignore
```

The `Assets` directory contains the game's content and is the main place to look when exploring or modifying the game. The `Packages` and `ProjectSettings` directories contain the dependencies and configuration needed to open the project in Unity.

## Building the Game
To create a standalone build:
1. Open the project in Unity.
2. Open **File > Build Profiles** or **File > Build Settings**, depending on your Unity version.
3. Select the target platform.
4. Make sure the main game scene is included in the build.
5. Configure the required build options.
6. Build and run the game.
The available platforms depend on the project's configuration and the Unity modules installed on your system.
