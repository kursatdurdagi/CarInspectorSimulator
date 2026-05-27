using UnityEngine;
using DG.Tweening;

public class CarDoor : MonoBehaviour
{
    [SerializeField] float openSpeed = 0.5f;
    public Outline myOutline;
    public Transform myPivot;

    public bool isOpen;

    public Vector3 closeRot;
    public Vector3 openRot;

    // Start ve Update'i kullanmýyorsak silebiliriz, kalabalýk yapmasýn.

    // Kapýyý açýp kapatacak ana metodumuz
    public void ToggleDoor()
    {
        if (isOpen)
        {
            // Kapýyý Kapat
            myPivot.transform.DOLocalRotate(closeRot, openSpeed).SetEase(Ease.InOutSine);
            isOpen = false;
        }
        else
        {
            // Kapýyý Aç
            myPivot.transform.DOLocalRotate(openRot, openSpeed).SetEase(Ease.InOutSine);
            isOpen = true;
        }
    }
}