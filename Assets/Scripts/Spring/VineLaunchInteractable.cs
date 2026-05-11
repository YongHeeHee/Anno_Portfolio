using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class VineLaunchInteractable : MonoBehaviour
{
    public Vector2 AnchorPosition => transform.position;
}
