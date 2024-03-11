using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour, IDamagable
{
    NavMeshAgent agent;
    Rigidbody rb;
    public float maxHp = 5;
    public float hp;
    public float speed;
    public LayerMask groundLayer;
    Vector3 originalPos;

    bool canBeHit = true;
    public float recoverDuration = 3;
    public float recoverTimer;

    public GameObject hitParticle;
    public GameObject dieParticle;

    [SerializeField] private Shader shader;
    Material[] materials;

#if UNITY_EDITOR
    string debugString;
#endif

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        rb = GetComponent<Rigidbody>();
        gameObject.layer = 7;
        hp = maxHp;
        originalPos = transform.position;
        //materials
        materials = GetComponentInChildren<MeshRenderer>().materials;

        foreach (Material mat in materials)
        {
            if (mat.shader == shader)
            {
                mat.SetFloat("_Hurt_Strength", 0);
                mat.SetFloat("_Hurt_Noise_Speed", 0);
            }
        }
    }

    void ResetEnemey()
    {
        hp = maxHp;
        transform.position = originalPos;

        foreach (Material mat in materials)
        {
            if (mat.shader == shader)
            {
                mat.SetFloat("_Hurt_Strength", 0);
                mat.SetFloat("_Hurt_Noise_Speed", 0);
            }
        }
    }

    void Update()
    {
        if (agent.enabled)
            GoToDestination(GameManager.Instance.playerController.transform.position);
    }

    public virtual void Damage(float damage, float force, float upward)
    {
        hp -= damage;

        Instantiate(hitParticle, transform.position + new Vector3(0, 1.17f, 0), Quaternion.identity);

        foreach (Material mat in materials)
        {
            if (mat.shader == shader)
            {
                mat.SetFloat("_Hurt_Strength", Mathf.Lerp(0, 0.6f, 1 - hp / maxHp));
                mat.SetFloat("_Hurt_Noise_Speed", Mathf.Lerp(0, 0.1f, 1 - hp / maxHp));
            }
        }

        if (hp <= 0)
        {
            GameManager.Instance.KillEnemy(this);
            gameObject.SetActive(false);
            Instantiate(dieParticle, transform.position, Quaternion.identity);
            AudioManager.Instance.oneShotFx.PlayOneShot(AudioManager.Instance.library.enemyDie);
            return;
        }

        recoverTimer += .1f;

        if (canBeHit)
            StartCoroutine(TakeDamage(damage, force, upward));

        Vector3 impulseForce = (transform.position - GameManager.Instance.playerController.transform.position).normalized;
        impulseForce *= force * .25f;
        impulseForce.y = upward * .8f;

        rb.AddForce(impulseForce, ForceMode.Impulse);

        AudioManager.Instance.oneShotFx.PlayOneShot(AudioManager.Instance.library.hit.RandomElement());
    }

    IEnumerator TakeDamage(float damage, float force, float upward)
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

        yield return new WaitForEndOfFrame();

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
        s += $"\n {hp}";
        s += "\n" + debugString;

        GUI.Label(labelRect, s);
    }
#endif
}
