using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    [HideInInspector] public CharacterController controller;
    public Transform camAnchor;
    public Camera mainCamera;
    public Animator animator;
    public Transform[] bones;
    public bool isPlaying = false;

    [Header("Mesh renderers")]
    public SkinnedMeshRenderer[] bodyParts;

    [Header("Camera")]
    public Vector3 followOffset = new Vector3(0, 0, 0);
    public float xRot, yRot;
    public float sensitivityX = 5;
    public float sensitivityY = 5;
    public Vector3 camLocalOffset;
    public float maxZoom = 10;
    Vector3 camForward;

    [Header("Movement")]
    public LayerMask groundMask;
    public float speedMultiplier = 1;
    Vector3 velocity;
    Vector3 inputDir;
    Vector3 lastDir;
    [SerializeField] bool isGrounded;
    [SerializeField] float gravity;
    [SerializeField] float gravityMultiplier = 1;
    float rotateLerpSpeed = 10;
    public float rollDuration = .2f;
    float rollTimer;

    [Header("Combat")]
    #region combat
    public LayerMask hitableMask;
    bool lockRotation;
    int combo = 0;
    bool canAttack = true;
    int currAttackSide = -1;
    int lastAttackSide = -1; //0: left, 1: right
    public float attackDuration = .5f;
    float attackTimer;
    [Range(.01f, 1)] public float leftPunchValue;
    [Range(.01f, 1)] public float leftSwordValue;
    [Range(.01f, 1)] public float leftShootValue;
    [Space(4)]
    [Range(.01f, 1)] public float rightPunchValue;
    [Range(.01f, 1)] public float rightSwordValue;
    [Range(.01f, 1)] public float rightShootValue;
    float leftPunchPercentage;
    float leftSwordPercentage;
    float leftShootPercentage;
    float rightPunchPercentage;
    float rightSwordPercentage;
    float rightShootPercentage;
    public GameObject bulletPrefab;
    #endregion
    [Header("Attributes")]
    public int maxHp;
    public int curHp;
    public float defense = 0; //5 to 50%
    public float speed;
    public float leftDamage;
    public float rightDamage;
    public float leftAttackSpeed = 1;
    public float rightAttackSpeed = 1;
    public float leftKnockback = 1;
    public float rightKnockback = 1;
    public float jumpHeight;

    [Header("Debug")]
    public float explosionForce = 10;
    public float explosionUpward = 1;
    public float[] armsFix = new float[2] { 10, -1};
    public float[] legsFix = new float[2] { 10, -1 };
    public float shootHeightOffset = 0;
#if UNITY_EDITOR
    Vector3 debugPosition1;
    Vector3 debugPosition2;
    string debugStringUpdate = "";
