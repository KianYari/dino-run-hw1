# Dino Run

A Chrome dino style runner made in Unity 6 for the Game Design course (HW1).

Controls: Space to jump, Shift to dash, Esc to pause.

## Running it

Open the folder with Unity Hub (Unity 6000.0.x). After it finishes importing, open
`Assets/Scenes/Game.unity` and press Play.

If the scene is missing or something looks broken, use the menu `Dino > Build Everything`.
It regenerates the sprites, animator, prefabs and the scene. It also sets Active Input
Handling to the new Input System, so Unity may ask to restart.

`Dino > Build Executable` makes a build for whatever platform is selected.

## Scripts

All scripts are in `Assets/Scripts`.

- `GameManager`: game state, speed, score, high score (PlayerPrefs)
- `PlayerController`: jump, dash, lives, hit handling
- `Interactable`: base class for obstacles and the star
- `Obstacle`, `GroundObstacle`, `AirObstacle`: cactus and bird
- `StarItem`: gives bonus score
- `ObstacleSpawner`: spawns things on the right side
- `ParallaxLayer`: background layers
- `UIManager`: HUD and the menus
- `Editor/`: the generator used by the `Dino` menu
