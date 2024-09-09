using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float smoothness;
    private Vector3 distance;
    private Vector3 currentSpeed = Vector3.zero;

    private void Awake()
    {
        distance = transform.position - player.position;
    }

    private void LateUpdate()
    {
        Vector3 playerPosition = player.position + distance;
        transform.position = Vector3.SmoothDamp(transform.position, playerPosition, ref currentSpeed, smoothness);
    }

}
