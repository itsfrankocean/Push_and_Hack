using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    // 더 이상 Start에서 고정하지 않고, 흔들기 직전 위치를 저장합니다.
    private Vector3 currentCenterPos;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Shake(float duration, float magnitude)
    {
        // 흔들림 시작 시점의 카메라 위치(CameraFit이 잡아준 위치)를 기억
        currentCenterPos = transform.position;

        StopAllCoroutines();
        StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            // 기억해둔 중심점(currentCenterPos)을 기준으로 흔듭니다.
            transform.position = new Vector3(currentCenterPos.x + x, currentCenterPos.y + y, currentCenterPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 흔들림이 끝나면 CameraFit이 잡아준 원래 위치로 정확히 복구
        transform.position = currentCenterPos;
    }
}