using UnityEngine;

public interface IInteractable
{
    public void Interact();
}

public interface IEntity
{
    public enum DamageSource
    {
        ENEMY,
        PLAYER,
        ENVIRONMENT
    }

    GameObject gameObject { get; }

    public void Damage(float damage, float explosionForce = 10, float explosionUpward = 1, DamageSource source = DamageSource.PLAYER);

    public void ResetEntity();

    public void ToggleEntity(bool enable);
}
