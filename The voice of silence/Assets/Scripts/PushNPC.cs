using UnityEngine;

public class PushNPC : MonoBehaviour
{
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider == null)
        {
            return;
        }
    }
}
