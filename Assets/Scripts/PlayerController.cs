using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PhotonView))]
public class PlayerController : MonoBehaviourPun, IPunObservable
{
    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int GroundedHash = Animator.StringToHash("Grounded");
    static readonly int JumpHash = Animator.StringToHash("Jump");
    static readonly int DoubleJumpHash = Animator.StringToHash("DoubleJump");

    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float jumpForce = 6f;
    [SerializeField] float doubleJumpForce = 5f;
    [SerializeField] float mouseSensitivity = 0.15f;
    [SerializeField] float groundCheckDistance = 0.2f;
    [SerializeField] float fallLimitY = -10f;

    Rigidbody body;
    CapsuleCollider capsule;
    Animator animator;
    PlayerCameraFollow cameraFollow;
    Vector3 lastPosition;
    Quaternion networkRotation;
    float yaw;
    int jumpsUsed;
    int jumpEventToSync;
    bool isGrounded;
    bool wasGrounded = true;
    bool hasFinished;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        body.freezeRotation = true;
        networkRotation = transform.rotation;
        yaw = transform.eulerAngles.y;

        if (photonView.IsMine)
            body.interpolation = RigidbodyInterpolation.Interpolate;
        else
            body.isKinematic = true;

        lastPosition = transform.position;
    }

    void Start()
    {
        if (!photonView.IsMine)
            return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        PlayerSpawner.LocalPlayerInstance = gameObject;
        SetupLocalCamera();
    }

    void OnDestroy()
    {
        if (photonView != null && photonView.IsMine)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (photonView != null && photonView.IsMine && PlayerSpawner.LocalPlayerInstance == gameObject)
            PlayerSpawner.LocalPlayerInstance = null;
    }

    void Update()
    {
        if (hasFinished)
            return;

        UpdateGrounded();

        if (!photonView.IsMine)
        {
            UpdateAnimator();
            ApplyRemoteRotation();
            return;
        }

        HandleMouseLook();
        TryJump();
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine || hasFinished)
            return;

        var input = ReadMoveInput();
        var move = transform.right * input.x + transform.forward * input.y;

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        var velocity = body.linearVelocity;
        velocity.x = move.x * moveSpeed;
        velocity.z = move.z * moveSpeed;
        body.linearVelocity = velocity;

        if (transform.position.y < fallLimitY)
            RespawnAtStart();
    }

    void TryJump()
    {
        if (!WasJumpPressed())
            return;

        if (!isGrounded && jumpsUsed == 1)
        {
            PerformJump(true);
            jumpsUsed = 2;
            return;
        }

        if (!isGrounded || jumpsUsed != 0)
            return;

        PerformJump(false);
        jumpsUsed = 1;
    }

    void RespawnAtStart()
    {
        var spawnPosition = PlayerSpawner.GetSpawnPosition(PhotonNetwork.LocalPlayer.ActorNumber);

        if (PhotonNetwork.InRoom)
            photonView.RPC(nameof(RPC_Respawn), RpcTarget.All, spawnPosition, yaw);
        else
            RPC_Respawn(spawnPosition, yaw);
    }

    [PunRPC]
    void RPC_Respawn(Vector3 spawnPosition, float spawnYaw)
    {
        yaw = spawnYaw;
        var rotation = Quaternion.Euler(0f, yaw, 0f);

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(spawnPosition, rotation);
        body.position = spawnPosition;
        body.rotation = rotation;

        jumpsUsed = 0;
        isGrounded = false;
        wasGrounded = true;
        lastPosition = spawnPosition;
    }

    public void ReportFinish()
    {
        if (!photonView.IsMine || hasFinished)
            return;

        hasFinished = true;

        if (PhotonNetwork.InRoom)
            photonView.RPC(nameof(RPC_ReportFinish), RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
        else
            RPC_ReportFinish(PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : 1);
    }

    [PunRPC]
    void RPC_ReportFinish(int actorNumber)
    {
        if (GameFinishManager.Instance != null)
            GameFinishManager.Instance.RegisterFinish(actorNumber);
    }

    public void RequestDoorActivation()
    {
        if (!photonView.IsMine)
            return;

        photonView.RPC(nameof(RPC_ActivateDoor), RpcTarget.AllBuffered);
    }

    [PunRPC]
    void RPC_ActivateDoor()
    {
        var doorButton = FindAnyObjectByType<DoorButton>();
        if (doorButton != null)
            doorButton.Activate();
    }

    void PerformJump(bool isDoubleJump)
    {
        isGrounded = false;

        if (animator != null)
        {
            if (isDoubleJump)
            {
                animator.ResetTrigger(JumpHash);
                animator.SetTrigger(DoubleJumpHash);
                jumpEventToSync = 2;
            }
            else
            {
                animator.ResetTrigger(DoubleJumpHash);
                animator.SetTrigger(JumpHash);
                jumpEventToSync = 1;
            }

            animator.Update(0f);
        }

        var velocity = body.linearVelocity;
        velocity.y = 0f;
        body.linearVelocity = velocity;
        body.AddForce(Vector3.up * (isDoubleJump ? doubleJumpForce : jumpForce), ForceMode.Impulse);
    }

    void HandleMouseLook()
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        yaw += mouse.delta.x.ReadValue() * mouseSensitivity;
        var rotation = Quaternion.Euler(0f, yaw, 0f);
        transform.rotation = rotation;
        body.rotation = rotation;

        cameraFollow?.AddPitchInput(mouse.delta.y.ReadValue() * mouseSensitivity);
    }

    void ApplyRemoteRotation()
    {
        var rotation = Quaternion.Slerp(transform.rotation, networkRotation, 12f * Time.deltaTime);
        transform.rotation = rotation;
        body.rotation = rotation;
    }

    void UpdateAnimator()
    {
        if (animator == null)
            return;

        float speed = photonView.IsMine
            ? new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z).magnitude
            : (transform.position - lastPosition).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

        animator.SetFloat(SpeedHash, speed);
        animator.SetBool(GroundedHash, isGrounded);

        if (!photonView.IsMine && wasGrounded && !isGrounded)
            animator.SetTrigger(JumpHash);

        wasGrounded = isGrounded;
        lastPosition = transform.position;
    }

    void SetupLocalCamera()
    {
        var follow = FindFirstObjectByType<PlayerCameraFollow>();
        if (follow == null)
        {
            var cameraObject = GameObject.FindGameObjectWithTag("MainCamera");
            if (cameraObject == null)
                return;

            follow = cameraObject.GetComponent<PlayerCameraFollow>();
            if (follow == null)
                follow = cameraObject.AddComponent<PlayerCameraFollow>();
        }

        cameraFollow = follow;
        follow.SetTarget(transform);
    }

    void UpdateGrounded()
    {
        var groundedNow = CheckGrounded();

        if (groundedNow && !isGrounded)
            jumpsUsed = 0;

        isGrounded = groundedNow;
    }

    bool CheckGrounded()
    {
        if (capsule == null)
        {
            var rayOrigin = transform.position + Vector3.up * 0.1f;
            return Physics.Raycast(rayOrigin, Vector3.down, GetGroundCheckDistance(), ~0, QueryTriggerInteraction.Ignore);
        }

        var worldCenter = transform.TransformPoint(capsule.center);
        var halfHeight = capsule.height * 0.5f - capsule.radius;
        var bottom = worldCenter - Vector3.up * halfHeight;
        var sphereOrigin = bottom + Vector3.up * (capsule.radius * 0.25f);

        return Physics.SphereCast(sphereOrigin, capsule.radius * 0.85f, Vector3.down, out _,
            groundCheckDistance, ~0, QueryTriggerInteraction.Ignore);
    }

    float GetGroundCheckDistance()
    {
        if (capsule != null)
            return capsule.height * 0.5f + groundCheckDistance;

        return 1.1f;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.rotation);
            stream.SendNext(jumpEventToSync);
            jumpEventToSync = 0;
        }
        else
        {
            networkRotation = (Quaternion)stream.ReceiveNext();
            var jumpEvent = (int)stream.ReceiveNext();

            if (animator == null)
                return;

            if (jumpEvent == 1)
                animator.SetTrigger(JumpHash);
            else if (jumpEvent == 2)
                animator.SetTrigger(DoubleJumpHash);
        }
    }

    static Vector2 ReadMoveInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return Vector2.zero;

        float x = 0f;
        float y = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;

        return new Vector2(x, y);
    }

    static bool WasJumpPressed()
    {
        var keyboard = Keyboard.current;
        return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
    }
}
