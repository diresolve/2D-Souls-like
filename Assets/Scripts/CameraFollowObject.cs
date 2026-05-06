using UnityEngine;
using System.Collections;

public class CameraFollowObject : MonoBehaviour
{
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private float _playerRotationTime = 0.5f;

    private PlayerController _player;
    private bool _isFacingRight;

    private void Awake()
    {
        _player = _playerTransform.gameObject.GetComponent<PlayerController>();
        _isFacingRight = _player.IsFacingRight;
    }

    private void LateUpdate()
    {
        transform.position = _playerTransform.position;
    }

    public void CallTurn()
    {
        StartCoroutine(FlipYLerp());
    }

    private IEnumerator FlipYLerp()
    {
        float startRotation = transform.localEulerAngles.y;
        float endRotationAmount = DetermineEndRotation();
        float elapsedTime = 0f;

        while (elapsedTime < _playerRotationTime)
        {
            elapsedTime += Time.deltaTime;
            float yRotation = Mathf.Lerp(startRotation, endRotationAmount, elapsedTime / _playerRotationTime);
            transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
            yield return null;
        }
    }

    public float DetermineEndRotation()
    {
        _isFacingRight = _player.IsFacingRight;

        if (_isFacingRight)
        {
            return 180f;
        }
        else
        {
            return 0f;
        }
    }
}