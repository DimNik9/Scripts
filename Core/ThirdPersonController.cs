using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using Unity.Collections;

public class ThirdPersonController : NetworkBehaviour
{
    public bool CanMove { get; private set; } = true;
    private bool IsSprinting => canSprint && Input.GetKey(sprintKey);

    private bool IsWalkingForwards => Input.GetKey(KeyCode.W);

    private bool IsWalkingBackwards => Input.GetKey(KeyCode.S);

    private bool IsReloading => Input.GetKey(KeyCode.R);
    private bool ShouldJump => Input.GetKeyDown(jumpKey) && characterController.isGrounded;

    private bool isAiming => Input.GetKey(aimKey);

    private bool isShooting => isAiming && Input.GetKey(shootKey);

    [Header("Functional Options")]
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;

    [SerializeField] private bool useFootsteps = true;

    [Header("Controls")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode aimKey = KeyCode.Mouse1;
    [SerializeField] private KeyCode shootKey = KeyCode.Mouse0;

    [Header("Movement Parameters")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float sprintSpeed = 6.0f;

    [Header("Movement Parameters")]
    [SerializeField, Range(1, 10)] private float lookSpeedX = 2.0f;
    [SerializeField, Range(1, 10)] private float lookSpeedY = 2.0f;
    [SerializeField, Range(1, 180)] private float upperLookLimit = 80.0f;
    [SerializeField, Range(1, 180)] private float lowerLookLimit = 80.0f;

    [Header("Jumping Parameters")]
    [SerializeField] private float jumpForce = 8.0f;
    [SerializeField] private float gravity = 25.0f;

    [Header("Footstep Parameters")]
    [SerializeField] private float baseStepSpeed = 0.5f;
    [SerializeField] private float sprintStepMultiplier = 0.6f;
    [SerializeField] private AudioSource footstepAudioSource = default;
    [SerializeField] private AudioClip[] roadClips = default;
    [SerializeField] private AudioClip[] grassClips = default;
    [SerializeField] private AudioClip[] metalClips = default;
    private float footstepTimer = 0f;
    private float GetCurrentOffset => IsSprinting ? baseStepSpeed * sprintStepMultiplier : baseStepSpeed;

    private Camera playerCamera;
    private CharacterController characterController;
    private Animator animator;

    private Vector3 moveDirection;
    private Vector2 currentInput;

    private float rotationX = 0f;

    public static event Action<ThirdPersonController> OnPlayerSpawned;
    public static event Action<ThirdPersonController> OnPlayerDespawned;

    public NetworkVariable<int> CurrentPoints = new NetworkVariable<int>();

    [field: SerializeField] public Health Health { get; private set; }

    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            UserData userData = null;
            if (IsHost)
            {
                userData =
                    HostSingleton.Instance.GameManager.NetworkServer.GetUserDataByClientId(OwnerClientId);
            }
            else
            {
                userData =
                    ServerSingleton.Instance.GameManager.NetworkServer.GetUserDataByClientId(OwnerClientId);
                
            }

            PlayerName.Value = userData.userName;
            OnPlayerSpawned?.Invoke(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            OnPlayerDespawned?.Invoke(this);
        }
    }

    // Start is called before the first frame update
    void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        animator.SetFloat("speed", 0f);
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
    }

    private void Start()
    {    
        if (IsOwner)
        {
            playerCamera.GetComponent<Camera>().enabled = true;
            playerCamera.GetComponent<AudioListener>().enabled = true;
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        if (CanMove)
        {
            HandleMovementInput();
            HandleMouseLook();

            if (canJump)
                HandleJump();

            if (useFootsteps)
                HandleFootsteps();

            HandleAim();
            
            ApplyFinalMovements();
        }
    }

    private void HandleMovementInput()
    {
        
        currentInput = new Vector2((IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Vertical"), (IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Horizontal"));

        if (IsWalkingForwards && !IsSprinting) {
            animator.SetFloat("speed", walkSpeed);
            animator.SetBool("isWalkingForwards", true);
        }else if (IsSprinting && IsWalkingForwards)
        {
            animator.SetFloat("speed", sprintSpeed);
            animator.SetBool("isWalkingForwards", true);
        }
        else if (IsWalkingBackwards && !IsSprinting)
        {
            animator.SetFloat("speed", walkSpeed);
            animator.SetBool("isWalkingBackwards", true);
        }
        else if (IsSprinting && IsWalkingBackwards)
        {
            animator.SetFloat("speed", sprintSpeed);
            animator.SetBool("isWalkingBackwards", true);
        }
        else
        {
            animator.SetFloat("speed", 0f);
            animator.SetBool("isWalkingForwards", false);
            animator.SetBool("isWalkingBackwards", false);
        }
       
        float moveDirectionY = moveDirection.y;
        moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x) + (transform.TransformDirection(Vector3.right) * currentInput.y);
        moveDirection.y = moveDirectionY;
    }

    private void HandleMouseLook()
    {
        rotationX -= Input.GetAxis("Mouse Y") * lookSpeedY;
        rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);

        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        transform.rotation *= Quaternion.Euler(0,Input.GetAxis("Mouse X") * lookSpeedX, 0f);
    }

    private void ApplyFinalMovements()
    {
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        characterController.Move(moveDirection* Time.deltaTime);
    }

    private void HandleJump()
    {
        if (ShouldJump)
        {
            moveDirection.y = jumpForce;
            animator.SetBool("isJumping", true);
        }else
        {
            animator.SetBool("isJumping", false);
        }

    }

    private void HandleAim()
    {
        if (isAiming) {
            animator.SetBool("isAiming", true);
        }else
        {
            animator.SetBool("isAiming", false);
        }
        if (isShooting)
        {
            animator.SetBool("isShooting", true);
        }else
        {
            animator.SetBool("isShooting", false);
        }
        if (IsReloading)
        {
            animator.SetBool("isReloading", true);
        }else
        {
            animator.SetBool("isReloading", false);
        }
    }

    private void HandleFootsteps()
    {
        if (!characterController.isGrounded) return;

        if (currentInput == Vector2.zero) return;

        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0)
        {
            if (Physics.Raycast(characterController.transform.position, Vector3.down, out RaycastHit hit, 3))
            {
                switch(hit.collider.tag)
                {
                   
                    case "Road":
                        footstepAudioSource.PlayOneShot(roadClips[UnityEngine.Random.Range(0,roadClips.Length - 1)]);
                        break;
                    case "Grass":
                        footstepAudioSource.PlayOneShot(grassClips[UnityEngine.Random.Range(0, roadClips.Length - 1)]);
                        break;
                    case "Metal":
                        footstepAudioSource.PlayOneShot(metalClips[UnityEngine.Random.Range(0, roadClips.Length - 1)]);
                        break;
                    default:
                        footstepAudioSource.PlayOneShot(grassClips[UnityEngine.Random.Range(0, roadClips.Length - 1)]);
                        break;
                }
            }

            footstepTimer = GetCurrentOffset;
        }
    }
}
