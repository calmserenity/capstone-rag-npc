using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class BackendClient : MonoBehaviour
{
    [SerializeField] private string backendBaseUrl = "http://localhost:5000";
    [SerializeField] private int timeoutSeconds = 30;

    public IEnumerator CheckHealth(Action<bool, string> onComplete)
    {
        using UnityWebRequest request = UnityWebRequest.Get($"{backendBaseUrl}/health");
        request.timeout = timeoutSeconds;
        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        string message = success ? request.downloadHandler.text : request.error;
        onComplete?.Invoke(success, message);
    }

    public IEnumerator SendChat(
        string playerQuery,
        GameState gameState,
        Action<ChatResponse> onSuccess,
        Action<string> onError)
    {
        ChatRequest chatRequest = new ChatRequest
        {
            player_query = playerQuery,
            game_state = gameState
        };

        string json = JsonUtility.ToJson(chatRequest);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new UnityWebRequest($"{backendBaseUrl}/chat", "POST");
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = timeoutSeconds;
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.error);
            yield break;
        }

        ChatResponse response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
        onSuccess?.Invoke(response);
    }
}
