# Game State Schema

Unity should send a complete snapshot of the current puzzle state with each dialogue request.

Example request:

```json
{
  "player_query": "Where should I go next?",
  "game_state": {
    "scene_id": "maze_room_01",
    "puzzle_id": "maze_escape",
    "player_position": {
      "x": 2,
      "y": 0,
      "z": 5
    },
    "objective": "Reach the exit gate",
    "inventory": [],
    "visited_locations": ["entrance"],
    "puzzle_state": {
      "is_solved": false,
      "available_paths": ["north", "east"],
      "blocked_paths": ["west"]
    }
  }
}
```

Example response:

```json
{
  "npc_response": "The northern path feels promising. Look for a symbol near the next turn.",
  "retrieved_context": []
}
```

The exact schema can evolve once the Unity puzzle design is clearer.
