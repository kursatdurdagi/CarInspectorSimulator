using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGetCar : MonoBehaviour
{
    public RCCP_CarController currentCar;
    [SerializeField] FPSController playerController;
    [SerializeField] CharacterController controller;


    public GameObject mainCamera;
    public GameObject carCamera;


    public Transform outPoint;

    public bool isInCar;

    [Header("Input Settings")]
    public InputActionReference getCarAction;

    private void OnEnable()
    {
        if (getCarAction != null)
        {
            getCarAction.action.Enable();
            getCarAction.action.performed += OnGetCarInput;
        }
    }

    private void OnDisable()
    {
        if (getCarAction != null)
        {
            getCarAction.action.performed -= OnGetCarInput;
            getCarAction.action.Disable();
        }
    }

    private void OnGetCarInput(InputAction.CallbackContext context)
    {
        // Arabanýn içindeysek çýkmak için currentCar'ýn null olmamasý þartýný aramýyoruz, 
        // çünkü çýkarken zaten arabanýn içindeyiz (tetikleyiciden uzaklaþmýþ olabiliriz)
        if (isInCar)
        {
            GetOutCar();
        }
        else if (currentCar != null)
        {
            GetInCar();
        }
    }

    public void GetInCar()
    {
        isInCar = true;
        currentCar.Engine.StartEngine();
        currentCar.canControl = true;
        mainCamera.SetActive(false);
        carCamera.SetActive(true);

        // Arabaya binerken karakter kontrolcüsünü kapat
        if (playerController != null) playerController.enabled = false;
        controller.enabled = false;
    }

    public void GetOutCar()
    {
        // ÖNEMLÝ: Karakter kontrolcüsünün transformu ezmesini engellemek için önce scripti kapatalým
        if (playerController != null) playerController.enabled = false;
        controller.enabled = false;

        // Eðer bir outPoint referansýmýz varsa oyuncuyu oraya ýþýnla
        if (outPoint != null)
        {
            transform.position = outPoint.position;
            transform.rotation = outPoint.rotation; // Ýstersen yönünü de outPoint'e eþitleyelim
        }

        currentCar.Engine.StopEngine();
        currentCar.canControl = false;
        mainCamera.SetActive(true);
        carCamera.SetActive(false);
        isInCar = false;

        // Konum deðiþti, þimdi karakter hareket scriptini güvenle geri açabiliriz
        if (playerController != null) playerController.enabled = true;
        controller.enabled = true;

        // Ýþimiz bitti, referanslarý temizliyoruz
        currentCar = null;
        outPoint = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "GetCar")
        {
            currentCar = other.transform.parent.GetComponent<RCCP_CarController>();
            outPoint = other.transform; // "GetCar" objesinin pozisyonunu iniþ noktasý alýyoruz
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Eðer arabanýn içinde deðilsek ve alandan çýktýysak referanslarý temizle
        if (!isInCar && other.gameObject.name == "GetCar")
        {
            currentCar = null;
            outPoint = null;
        }
    }
}