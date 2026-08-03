using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class ProceduralOrderingPuzzleTests
{
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void Generate_ProducesExactlyOneValidSolution(int symbolCount)
    {
        for (int seed = 1; seed <= 25; seed++)
        {
            ProceduralOrderingPuzzle puzzle = ProceduralOrderingPuzzleGenerator.Generate(symbolCount, seed);

            Assert.That(puzzle.Symbols, Has.Length.EqualTo(symbolCount));
            Assert.That(puzzle.Symbols.Distinct().Count(), Is.EqualTo(symbolCount));
            Assert.That(
                ProceduralOrderingPuzzleGenerator.CountSolutions(puzzle.Symbols, puzzle.Constraints),
                Is.EqualTo(1));
            Assert.That(puzzle.IsCorrect(puzzle.Solution), Is.True);
        }
    }

    [Test]
    public void Generate_RandomizesPuzzleAcrossSeeds()
    {
        ProceduralOrderingPuzzle first = ProceduralOrderingPuzzleGenerator.Generate(4, 100);
        ProceduralOrderingPuzzle second = ProceduralOrderingPuzzleGenerator.Generate(4, 200);

        Assert.That(first.PuzzleId, Is.Not.EqualTo(second.PuzzleId));
        Assert.That(first.Solution.SequenceEqual(second.Solution), Is.False);
    }

    [Test]
    public void PublicPuzzleState_DoesNotSerializeHiddenSolution()
    {
        ActivePuzzleState state = new ActivePuzzleState
        {
            puzzle_id = "ordering-safe",
            symbols = new[] { "flower", "stone", "lantern" },
            constraints = new[] { "The flower comes before the stone." },
            player_attempt = new[] { "stone", "flower", "lantern" }
        };

        string json = JsonUtility.ToJson(state);

        Assert.That(json, Does.Contain("ordering-safe"));
        Assert.That(json, Does.Not.Contain("solution"));
    }

    [Test]
    public void LocationDoesNotAdvanceUntilGeneratedPuzzleIsSolved()
    {
        GameObject locationObject = new GameObject("TestPond");
        HintLocation location = locationObject.AddComponent<HintLocation>();
        location.Configure("pond", "Pond", "It reflects the sky.");

        GameObject managerObject = new GameObject("TestPuzzleManager");
        PuzzleManager manager = managerObject.AddComponent<PuzzleManager>();
        typeof(PuzzleManager)
            .GetField("possibleLocations", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(manager, new[] { location });
        manager.GenerateHintSequence();

        Assert.That(manager.TryDiscoverLocation("pond"), Is.True);
        Assert.That(manager.CurrentHintIndex, Is.EqualTo(0), "Reaching a location must not unlock the next hint.");
        Assert.That(manager.GetActivePuzzleState().is_active, Is.True);

        ProceduralOrderingPuzzle generated = (ProceduralOrderingPuzzle)typeof(PuzzleManager)
            .GetField("activePuzzle", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(manager);
        string[] wrong = generated.Solution.ToArray();
        (wrong[0], wrong[1]) = (wrong[1], wrong[0]);

        Assert.That(manager.SubmitActivePuzzle(wrong), Is.False);
        Assert.That(manager.CurrentHintIndex, Is.EqualTo(0));
        Assert.That(manager.GetActivePuzzleState().attempts, Is.EqualTo(1));

        Assert.That(manager.SubmitActivePuzzle(generated.Solution), Is.True);
        Assert.That(manager.CurrentHintIndex, Is.EqualTo(1));
        Assert.That(manager.BlueFound, Is.True);
        Assert.That(manager.GetActivePuzzleState().is_solved, Is.True);

        foreach (OrderingPuzzleController controller in Object.FindObjectsByType<OrderingPuzzleController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(controller.gameObject);
        }
        Object.DestroyImmediate(managerObject);
        Object.DestroyImmediate(locationObject);
    }

    [Test]
    public void LeavingPuzzlePreservesDraftWithoutCountingAnAttempt()
    {
        GameObject locationObject = new GameObject("TestPond");
        HintLocation location = locationObject.AddComponent<HintLocation>();
        location.Configure("pond", "Pond", "It reflects the sky.");

        GameObject managerObject = new GameObject("TestPuzzleManager");
        PuzzleManager manager = managerObject.AddComponent<PuzzleManager>();
        typeof(PuzzleManager)
            .GetField("possibleLocations", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(manager, new[] { location });
        manager.GenerateHintSequence();
        manager.TryDiscoverLocation("pond");

        ActivePuzzleState initialState = manager.GetActivePuzzleState();
        string[] draft = initialState.symbols.Take(2).ToArray();
        manager.SaveActivePuzzleDraft(draft);

        ActivePuzzleState savedState = manager.GetActivePuzzleState();
        Assert.That(savedState.player_attempt, Is.EqualTo(draft));
        Assert.That(savedState.attempts, Is.EqualTo(0));
        Assert.That(savedState.is_active, Is.True);
        Assert.That(manager.CurrentHintIndex, Is.EqualTo(0));

        foreach (OrderingPuzzleController controller in Object.FindObjectsByType<OrderingPuzzleController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(controller.gameObject);
        }
        Object.DestroyImmediate(managerObject);
        Object.DestroyImmediate(locationObject);
    }

    [Test]
    public void PuzzleHintProgressTracksSubmittedAttemptAndResetsPositionalHints()
    {
        GameObject locationObject = new GameObject("TestPond");
        HintLocation location = locationObject.AddComponent<HintLocation>();
        location.Configure("pond", "Pond", "It reflects the sky.");

        GameObject managerObject = new GameObject("TestPuzzleManager");
        PuzzleManager manager = managerObject.AddComponent<PuzzleManager>();
        typeof(PuzzleManager)
            .GetField("possibleLocations", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(manager, new[] { location });
        manager.GenerateHintSequence();
        manager.TryDiscoverLocation("pond");

        ProceduralOrderingPuzzle generated = (ProceduralOrderingPuzzle)typeof(PuzzleManager)
            .GetField("activePuzzle", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(manager);
        string[] firstWrong = generated.Solution.ToArray();
        (firstWrong[1], firstWrong[2]) = (firstWrong[2], firstWrong[1]);

        manager.RecordActivePuzzleHintGiven();
        Assert.That(manager.GetActivePuzzleState().hints_given, Is.EqualTo(1));

        Assert.That(manager.SubmitActivePuzzle(firstWrong), Is.False);
        manager.RecordActivePuzzleHintGiven();
        Assert.That(manager.GetActivePuzzleState().hints_given, Is.EqualTo(2));

        string[] draft = generated.Symbols.Take(2).ToArray();
        manager.SaveActivePuzzleDraft(draft);
        ActivePuzzleState stateWithDraft = manager.GetActivePuzzleState();
        Assert.That(stateWithDraft.player_attempt, Is.EqualTo(draft));
        Assert.That(stateWithDraft.submitted_attempt, Is.EqualTo(firstWrong));

        string[] secondWrong = generated.Solution.Reverse().ToArray();
        if (generated.IsCorrect(secondWrong))
        {
            (secondWrong[0], secondWrong[1]) = (secondWrong[1], secondWrong[0]);
        }
        Assert.That(manager.SubmitActivePuzzle(secondWrong), Is.False);
        Assert.That(manager.GetActivePuzzleState().hints_given, Is.EqualTo(1));

        foreach (OrderingPuzzleController controller in Object.FindObjectsByType<OrderingPuzzleController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(controller.gameObject);
        }
        Object.DestroyImmediate(managerObject);
        Object.DestroyImmediate(locationObject);
    }
}
