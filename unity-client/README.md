# Unity Client

The Unity project is located at `unity-client/RabbitInGarden` and uses Unity
`6000.4.5f1`.

Recommended Unity editor settings:

- Version Control: Visible Meta Files
- Asset Serialization: Force Text

Commit these Unity folders:

- `Assets/`
- `Packages/`
- `ProjectSettings/`

Do not commit generated folders such as `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, or `Logs/`.

The implemented Unity client sends procedural `GameState` dialogue requests to:

```text
POST http://localhost:5000/chat
```
