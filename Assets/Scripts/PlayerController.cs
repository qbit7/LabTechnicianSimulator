using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Передвижение")]
    public float walkSpeed = 5f;

    [Header("Вращение камеры")]
    public float lookSensitivity = 0.1f;

    [Header("Взаимодействие")]
    public float interactionDistance = 2.5f;

    [Header("Гравитация")]
    public float gravityMultiplier = 2f;        // множитель силы падения
    private float _verticalVelocity;            // текущая скорость падения

    [Header("Покачивание камеры")]
    public float bobAmount = 0.05f;   // амплитуда покачивания (чем больше, тем сильнее трясёт)
    public float bobSpeed = 10f;      // скорость покачивания (чем выше, тем чаще шаги)

    private float _defaultCameraY;    // исходная высота камеры
    private float _bobTimer;          // таймер для синусоиды

    private CharacterController _controller;
    private Camera _playerCamera;
    private PlayerControls _input;

    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private float _cameraPitch = 0f;

    private bool _groundedBuffer = false;
    private float _groundedTimer = 0f;
    private const float GroundedTimeout = 0.05f; // 50 мс — столько "прощаем" потерю контакта

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _playerCamera = GetComponentInChildren<Camera>();
        _input = new PlayerControls();

        _defaultCameraY = _playerCamera.transform.localPosition.y;

        // Прячем и блокируем курсор
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnEnable()
    {
        _input.Player.Enable();
        _input.Player.Move.performed += OnMove;
        _input.Player.Move.canceled += OnMove;
        _input.Player.Look.performed += OnLook;
        _input.Player.Look.canceled += OnLook;
        _input.Player.Interact.performed += OnInteract;
        _input.Player.Quit.performed += OnQuit;
    }

    void OnDisable()
    {
        _input.Player.Move.performed -= OnMove;
        _input.Player.Move.canceled -= OnMove;
        _input.Player.Look.performed -= OnLook;
        _input.Player.Look.canceled -= OnLook;
        _input.Player.Interact.performed -= OnInteract;
        _input.Player.Quit.performed -= OnQuit;
        _input.Player.Disable();
    }

    //void HandleHeadBob()
    //{
    //    // Получаем горизонтальную скорость (без учёта вертикали)
    //    float horizontalSpeed = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;

    //    // Если игрок движется и находится на земле – покачиваем камеру
    //    if (horizontalSpeed > 0.1f && _controller.isGrounded)
    //    {
    //        // Двигаем таймер в зависимости от скорости и настройки bobSpeed
    //        _bobTimer += Time.deltaTime * bobSpeed;

    //        // Считаем смещение по синусоиде
    //        float newY = _defaultCameraY + Mathf.Sin(_bobTimer) * bobAmount;

    //        // Применяем новую позицию камеры (сохраняя локальные X и Z)
    //        Vector3 pos = _playerCamera.transform.localPosition;
    //        pos.y = newY;
    //        _playerCamera.transform.localPosition = pos;
    //    }
    //    else
    //    {
    //        // Если стоим – сбрасываем таймер и плавно возвращаем камеру в исходное положение
    //        _bobTimer = 0f;
    //        Vector3 pos = _playerCamera.transform.localPosition;
    //        pos.y = Mathf.Lerp(pos.y, _defaultCameraY, Time.deltaTime * bobSpeed);
    //        _playerCamera.transform.localPosition = pos;
    //    }
    //}

    void HandleHeadBob()
    {
        float horizontalSpeed = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;

        // Плавно меняем целевое смещение: 1.0 когда идём по земле, 0 когда стоим или в воздухе
        float targetBobWeight = (horizontalSpeed > 0.1f && _controller.isGrounded) ? 1.0f : 0.0f;
        _bobTimer += Time.deltaTime * bobSpeed * targetBobWeight; // таймер растёт только когда идём

        // Если не идём — таймер медленно угасает к нулю для плавного возврата
        if (targetBobWeight < 0.01f)
            _bobTimer = Mathf.MoveTowards(_bobTimer, 0f, Time.deltaTime * bobSpeed);

        float bobOffset = Mathf.Sin(_bobTimer) * bobAmount * targetBobWeight;
        float newY = _defaultCameraY + bobOffset;

        Vector3 pos = _playerCamera.transform.localPosition;
        pos.y = Mathf.Lerp(pos.y, newY, Time.deltaTime * 15f); // сильное сглаживание
        _playerCamera.transform.localPosition = pos;
    }

    //void ApplyGravity()
    //{
    //    // Если персонаж на земле и падает (вертикальная скорость отрицательная),
    //    // прижимаем его к поверхности, чтобы isGrounded стабильно работал
    //    if (_controller.isGrounded && _verticalVelocity < 0f)
    //    {
    //        _verticalVelocity = -2f; // маленькое отрицательное значение для прижима
    //    }
    //    else
    //    {
    //        // Добавляем ускорение свободного падения
    //        _verticalVelocity += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
    //    }

    //    // Применяем вертикальное движение
    //    _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
    //}

    void ApplyGravity()
    {
        // Буферизованная проверка земли
        if (_controller.isGrounded)
        {
            _groundedBuffer = true;
            _groundedTimer = 0f;
        }
        else
        {
            _groundedTimer += Time.deltaTime;
            if (_groundedTimer > GroundedTimeout)
                _groundedBuffer = false;
        }

        if (_groundedBuffer && _verticalVelocity < 0f)
        {
            _verticalVelocity = -2f;
        }
        else
        {
            _verticalVelocity += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
        }

        _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
    }

    void Update()
    {
        HandleMovement();
        HandleMouseLook();
        HandleHeadBob();
        ApplyGravity();       // <-- новый вызов
    }

    // ── Обработчики ввода ──

    private void OnMove(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext ctx)
    {
        _lookInput = ctx.ReadValue<Vector2>();
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) TryInteract();
    }

    private void OnQuit(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    // ── Движение ──

    void HandleMovement()
    {
        Vector3 forward = _playerCamera.transform.forward;
        Vector3 right = _playerCamera.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = (forward * _moveInput.y + right * _moveInput.x).normalized;
        _controller.Move(moveDir * (walkSpeed * Time.deltaTime));

        // Если у тебя уже есть CurrentSpeed, обнови её
        // CurrentSpeed = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;
    }

    // ── Взгляд ──

    void HandleMouseLook()
    {
        float yaw = _lookInput.x * lookSensitivity;
        transform.Rotate(Vector3.up * yaw);

        _cameraPitch -= _lookInput.y * lookSensitivity;
        _cameraPitch = Mathf.Clamp(_cameraPitch, -90f, 90f);
        _playerCamera.transform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
    }

    // ── Взаимодействие ──

    void TryInteract()
    {
        Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            // Interactable interactable = hit.collider.GetComponentInParent<Interactable>();
            // interactable?.Interact();
            Debug.Log("Взаимодействие с " + hit.collider.name + " (скрипт Interactable отсутствует)");
        }
    }
}