using UnityEngine;
using UnityEngine.SceneManagement;

public class MapController : MonoBehaviour
{
    // 어디서든 접근 가능하도록 싱글톤 설정
    public static MapController Instance;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 플레이어가 전기 타일에 닿았을 때 호출될 함수
    public void ResetLevel()
    {
        Debug.Log("<color=yellow>전기 타일 접촉: 맵을 초기화합니다.</color>");

        // 현재 활성화된 씬을 다시 로드하여 모든 오브젝트 상태 초기화
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
}