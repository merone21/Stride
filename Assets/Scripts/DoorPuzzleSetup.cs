using Photon.Pun;
using UnityEngine;

public class DoorPuzzleSetup : MonoBehaviour
{
    const string ButtonObjectName = "SM_Gen_Prop_Button_01";
    const string DoorObjectName = "Door";

    void Awake()
    {
        if (FindAnyObjectByType<DoorButton>() != null)
            return;

        var buttonObject = GameObject.Find(ButtonObjectName);
        if (buttonObject == null)
        {
            Debug.LogWarning("DoorPuzzleSetup: Sahnedeki buton bulunamadı (" + ButtonObjectName + ").");
            return;
        }

        var doorObject = GameObject.Find(DoorObjectName);
        if (doorObject == null)
        {
            Debug.LogWarning("DoorPuzzleSetup: Sahnedeki kapı bulunamadı (" + DoorObjectName + ").");
            return;
        }

        var doorButton = buttonObject.GetComponent<DoorButton>();
        if (doorButton == null)
            doorButton = buttonObject.AddComponent<DoorButton>();

        doorButton.SetDoor(doorObject.transform);
        doorButton.SetPlungerPressOffset(new Vector3(0f, 0f, -0.08f));
    }
}
