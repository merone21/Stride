using UnityEngine;
using UnityEngine.InputSystem;

public class DoorButton : MonoBehaviour
{
    [SerializeField] Transform plunger;
    [SerializeField] Transform door;
    [SerializeField] Vector3 plungerPressLocalOffset = new Vector3(0f, -0.12f, 0f);
    [SerializeField] float doorRaiseHeight = 4f;
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float interactRadius = 4f;

    Vector3 plungerRestLocalPosition;
    Vector3 doorClosedWorldPosition;
    Vector3 doorOpenWorldPosition;
    float activationProgress;
    bool isActivated;

    public void SetDoor(Transform doorTransform)
    {
        door = doorTransform;
        CacheDoorPositions();
    }

    public void SetPlungerPressOffset(Vector3 localOffset)
    {
        plungerPressLocalOffset = localOffset;
    }

    void Awake()
    {
        CacheReferences();
    }

    void CacheReferences()
    {
        if (plunger == null)
            plunger = FindPlungerTransform();

        if (plunger != null)
            plungerRestLocalPosition = plunger.localPosition;

        if (door == null)
        {
            var doorObject = GameObject.Find("Door");
            if (doorObject != null)
                door = doorObject.transform;
        }

        CacheDoorPositions();
    }

    void CacheDoorPositions()
    {
        if (door == null)
            return;

        doorClosedWorldPosition = door.position;
        doorOpenWorldPosition = doorClosedWorldPosition + Vector3.up * doorRaiseHeight;
    }

    Transform FindPlungerTransform()
    {
        foreach (var child in GetComponentsInChildren<Transform>())
        {
            if (child == transform)
                continue;

            if (child.name.Contains("Button_"))
                return child;
        }

        return transform.childCount > 0 ? transform.GetChild(0) : null;
    }

    void Update()
    {
        if (!isActivated && IsLocalPlayerInRange() && WasInteractPressed())
            TryInteract();

        activationProgress = Mathf.MoveTowards(activationProgress, isActivated ? 1f : 0f, moveSpeed * Time.deltaTime);
        ApplyMotion();
    }

    void ApplyMotion()
    {
        if (plunger != null)
        {
            plunger.localPosition = Vector3.Lerp(
                plungerRestLocalPosition,
                plungerRestLocalPosition + plungerPressLocalOffset,
                activationProgress);
        }

        if (door != null)
            door.position = Vector3.Lerp(doorClosedWorldPosition, doorOpenWorldPosition, activationProgress);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isActivated || !IsLocalPlayerCollider(collision.collider))
            return;

        TryInteract();
    }

    public void TryInteract()
    {
        if (isActivated || !IsLocalPlayerInRange())
            return;

        var controller = PlayerSpawner.LocalPlayerInstance?.GetComponent<PlayerController>();
        if (controller != null)
            controller.RequestDoorActivation();
        else
            Activate();
    }

    public void Activate()
    {
        if (isActivated)
            return;

        isActivated = true;
        CacheDoorPositions();
    }

    bool IsLocalPlayerInRange()
    {
        var player = PlayerSpawner.LocalPlayerInstance;
        if (player == null)
            return false;

        return Vector3.Distance(player.transform.position, transform.position) <= interactRadius;
    }

    static bool IsLocalPlayerCollider(Collider other)
    {
        var controller = other.GetComponentInParent<PlayerController>();
        if (controller == null)
            return false;

        var view = controller.GetComponent<Photon.Pun.PhotonView>();
        return view != null && view.IsMine;
    }

    static bool WasInteractPressed()
    {
        var keyboard = Keyboard.current;
        return keyboard != null && keyboard.eKey.wasPressedThisFrame;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
