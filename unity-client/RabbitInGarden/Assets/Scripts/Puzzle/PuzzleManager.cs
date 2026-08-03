using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    [SerializeField] private HintLocation[] possibleLocations;
    [SerializeField] private int hintsPerRun = 3;

    private readonly List<HintLocation> selectedLocations = new List<HintLocation>();
    private readonly HashSet<string> foundLocationIds = new HashSet<string>();
    private ProceduralOrderingPuzzle activePuzzle;
    private string activePuzzleLocationId = "";
    private string[] lastPuzzleAttempt = Array.Empty<string>();
    private string[] lastSubmittedPuzzleAttempt = Array.Empty<string>();
    private int activePuzzleAttempts;
    private int activePuzzleHintsGiven;
    private bool activePuzzleSolved;
    private readonly Dictionary<string, string> riddleByLocation = new Dictionary<string, string>
    {
        { "pond", "It reflects the sky, but it is not a mirror." },
        { "flower_bed", "I bloom with colors and hide among petals." },
        { "tree_roots", "I drink from the soil, but I never walk." },
        { "stone_path", "Many feet pass over me, but I never move." },
        { "bench", "I wait for tired legs beneath the open sky." },
        { "garden_gate", "I open the way, but only when the secret is found." },
        { "sunflower_patch", "I follow the sun with a golden face." },
        { "watering_can", "I pour rain when clouds are away." },
        { "old_lantern", "I glow when the garden grows dark." },
        { "bird_bath", "Small wings visit me when they need a drink." }
    };

    public event Action HintFound;
    public event Action PuzzleCompleted;

    public int CurrentHintIndex { get; private set; }
    public int HintCount => selectedLocations.Count;
    public bool BlueFound { get; private set; }
    public string CurrentTargetLocationId => CurrentHintIndex < selectedLocations.Count
        ? selectedLocations[CurrentHintIndex].LocationId
        : "";

    public string CurrentRiddle
    {
        get
        {
            if (selectedLocations.Count == 0 || CurrentHintIndex >= selectedLocations.Count)
            {
                return "";
            }

            return selectedLocations[CurrentHintIndex].Riddle;
        }
    }

    private void Awake()
    {
        GenerateHintSequence();
    }

    public bool EnsureSequenceGenerated()
    {
        if (selectedLocations.Count == 0)
        {
            GenerateHintSequence();
        }

        return selectedLocations.Count > 0;
    }

    public void GenerateHintSequence()
    {
        selectedLocations.Clear();
        foundLocationIds.Clear();
        CurrentHintIndex = 0;
        BlueFound = false;
        ClearActivePuzzle();

        List<HintLocation> validLocations = (possibleLocations ?? Array.Empty<HintLocation>())
            .Where(location => location != null && !string.IsNullOrWhiteSpace(location.LocationId))
            .ToList();

        if (validLocations.Count == 0)
        {
            validLocations = BuildLocationsFromInteractables();
        }

        validLocations = validLocations
            .GroupBy(location => location.LocationId)
            .Select(group => group.First())
            .OrderBy(_ => Guid.NewGuid())
            .Take(Mathf.Min(hintsPerRun, validLocations.Count))
            .ToList();

        selectedLocations.AddRange(validLocations);
        Debug.Log($"[Puzzle] Generated {selectedLocations.Count} hints: {string.Join(" -> ", selectedLocations.Select(x => x.LocationId))}");
    }

    public bool TryDiscoverLocation(string locationId)
    {
        if (!EnsureSequenceGenerated()
            || BlueFound
            || string.IsNullOrWhiteSpace(locationId)
            || CurrentHintIndex >= selectedLocations.Count)
        {
            return false;
        }

        if (!string.Equals(CurrentTargetLocationId, locationId, StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"[Puzzle] {locationId} is not the current riddle answer.");
            return false;
        }

        if (activePuzzle == null || activePuzzleSolved)
        {
            ClearActivePuzzle();
            int symbolCount = Mathf.Clamp(3 + CurrentHintIndex, 3, 5);
            int seed = unchecked(Environment.TickCount * 397) ^ CurrentHintIndex ^ locationId.GetHashCode();
            activePuzzle = ProceduralOrderingPuzzleGenerator.Generate(symbolCount, seed);
            activePuzzleLocationId = locationId;
            lastPuzzleAttempt = Array.Empty<string>();
            lastSubmittedPuzzleAttempt = Array.Empty<string>();
            activePuzzleAttempts = 0;
            activePuzzleHintsGiven = 0;
            activePuzzleSolved = false;
            Debug.Log($"[Puzzle] Reached {locationId}; generated {activePuzzle.PuzzleId} with {symbolCount} symbols and one validated solution.");
        }

        OrderingPuzzleController.Open(this);
        return true;
    }

    public bool SubmitActivePuzzle(string[] attempt)
    {
        if (activePuzzle == null || activePuzzleSolved)
        {
            return false;
        }

        lastPuzzleAttempt = attempt?.ToArray() ?? Array.Empty<string>();
        lastSubmittedPuzzleAttempt = lastPuzzleAttempt.ToArray();
        activePuzzleAttempts += 1;
        if (activePuzzleHintsGiven > 0)
        {
            activePuzzleHintsGiven = 1;
        }
        activePuzzleSolved = activePuzzle.IsCorrect(lastPuzzleAttempt);
        Debug.Log($"[Puzzle] Attempt {activePuzzleAttempts} for {activePuzzle.PuzzleId}: solved={activePuzzleSolved}.");

        if (activePuzzleSolved)
        {
            MarkCurrentHintFound();
        }
        return activePuzzleSolved;
    }

    public void SaveActivePuzzleDraft(string[] attempt)
    {
        if (activePuzzle == null || activePuzzleSolved || attempt == null)
        {
            return;
        }

        lastPuzzleAttempt = attempt
            .Where(symbol => activePuzzle.Symbols.Contains(symbol))
            .Distinct()
            .Take(activePuzzle.Symbols.Length)
            .ToArray();
    }

    public ActivePuzzleState GetActivePuzzleState()
    {
        if (activePuzzle == null)
        {
            return new ActivePuzzleState();
        }

        return new ActivePuzzleState
        {
            puzzle_id = activePuzzle.PuzzleId,
            puzzle_type = "symbol_ordering",
            location_id = activePuzzleLocationId,
            symbols = activePuzzle.Symbols.ToArray(),
            constraints = activePuzzle.Constraints.Select(constraint => constraint.text).ToArray(),
            player_attempt = lastPuzzleAttempt.ToArray(),
            submitted_attempt = lastSubmittedPuzzleAttempt.ToArray(),
            attempts = activePuzzleAttempts,
            hints_given = activePuzzleHintsGiven,
            is_active = !activePuzzleSolved,
            is_solved = activePuzzleSolved
        };
    }

    public void RecordActivePuzzleHintGiven()
    {
        if (activePuzzle != null && !activePuzzleSolved)
        {
            activePuzzleHintsGiven += 1;
        }
    }

    public void MarkCurrentHintFound()
    {
        if (CurrentHintIndex >= selectedLocations.Count)
        {
            return;
        }

        foundLocationIds.Add(selectedLocations[CurrentHintIndex].LocationId);
        CurrentHintIndex += 1;
        HintFound?.Invoke();

        if (CurrentHintIndex >= selectedLocations.Count)
        {
            BlueFound = true;
            PuzzleCompleted?.Invoke();
            Debug.Log("[Puzzle] The final hint was found. Blue can now be revealed.");
        }
    }

    public HintState[] GetHintStates()
    {
        HintState[] states = new HintState[selectedLocations.Count];

        for (int i = 0; i < selectedLocations.Count; i++)
        {
            HintLocation location = selectedLocations[i];
            states[i] = new HintState
            {
                hint_index = i,
                location_id = location.LocationId,
                riddle = location.Riddle,
                is_found = foundLocationIds.Contains(location.LocationId)
            };
        }

        return states;
    }

    public string[] GetFoundHintLocations()
    {
        return foundLocationIds.ToArray();
    }

    private void ClearActivePuzzle()
    {
        activePuzzle = null;
        activePuzzleLocationId = "";
        lastPuzzleAttempt = Array.Empty<string>();
        lastSubmittedPuzzleAttempt = Array.Empty<string>();
        activePuzzleAttempts = 0;
        activePuzzleHintsGiven = 0;
        activePuzzleSolved = false;
    }

    private List<HintLocation> BuildLocationsFromInteractables()
    {
        List<HintLocation> locations = new List<HintLocation>();
        GardenInteractable[] interactables = FindObjectsByType<GardenInteractable>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (GardenInteractable interactable in interactables)
        {
            string id = interactable.PuzzleLocationId;
            if (!riddleByLocation.TryGetValue(id, out string riddle))
            {
                continue;
            }

            HintLocation hint = interactable.GetComponent<HintLocation>();
            if (hint == null)
            {
                hint = interactable.gameObject.AddComponent<HintLocation>();
                hint.Configure(id, id.Replace('_', ' '), riddle);
            }
            locations.Add(hint);
        }
        return locations;
    }
}
