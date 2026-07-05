using UnityEngine;

public interface IDamageable
{
    // "데미지를 입는 기능"에 대한 약속
    void TakeDamage(int damage);
}