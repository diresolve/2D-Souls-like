using UnityEngine;

public class EnemyBoundary : MonoBehaviour
{
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        SkeletonController skeleton = other.GetComponent<SkeletonController>();

        if (skeleton == null || skeleton.currentState == SkeletonController.State.Dead)
            return;

        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 dirToCenter = (transform.position - other.transform.position).normalized;
            rb.linearVelocity = dirToCenter * 3f;
        }
    }
}