using System.Collections;
using UnityEngine;

public class HeavyDoor : MonoBehaviour
{
    [SerializeField] private float openDistance = 4f;
    [SerializeField] private float openDuration = 2.5f;

    private bool isOpened = false;

    public void Interact()
    {
        if (!isOpened)
        {
            StartCoroutine(OpenDoorRoutine());
        }
    }

    private IEnumerator OpenDoorRoutine()
    {
        isOpened = true;

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + new Vector3(0f, openDistance, 0f);
        float elapsedTime = 0f;

        while (elapsedTime < openDuration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / openDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
    }
}