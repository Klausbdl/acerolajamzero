using UnityEngine;
using UnityEngine.Events;

public class EndingTrigger : MonoBehaviour
{
    public UnityEvent<Collider> triggerEvent;
    private void OnTriggerEnter(Collider other)
    {
        triggerEvent.Invoke(other);
    }
}
