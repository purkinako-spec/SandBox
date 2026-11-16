# Sandbox Mini Game (Coin Dash)

A tiny test game made to verify the GitHub workflow:
Muse edits -> pushes -> the PC pulls automatically.

## How to play
1. Open `Scenes/MiniGame.unity` in Unity and press Play.
2. Move the orange ball with WASD / arrow keys.
3. Collect all 10 coins within 60 seconds to clear the game.
4. Press R to restart.

## Structure
- All scripts are in the `SandBox.MiniGame` namespace and in their own
  assembly (`SandBox.MiniGame.asmdef`), isolated from the other projects
  in this repository (DialogueSystem, MyActionGame, ...).
- The scene itself only holds a camera, a directional light,
  `MiniGameBootstrap` and `GameManager`. The stage (ground, walls,
  player, coins) is built at runtime by `MiniGameBootstrap`.
