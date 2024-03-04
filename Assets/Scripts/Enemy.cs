using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour, IDamagable
{
    NavMeshAgent agent;
    Rigidbody rb;
    public float hp;
    public float speed;
    
    bool canBeHit = true;
    public float recoverDuration = 3;
    public float recoverTimer;


    public LayerMask groundLayer;

#if UNITY_EDITOR
    string debugString;
#endif

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        rb = GetComponent<Rigidbody>();
        gameObject.layer = 7;
    }

    void Update()
    {
        if (agent.enabled)
            GoToDestination(GameManager.Instance.playerController.transform.position);
    }

    public virtual void Damage(float damage)
    {
        hp -= damage;

        if (hp < 0)
        {
            Destroy(gameObject);
        }

        if (canBeHit)
            StartCoroutine(TakeDamage(damage));
    }

    IEnumerator TakeDamage(float damage)
    {
        canBeHit = false;

        while (agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
            yield return new WaitForEndOfFrame();
        }

        recoverTimer = recoverDuration;

        rb.isKinematic = false;

        while (true)
        {
            recoverTimer -= Time.deltaTime;
#if UNITY_EDITOR
            debugString = recoverTimer.ToString(); //TODO: debug
#endif

            if (Physics.CheckSphere(transform.position, .5f, groundLayer) && recoverTimer < 0)
            {
                agent.enabled = true;
                rb.isKinematic = true;
                canBeHit = true;
                yield break;
            }

            yield return null;
        }
    }

    virtual public void GoToDestination(Vector3 destination)
    {
        NavMeshHit hit;
        Vector3 finalPosition = Vector3.zero;
        if (NavMesh.SamplePosition(destination, out hit, 10, 1))
            finalPosition = hit.position;

        agent.SetDestination(finalPosition);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if(agent != null && !agent.enabled)
            Gizmos.DrawWireSphere(transform.position, .5f);
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        Rect labelRect = new Rect(500, 100, 600, 1000);
        GUI.color = Color.green;
        GUI.skin.label.fontSize = 24;
        string s = rb.velocity.magnitude.ToString();
        s += "\n" + debugString;

        GUI.Label(labelRect, s);
    }
#endif
}
