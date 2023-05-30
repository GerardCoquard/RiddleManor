using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;
    [Header("References")]
    [SerializeField] public CharacterController characterController;
    [SerializeField] Animator myAnimator;
    [SerializeField] Animator mirrorAnimator;

    [Header("Movement")]
    public float maxLinealSpeed = 7f;
    public float acceleration;
    [SerializeField] float rotationFractionPerFrame = 45f;
    Vector3 movement;
    private Vector2 tempDirection;
    private Vector2 movementAcceleration;


    [Header("Jumping")]
    [SerializeField] private float maxJumpHeight = 4;
    [SerializeField] private float maxJumpTime = 0.5f;
    [SerializeField] private float gravityIncreseValue;
    [SerializeField] private float accelerationOnAirMultiplier;
    [SerializeField] private float linealSpeedAirMultiplier;
    bool onGround;
    float jumpForce;
    float gravity;
    float initialGravity;
    bool isJumping;
    bool isPushing;

    [Header("Push")]
    [SerializeField] float closeEnoughtDetection = 0.3f;
    [SerializeField] float detectionHeight = 0.6f;
    [SerializeField] float pushForce = 40f;
    [SerializeField] float angleDot = 0.8f;
    [SerializeField] float distanceBetween = 0.1f;
    [SerializeField] Transform pushStartDetectionPoint;
    PusheableObject currentObjectPushing;
    bool bookOpened;

    public delegate void BookActivated();
    public delegate void PlayerActivated();
    public delegate void ObjectPushed();
    public delegate void StoppedPushing();
    public event BookActivated OnBookActivated;
    public event PlayerActivated OnPlayerActivated;
    public event ObjectPushed OnObjectPushed;
    public event StoppedPushing OnStoppedPushing;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else Destroy(this);
        SetUpJumpVariables();
        pushStartDetectionPoint.position = transform.position + new Vector3(0, detectionHeight, 0) + transform.forward * closeEnoughtDetection;
    }
    private void OnEnable()
    {
        InputManager.GetAction("Move").action += OnMovementInput;
        InputManager.GetAction("ChangeMode").action += OnChangeModeInput;
        InputManager.GetAction("Push").action += OnPushInput;
        InputManager.GetAction("Jump").action += OnJumpInput;
    }
    private void OnDisable()
    {
        InputManager.GetAction("Move").action -= OnMovementInput;
        InputManager.GetAction("ChangeMode").action -= OnChangeModeInput;
        InputManager.GetAction("Push").action -= OnPushInput;
        InputManager.GetAction("Jump").action -= OnJumpInput;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(pushStartDetectionPoint.position, pushStartDetectionPoint.position + transform.forward * closeEnoughtDetection);
    }
    void OnPushInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (currentObjectPushing != null) StopPushing();
            else CheckPush();
        }
    }
    void OnChangeModeInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            SwapControl();
        }
    }
    private void OnJumpInput(InputAction.CallbackContext context)
    {
        if (context.started) Jump();
    }

    private void OnMovementInput(InputAction.CallbackContext context)
    {
        tempDirection = context.ReadValue<Vector2>();
        tempDirection.Normalize();
    }
    public void SwapControl()
    {
        if (isJumping || !onGround) return;
        if (bookOpened)
        {
            if (CanInteract()) return; //?
            InputManager.GetAction("Move").action += OnMovementInput;
            InputManager.GetAction("Push").action += OnPushInput;
            InputManager.GetAction("Jump").action += OnJumpInput;
            bookOpened = false;
            characterController.enabled = true;
            if (InputManager.GetAction("Move").GetEnabled())
            {
                tempDirection = InputManager.GetAction("Move").context.ReadValue<Vector2>().normalized;
            }
            movement = Vector3.zero;
            OnPlayerActivated?.Invoke();
        }
        else
        {
            StopPushing();
            InputManager.GetAction("Move").action -= OnMovementInput;
            InputManager.GetAction("Push").action -= OnPushInput;
            InputManager.GetAction("Jump").action -= OnJumpInput;
            bookOpened = true;
            movement = Vector3.zero;
            characterController.enabled = false;
            OnBookActivated?.Invoke();
        }
    }
    private void SetUpJumpVariables()
    {
        maxJumpHeight += maxJumpHeight * 0.05f;
        float timeToApex = maxJumpTime / 2f; //The time to reach the maximum height of the jump.
        gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        initialGravity = gravity;
        jumpForce = (2 * maxJumpHeight) / timeToApex;
    }

    private void FixedUpdate()
    {
        if (currentObjectPushing != null && !isJumping) Push();
    }

    private void HandleRotation()
    {
        Vector3 positionToLookAt;

        positionToLookAt.x = movement.x;
        positionToLookAt.y = 0.0f;
        positionToLookAt.z = movement.z;
        Quaternion currentRotation = transform.rotation;

        if (tempDirection != Vector2.zero && positionToLookAt != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, rotationFractionPerFrame * Time.deltaTime);
        }
    }
    void Update()
    {
        if (characterController.enabled)
        {
            HandleRotation();
            HandleAcceleration();
            CollisionFlags collisionFlags = characterController.Move(movement * Time.deltaTime);
            CheckCollision(collisionFlags);
        }
        myAnimator.SetBool("isMoving", tempDirection != Vector2.zero);
        if (mirrorAnimator != null) mirrorAnimator.SetBool("isMoving", tempDirection != Vector2.zero);
        CheckPushAvailable();
        SetGravity();

        if (isJumping && !onGround && movement.y < 0.1f)
        {
            myAnimator.SetTrigger("isFalling");
        }
    }

    private void HandleAcceleration()
    {
        if (tempDirection != Vector2.zero)
        {
            movementAcceleration += isJumping ? tempDirection * acceleration * accelerationOnAirMultiplier * Time.deltaTime : tempDirection * acceleration * Time.deltaTime;
            movementAcceleration = Vector2.ClampMagnitude(movementAcceleration, tempDirection.magnitude);
        }
        else
        {

            movementAcceleration -= movementAcceleration * acceleration * Time.deltaTime;
            movementAcceleration = Vector2.ClampMagnitude(movementAcceleration, 1);

        }

        Vector2 currentSpeedVector = isJumping ? maxLinealSpeed * movementAcceleration * linealSpeedAirMultiplier : maxLinealSpeed * movementAcceleration;
        movement.x = currentSpeedVector.x;
        movement.z = currentSpeedVector.y;
    }
    private void Jump()
    {
        if (CanJump())
        {
            movement.y = jumpForce * .5f;
            isJumping = true;
            onGround = false;
            myAnimator.SetTrigger("Jump");
            if (mirrorAnimator != null) mirrorAnimator.SetTrigger("Jump");

        }

    }
    void CheckPushAvailable()
    {
        PusheableObject pusheable;
        if (CanInteract() && PusheableDetected(out pusheable, out RaycastHit hit))
        {
            WorldScreenUI.instance.SetIcon(IconType.Push, pusheable.uiPosition.position);
        }
        else WorldScreenUI.instance.HideIcon(IconType.Push);
    }
    public bool CanInteract()
    {
        if (isJumping || !onGround) return false;
        if (currentObjectPushing != null) return false;
        if (bookOpened) return false;
        if (!characterController.enabled) return false;
        return true;
    }
    bool PusheableDetected(out PusheableObject pusheable, out RaycastHit hit)
    {
        pusheable = null;
        hit = new RaycastHit();

        if (isJumping || !onGround) return false;

        Ray ray = new Ray(pushStartDetectionPoint.position, transform.forward);


        if (Physics.Raycast(ray, out hit, closeEnoughtDetection, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            pusheable = hit.collider.GetComponentInParent<PusheableObject>();
            if (pusheable != null && Vector3.Dot(transform.forward, -hit.normal) >= angleDot)
            {
                transform.forward = -hit.normal;
                return true;
            }
        }
        return false;
    }
    void CheckPush()
    {
        PusheableObject pusheable;
        RaycastHit hit;
        if (PusheableDetected(out pusheable, out hit) && CanInteract())
        {
            currentObjectPushing = pusheable;
            currentObjectPushing.MakePusheable();
            characterController.enabled = false;
            transform.position = new Vector3(hit.point.x, transform.position.y, hit.point.z) + hit.normal * (characterController.radius + distanceBetween);
            transform.forward = -hit.normal;
            transform.SetParent(currentObjectPushing.transform);
            isPushing = true;
            myAnimator.SetBool("isPushing", true);
            if (mirrorAnimator != null) mirrorAnimator.SetBool("isPushing", true);
            OnObjectPushed?.Invoke();
        }
    }
    void StopPushing()
    {
        if (currentObjectPushing == null) return;
        OnStoppedPushing?.Invoke();
        transform.SetParent(null);
        currentObjectPushing.NotPusheable();
        isPushing = false;
        myAnimator.SetBool("isPushing", false);
        if (mirrorAnimator != null) mirrorAnimator.SetBool("isPushing", false);
        currentObjectPushing = null;
        characterController.enabled = true;
    }
    private void Push()
    {

        currentObjectPushing.AddForceTowardsDirection(pushForce, tempDirection);
        HandlePushAnimation();
        if (mirrorAnimator != null)
        {
            mirrorAnimator.SetFloat("VelX", tempDirection.x);
            mirrorAnimator.SetFloat("VelZ", tempDirection.y);
        }
    }

    private void HandlePushAnimation()
    {
        Vector2 newDirection;
        float radianAngle = transform.localEulerAngles.y * Mathf.Deg2Rad;

        if (tempDirection == new Vector2(0, 1))
        {
            if (transform.localEulerAngles.y == 180) newDirection = new Vector2(Mathf.Sin(radianAngle), Mathf.Cos(radianAngle));
            else newDirection = new Vector2(-Mathf.Sin(radianAngle), Mathf.Cos(radianAngle));

        }
        else if (tempDirection == new Vector2(0, -1))
        {
            if (transform.localEulerAngles.y == 180) newDirection = new Vector2(Mathf.Sin(radianAngle), -Mathf.Cos(radianAngle));
            else newDirection = new Vector2(Mathf.Sin(radianAngle), Mathf.Cos(radianAngle));

        }
        else if (tempDirection == new Vector2(1, 0))
        {
            if (transform.localEulerAngles.y == 180) newDirection = new Vector2(-Mathf.Cos(radianAngle), Mathf.Sin(radianAngle));
            else newDirection = new Vector2(Mathf.Cos(radianAngle), Mathf.Sin(radianAngle));

        }
        else if (tempDirection == new Vector2(-1, 0))
        {
            newDirection = new Vector2(Mathf.Cos(radianAngle), -Mathf.Sin(radianAngle));
        }
        else
        {
            newDirection = Vector2.zero;
        }



        myAnimator.SetFloat("VelX", Mathf.RoundToInt(newDirection.x));
        myAnimator.SetFloat("VelZ", Mathf.RoundToInt(newDirection.y));
    }

    private bool CanJump()
    {
        return currentObjectPushing == null && !isJumping && onGround;
    }

    void SetGravity()
    {

        if (isJumping || onGround)
        {
            gravity -= gravityIncreseValue * Time.deltaTime;
        }

        float previousYVelocity = movement.y;
        float newYVelocity = movement.y + (gravity * Time.deltaTime);
        float nextYVelocity = (previousYVelocity + newYVelocity) * .5f;

        movement.y = nextYVelocity;

    }

    void CheckCollision(CollisionFlags collisionFlag)
    {
        if ((collisionFlag & CollisionFlags.Above) != 0 && movement.y > 0.0f)
        {
            movement.y = 0.0f;
        }

        if ((collisionFlag & CollisionFlags.Below) != 0 && movement.y < 0.0f)
        {
            if (isJumping) { myAnimator.SetTrigger("Landed"); mirrorAnimator.SetTrigger("Landed"); }
            movement.y = 0.0f;
            gravity = initialGravity;
            isJumping = false;
            onGround = true;
        }
    }
    public Vector2 GetDirection()
    {
        if (currentObjectPushing != null) return tempDirection;
        else
        {
            Vector2 dir = new Vector2(movement.x, movement.z);
            dir = dir.normalized * dir.magnitude / maxLinealSpeed;
            return dir.magnitude > 0.01 ? dir : Vector2.zero;
        }
    }

}

