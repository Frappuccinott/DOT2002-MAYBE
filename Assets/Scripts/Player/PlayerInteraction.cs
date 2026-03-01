using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Oyuncunun etkileşim kontrolcüsü.
/// F tuşu: Parça al / yerleştir / sök / yere bırak
/// Sol tık + sürükle: Kapı açma/kapama
/// Scroll: Elde tutulan parçanın mesafesini ayarlar
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    #region Settings

    [Header("Raycast")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactionLayer = ~0;

    [Header("Elde Tutma")]
    [SerializeField] private float holdDistanceMin = 0.5f;
    [SerializeField] private float holdDistanceMax = 2f;
    [SerializeField] private float holdDistanceDefault = 1f;
    [SerializeField] private float scrollSensitivity = 0.1f;
    [SerializeField] private float heldPartScale = 0.5f;

    [Header("Motor Taşıma")]
    [SerializeField] private float engineCarrySpeedMultiplier = 0.5f;

    [Header("Referanslar")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerCrosshair crosshair;
    [SerializeField] private PlayerController playerController;

    #endregion

    // Input
    private PlayerInputActions inputActions;

    // Raycast sonuçları
    private IInteractable currentInteractable;
    private HingeDoor currentDoor;
    private CarPartSlot lastLookedSlot;

    // Elde tutulan parça
    private PickupableCarPart heldPart;
    private Vector3 heldPartOriginalScale;
    private float currentHoldDistance;

    // Kapı sürükleme
    private HingeDoor draggedDoor;

    public bool HasCarPart => heldPart != null;
    public CarPartType HeldPartType => heldPart != null ? heldPart.PartType : default;

    #region Lifecycle

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        currentHoldDistance = holdDistanceDefault;

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null) Debug.LogError("[PlayerInteraction] Kamera bulunamadı!");
        }
        if (crosshair == null) crosshair = GetComponentInChildren<PlayerCrosshair>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Pickup.performed += OnPickupPressed;
    }

    private void OnDisable()
    {
        inputActions.Player.Pickup.performed -= OnPickupPressed;
        inputActions.Player.Disable();
    }

    private void OnDestroy() => inputActions?.Dispose();

    private void Update()
    {
        PerformRaycast();

        if (HasCarPart)
        {
            HandleScrollWheel();
            UpdateHeldPartPosition();
        }

        HandleDoorDrag();
    }

    #endregion

    #region Raycast

    private void PerformRaycast()
    {
        currentInteractable = null;
        currentDoor = null;
        bool canInteract = false;
        CarPartSlot lookedSlot = null;

        if (playerCamera != null)
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayer))
            {
                // IInteractable
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    currentInteractable = interactable;
                    canInteract = interactable.CanInteract;
                }

                // HingeDoor
                HingeDoor door = hit.collider.GetComponent<HingeDoor>();
                if (door != null && door.CanOperate)
                {
                    currentDoor = door;
                    if (!HasCarPart) canInteract = true;
                }

                // CarPartSlot preview bildirimi
                lookedSlot = hit.collider.GetComponent<CarPartSlot>();
            }
        }

        // Preview güncelle — sadece değişen slot'ları bilgilendir
        if (lookedSlot != lastLookedSlot)
        {
            lastLookedSlot?.SetLookedAt(false, false);
            lastLookedSlot = lookedSlot;
        }

        if (lastLookedSlot != null)
        {
            bool hasCorrectPart = HasCarPart && heldPart.PartType == lastLookedSlot.AcceptedPartType;
            lastLookedSlot.SetLookedAt(true, hasCorrectPart);
        }

        crosshair?.SetInteractable(canInteract);
    }

    #endregion

    #region F Tuşu

    private void OnPickupPressed(InputAction.CallbackContext context)
    {
        // 1) Boş el → al veya sök
        if (!HasCarPart && currentInteractable != null && currentInteractable.CanInteract)
        {
            if (currentInteractable is PickupableCarPart pickupable)
            {
                GrabPart(pickupable);
                return;
            }

            if (currentInteractable is CarPartSlot slot && slot.IsInstalled)
            {
                PickupableCarPart removed = slot.Uninstall();
                if (removed != null) GrabPart(removed);
                return;
            }
        }

        // 2) Elde parça → yerleştir veya yere bırak
        if (HasCarPart)
        {
            if (currentInteractable is CarPartSlot targetSlot &&
                !targetSlot.IsInstalled &&
                heldPart.PartType == targetSlot.AcceptedPartType)
            {
                InstallPart(targetSlot);
                return;
            }

            DropPart();
        }
    }

    #endregion

    #region Parça Yönetimi

    private void GrabPart(PickupableCarPart part)
    {
        heldPart = part;
        GameObject obj = part.gameObject;

        obj.SetActive(true);

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        heldPartOriginalScale = obj.transform.localScale;
        obj.transform.SetParent(playerCamera.transform);
        obj.transform.localScale = heldPartOriginalScale * heldPartScale;
        SetCollidersEnabled(obj, false);
        currentHoldDistance = holdDistanceDefault;

        if (part.PartType == CarPartType.Engine && playerController != null)
            playerController.SetCarrySpeedMultiplier(engineCarrySpeedMultiplier);

        Debug.Log($"[PlayerInteraction] {part.PartType} alındı!");
    }

    private void InstallPart(CarPartSlot slot)
    {
        PickupableCarPart part = heldPart;
        slot.Install(part);

        // Parçayı elden çıkar
        GameObject obj = part.gameObject;
        obj.transform.SetParent(null);
        obj.transform.localScale = heldPartOriginalScale;
        obj.SetActive(false);

        ResetCarryState();
        Debug.Log($"[PlayerInteraction] {slot.AcceptedPartType} slot'a yerleştirildi!");
    }

    private void DropPart()
    {
        if (heldPart == null) return;

        GameObject obj = heldPart.gameObject;
        obj.transform.SetParent(null);
        obj.transform.localScale = heldPartOriginalScale;
        SetCollidersEnabled(obj, true);
        obj.SetActive(true);

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(playerCamera.transform.forward * 1.5f + Vector3.down * 0.5f, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.VelocityChange);
        }

        CarPartType droppedType = heldPart.PartType;
        ResetCarryState();
        Debug.Log($"[PlayerInteraction] {droppedType} yere bırakıldı!");
    }

    private void ResetCarryState()
    {
        heldPart = null;
        if (playerController != null) playerController.SetCarrySpeedMultiplier(1f);
    }

    private static void SetCollidersEnabled(GameObject obj, bool enabled)
    {
        foreach (Collider col in obj.GetComponentsInChildren<Collider>())
            col.enabled = enabled;
    }

    #endregion

    #region Scroll ve Pozisyon

    private void HandleScrollWheel()
    {
        Vector2 scroll = inputActions.Player.ScrollWheel.ReadValue<Vector2>();
        if (Mathf.Abs(scroll.y) > 0.01f)
        {
            currentHoldDistance += scroll.y * scrollSensitivity * Time.deltaTime;
            currentHoldDistance = Mathf.Clamp(currentHoldDistance, holdDistanceMin, holdDistanceMax);
        }
    }

    private void UpdateHeldPartPosition()
    {
        if (playerCamera == null) return;
        Vector3 targetPos = playerCamera.transform.position + playerCamera.transform.forward * currentHoldDistance;
        heldPart.transform.position = Vector3.Lerp(heldPart.transform.position, targetPos, 15f * Time.deltaTime);
        heldPart.transform.rotation = playerCamera.transform.rotation;
    }

    #endregion

    #region Kapı Sürükleme

    private void HandleDoorDrag()
    {
        bool isLeftMouseHeld = inputActions.Player.Attack.ReadValue<float>() > 0.5f;

        if (isLeftMouseHeld)
        {
            if (draggedDoor == null && currentDoor != null)
                draggedDoor = currentDoor;

            if (draggedDoor != null)
                draggedDoor.DragDoor(inputActions.Player.Look.ReadValue<Vector2>());
        }
        else
        {
            draggedDoor = null;
        }
    }

    #endregion
}
