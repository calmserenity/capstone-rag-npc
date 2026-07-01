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

    public int CurrentHintIndex { get; private set; }
    public bool BlueFound { get; private set; }

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

    private void Start()
    {
        GenerateHintSequence();
    }

    public void GenerateHintSequence()
    {
        selectedLocations.Clear();
        foundLocationIds.Clear();
        CurrentHintIndex = 0;
        BlueFound = false;

        List<HintLocation> validLocations = possibleLocations
            .Where(location => location != null && !string.IsNullOrWhiteSpace(location.LocationId))
            .OrderBy(_ => Guid.NewGuid())
            .Take(hintsPerRun)
            .ToList();

        selectedLocations.AddRange(validLocations);
    }

    public void MarkCurrentHintFound()
    {
        if (CurrentHintIndex >= selectedLocations.Count)
        {
            return;
        }

        foundLocationIds.Add(selectedLocations[CurrentHintIndex].LocationId);
        CurrentHintIndex += 1;

        if (CurrentHintIndex >= selectedLocations.Count)
        {
            BlueFound = true;
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
}
