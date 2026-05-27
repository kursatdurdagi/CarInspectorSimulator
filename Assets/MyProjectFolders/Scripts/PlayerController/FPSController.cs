using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Camera Assignment")]
    [SerializeField] private Transform playerCameraTransform;

    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 5.0f;
    [SerializeField] private float sprintSpeed = 8.0f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float speedSmoothTime = 0.1f;

    [Header("Look Sensitivity")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float upperLookLimit = 80.0f;
    [SerializeField] private float lowerLookLimit = -80.0f;

    [Header("Head Bob Settings")]
    [SerializeField] private bool useHeadBob = true;
    [SerializeField] private float bobFrequency = 14.0f; // Sallanma hýzý
    [SerializeField] private float bobHorizontalAmplitude = 0.05f; // Yatay sallanma mesafesi
    [SerializeField] private float bobVerticalAmplitude = 0.05f; // Dikey sallanma mesafesi
    [SerializeField] private float headBobSmooth = 10.0f; // Pozisyona geri dönme yumuþaklýðý

    [Header("Camera Tilt Settings")]
    [SerializeField] private bool useTilt = true;
    [SerializeField] private float tiltAmount = 2.0f; // Saða-sola yatma açýsý
    [SerializeField] private float tiltSpeed = 5.0f; // Yatma hýzý

    // References
    private CharacterController characterController;

    // Movement Internal Variables
    private Vector2 rawInputVector;
    private Vector2 currentInputVector;
    private Vector2 smoothInputVelocity;
    private Vector3 velocity;
    private bool isSprinting;
    private bool isGrounded;

    // Look Internal Variables
    private float xRotation = 0f;
    private float currentTilt = 0f;

    // Head Bob Internal Variables
    private float bobTimer = 0f;
    private Vector3 cameraDefaultLocalPos;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (playerCameraTransform != null)
            cameraDefaultLocalPos = playerCameraTransform.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleGravity();
        HandleMovement();
        HandleHeadBob();
    }

    private void HandleMovement()
    {
        isGrounded = characterController.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        currentInputVector = Vector2.SmoothDamp(currentInputVector, rawInputVector, ref smoothInputVelocity, speedSmoothTime);

        float targetSpeed = isSprinting && currentInputVector.magnitude > 0.01f ? sprintSpeed : walkSpeed;

        Vector3 moveDirection = transform.right * currentInputVector.x + transform.forward * currentInputVector.y;

        characterController.Move(moveDirection * targetSpeed * Time.deltaTime);
    }

    private void HandleGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleHeadBob()
    {
        if (!useHeadBob || playerCameraTransform == null) return;

        // Karakter hareket ediyorsa ve yerdeyse, sinüs dalgasý oluþtur
        if (characterController.velocity.magnitude > 0.1f && isGrounded && rawInputVector.magnitude > 0.01f)
        {
            // Koþarken bobbing hýzý ve þiddeti artýyor
            float currentBobFrequency = isSprinting ? bobFrequency * 1.5f : bobFrequency;
            float currentBobHorizontalAmplitude = isSprinting ? bobHorizontalAmplitude * 1.2f : bobHorizontalAmplitude;
            float currentBobVerticalAmplitude = isSprinting ? bobVerticalAmplitude * 1.2f : bobVerticalAmplitude;

            bobTimer += Time.deltaTime * currentBobFrequency;

            // Sinüs dalgalarýyla pozisyon hesaplama (Pürüzsüz hareketin sýrrý)
            float newX = cameraDefaultLocalPos.x + Mathf.Cos(bobTimer / 2) * currentBobHorizontalAmplitude;
            float newY = cameraDefaultLocalPos.y + Mathf.Sin(bobTimer) * currentBobVerticalAmplitude;

            Vector3 targetBobPos = new Vector3(newX, newY, cameraDefaultLocalPos.z);
            playerCameraTransform.localPosition = Vector3.Lerp(playerCameraTransform.localPosition, targetBobPos, Time.deltaTime * headBobSmooth);
        }
        else
        {
            // Dururken kamerayý orijinal pozisyonuna yumuþakça döndür (Tetikleyiciyi sýfýrla)
            bobTimer = Mathf.Lerp(bobTimer, 0f, Time.deltaTime * headBobSmooth);
            playerCameraTransform.localPosition = Vector3.Lerp(playerCameraTransform.localPosition, cameraDefaultLocalPos, Time.deltaTime * headBobSmooth);
        }
    }

    // --- NEW INPUT SYSTEM EVENTS ---

    public void OnMove(InputValue value)
    {
        rawInputVector = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        if (playerCameraTransform == null) return;

        Vector2 lookInput = value.Get<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, lowerLookLimit, upperLookLimit);

        // --- CAMERA TILT ---
        if (useTilt)
        {
            float targetTilt = -currentInputVector.x * tiltAmount;
            currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);
        }
        else
        {
            currentTilt = 0f;
        }

        playerCameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, currentTilt);
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    public void OnSprint(InputValue value)
    {
        isSprinting = value.isPressed;
    }
}