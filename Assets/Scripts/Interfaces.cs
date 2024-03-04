using UnityEngine;

public interface IInteractable
{
    public void Interact();
}

public interface IDamagable
{
    //public abstract void Damage();
    
    public void Damage(float damage);
}
