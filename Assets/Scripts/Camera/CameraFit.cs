using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    [Header("스테이지 전체를 감싸는 콜라이더(LevelBounds)")]
    public Collider2D levelBoundsCollider;

    [Header("카메라")]
    public Camera targetCamera;

    [Header("여백(월드 유닛)")]
    public float padding = 1f;

    private IEnumerator Start()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        // 콜라이더/렌더 관련 갱신이 끝난 뒤 계산되도록 1프레임 대기
        yield return new WaitForEndOfFrame();

        FitCamera();
    }

    [ContextMenu("Fit Now")]
    public void FitCamera()
    {
        if (targetCamera == null) return;

        if (levelBoundsCollider == null)
        {
            Debug.LogWarning("[CameraFit] levelBoundsCollider가 비어있습니다. LevelBounds 콜라이더를 할당하세요.");
            return;
        }

        Bounds b = levelBoundsCollider.bounds;

        if (b.size.sqrMagnitude <= 0.0001f)
        {
            Debug.LogWarning("[CameraFit] LevelBounds bounds가 너무 작거나 유효하지 않습니다.");
            return;
        }

        // 1) 카메라 위치를 bounds 중심으로 이동
        Transform camTr = targetCamera.transform;
        camTr.position = new Vector3(b.center.x, b.center.y, camTr.position.z);

        // 2) orthographicSize 계산 (맵이 화면에 다 들어오게)
        float aspect = targetCamera.aspect;

        float halfHeight = b.extents.y + padding;
        float halfWidth = b.extents.x + padding;

        float sizeByWidth = halfWidth / aspect; // 가로가 다 들어오게 필요한 orthoSize
        targetCamera.orthographicSize = Mathf.Max(halfHeight, sizeByWidth);
    }
}
