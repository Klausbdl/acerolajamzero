using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    CharacterController controller;
    Transform camAnchor;
    Camera mainCamera;
    Animator animator;

    [Header("Mesh renderers")]
    public SkinnedMeshRenderer leftArm;
    public SkinnedMeshRenderer rightArm;
    public SkinnedMeshRenderer leftLeg;
    public SkinnedMeshRenderer rightLeg;
    public SkinnedMeshRenderer body;

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
    float rotationDuration = .5f;
    float rotateLerpSpeed = 10;
    bool rolling = false;

    [Header("Combat")]
    bool lockRotation;
    int combo = 0;
    int currAttackSide = -1;
    int lastAttackSide = -1; //0: left, 1: right
    float attackTimer;
    float attackDuration = .2f;

    [Header("Attributes")]
    public float maxHp;
    public float curHp;
    public float speed;
    public float damage;
    public float attackSpeed;
    public float jumpHeight;

    [Range(.01f, 1)] public float punchValue;
    [Range(.01f, 1)] public float swordValue;
    [Range(.01f, 1)] public float shootValue;

    [Header("Debug")]
    Vector3 debugPosition;
    string debugString = "";

    #region animator hashes
    static int speedHash = Animator.StringToHash("Speed");
    static int speedMultiHash = Animator.StringToHash("Speed Multiplier");
    static int groundedHash = Animator.StringToHash("Grounded");
    static int yVelHash = Animator.StringToHash("Y Velocity");
    static int rollHash = Animator.StringToHash("Roll");

    static int comboHash = Animator.StringToHash("Combo");
    static int combatXHash = Animator.StringToHash("Combat X");
    static int combatYHash = Animator.StringToHash("Combat Y");
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
        debugString = "";

        ProcessMovement();
        ProcessCamera();
        ProcessCombat();
    }

    private void LateUpdate()
    {
        //fix zoom on collision
        if (Physics.Linecast(camAnchor.position, camAnchor.position + camAnchor.forward * 10, out RaycastHit hit, groundMask))
            zoom = hit.distance-.2f;
        else
            zoom = 10;
        mainCamera.transform.localPosition = new Vector3(0, 0, zoom);
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
        if(Input.GetButtonDown("Fire3") && isGrounded && !rolling)
        {
            animator.SetTrigger(rollHash);
            StartCoroutine(RollRoutine((move.magnitude > 0.1f ? move : transform.forward) * 2, .5f, 0));
        }

        //movement
        speedMultiplier = Mathf.Lerp(speedMultiplier,
            rolling ? 0.2f : (attackTimer > 0 ? 0 : 1),
            Time.deltaTime * (rolling ? 10 : 3));
        
        controller.Move(move * speed * Time.deltaTime * speedMultiplier);
        animator.SetFloat(speedHash, move.magnitude);
        
        debugString += $"\nspeed multiplier: {speedMultiplier}"; //------------------------------------------------------DEBUG

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
        debugString += $"\nlock rotation: {lockRotation}";

        //combat animator weights
        float total = punchValue + swordValue + shootValue;
        float punch = punchValue / total;
        float sword = swordValue / total;
        float shoot = shootValue / total;
        //debugString = $"p:{punch.ToString("0.##")}% sw:{sword.ToString("0.##")}% sh:{shoot.ToString("0.##")}%";
        
        float combatX = punch * 0 + sword * 0.43301f + shoot * -0.43301f;
        float combatY = punch * 0.5f + sword * -.25f + shoot * -.25f;
        animator.SetFloat(combatXHash, combatX);
        animator.SetFloat(combatYHash, combatY);

        bool attackedLeft = Input.GetButtonDown("Fire1");
        bool attackedRight = Input.GetButtonDown("Fire2");

        if ((attackedLeft || attackedRight) && attackTimer <= 0)
        {
            currAttackSide = attackedLeft ? 0 : 1;
            ProcessAttack();
            animator.SetTrigger(attackedLeft ? attackLeftHash : attackRightHash);
            attackTimer = attackDuration;
            rotationTimer = rotationDuration;
        }

        if(attackTimer > 0)
        {
            Vector3 move = lockRotation ? (this.move.magnitude > 0.1f ? this.move : camForward) : transform.forward;
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

        debugString += $"\nattack timer: {attackTimer}\nrotation timer: {rotationTimer}";
    }

    void ProcessAttack()
    {
        animator.SetInteger(comboHash, combo);
        combo++;

        if (lastAttackSide == -1) lastAttackSide = currAttackSide;
        
        if (currAttackSide != lastAttackSide)
            combo = 1;
        if (combo == 3) combo = 1;
        
        lastAttackSide = currAttackSide;
    }

    //called from animation trigger
    public void ResetCombo()
    {
        combo = 0;
        lastAttackSide = -1;
        currAttackSide = -1;
    }

    IEnumerator RollRoutine(Vector3 direction, float duration = 0.2f, float alphaSpeed = 3)
    {
        if (rolling) yield break;

        rolling = true;
        float alpha = 1;
        float tick = 0;
        while (tick < duration)
        {
            Vector3 move = direction * alpha;
            controller.Move(move * speed * Time.deltaTime);
            tick += Time.deltaTime;
            alpha -= Time.deltaTime * alphaSpeed;
            if (alpha < 0) alpha = 0;
            yield return null;
        }

        rolling = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(debugPosition, 1);
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        Rect labelRect = new Rect(100, 100, 600, 1000);
        GUI.color = Color.yellow;
        GUI.skin.label.fontSize = 24;
        string debugtext = "";
        debugtext += $"combo: {combo}";
        debugtext += $"\ndebug string:\n{debugString}";

        GUI.Label(labelRect, debugtext);
    }
#endif
}
