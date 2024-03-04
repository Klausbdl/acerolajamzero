using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    CharacterController controller;
    Transform camAnchor;
    Camera mainCamera;
    Animator animator;
    public Transform[] bones;

    [Header("Mesh renderers")]
    public SkinnedMeshRenderer[] bodyParts;

    [Header("Camera")]
    [SerializeField] Vector3 followOffset = new Vector3(0, 0, 0);
    float xRot, yRot;
    [SerializeField] float sensitivity = 500;
    float zoom = 10;
    Vector3 camForward;

    [Header("Movement")]
    public LayerMask groundMask;
    float speedMultiplier = 1;
    Vector3 velocity;
    Vector3 move;
    [SerializeField] bool isGrounded;
    [SerializeField] float gravity;
    [SerializeField] float gravityMultiplier = 1;
    float rotationTimer;
    public float rotationDuration = .5f;
    float rotateLerpSpeed = 10;

    public float rollDuration = .2f;
    float rollTimer;
    bool rolling = false;

    [Header("Combat")]
    public LayerMask hitableMask;
    bool lockRotation;
    int combo = 0;
    bool canAttack = true;
    int currAttackSide = -1;
    int lastAttackSide = -1; //0: left, 1: right
    public float attackDuration = .5f;
    float attackTimer;
    [Range(.01f, 1)] public float leftPunchValue;
    float leftPunchPercentage;
    [Range(.01f, 1)] public float rightPunchValue;
    float rightPunchPercentage;
    Coroutine punchRoutine;
    [Range(.01f, 1)] public float leftSwordValue;
    float leftSwordPercentage;
    [Range(.01f, 1)] public float rightSwordValue;
    float rightSwordPercentage;
    Coroutine swordRoutine;
    [Range(.01f, 1)] public float leftShootValue;
    float leftShootPercentage;
    [Range(.01f, 1)] public float rightShootValue;
    float rightShootPercentage;
    Coroutine shootRoutine;

    [Header("Attributes")]
    public float maxHp;
    public float curHp;
    public float speed;
    public float damage;
    public float attackSpeed = 1;
    public float jumpHeight;

    [Header("Debug")]
    public float explosionForce = 10;
    public float explosionUpward = 1;
    public float[] armsFix = new float[2] { 10, -1};
    public float[] legsFix = new float[2] { 10, -1 };
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

    static int attackSpeedHash = Animator.StringToHash("Attack Speed");
    static int attackLeftHash = Animator.StringToHash("Attack Left");
    static int attackRightHash = Animator.StringToHash("Attack Right");
    #endregion

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        camAnchor = GameObject.Find("Cam Anchor").transform;
        mainCamera = GameObject.Find("Creature Camera").GetComponent<Camera>();
    }

    void Update()
    {
#if UNITY_EDITOR
        debugStringUpdate = "";
#endif

        ProcessMovement();
        ProcessCamera();
        ProcessCombat();
    }

    private void FixedUpdate()
    {
        //fix zoom on collision
        if (Physics.Linecast(camAnchor.position, camAnchor.position + camAnchor.forward * 10, out RaycastHit hit, groundMask))
            zoom = hit.distance-.2f;
        else
            zoom = 10;
        mainCamera.transform.localPosition = new Vector3(0, 0, zoom);

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

        if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
        {
            yRot += Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
            if (yRot < 0) yRot = 360;
            if (yRot > 360) yRot = 0;

            xRot += Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;
            xRot = Mathf.Clamp(xRot, -89f, 89f);
            camAnchor.rotation = Quaternion.Euler(xRot, yRot, 0);
        }
    }
    void ProcessMovement()
    {
        isGrounded = Physics.CheckSphere(transform.position + new Vector3(0, .9f, 0), 1, groundMask);
        animator.SetBool(groundedHash, isGrounded);

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        camForward = mainCamera.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        move = mainCamera.transform.right * x + camForward * z;
        move.y = 0;

        // jump
        if (Input.GetButtonDown("Jump") && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * (gravity * gravityMultiplier));

        //roll
        if(Input.GetButtonDown("Fire3") && isGrounded && rollTimer <= 0)
        {
            animator.SetTrigger(rollHash);
            velocity.y = Mathf.Sqrt(jumpHeight * .2f * -2f * (gravity * gravityMultiplier));
            rollTimer = rollDuration;
        }

        if(rollTimer > 0)
        {
            Vector3 move = (this.move.magnitude > 0.1f ? this.move : transform.forward) * 2;
            move *= (rollTimer / rollDuration) + 1;
            controller.Move(move * speed * Time.deltaTime);

            rollTimer -= Time.deltaTime;
        }

        //movement
        speedMultiplier = Mathf.Lerp(speedMultiplier,
            rolling ? 0.2f : (attackTimer > 0 ? 0 : 1),
            Time.deltaTime * (rolling ? 10 : 3));
        
        controller.Move(move * speed * Time.deltaTime * speedMultiplier);
        animator.SetFloat(speedHash, move.magnitude);

#if UNITY_EDITOR
        debugStringUpdate += $"\nspeed multiplier: {speedMultiplier}"; //------------------------------------------------------DEBUG
#endif

        //rotation
        if (move.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(rotationTimer > 0 && lockRotation ? camForward : move, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateLerpSpeed * Time.deltaTime);
        }

        //gravity
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -5f;
        }

        velocity.y += (gravity * gravityMultiplier) * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        animator.SetFloat(yVelHash, UtilsFunctions.Map(-5, 5, -1, 1, velocity.y));
    }
    void ProcessCombat()
    {
        if(Input.GetKeyDown(KeyCode.R))
            lockRotation = !lockRotation;
#if UNITY_EDITOR
        debugStringUpdate += $"\nlock rotation: {lockRotation}";
#endif

        //combat animator weights
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

        bool attackedLeft = Input.GetButton("Fire1");
        bool attackedRight = Input.GetButton("Fire2");

        if ((attackedLeft || attackedRight) && canAttack)
        {
            currAttackSide = attackedLeft ? 0 : 1;
            canAttack = false;
            ProcessCombo();
            animator.SetFloat(attackSpeedHash, attackSpeed);
            animator.SetTrigger(attackedLeft ? attackLeftHash : attackRightHash);
            attackTimer = attackDuration;
            rotationTimer = rotationDuration;
        }

        //push forward when attacking
        if(attackTimer > 0)
        {
            Vector3 move = lockRotation ? (this.move.magnitude > 0.1f ? this.move : camForward) : transform.forward;
            move *= (attackTimer/attackDuration) + this.move.normalized.magnitude;
            controller.Move(move * speed * Time.deltaTime);

            if (lockRotation)
            {
                Quaternion targetRotation = Quaternion.LookRotation(camForward, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 50 * Time.deltaTime);
            }

            attackTimer -= Time.deltaTime;
        }

        if(rotationTimer > 0)
            rotationTimer -= Time.deltaTime;
#if UNITY_EDITOR
        debugStringUpdate += $"\nattack timer: {attackTimer}\nrotation timer: {rotationTimer}";
        debugStringUpdate += $"\ncurrent attack: {currAttackSide}    last attack: {lastAttackSide}";
        debugStringUpdate += $"\ncan attack: {canAttack}";
#endif
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
    public void AttackCollider(string args)
    {
        //args: attackType_on/off
        if (args.Split('_').Length != 2) return;

        string type = args.Split('_')[0];
        string state = args.Split('_')[1];
        
        bool left = currAttackSide == 0;
        
        switch (type)
        {
            case "punch":
                if((left ? leftPunchPercentage : rightPunchPercentage) >= 0.33f)
                {
                    if (state == "on")
                        punchRoutine = StartCoroutine(AttackHitbox(0));
                    else
                        StopCoroutine(punchRoutine);
                }
                break;
            case "sword":
                if ((left ? leftSwordPercentage : rightSwordPercentage) >= 0.33f)
                {
                    if (state == "on")
                        swordRoutine = StartCoroutine(AttackHitbox(1));
                    else
                        StopCoroutine(swordRoutine);
                }
                break;
            case "shoot":
                if ((left ? leftShootPercentage : rightShootPercentage) >= 0.33f)
                    if (state == "on")
                        shootRoutine = StartCoroutine(AttackHitbox(2));
                break;
        }
    }

    IEnumerator AttackHitbox(int i)
    {
        var tick = new WaitForSeconds(.5f);
        float countDown = 1; //measure to stop coroutine

        Transform boneToUse = currAttackSide == 0 ? bones[0] : bones[1];
        float armDistance = 0;
        float radius = .5f;

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

            while (countDown > 0)
            {
                Vector3 p1 = boneToUse.position + (currAttackSide == 0 ? -boneToUse.right : boneToUse.right);
                Vector3 p2 = p1 + (boneToUse.up * armDistance);

                int hitCount = Physics.CapsuleCastNonAlloc(p1, p2, radius, boneToUse.up, results, Vector3.Distance(p1, p2), hitableMask);
                
                if(hitCount > 0)
                    for (int j = 0; j < results.Length; j++)
                        if (results[j].collider != null)
                            if (results[j].collider.TryGetComponent(out IDamagable hitable))
                            {
                                hitable.Damage(1); //TODO: change to damage
                                Vector3 impulseForce = (results[j].transform.position - transform.position).normalized;
                                impulseForce *= explosionForce;
                                impulseForce.y = explosionUpward;
#if UNITY_EDITOR
                                debugPosition1 = results[j].transform.position;
                                debugPosition2 = debugPosition1 + impulseForce;
#endif
                                results[j].rigidbody.AddForce(impulseForce, ForceMode.Impulse);
                            }                
                countDown -= Time.deltaTime;
                yield return tick;
            }
        }
        else //shoot stuff
        {

        }
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(debugPosition1, 1);
        Gizmos.DrawWireSphere(debugPosition2, 1);
        Gizmos.DrawLine(debugPosition1, debugPosition2);
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
