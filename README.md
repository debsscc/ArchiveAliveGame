# ArchiveAlive

ArchiveAlive is a narrative memory-exploration game built with Unity UI.

Players interact with pinned photos on a paper board, trigger cinematic zoom transitions, reveal memory overlays with visual effects, and return to the board to explore other memories.


## Tech Stack

- Unity 6
  - Editor version: 6000.4.4f1
- C# scripts under Assets/Scripts
- Unity UI (Canvas, Image, Button, CanvasGroup)


## Core Gameplay Flow

1. Player starts in Menu.
2. Pressing Play triggers scene and audio fade transition to Game.
3. In Game, player clicks a photo pin.
4. The selected photo runs the open transition and memory overlay appears.
5. Player presses Back to close the memory and return to board state.

## How To Run

1. Open project folder in Unity Hub.
2. Use Unity Editor 6000.4.4f1.
3. Open the Menu scene.
4. Press Play.

## Controls

- Mouse click: interact with photo buttons
- Back button: close current memory overlay
- Exit button (Menu): quit game (or stop Play mode in editor)


