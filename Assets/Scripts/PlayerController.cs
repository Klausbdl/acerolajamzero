using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    CharacterController controller;
    Transform camAnchor;
    Camera mainCamera;
    Animator animator;

    [Header("Camera")]
    [SerializeField] Vector3 followOffset = new Vector3(0, 0, 0);
    float xRot, yRot;
    [SerializeField] float sensitivity = 500;
    float zoom = 10;

    [Header("Movement")]
    public LayerMask groundMask;
    Vector3 velocity;
    [SerializeField] bool isGrounded;
    [SerializeField] float gravity;
    [SerializeField] float gravityMultiplier = 1;

    [Header("Attributes")]
    public float maxHp;
    public float curHp;
    public float speed;
    public float damage;
    public float attackSpeed;
    public float jumpHeight;

    [Header("Debug")]
    Vector3 debugPosition;

    #region animator hashes
    static int speedHash = Animator.StringToHash("Speed");
    static int groundedHash = Animator.StringToHash("Grounded");
    static int yVelHash = Animator.StringToHash("Y Velocity");
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
        ProcessMovement();
        ProcessCamera();
    }

    private void LateUpdate()
    {
        //fix zoom on collision
        if (Physics.Linecast(camAnchor.position, camAnchor.position + camAnchor.forward * 10, out RaycastHit hit, groundMask))
            zoom = hit.distance;
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

        Vector3 camForward = mainCamera.transform.forward;
        camForward.y = 0;
        camForward.Normalize();
        Vector3 move = mainCamera.transform.right * x + camForward * z;

        move.y = 0;

        // jump
        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded)
            {
                //isGrounded = false;
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * (gravity * gravityMultiplier));
            }
        }

        //movement
        controller.Move(move * speed * Time.deltaTime);
        animator.SetFloat(speedHash, move.magnitude);

        //rotation
        if (move.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10 * Time.deltaTime);
        }

        //gravity
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -5f;
        }

        velocity.y += (gravity * gravityMultiplier) * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        animator.SetFloat(yVelHash, UtilsFunctions.Map(-10, 10, -1, 1, velocity.y));
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(debugPosition, 1);
    }
}
