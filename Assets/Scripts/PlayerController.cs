using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.ProBuilder;

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
    public float attackPushDuration = .5f;
    float attackPushTimer;
    float comboTimer;
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
    int dominantLeft;
    int dominantRight;
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
    public float slopeMapOffset = 0.5f;
    public float groundedYVel = 5f;
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

        if ((attackedLeft || attackedRight) && isPlaying && canAttack)
        {
            canAttack = false;
            Debug.Log("attacking");
            currAttackSide = attackedLeft ? 0 : 1;
            animator.SetBool(attackLeftHash, attackedLeft);
            animator.SetBool(attackRightHash, attackedRight);

            attackPushTimer = attackPushDuration;

            float punchValue = attackedLeft ? leftPunchPercentage : rightPunchPercentage;
            float swordValue = attackedLeft ? leftSwordPercentage : rightSwordPercentage;
            float shootValue = attackedLeft ? leftShootPercentage : rightShootPercentage;
            
            lockRotation = shootValue >= punchValue && shootValue >= swordValue;
            
            lastDir = Mathf.Abs(x) > .5f || Mathf.Abs(z) > .5f ? inputDir : transform.forward;
        }

        if (attackPushTimer > 0)
        {
            if (!lockRotation)
            {
                move = Mathf.Abs(x) > 0 || Mathf.Abs(z) > 0 ? lastDir : transform.forward;
                move *= (attackPushTimer / attackPushDuration) + inputDir.normalized.magnitude;

                if(move != Vector3.zero)
                    targetRotation = Quaternion.LookRotation(lastDir.normalized + inputDir.normalized * .1f);
            }
            else
                targetRotation = Quaternion.LookRotation(camForward);
            
            if(aim)
                targetRotation = Quaternion.LookRotation(camForward);

            attackPushTimer -= Time.deltaTime;

            if (attackPushTimer <= 0)
                lockRotation = false;
        }

        if(comboTimer > 0)
            comboTimer -= Time.deltaTime;
        #endregion

        #region move body---------------------------------------------

        #region jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * (gravity * gravityMultiplier));
            AudioManager.Instance.oneShotFx.PlayOneShot(AudioManager.Instance.library.jump);
        }
        #endregion

        #region roll
        if (Input.GetButtonDown("Fire3") && isGrounded && rollTimer <= 0)
        {
            rollTimer = rollDuration;
            canAttack = false;
            animator.SetTrigger(rollHash);
            
            velocity.y = Mathf.Sqrt(jumpHeight * .2f * -2f * (gravity * gravityMultiplier));
            lastDir = inputDir != Vector3.zero ? inputDir : transform.forward;

            AudioManager.Instance.oneShotFx.PlayOneShot(AudioManager.Instance.library.dash);
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

        #region ground check
        Vector3 checkOffset = new Vector3(move.x, (-0.4f * move.magnitude) + 0.9f, move.z);
        float slopeAngle;
        if (Physics.Raycast(transform.position + checkOffset, -transform.up, out RaycastHit hit, Mathf.Infinity, groundMask))
            slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
        else
            slopeAngle = 0;

        checkOffset.y -= UtilsFunctions.Map(0, 45, 0, slopeMapOffset, slopeAngle);

        Vector3 checkCaps1 = transform.position + checkOffset;
        checkCaps1.y = transform.position.y + .9f;
        Vector3 checkCaps2 = transform.position + checkOffset;
        isGrounded = Physics.CheckCapsule(checkCaps1, checkCaps2, 1, groundMask);
        animator.SetBool(groundedHash, isGrounded);
#if UNITY_EDITOR
        debugPosition1 = checkCaps1;
        debugPosition2 = checkCaps2;
#endif
        #endregion

#if UNITY_EDITOR
        debugStringUpdate += $"\nx: {x} z: {z}";
        debugStringUpdate += $"\nroll timer: {rollTimer}";
        debugStringUpdate += $"\ninput: {inputDir}";
        debugStringUpdate += $"\nmove: {move} mag: {move.magnitude}";
        debugStringUpdate += $"\nslope: {slopeAngle}";
        debugStringUpdate += $"\nlast dir: {lastDir}";
        debugStringUpdate += $"\nattack timer: {attackPushTimer}";
        debugStringUpdate += $"\ncan attack: {canAttack}";
#endif
        controller.Move(move * speed * Time.deltaTime);
        
        animator.SetFloat(speedHash, move.normalized.magnitude);
        animator.SetLayerWeight(1, Mathf.Lerp(animator.GetLayerWeight(1), attackPushTimer > 0 && move.magnitude > 0 ? 1 : 0, Time.deltaTime * 5));

        #region apply gravity
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -groundedYVel;
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

        //camAnchor.position = Vector3.Lerp(camAnchor.position, targetPosition, Time.deltaTime * (20 + speed * 2));
        camAnchor.position = targetPosition;
        
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
        animator.SetFloat(leftAttackSpeedHash, leftAttackSpeed);
        animator.SetFloat(rightAttackSpeedHash, rightAttackSpeed);

        if (leftPunchValue > leftSwordValue && leftPunchValue > leftShootValue)
            dominantLeft = 0;
        else if (leftSwordValue > leftPunchValue && leftSwordValue > leftShootValue)
            dominantLeft = 1;
        else if (leftShootValue > leftPunchValue && leftShootValue > leftSwordValue)
            dominantLeft = 2;

        if (rightPunchValue > rightSwordValue && rightPunchValue > rightShootValue)
            dominantRight = 0;
        else if (rightSwordValue > rightPunchValue && rightSwordValue > rightShootValue)
            dominantRight = 1;
        else if (rightShootValue > rightPunchValue && rightShootValue > rightSwordValue)
            dominantRight = 2;

    }

    //called from animation trigger
    public void ProcessCombo(int i)
    {
        if (comboTimer > 0) return;

        if ((currAttackSide == 0 ? dominantLeft : dominantRight) == i)
        {
            Debug.Log("process combo: " + i);
            NextCombo();
        }
    }
    void NextCombo()
    {
        if (combo == 1) combo = 2;
        else if (combo == 2 || combo == 0) combo = 1;
        
        animator.SetInteger(comboHash, combo);

        if (lastAttackSide == -1) lastAttackSide = currAttackSide;

        if (currAttackSide != lastAttackSide)
            combo = 1;

        comboTimer = .1f;

        lastAttackSide = currAttackSide;

        animator.SetBool(attackLeftHash, false);
        animator.SetBool(attackRightHash, false);

        AudioManager.Instance.oneShotFx.PlayOneShot(AudioManager.Instance.library.whoosh.RandomElement());
    }
    public void ResetCombo(int i)
    {
        if ((currAttackSide == 0 ? dominantLeft : dominantRight) == i)
        {
            Debug.Log("reset combo: " + i);
            ResetCombo();
        }
    }
    void ResetCombo()
    {
        combo = 0;
        lastAttackSide = -1;
        currAttackSide = -1;
        canAttack = true;
        animator.SetInteger(comboHash, combo);
    }
    public void CanAttackAgain(int i)
    {
        if ((currAttackSide == 0 ? dominantLeft : dominantRight) == i)
        {
            Debug.Log("can attack again: " + i);
            canAttack = true;
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
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(debugPosition1, 1);
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(debugPosition2, 1);
        Gizmos.color = Color.yellow;
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
