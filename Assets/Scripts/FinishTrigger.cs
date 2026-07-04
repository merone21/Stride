using Photon.Pun;
using UnityEngine;

public class FinishTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        var controller = other.GetComponentInParent<PlayerController>();
        if (controller == null)
            return;

        var photonView = controller.GetComponent<PhotonView>();
        if (photonView == null || !photonView.IsMine)
            return;

        controller.ReportFinish();
    }
}
