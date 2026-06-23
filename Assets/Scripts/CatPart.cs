using UnityEngine;

[System.Serializable]
public struct CatPartReaction
{
    public int satisfactionChange;
    public int angerChange;
    public string message;
}

public class CatPart : MonoBehaviour
{
    [Header("Part")]
    public string partName = "Head";
    public CatGroomingGame game;
    public int clickPriority;

    [Header("Hand Reaction")]
    public int handSatisfaction = 1;
    public int handAnger = 0;
    [TextArea]
    public string handMessage = "The cat lets you touch this place.";

    [Header("Brush Reaction")]
    public int brushSatisfaction = 10;
    public int brushAnger = 0;
    [TextArea]
    public string brushMessage = "The cat enjoys being brushed here.";

    private void Awake()
    {
        if (game == null)
        {
            game = FindFirstObjectByType<CatGroomingGame>();
        }
    }

    public CatPartReaction GetReaction(GroomingTool tool)
    {
        if (tool == GroomingTool.Hand)
        {
            return new CatPartReaction
            {
                satisfactionChange = handSatisfaction,
                angerChange = handAnger,
                message = partName + ": " + handMessage
            };
        }

        return new CatPartReaction
        {
            satisfactionChange = brushSatisfaction,
            angerChange = brushAnger,
            message = partName + ": " + brushMessage
        };
    }
}
