using UnityEngine;

public class GameStateBuilder : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private PuzzleManager puzzleManager;
    [SerializeField] private int cluePoints = 3;
    [SerializeField] private int rockQuestionsAsked;

    private readonly string[] possibleHintLocations =
    {
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
    };

    private void Awake()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.Find("Rabbit_Center");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (puzzleManager == null)
        {
            puzzleManager = FindFirstObjectByType<PuzzleManager>();
        }

        if (puzzleManager == null)
        {
            puzzleManager = gameObject.AddComponent<PuzzleManager>();
        }

        puzzleManager.EnsureSequenceGenerated();

        puzzleManager.HintFound += EarnCluePoint;

        if (FindAnyObjectByType<GardenGameHud>() == null)
        {
            gameObject.AddComponent<GardenGameHud>();
        }
    }

    private void OnDestroy()
    {
        if (puzzleManager != null)
        {
            puzzleManager.HintFound -= EarnCluePoint;
        }
    }

    public GameState Build()
    {
        Vector3 playerPosition = player != null ? player.position : Vector3.zero;
        HintState[] hintSequence = puzzleManager != null ? puzzleManager.GetHintStates() : DummyHints();

        return new GameState
        {
            clue_points = cluePoints,
            rock_questions_asked = rockQuestionsAsked,
            player_position = new PlayerPosition
            {
                x = playerPosition.x,
                y = playerPosition.y,
                z = playerPosition.z
            },
            hint_sequence = hintSequence,
            current_hint_index = puzzleManager != null ? puzzleManager.CurrentHintIndex : 0,
            max_hints = puzzleManager != null ? puzzleManager.HintCount : hintSequence.Length,
            current_riddle = puzzleManager != null ? puzzleManager.CurrentRiddle : hintSequence[0].riddle,
            current_location = puzzleManager != null ? puzzleManager.CurrentTargetLocationId : "",
            found_hint_locations = puzzleManager != null ? puzzleManager.GetFoundHintLocations() : new string[0],
            possible_hint_locations = possibleHintLocations,
            active_puzzle = puzzleManager != null ? puzzleManager.GetActivePuzzleState() : new ActivePuzzleState(),
            blue_found = puzzleManager != null && puzzleManager.BlueFound
        };
    }

    public int CluePoints => cluePoints;

    public bool CanAskRock => cluePoints > 0;

    public void SpendCluePoint()
    {
        if (cluePoints <= 0)
        {
            return;
        }

        cluePoints -= 1;
        rockQuestionsAsked += 1;
    }

    public void RecordPuzzleHintGiven()
    {
        if (puzzleManager != null)
        {
            puzzleManager.RecordActivePuzzleHintGiven();
        }
    }

    private void EarnCluePoint()
    {
        cluePoints += 1;
    }

    private static HintState[] DummyHints()
    {
        return new[]
        {
            new HintState
            {
                hint_index = 0,
                location_id = "pond",
                riddle = "It reflects the sky, but it is not a mirror.",
                is_found = false
            }
        };
    }
}
