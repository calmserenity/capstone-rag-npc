using UnityEngine;
using UnityEngine.UI;

public class RockDialogueController : MonoBehaviour
{
    [SerializeField] private BackendClient backendClient;
    [SerializeField] private GameStateBuilder gameStateBuilder;
    [SerializeField] private InputField questionInput;
    [SerializeField] private Text responseText;
    [SerializeField] private Button askButton;

    private void Awake()
    {
        if (askButton != null)
        {
            askButton.onClick.AddListener(AskRock);
        }
    }

    public void AskRock()
    {
        if (backendClient == null || gameStateBuilder == null || questionInput == null)
        {
            SetResponse("Rock is not connected yet.");
            return;
        }

        string question = questionInput.text.Trim();
        if (string.IsNullOrEmpty(question))
        {
            SetResponse("Mmm... ask Rock with a few little words.");
            return;
        }

        SetInteractable(false);
        SetResponse("Rock is listening...");

        GameState gameState = gameStateBuilder.Build();
        StartCoroutine(backendClient.SendChat(
            question,
            gameState,
            response =>
            {
                gameStateBuilder.SpendCluePoint();
                SetResponse(response.npc_response);
                SetInteractable(true);
            },
            error =>
            {
                SetResponse($"Rock cannot hear the garden right now. {error}");
                SetInteractable(true);
            }));
    }

    private void SetResponse(string message)
    {
        if (responseText != null)
        {
            responseText.text = message;
        }
    }

    private void SetInteractable(bool value)
    {
        if (askButton != null)
        {
            askButton.interactable = value;
        }

        if (questionInput != null)
        {
            questionInput.interactable = value;
        }
    }
}
