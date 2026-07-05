using UnityEngine;

// PushableBox의 모든 기능을 그대로 물려받습니다.
public class MetalBox : PushableBox
{
    public override bool CanRestoreAfterDamage => false;

    protected override AudioClip GetPushSound()
    {
        return AudioManager.I != null ? AudioManager.I.sfxBoxPush : null;
    }

    // 부모의 TakeDamage를 무시하고 금속 박스만의 동작을 정의합니다.
    public override void TakeDamage(int damage)
    {
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.03f, 0.02f);
        }

        // [사운드] 금속 상자는 안 부서짐
        if (AudioManager.I != null)
        {
            AudioManager.I.PlayOneShot(AudioManager.I.sfxMetalUnbreakable);
        }

        // 파괴 로직(Destroy)을 호출하지 않으므로 아무리 쏴도 파괴되지 않습니다.
        Debug.Log("금속 박스는 총알에 흠집도 나지 않습니다!");
    }
}
