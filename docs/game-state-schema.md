# Game State Schema

Unity should send a complete snapshot of the current hint, riddle, and clue-point state with each dialogue request.

Example request:

```json
{
  "player_query": "What does this riddle mean?",
  "game_state": {
    "scene_id": "garden_prototype",
    "player_character": "Red",
    "missing_character": "Blue",
    "npc": "Rock",
    "objective": "Follow the riddles to find Blue",
    "current_area": "outer_garden",
    "clue_points": 3,
    "rock_questions_asked": 0,
    "player_position": {
      "x": 2,
      "y": 0,
      "z": 5
    },
    "inventory": [],
    "visited_locations": ["old_rock", "pond"],
    "hint_sequence": [
      {
        "hint_index": 0,
        "location_id": "pond",
        "riddle": "It reflects the sky, but it is not a mirror.",
        "is_found": true
      },
      {
        "hint_index": 1,
        "location_id": "flower_bed",
        "riddle": "I bloom with colors and hide among petals.",
        "is_found": false
      },
      {
        "hint_index": 2,
        "location_id": "bench",
        "riddle": "I wait for tired legs beneath the open sky.",
        "is_found": false
      }
    ],
    "current_hint_index": 1,
    "max_hints": 3,
    "current_riddle": "I bloom with colors and hide among petals.",
    "found_hint_locations": ["pond"],
    "possible_hint_locations": [
      "pond",
      "flower_bed",
      "tree_roots",
      "stone_path",
      "bench",
      "garden_gate",
      "sunflower_patch",
      "watering_can",
      "old_lantern",
      "bird_bath"
    ],
    "blue_found": false
  }
}
```

Example response:

```json
{
  "npc_response": "Mmm... little Red, look for a place where colors gather and petals keep small secrets.",
  "retrieved_context": [],
  "clue_point_spent": true
}
```

The exact schema can evolve once the Unity prototype and procedural hint placement are implemented.
