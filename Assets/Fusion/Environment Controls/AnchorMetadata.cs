using UnityEngine;

public class AnchorMetadata : MonoBehaviour
{
    public double creationTime;

    void Awake()
    {
        creationTime = Time.timeAsDouble; // Use network time if available
    }
} 