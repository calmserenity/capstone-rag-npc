# Unity Client

Create or move the Unity project into this folder.

Recommended Unity editor settings:

- Version Control: Visible Meta Files
- Asset Serialization: Force Text

Commit these Unity folders:

- `Assets/`
- `Packages/`
- `ProjectSettings/`

Do not commit generated folders such as `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, or `Logs/`.

The Unity client will eventually send dialogue requests to:

```text
POST http://localhost:5000/chat
```
