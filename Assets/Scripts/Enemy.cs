using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using static IEntity;

public class Enemy : MonoBehaviour, IEntity
{
    NavMeshAgent agent;
    Rigidbody rb;
    Animator animator;
    public SpriteRenderer spriteRenderer;
    public float originalHp = 5;
    public float hp;
    public float speed;
    public float damage;
    public LayerMask groundLayer;
    public LayerMask hitMask;
    
    public float recoverDuration = 3;
    public float recoverTimer;
    bool canBeHit = true;
    float attackTimer = 0;

    Vector3 originalPos;
    Transform playerTransform;

    public GameObject spawnParticle;
    public GameObject hitParticle;
    public GameObject dieParticle;

    Collider[] hits = new Collider[10];

#if UNITY_EDITOR
    string debugString;
#endif

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        gameObject.layer = 7;
        originalPos = transform.position;
    }

    public void ResetEntity()
    {
        hp = GetMaxHp() - 4;
        transform.position = originalPos;
        agent.enabled = true;
        rb.isKinematic = true;
        canBeHit = true;
        animator.SetTrigger("Hit");
        damage *= 1 + (GameManager.Instance.PlayerAttributes.Level / 200f);
    }

    public float GetMaxHp()
    {
        //y\ =v\cdot.2 + d\cdot.3 + a\cdot.2 + s\cdot.4 + D\cdot.4 + j\cdot.2\ +\ 3
        PlayerAttributes p = GameManager.Instance.PlayerAttributes;
        return p.vitality * .2f + p.defense * .3f + p.agility * .2f + p.strength * .4f + p.dexterity * .4f + p.jump * .2f + originalHp;
    }

    void Update()
    {
        if (Vector3.Distance(originalPos, playerTransform.position) < 150)
        {
            if (agent.enabled == false) agent.enabled = true;

            GoToDestination(playerTransform.position);
        }
        else
        {
            if (agent.enabled == true)
                agent.enabled = false;
        }
        
        if (agent.enabled)
        {
            attackTimer -= Time.deltaTime;

            if(attackTimer < 0)
            {
                Vector3 hitPos = transform.position + new Vector3(0, 2, 0) + transform.forward;
                int hitCount = Physics.OverlapSphereNonAlloc(hitPos, 5, hits, hitMask);

                for (int i = 0; i < hitCount; i++)
                {
                    if (hits[i].TryGetComponent(out IEntity d) && canBeHit)
                    {
                        d.Damage(damage, 0, 0, DamageSource.ENEMY);
                    }
                }
                attackTimer = 1f;
            }
        }
        spriteRenderer.transform.rotation = Quaternion.identity;
    }

    public void Damage(float damage, float force, float upward, DamageSource source = DamageSource.PLAYER)
    {
        if (source == DamageSource.ENEMY) return;
        hp -= damage;

        animator.SetTrigger("Hit");
        Instantiate(hitParticle, transform.position + new Vector3(0, 4, 0), Quaternion.identity);
        
        float volume = Mathf.Clamp01(Vector3.Distance(transform.position, GameManager.Instance.playerController.transform.position) / 50f);

        if (hp <= 0)
        {
            GameManager.Instance.KillEnemy(gameObject, (int)GetMaxHp());
            gameObject.SetActive(false);
            Instantiate(dieParticle, transform.position + new Vector3(0, 4, 0), Quaternion.identity);
            
            AudioManager.Instance.oneShotFx.PlayOneShot(AudioManager.Instance.library.enemyDie, 1 - volume);
            StopAllCoroutines();
            return;
        }

        recoverTimer += .1f;

        if (canBeHit)
            StartCoroutine(TakeDamage(damage, force, upward));

        Vector3 impulseForce = (transform.position - GameManager.Instance.playerController.transform.position).normalized;
        impulseForce *= force * .25f;
        impulseForce.y = upward * .8f;

        rb.AddForce(impulseForce, ForceMode.Impulse);

        AudioManager.Instance.oneShotFx.PlayOneShot(AudioManager.Instance.library.hit.RandomElement(), 1 - volume);
    }

    IEnumerator TakeDamage(float damage, float force, float upward)
    {
        spriteRenderer.color = Color.red;

        canBeHit = false;

        while (agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
            yield return new WaitForEndOfFrame();
        }

        recoverTimer = recoverDuration;

        rb.isKinematic = false;

        yield return new WaitForEndOfFrame();

        spriteRenderer.color = Color.white;

        Vector3 impulseForce = (transform.position - GameManager.Instance.playerController.transform.position).normalized;
        impulseForce *= force;
        impulseForce.y = upward;

        rb.AddForce(impulseForce, ForceMode.Impulse);

        while (true)
        {
            recoverTimer -= Time.deltaTime;
#if UNITY_EDITOR
            debugString = recoverTimer.ToString(); //TODO: debug
#endif

            if ((Physics.CheckSphere(transform.position, .5f, groundLayer) && recoverTimer < 0) || recoverTimer < -20f )
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

    public void ToggleEntity(bool enable)
    {
        agent.enabled = enable;
        rb.isKinematic = true;

        playerTransform = GameManager.Instance.playerController.transform;

        if (enable)
            Instantiate(spawnParticle, transform.position + new Vector3(0, 2, 0), Quaternion.identity);
    }
    
    #if UNITY_EDITOR
    private void OnGUI()
    {
        Rect labelRect = new Rect(500, 100, 600, 1000);
        GUI.color = Color.green;
        GUI.skin.label.fontSize = 24;
        string s = rb.velocity.magnitude.ToString();
        s += $"\n {hp}";
        s += "\n" + debugString;

        GUI.Label(labelRect, s);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(originalPos, 5);
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position + new Vector3(0, 2, 0) + transform.forward, 5);
    }
#endif
}
