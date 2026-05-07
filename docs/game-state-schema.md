# Game State Schema

Unity should send a complete snapshot of the current puzzle and clue state with each dialogue request.

Example request:

```json
{
  "player_query": "What should I pay attention to here?",
  "game_state": {
    "scene_id": "garden_entrance",
    "player_character": "Red",
    "missing_character": "Blue",
    "npc": "Rock",
    "current_puzzle_number": 1,
    "total_puzzles": 10,
    "puzzle_id": "sunflower_pattern",
    "objective": "Find clues that lead to Blue's hiding place",
    "clue_points": 1,
    "rock_initial_clue_given": true,
    "rock_questions_asked": 0,
    "player_position": {
      "x": 2,
      "y": 0,
      "z": 5
    },
    "inventory": ["small_leaf"],
    "visited_locations": ["garden_gate", "old_rock"],
    "reward_clues_collected": [],
    "puzzle_state": {
      "is_solved": false,
      "visible_objects": ["sunflowers", "stone_path", "watering_can"],
      "interacted_objects": ["old_rock"],
      "available_actions": ["inspect_sunflowers", "follow_stone_path", "inspect_watering_can"]
    }
  }
}
```

Example response:

```json
{
  "npc_response": "The flowers are not only decoration, little Red. Notice whether they all agree on where to face.",
  "retrieved_context": [],
  "clue_point_spent": true
}
```

The exact schema can evolve once the puzzle designs are clearer.
