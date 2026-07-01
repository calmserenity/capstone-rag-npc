# Unity And Git Setup

## Current Local Setup

- Unity project path: `unity-client/RabbitInGarden`
- Unity version: `6000.4.5f1`
- Backend URL for local play mode testing: `http://localhost:5000`
- Git LFS is installed locally.
- `.gitattributes` already tracks common Unity binary assets with Git LFS.

## Recommended Workflow

1. Open `unity-client/RabbitInGarden` in Unity Hub.
2. Keep `Assets/`, `Packages/`, and `ProjectSettings/` committed in Git.
3. Keep generated folders such as `Library/`, `Temp/`, `Obj/`, `Build/`, `Logs/`, and `UserSettings/` ignored.
4. Make project changes through the Git workspace so they can be reviewed and committed clearly.
5. Use Unity Editor tools for scene setup, Play Mode checks, console review, and asset wiring.

## First Unity Wiring Pass

After opening Unity, create or wire these scene objects:

- `BackendClient`: add the `BackendClient` component.
- `GameStateBuilder`: assign the player transform and optional `PuzzleManager`.
- `RockDialogueController`: assign `BackendClient`, `GameStateBuilder`, input field, response text, and ask button.
- `Player`: add `Rigidbody2D`, collider, and `PlayerMovement2D`.
- `PuzzleManager`: assign `HintLocation` objects in the scene.
- `HintLocation` markers: set location id, display name, and riddle.
