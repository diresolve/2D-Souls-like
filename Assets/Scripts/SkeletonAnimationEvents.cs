using UnityEngine;

public class SkeletonAnimationEvents : MonoBehaviour
{
    private SkeletonController skeleton;

    private void Awake()
    {
        skeleton = GetComponentInParent<SkeletonController>();
    }

    public void EnableWeaponHitbox()
    {
        if (skeleton != null)
            skeleton.EnableWeaponHitbox();
    }

    public void DisableWeaponHitbox()
    {
        if (skeleton != null)
            skeleton.DisableWeaponHitbox();
    }
}