#endif

    #region animator hashes
    static int speedHash = Animator.StringToHash("Speed");
    static int speedMultiHash = Animator.StringToHash("Speed Multiplier");
    static int groundedHash = Animator.StringToHash("Grounded");
    static int yVelHash = Animator.StringToHash("Y Velocity");
    static int rollHash = Animator.StringToHash("Roll");

    static int comboHash = Animator.StringToHash("Combo");
    static int leftCombatXHash = Animator.StringToHash("Left Combat X");
    static int leftCombatYHash = Animator.StringToHash("Left Combat Y");
    static int rightCombatXHash = Animator.StringToHash("Right Combat X");
    static int rightCombatYHash = Animator.StringToHash("Right Combat Y");

    static int leftAttackSpeedHash = Animator.StringToHash("Left Attack Speed");
    static int rightAttackSpeedHash = Animator.StringToHash("Right Attack Speed");
    static int attackLeftHash = Animator.StringToHash("Attack Left");
    static int attackRightHash = Animator.StringToHash("Attack Right");
    #endregion

    void Start()
    {
        controller = GetComponent<CharacterController>();

        camAnchor = GameObject.Find("Cam Anchor").transform;
        mainCamera = GameObject.Find("Creature Camera").GetComponent<Camera>();

        animator.SetFloat(speedHash, 0);
        animator.SetFloat(yVelHash, 0);
        animator.SetBool(groundedHash, true);
    }

    void Update()
    {
#if UNITY_EDITOR
        debugStringUpdate = "";
#endif
        
        ProcessCamera();

        if (!isPlaying) return;

        #region ground check---------------------------------------------
        isGrounded = Physics.CheckSphere(transform.position + new Vector3(0, .9f, 0), 1, groundMask);
        animator.SetBool(groundedHash, isGrounded);
        #endregion

        #region direction---------------------------------------------
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        camForward = mainCamera.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        inputDir = mainCamera.transform.right * x + camForward * z;
        inputDir.y = 0;

        Vector3 move = inputDir;
        Quaternion targetRotation = Quaternion.LookRotation(inputDir != Vector3.zero ? inputDir : transform.forward);
        bool aim = Input.GetAxis("Aim L") > 0 || Input.GetAxis("Aim R") > 0;
        #endregion

        #region combat---------------------------------------------
        bool attackedLeft = Input.GetButton("Fire1");
        bool attackedRight = Input.GetButton("Fire2");

        //if attack
        bool meleeAttack = attackedLeft ? leftShootPercentage < .33f : rightShootPercentage < .33f; //only push if not gun module
        
        if ((attackedLeft || attackedRight) && canAttack && isPlaying)
        {
            canAttack = false;
            
            currAttackSide = attackedLeft ? 0 : 1;
            
            ProcessCombo();
            
            animator.SetFloat(leftAttackSpeedHash, leftAttackSpeed);
            animator.SetFloat(rightAttackSpeedHash, rightAttackSpeed);
            animator.SetTrigger(attackedLeft ? attackLeftHash : attackRightHash);
            
            attackTimer = attackDuration;
            lockRotation = !meleeAttack;
            lastDir = Mathf.Abs(x) > .5f || Mathf.Abs(z) > .5f ? inputDir : transform.forward;
        }

        if (attackTimer > 0)
        {
            if (!lockRotation)
            {
                move = Mathf.Abs(x) > 0 || Mathf.Abs(z) > 0 ? lastDir : transform.forward;
                move *= (attackTimer / attackDuration) + inputDir.normalized.magnitude;

                if(move != Vector3.zero)
                    targetRotation = Quaternion.LookRotation(lastDir.normalized + inputDir.normalized * .1f);
            }
            else
                targetRotation = Quaternion.LookRotation(camForward);
            
            if(aim)
                targetRotation = Quaternion.LookRotation(camForward);

            attackTimer -= Time.deltaTime;

            if (attackTimer <= 0)
                lockRotation = false;
        }
        #endregion

        #region move body---------------------------------------------

        #region jump
        if (Input.GetButtonDown("Jump") && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * (gravity * gravityMultiplier));
        #endregion

        #region roll
        if (Input.GetButtonDown("Fire3") && isGrounded && rollTimer <= 0)
        {
            rollTimer = rollDuration;
            canAttack = false;
            animator.SetTrigger(rollHash);
            
            velocity.y = Mathf.Sqrt(jumpHeight * .2f * -2f * (gravity * gravityMultiplier));
            lastDir = inputDir != Vector3.zero ? inputDir : transform.forward;
        }

        if (rollTimer > 0)
        {
            move = (lastDir != Vector3.zero ? lastDir : transform.forward) * 2;
            move *= (rollTimer / rollDuration) + 1;

            rollTimer -= Time.deltaTime;

            if(rollTimer <= 0)
                lastDir = Vector3.zero;
        }
        #endregion

        //speedMultiplier = Mathf.Lerp(speedMultiplier,
        //    rolling ? 0.2f : (attackTimer > 0 && meleeAttack ? 0 : 1),
        //    Time.deltaTime * (rolling ? 10 : 3));
#if UNITY_EDITOR
        debugStringUpdate += $"\nx: {x} z: {z}";
        debugStringUpdate += $"\nroll timer: {rollTimer}";
        debugStringUpdate += $"\ninput: {inputDir}";
        debugStringUpdate += $"\nmove: {move}";
        debugStringUpdate += $"\nlast dir: {lastDir}";
#endif
        controller.Move(move * speed * Time.deltaTime);
        
        animator.SetFloat(speedHash, inputDir.magnitude);

        #region apply gravity
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -5f;
        }

        velocity.y += (gravity * gravityMultiplier) * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);

        animator.SetFloat(yVelHash, UtilsFunctions.Map(-5, 5, -1, 1, velocity.y));
        #endregion

        #endregion

        #region rotate body---------------------------------------------
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateLerpSpeed * Time.deltaTime);
        #endregion
    }

    private void FixedUpdate()
    {
        //fix zoom on collision
        float targetZoom;

        if (Physics.Linecast(camAnchor.position, camAnchor.position + camAnchor.forward * 10, out RaycastHit hit, groundMask))
            targetZoom = hit.distance-.2f;
        else
            targetZoom = maxZoom;

        mainCamera.transform.localPosition = new Vector3(camLocalOffset.x, camLocalOffset.y, targetZoom);

        //fix material sorting
        for(int i = 0; i < 4; i++)
        {
            Vector3 boundsPos;
            boundsPos = transform.InverseTransformPoint(bones[i].transform.position);
            boundsPos.z += i <= 1 ? armsFix[1] : legsFix[1];
            boundsPos.z *= i <= 1 ? armsFix[0] : legsFix[0];
            bodyParts[i].localBounds = new Bounds(boundsPos, Vector3.one * 2);
        }
    }

    void ProcessCamera()
    {
        Vector3 targetPosition = transform.position +
                                 transform.right * followOffset.x +
                                 transform.up * followOffset.y +
                                 transform.forward * followOffset.z;

        camAnchor.position = Vector3.Lerp(camAnchor.position, targetPosition, Time.deltaTime * (20 + speed * 2));
        
        if ((Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0) && isPlaying)
        {
            yRot += Input.GetAxis("Mouse X") * sensitivityX;
            xRot += Input.GetAxis("Mouse Y") * sensitivityY;
        }
        if (yRot < 0) yRot = 360;
        if (yRot > 360) yRot = 0;

        xRot = Mathf.Clamp(xRot, -89f, 89f);
        camAnchor.rotation = Quaternion.Euler(xRot, yRot, 0);
    }
    void ProcessCombo()
    {
        animator.SetInteger(comboHash, combo);
        if (combo == 1) combo = 2;
        else if (combo == 2 || combo == 0) combo = 1;

        if (lastAttackSide == -1) lastAttackSide = currAttackSide;
        
        if (currAttackSide != lastAttackSide)
            combo = 1;
        
        lastAttackSide = currAttackSide;
    }
    public void UpdateModulePercentages()
    {
        #region left arm
        float total = leftPunchValue + leftSwordValue + leftShootValue;
        leftPunchPercentage = leftPunchValue / total;
        leftSwordPercentage = leftSwordValue / total;
        leftShootPercentage = leftShootValue / total;
#if UNITY_EDITOR
        debugStringUpdate += $"\np:{leftPunchPercentage.ToString("0.##")}% sw:{leftSwordPercentage.ToString("0.##")}% sh:{leftShootPercentage.ToString("0.##")}%";
#endif        
        float combatX = leftPunchPercentage * 0 + leftSwordPercentage * 0.43301f + leftShootPercentage * -0.43301f;
        float combatY = leftPunchPercentage * 0.5f + leftSwordPercentage * -.25f + leftShootPercentage * -.25f;
        animator.SetFloat(leftCombatXHash, combatX);
        animator.SetFloat(leftCombatYHash, combatY);
        #endregion

        #region right arm
        total = rightPunchValue + rightSwordValue + rightShootValue;
        rightPunchPercentage = rightPunchValue / total;
        rightSwordPercentage = rightSwordValue / total;
        rightShootPercentage = rightShootValue / total;
#if UNITY_EDITOR
        debugStringUpdate += $"\np:{rightPunchPercentage.ToString("0.##")}% sw:{rightSwordPercentage.ToString("0.##")}% sh:{rightShootPercentage.ToString("0.##")}%";
#endif
        combatX = rightPunchPercentage * 0 + rightSwordPercentage * 0.43301f + rightShootPercentage * -0.43301f;
        combatY = rightPunchPercentage * 0.5f + rightSwordPercentage * -.25f + rightShootPercentage * -.25f;
        animator.SetFloat(rightCombatXHash, combatX);
        animator.SetFloat(rightCombatYHash, combatY);
        #endregion

        animator.SetFloat(speedMultiHash, speedMultiplier);
    }

    //called from animation trigger
    public void ResetCombo()
    {
        combo = 0;
        lastAttackSide = -1;
        currAttackSide = -1;
        canAttack = true;
    }
    public void CanAttackAgain(int i)
    {
        bool left = currAttackSide == 0;
        switch (i)
        {
            case 0: if((left ? leftPunchPercentage : rightPunchPercentage) >= 0.33f) canAttack = true; break;
            case 1: if((left ? leftSwordPercentage : rightSwordPercentage) >= 0.33f) canAttack = true; break;
            case 2: if((left ? leftShootPercentage : rightShootPercentage) >= 0.33f) canAttack = true; break;
        }
    }
    public void AttackCollider(int type) {
        //args: attackType        
        bool left = currAttackSide == 0;
        
        switch (type)
        {
            case 0:
                if((left ? leftPunchPercentage : rightPunchPercentage) >= 0.33f)
                    AttackHitbox(0);                
                break;
            case 1:
                if ((left ? leftSwordPercentage : rightSwordPercentage) >= 0.33f)
                    AttackHitbox(1);
                break;
            case 2:
                if ((left ? leftShootPercentage : rightShootPercentage) >= 0.33f)
                    AttackHitbox(2);
                break;
        }
    }
    public void PlaySFX(string args)
    {
        string[] audios = args.Split('_');

        foreach (string audio in audios)
        {
            switch (audio)
            {
                case "footstep":
                    AudioManager.Instance.oneShotFx.PlayOneShot(AudioManager.Instance.library.footsteps.RandomElement());
                    break;
            }
        }
    }

    void AttackHitbox(int i)
    {
        Transform boneToUse = currAttackSide == 0 ? bones[0] : bones[1];
        float armDistance = 0;
        float radius = .5f;
        float damage = currAttackSide == 0 ? leftDamage : rightDamage;
        float knockback = currAttackSide == 0 ? leftKnockback : rightKnockback;

        switch (i)
        {
            default:
            case 0:
                armDistance = 3;
                radius = .7f;
                break;
            case 1:
                armDistance = 4;
                radius = 1.3f;
                break;
            case 2:
                break;
        }

        if(i != 2) //do collider stuff
        {
            RaycastHit[] results = new RaycastHit[10];

            Vector3 p1 = boneToUse.position + (currAttackSide == 0 ? -boneToUse.right : boneToUse.right);
            Vector3 p2 = p1 + (boneToUse.up * armDistance);

            int hitCount = Physics.CapsuleCastNonAlloc(p1, p2, radius, boneToUse.up, results, Vector3.Distance(p1, p2), hitableMask);

            if (hitCount > 0)
                for (int j = 0; j < results.Length; j++)
                    if (results[j].collider != null)
                        if (results[j].collider.TryGetComponent(out IDamagable hitable))
                            hitable.Damage(damage, explosionForce * knockback, explosionUpward);
        }
        else //shoot stuff
        {
            Vector3 dir = lockRotation ?
                (transform.forward + new Vector3(0, -camAnchor.forward.y + shootHeightOffset, 0)).normalized
                : transform.forward;
            Bullet newBullet = Instantiate(bulletPrefab, boneToUse.position + boneToUse.up, Quaternion.LookRotation(dir)).GetComponent<Bullet>();
            newBullet.SetBullet(damage, 100, dir); //TODO: adjust bullet speed
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(debugPosition1, 1);
        Gizmos.DrawWireSphere(debugPosition2, 1);
        Gizmos.DrawLine(debugPosition1, debugPosition2);
        Gizmos.color = Color.red;
        if (camAnchor)
        {
            Vector3 point1 = bones[1].position;
            Vector3 point2 = point1 + (transform.forward + new Vector3(0, -camAnchor.forward.y + shootHeightOffset, 0)).normalized * 3;
            Gizmos.DrawLine(point1, point2);
            Gizmos.DrawWireSphere(point2, .3f);
        }
        
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + inputDir);
    }

    private void OnGUI()
    {
        Rect labelRect = new Rect(100, 100, 600, 1000);
        GUI.color = Color.yellow;
        GUI.skin.label.fontSize = 24;
        string debugtext = "";
        debugtext += $"combo: {combo}";
        debugtext += $"\ndebug string:\n{debugStringUpdate}";

        GUI.Label(labelRect, debugtext);
    }
#endif
}
