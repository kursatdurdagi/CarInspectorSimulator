using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerDoorInteract : MonoBehaviour
{
    [Header("Raycast Ayarlarý")]
    public float interactDistance = 3f;
    public Transform playerCamera; // Kamerayý Inspector'dan sürükle býrak yapacaksýn

    [Header("UI ve Lokalizasyon Ayarlarý")]
    public TextMeshProUGUI interactText;

    // Ýleride buraya "Aç" yerine lokalizasyon keylerini (örn: "KEY_DOOR_OPEN") girebilirsin
    [SerializeField] private string openTextKey = "Aç";
    [SerializeField] private string closeTextKey = "Kapat";

    [Header("Input (New Input System)")]
    public InputActionReference interactAction;

    private CarDoor currentDoor;

    void Update()
    {
        CheckForDoor();

        if (currentDoor != null && interactAction.action.WasPressedThisFrame())
        {
            currentDoor.ToggleDoor();
        }
    }

    void CheckForDoor()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            CarDoor door = hit.collider.GetComponent<CarDoor>();

            if (door != null)
            {
                currentDoor = door;
                interactText.gameObject.SetActive(true);
                door.myOutline.enabled = true;
                // Lokalizasyon yapýsýna uygun olarak ayrýldý. 
                // Ýleride burayý: interactText.text = LocalizationManager.Get(closeTextKey); gibi deðiþtirebilirsin.
                if (door.isOpen)
                {
                    interactText.text = closeTextKey;
                }
                else
                {
                    interactText.text = openTextKey;
                }

                return;
            }
           
        }
        if(currentDoor != null) currentDoor.myOutline.enabled = false;
        currentDoor = null;
        interactText.gameObject.SetActive(false);
    }
}