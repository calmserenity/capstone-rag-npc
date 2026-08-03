using UnityEngine;

public class HintLocation : MonoBehaviour
{
    [SerializeField] private string locationId;
    [SerializeField] private string displayName;
    [TextArea]
    [SerializeField] private string riddle;

    public string LocationId => locationId;
    public string DisplayName => displayName;
    public string Riddle => riddle;

    public void Configure(string id, string name, string clue)
    {
        locationId = id;
        displayName = name;
        riddle = clue;
    }
}
