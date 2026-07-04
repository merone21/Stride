using UnityEngine;

public class PlayerCameraFollow : MonoBehaviour
{
    [SerializeField] float height = 2f;
    [SerializeField] float distance = 5f;
    [SerializeField] float positionSmooth = 10f;
    [SerializeField] float lookAtHeight = 1.4f;
    [SerializeField] float defaultPitch = 15f;
    [SerializeField] float minPitch = -30f;
    [SerializeField] float maxPitch = 70f;

    Transform target;
    float pitch;

    void Awake()
    {
        pitch = defaultPitch;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void AddPitchInput(float delta)
    {
        pitch -= delta;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    void LateUpdate()
    {
        if (target == null && PlayerSpawner.LocalPlayerInstance != null)
            target = PlayerSpawner.LocalPlayerInstance.transform;

        if (target == null)
            return;

        var yaw = target.eulerAngles.y;
        var orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        var desiredPosition = target.position + orbitRotation * new Vector3(0f, height, -distance);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmooth * Time.deltaTime);

        var lookPoint = target.position + Vector3.up * lookAtHeight;
        var lookDirection = lookPoint - transform.position;

        if (lookDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(lookDirection.normalized);
    }
}
