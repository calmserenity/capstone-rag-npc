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
    "current_location": "flower_bed",
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
    "active_puzzle": {
      "puzzle_id": "ordering-a17c39ef",
      "puzzle_type": "symbol_ordering",
      "location_id": "flower_bed",
      "symbols": ["lantern", "flower", "stone"],
      "constraints": [
        "The flower comes before the stone.",
        "The stone is immediately before the lantern."
      ],
      "player_attempt": ["lantern", "flower", "stone"],
      "submitted_attempt": ["lantern", "flower", "stone"],
      "attempts": 1,
      "hints_given": 1,
      "is_active": true,
      "is_solved": false
    },
    "blue_found": false
  }
}
```

Example response:

```json
{
  "npc_response": "Mmm... little Red, look for a place where colors gather and petals keep small secrets.",
  "retrieved_context": [],
  "clue_point_spent": true,
  "puzzle_hint_given": false
}
```

The implemented Unity client serializes this snapshot for every Rock request.
`hint_sequence` is regenerated from three unique enabled scene interactables at
the beginning of each run; Unity remains authoritative for progress, clue points,
and `blue_found`. Reaching the active location creates `active_puzzle`; the next
hint remains locked until Unity validates a correct full ordering.

The contract intentionally has no `solution` field. The hidden ordering remains
inside Unity. `player_attempt` preserves the current draft, while
`submitted_attempt` preserves the latest complete submitted order for positional
feedback. `hints_given` advances only when Rock provides a new puzzle hint and
resets positional progression after a new submission. `puzzle_hint_given` tells
Unity whether to advance that counter.
