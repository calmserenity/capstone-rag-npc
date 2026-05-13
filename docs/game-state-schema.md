# Game State Schema

Unity should send a complete snapshot of the current puzzle and clue state with each dialogue request.

Example request:

```json
{
  "player_query": "Where should I look for the key?",
  "game_state": {
    "scene_id": "outer_garden",
    "player_character": "Red",
    "missing_character": "Blue",
    "npc": "Rock",
    "current_puzzle_number": 1,
    "total_puzzles": 10,
    "puzzle_id": "outer_garden_key",
    "objective": "Find the key that unlocks the gate to the inner garden",
    "current_area": "outer_garden",
    "available_areas": ["outer_garden"],
    "locked_areas": ["inner_garden"],
    "area_scale": {
      "outer_garden": 1,
      "inner_garden": 3
    },
    "clue_points": 1,
    "rock_initial_clue_given": true,
    "rock_questions_asked": 0,
    "player_position": {
      "x": 2,
      "y": 0,
      "z": 5
    },
    "inventory": [],
    "visited_locations": ["garden_gate", "old_rock"],
    "reward_clues_collected": [],
    "puzzle_state": {
      "is_solved": false,
      "gate_to_inner_garden_locked": true,
      "key_found": false,
      "key_location_seed": 1842,
      "key_location_hint_region": "near_flower_beds",
      "visible_objects": ["locked_gate", "flower_beds", "stone_path", "old_rock"],
      "interacted_objects": ["old_rock"],
      "available_actions": ["inspect_flower_beds", "follow_stone_path", "inspect_locked_gate"]
    }
  }
}
```

Example response:

```json
{
  "npc_response": "Mmm... little Red, the gate waits for something small and bright. Search where petals gather close to the path.",
  "retrieved_context": [],
  "clue_point_spent": true
}
```

The exact schema can evolve once the procedural key placement and Unity puzzle implementation are clearer.
