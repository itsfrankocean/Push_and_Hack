using UnityEngine;

public interface IProjectileDisplaceable
{
    bool IsBusy { get; }
    Vector3 GetCurrentTilePosition();
    bool CanDisplace(Vector3 direction);
    bool Displace(Vector3 direction);
    void WarpTo(Vector3 position);
    SpriteRenderer GetDisplacementRenderer();
}
