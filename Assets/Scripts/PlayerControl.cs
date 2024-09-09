using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerControl : MonoBehaviour
{
    // Movement & jumping

    [SerializeField] private CharacterController characterController;
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 3f;
    [SerializeField] private float gravity = -20f;
    private Vector3 direction;
    private Vector3 velocity;

    // Ground check

    [SerializeField] private float maxStepHeight = 1f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDist = 0.4f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private bool isGrounded;

    // Stairs check

    [SerializeField] private Transform stairsCheck;
    [SerializeField] private Transform stairsRaycast;
    [SerializeField] private LayerMask stairsMask;
    [SerializeField] private bool stairsAhead;
    [SerializeField] private float stepHeight = 0f;
    //private Transform raycastStart;
    private float raycastLength = 0.1f;

    // Dashing

    [SerializeField] private bool isDashing = false;
    [SerializeField] private float dashSpeed = 50f;
    [SerializeField] private float dashTime = 0.2f;
    [SerializeField] private float dashTimeLeft;
    [SerializeField] private bool haveIDashed = false;
    private float dashCooldown = 1f;
    private float lastDashTime = 0f;

    // Bunnyhopping

    [SerializeField] private float timeOnTheGround;
    [SerializeField] private Text bunnychain;
    [SerializeField] private Text playerSpeed;
    private float landTime;
    private float bunnyHopChain = 0;

    // Frontflip

    [SerializeField] private float flipDuration = 0.5f;
    [SerializeField] private float flipAngle = 360f;
    [SerializeField] private bool isFlipping = false;

    // Crouching

    private Vector3 standingScale;
    private Vector3 crouchingScale = new Vector3(1f, 0.5f, 1f);
    [SerializeField] private float crouchSlideSpeed = 20f;
    [SerializeField] private float crouchDownForce = -5f;
    [SerializeField] private bool isCrouching = false;
    [SerializeField] private bool isSliding = false;
    [SerializeField] private float slideTime = 0f;
    [SerializeField] private float slideDuration = 3f;
    [SerializeField] private float crouchMoveSpeed = 3f;

    // Used to calculate velocity

    private Vector3 previousPosition;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        standingScale = transform.localScale;
    }   

    private void Update()
    {
        stairsDetection();
        bool wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDist, groundMask);

        if (!wasGrounded && isGrounded)
        {
            landTime = Time.time;
            if (bunnyHopChain > 0 && Time.time - landTime > 0.5f)
            {
                bunnyHopChain = 0;
            }
        }

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // just to make sure the character actually stays on the ground
        }

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        direction = new Vector3(moveX, 0f, moveZ).normalized;

        velocity.y += gravity * Time.deltaTime; // gravity. each time a player is in the air, he will be pulled towards the ground by this force. otherwise he would just float. gravity set to -20f to increase the pull and make falling more dynamic, regular earth's gravity would be -9.81f

        if (isDashing)
        {
            if (Input.GetKey(KeyCode.W))
            {
                characterController.Move(transform.forward * dashSpeed * Time.deltaTime);
            }
            if (Input.GetKey(KeyCode.S))
            {
                characterController.Move(-transform.forward * dashSpeed * Time.deltaTime);
            }
            if (Input.GetKey(KeyCode.A))
            {
                characterController.Move(-transform.right * dashSpeed * Time.deltaTime);
            }
            if (Input.GetKey(KeyCode.D))
            {
                characterController.Move(transform.right * dashSpeed * Time.deltaTime);
            }

            dashTimeLeft -= Time.deltaTime;

            if (dashTimeLeft < 0)
            {
                isDashing = false;
                dashTimeLeft = 0;
            }
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            if (Time.time - landTime < 0.5f)
            {
                bunnyHopChain++;
                velocity.y = Mathf.Sqrt((jumpForce + bunnyHopChain) * -2f * gravity);
            }
            else
            {
                bunnyHopChain = 0;
                velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity); // v^2 = u^2 + 2*a*s, where v = final velocity, u = initial velocity (0), a = -g, s = desired height; v = sqrt(-2*g*h)
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftAlt) && !isDashing && (haveIDashed ? Time.time >= lastDashTime + dashCooldown : true))
        {
            Dash();
        }

        if (Input.GetKeyDown(KeyCode.LeftControl) && isGrounded)
        {
            EnterCrouch();
        }

        if (Input.GetKeyUp(KeyCode.LeftControl) && isGrounded)
        {
            ExitCrouch();
        }

        Vector3 movementCombined = Vector3.zero;

        if (isSliding)
        {
            slideTime += Time.deltaTime;
            if (slideTime < slideDuration)
            {
                movementCombined = velocity;
            }
            else
            {
                isSliding = false;
                velocity = Vector3.zero;
            }
        }
        else
        {
            float currentSpeed = isCrouching ? crouchMoveSpeed : moveSpeed;
            movementCombined = direction * currentSpeed;
        }

        movementCombined.y = velocity.y;
        characterController.Move(movementCombined * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.F) && !isGrounded && !isFlipping)
        {
            //StartCoroutine(frontflip());
        }

        calcSpeedAndBHopChainForTest();
    }

    private void Dash()
    {
        isDashing = true;
        dashTimeLeft = dashTime;
        lastDashTime = Time.time;
        haveIDashed = true;
    }

    private IEnumerator frontflip() // works but in a situation where the capsule is flipped on the Z axis it isn't useful. change targetRotation so that the flipAngle is at Z and rest stays at 0 to make it work with a rotated capsule
    {
        isFlipping = true;
        float flipTime = 0f;

        Vector3 currentRotation = transform.localEulerAngles;
        Vector3 targetRotation = currentRotation + new Vector3(flipAngle, 0f, 0f);

        while (flipTime < flipDuration)
        {
            Vector3 actualRotation = Vector3.Lerp(currentRotation, targetRotation, flipTime / flipDuration);
            transform.localEulerAngles = actualRotation;
            flipTime += Time.deltaTime;
            yield return null;
        }
        transform.localEulerAngles = targetRotation;
        isFlipping = false;
    }


    private void EnterCrouch()
    {
        isCrouching = true;
        transform.localScale = crouchingScale;
        transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
        velocity.y = crouchDownForce;

        if (velocity.magnitude > 2f && isGrounded)
        {
            isSliding = true;
            slideTime = 0f;
            Vector3 slideDirection = new Vector3(direction.x, 0f, direction.z).normalized;
            velocity = slideDirection * crouchSlideSpeed;
        }
        else
        {
            isSliding = false;
            velocity = Vector3.zero;
        }
    }

    private void ExitCrouch()
    {
        isCrouching = false;
        transform.localScale = standingScale;
        transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
        isSliding = false;
        slideTime = 0f;
        velocity = Vector3.zero;
    }

    private void stairsDetection()
    {
        RaycastHit hit;
        if (Physics.Raycast(stairsRaycast.position, transform.forward, out hit, raycastLength, stairsMask))
        {
            stepHeight = hit.point.y - stairsCheck.position.y;
            if (stepHeight <= maxStepHeight)
            {
                stairsAhead = true;
                ClimbStep(stepHeight);
            }
            else
            {
                stairsAhead = false;
            }
        }
        else
        {
            stairsAhead = false;
        }
    }

    private void ClimbStep(float stepHeight)
    {
        if (stairsAhead)
        {
            Vector3 actualStep = new Vector3(0, stepHeight, 0);
            characterController.Move(actualStep);
        }
    }

    private void calcSpeedAndBHopChainForTest()
    {
        Vector3 currentPosition = transform.position;
        Vector3 calculatedVelocity = (currentPosition - previousPosition) / Time.deltaTime;
        previousPosition = currentPosition;
        float speed = calculatedVelocity.magnitude;

        bunnychain.text = "Bunnyhop chain: " + bunnyHopChain.ToString("F0");
        playerSpeed.text = "Current speed: " + speed.ToString("F2");
    }
}
