using UnityEngine;

public class FinishLineSetup : MonoBehaviour
{
    const string FinishObjectName = "Plane (1)";

    void Awake()
    {
        if (FindAnyObjectByType<FinishTrigger>() != null)
            return;

        var finish = GameObject.Find(FinishObjectName);
        if (finish == null)
        {
            Debug.LogWarning("FinishLineSetup: Varış noktası bulunamadı (" + FinishObjectName + ").");
            return;
        }

        var triggerObject = new GameObject("FinishTrigger");
        triggerObject.transform.SetParent(finish.transform, false);
        triggerObject.transform.localPosition = new Vector3(0f, 2f, 0f);

        var box = triggerObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(25f, 4f, 8f);

        triggerObject.AddComponent<FinishTrigger>();
    }
}
