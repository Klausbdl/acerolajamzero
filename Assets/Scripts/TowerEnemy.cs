using System.Collections;
using UnityEngine;
using static IEntity;

public class TowerEnemy : MonoBehaviour, IEntity
{
    public float hp = 100;
    public float damage = 100;
    public Transform aimTransform;
    public GameObject laserObj;
    public GameObject explosionVFX;
    public AudioSource explosionSFX;
    public AudioSource laserSFX;
    public AudioSource windupSFX;
    public LayerMask groundMask;
    public LayerMask hitMask;
    bool active = true;

    Transform targetTransform;
    Vector3 trackTarget;
    Material redMat;

    bool runningRoutine;
    bool isTracking;
    public float trackSpeed = 1;
    public float trackDuration = 5;
    public float windUpTime = 3;
    public float trackDelay = 1;
    public float attackDelay = .2f;
    float attackTimer = 0;
    float checkTimer = 0;
    Collider[] hits = new Collider[10];

    void Start()
    {
        laserObj.SetActive(false);
        aimTransform.GetComponent<MeshRenderer>().enabled = false;
        explosionVFX.GetComponent<ParticleSystem>().Stop();
        redMat = GetComponent<MeshRenderer>().materials[1];
    }

    void Update()
    {
        if(!active) return;

        if (!runningRoutine)
        {
            checkTimer -= Time.deltaTime;
            if (checkTimer <= 0)
            {
                checkTimer = .5f;
                int hitCount = Physics.OverlapSphereNonAlloc(transform.position, 40, hits, hitMask);

                for (int i = 0; i < hitCount; i++)
                {
                    if (hits[i].transform == transform) continue;

                    if (hits[i].TryGetComponent(out IEntity _))
                    {
                        targetTransform = hits[i].transform;
                        StartCoroutine(TrackTarget());
                        runningRoutine = true;
                        return;
                    }
                }
            }

            return;
        }

        if (isTracking && !GameManager.Instance.pause && active)
        {
            aimTransform.rotation = Quaternion.Lerp(aimTransform.rotation, Quaternion.LookRotation(aimTransform.position - trackTarget), Time.deltaTime * trackSpeed);

            if(attackTimer > 0)
            {
                attackTimer -= Time.deltaTime;
            }

            bool hit = Physics.Raycast(aimTransform.position, -aimTransform.forward, out RaycastHit hitInfo, 1000, groundMask);
            if (hit)
            {
                explosionVFX.transform.position = hitInfo.point;

                if (attackTimer <= 0)
                {
                    attackTimer = attackDelay;

                    int hitCount = Physics.OverlapSphereNonAlloc(hitInfo.point, 7, hits, hitMask);

                    for (int i = 0; i < hitCount; i++)
                    {
                        if (hits[i].TryGetComponent(out IEntity d))
                        {
                            d.Damage(damage, 0, 0, DamageSource.ENVIRONMENT);
                        }
                    }
                }
            }
        }
    }

    IEnumerator TrackTarget()
    {
        runningRoutine = true;
        float timer = trackDuration;
        windupSFX.volume = 2;
        windupSFX.Play();
        
        trackTarget = targetTransform.position;

        yield return new WaitForSeconds(windUpTime);
        
        aimTransform.rotation = Quaternion.LookRotation(aimTransform.position - trackTarget);

        //som de laser
        laserSFX.Play();
        laserSFX.volume = 1;
        explosionSFX.Play();
        explosionSFX.volume = 1;

        isTracking = true;

        laserObj.SetActive(true);
        aimTransform.GetComponent<MeshRenderer>().enabled = true;
        explosionVFX.GetComponent<ParticleSystem>().Play();

        while (timer > 0)
        {
            float waitStartTime = Time.realtimeSinceStartup;

            trackTarget = targetTransform.position;

            yield return new WaitForSeconds(trackDelay);

            float waitTime = Time.realtimeSinceStartup - waitStartTime;
            timer -= Mathf.Min(trackDelay, waitTime);
        }
        
        isTracking = false;

        laserObj.SetActive(false);
        aimTransform.GetComponent<MeshRenderer>().enabled = false;
        explosionVFX.GetComponent<ParticleSystem>().Stop();
        explosionSFX.Stop();
        explosionSFX.volume = 0;
        laserSFX.Stop();
        laserSFX.volume = 0;

        targetTransform = null;
        runningRoutine = false;
    }

    public void Damage(float damage, float explosionForce = 10, float explosionUpward = 1, DamageSource source = DamageSource.PLAYER)
    {
        if (source == DamageSource.ENVIRONMENT || hp <= 0) return;

        float volume = Mathf.Clamp01(Vector3.Distance(transform.position, GameManager.Instance.playerController.transform.position) / 50f);
        AudioManager.Instance.oneShotFx.PlayOneShot(AudioManager.Instance.library.hitMetal.RandomElement(), 1 - volume);

        hp -= damage;
        
        if(hp <= 0)
        {
            active = false;
            redMat.SetColor("_EmissionColor", Color.black);
            AudioManager.Instance.oneShotFx.PlayOneShot(AudioManager.Instance.library.towerShutdown, 1 - volume);
            StopAllCoroutines();
            
            isTracking = false;

            laserObj.SetActive(false);
            aimTransform.GetComponent<MeshRenderer>().enabled = false;
            explosionVFX.GetComponent<ParticleSystem>().Stop();
            explosionSFX.Stop();
            explosionSFX.volume = 0;
            laserSFX.Stop();
            laserSFX.volume = 0;

            GameManager.Instance.KillEnemy(gameObject, 20);
            targetTransform = null;
            runningRoutine = false;
            return;
        }
    }

    public void ResetEntity()
    {
        hp = 100;
        active = true;
        redMat.SetColor("_EmissionColor", Color.red);
    }

    public void ToggleEntity(bool enable)
    {
        active = enable;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, 40);
    }
#endif
}
