using UnityEngine;

public class PlayerCameraFollow : MonoBehaviour
{
    private Transform playerTransform;

    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0, 13f, -13f); // raised + behind
    public float smoothSpeed = 0.125f;
    public Vector3 lookAtOffset = new Vector3(0, 0, 0); // tweak if you want to look slightly ahead of player
    bool playerFound = false;

    void LateUpdate()
    {
        if (playerTransform == null && !playerFound)
        {
            // Find the first player in the scene with the Player tag
            GameObject playerGameObject = GameObject.FindWithTag("Player");
            if (playerGameObject != null)
            {
                playerTransform = playerGameObject.transform;
                playerFound = true;
            }
        }

        if (playerTransform == null) return;
        // Desired position (player position + offset)
        Vector3 desiredPosition = playerTransform.position + offset;

        // Smoothly move to position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        transform.position = smoothedPosition;

        // Look at the player (or slightly offset)
        transform.LookAt(playerTransform.position + lookAtOffset);
    }
}

