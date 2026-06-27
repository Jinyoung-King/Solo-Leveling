using UnityEngine;
using System.Collections;

public class ObjectShaker : MonoBehaviour
{
    // 흔들림 강도와 시간
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 10f; // UI는 좌표 단위가 커서 숫자가 좀 커야 함

    private Vector3 initialPosition;
    private bool isShaking = false;

    void OnEnable()
    {
        initialPosition = transform.localPosition; // 원래 위치 기억
    }

    public void Shake()
    {
        if (!isShaking)
        {
            StartCoroutine(ShakeCoroutine());
        }
    }

    IEnumerator ShakeCoroutine()
    {
        isShaking = true;
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            // 랜덤한 위치로 순간이동 (지진 효과)
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            transform.localPosition = initialPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 원상복구
        transform.localPosition = initialPosition;
        isShaking = false;
    }
}