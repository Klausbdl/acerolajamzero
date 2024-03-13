using UnityEngine;

public class Bullet : MonoBehaviour
{
    float damage;
    float speed;

    Rigidbody rb;

    public void SetBullet(float damage, float speed, Vector3 direction)
    {
        this.damage = damage;
        this.speed = speed;
        rb = GetComponent<Rigidbody>();
        rb.velocity = direction * speed;
        Invoke(nameof(InvokeDestroy), 5);
    }

    void InvokeDestroy()
    {
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent(out IEntity hitable))
        {
            hitable.Damage(damage);
            Destroy(gameObject);
        }
    }
}
