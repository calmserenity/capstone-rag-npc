using System;

[Serializable]
public class ChatRequest
{
    public string player_query;
    public GameState game_state;
}

[Serializable]
public class ChatResponse
{
    public string npc_response;
    public string[] retrieved_context;
}

[Serializable]
public class GameState
{
    public string scene_id = "garden_prototype";
    public string player_character = "Red";
    public string missing_character = "Blue";
    public string npc = "Rock";
    public string current_area = "outer_garden";
    public string objective = "Follow the riddles to find Blue";
    public int clue_points = 3;
    public int rock_questions_asked;
    public PlayerPosition player_position = new PlayerPosition();
    public HintState[] hint_sequence = Array.Empty<HintState>();
    public int current_hint_index;
    public int max_hints = 3;
    public string current_riddle = "";
    public string[] found_hint_locations = Array.Empty<string>();
    public string[] possible_hint_locations = Array.Empty<string>();
    public bool blue_found;
}

[Serializable]
public class PlayerPosition
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class HintState
{
    public int hint_index;
    public string location_id;
    public string riddle;
    public bool is_found;
}
