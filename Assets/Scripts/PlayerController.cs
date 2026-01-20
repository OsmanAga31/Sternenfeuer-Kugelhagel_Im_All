using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;



public class PlayerController : NetworkBehaviour, IDamagable
{
    [Header("UI")]
    [SerializeField] private NameDisplay nameDisplay; // reference to the NameDisplay script

    [Header("SyncVars")]
    private readonly SyncVar<string> playerName = new SyncVar<string>(); // name of the player
    private readonly SyncVar<Color> playerColor = new SyncVar<Color>(); // color of the player

    [Header("Player Stats")]
    private const int maxHP = 100;
    private readonly SyncVar<int> playerHP = new SyncVar<int>(100); // health points of the player
    public int CurrentHP => playerHP.Value;
    public int MaxHP => maxHP;
    public System.Action<int, int> OnHealthChanged;

    [Header("Shooting")]
    [SerializeField] private int bulletDamage = 10;
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private float bulletLifeTime = 3f;
    [SerializeField] private float fireRate = 0.2f; // seconds between shots
    [SerializeField] private Transform firePoint; // spawn point for bullet

    private float lastFireTime;

    // available colors for players
    private static readonly Color[] availableColors = new Color[]
    {
        new Color(1f, 0.92f, 0.016f), // Yellow
        new Color(1f, 0.5f, 0f),      // Orange
        new Color(0.58f, 0f, 0.83f),  // Purple
        new Color(1f, 0.08f, 0.58f),  // Pink
        new Color(0f, 1f, 1f)         // Cyan
    };

    private static int colorIndex = 0; // index to track assigned colors

    [SerializeField] private float playerSpeed;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject goToRotate; // part of the player to rotate towards mouse

    //public CharacterController cc;
    private InputSystem_Actions inputActions;
    private Camera mainCamera;
    private MeshRenderer meshRenderer;

    private void Start() 
    { 
        inputActions = new InputSystem_Actions();
        inputActions.Player.Enable();
        TimeManager.OnTick += OnServerTick;
        mainCamera = Camera.main;
        meshRenderer = GetComponentInChildren<MeshRenderer>();

        // Assign a color to the player (server-side)
        if(IsServerInitialized)
        {
            AssignPlayerColor();
        }

        // react to color changes (for clients)
        playerColor.OnChange += OnPlayerColorChanged;

        // set initial color, if already existing
        if(playerColor.Value != Color.clear)
        {
            ApplyColor(playerColor.Value);
        }

        // react to HP changes
        playerHP.OnChange += OnPlayerHPChanged;

        ///TODO:

        // --------------- Name Handling --------------- 
        // GameManager or Login system should be implemented to get player names
        //// set player name 
        //if (IsOwner)
        //{
        //   // get name from for example Login or input field
        //   string username = MyGameManager.LocalUserName; // placeholder for actual name retrieval

        //   // fill SyncVar
        //   SetPlayerNameServerRPC(username);

        //   // update local UI instantly
        //   nameDisplay.SetName(username);
        //}

        // react to name changes so other clients see the updated name
        // playerName.OnChange += OnPlayerNameChanged;
    }

    private void OnServerTick() 
    {
        // read movement input 
        Vector2 movementInput = inputActions.Player.Move.ReadValue<Vector2>();

        // if we are the client and owner, request move
        if (IsClientInitialized && IsOwner){

            MoveServerRPC(movementInput);

            // send mouse position to server for rotation
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            RotateServerRPC(mouseWorldPos);
        }

        // shooting input
        if (inputActions.Player.Shoot.IsPressed() && Time.time >= lastFireTime + fireRate)
        {
            lastFireTime = Time.time;
            ShootServerRPC();
        }
    }

    private Vector3 GetMouseWorldPosition() 
    {
        if (mainCamera == null) return transform.position;

        // get mouse position in screen space
        Vector2 mouseScreenPos = inputActions.Player.Look.ReadValue<Vector2>();

        // create ray from camera through mouse position
        Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);

        // plane at y = playerTransform.position.y
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        // raycast to plane
        if (groundPlane.Raycast(ray, out float distance)) 
        {
            return ray.GetPoint(distance);
        }
        return transform.position;
    }

    #region Movement
    private void Move(Vector2 _input) 
    {
        Vector3 move = new Vector3(_input.x, 0, _input.y);
        //cc.Move(move * playerSpeed * (float)TimeManager.TickDelta);
        transform.Translate(move * playerSpeed * (float)TimeManager.TickDelta, Space.World);
    }

    [ServerRpc]
    private void MoveServerRPC(Vector2 _input) 
    {
        Move(_input);
    }

    #endregion

    #region Rotation
    private void RotateTowards(Vector3 targetPosition)
    {
        // direction to look at
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0; // keep only horizontal direction

        // only rotate if there is a valid direction
        if (direction.sqrMagnitude > 0.01f) 
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            goToRotate.transform.rotation = targetRotation;
        }
    }

    [ServerRpc]
    private void RotateServerRPC(Vector3 mouseWorldPosition) 
    {
        RotateTowards(mouseWorldPosition);
    }

    #endregion

    #region Color Handling

    private void AssignPlayerColor() 
    {
        // assign next available color and put it in the SyncVar
        Color assignedColor = availableColors[colorIndex % availableColors.Length];
        colorIndex++;

        Debug.Log($"[Server] Assigning color: {assignedColor} to player");

        playerColor.Value = assignedColor;

        ApplyColor(assignedColor);
    }

    private void OnPlayerColorChanged(Color odlColor, Color newColor, bool asServer) 
    {
        ApplyColor(newColor);
    }

    private void ApplyColor(Color color) 
    {
        if (meshRenderer == null)
        {
            Debug.LogWarning("MeshRenderer is null, cannot apply color.");
            return;
        }

        // create material and set color
        Material playerMaterial = new Material(meshRenderer.material);
        playerMaterial.color = color;
        meshRenderer.material = playerMaterial;

        Debug.Log($"Applying color: {color}");
    }

    #endregion

    #region Health / IDamagable Implementation

    [Server]
    // Implement Damage method from IDamagable interface
    public void Damage(int damageAmount)
    {
        // only process damage on server
        if (!IsServerInitialized) return;

        // reduce player HP
        playerHP.Value -= damageAmount;
        if(playerHP.Value < 0 ) playerHP.Value = 0;
        Debug.Log($"[Server] Player damaged by {damageAmount}, current HP: {playerHP.Value}");

        // check for death
        if (playerHP.Value <= 0)
        {
            Debug.Log($"[Server] Player has died.");
            // Handle player death (respawn, game over, etc.)
        }
    }

    // Callback for when player HP changes
    private void OnPlayerHPChanged(int previous, int current, bool asServer)
    {
        OnHealthChanged?.Invoke(previous, current); // notify subscribers about HP change
        Debug.Log($"[Client] Player HP changed from {previous} to: {current}");
    }

    #endregion

    #region Shooting

    [ServerRpc]
    private void ShootServerRPC()
    {
        if (!IsServerInitialized) return;

        // get bullet from object pool
        NetworkObject bulletPrefab = NewObjectPoolManager.Instance.getObject(PoolObjectType.Bullet);

        // spawn bullet (fishnet automatically uses object pool)
        NetworkObject bulletNetObj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Spawn(bulletNetObj.gameObject);

        // konfig bullet
        Bullet bullet = bulletNetObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.ShootBullet(bulletDamage, bulletSpeed, bulletLifeTime, ShooterType.Player);
        }
    }

    #endregion

    private void OnDestroy() 
    {
        if (TimeManager != null)
        {
            TimeManager.OnTick -= OnServerTick;
        }

        // unsubscribe from SyncVar changes
        playerColor.OnChange -= OnPlayerColorChanged;

        // unsubscribe from HP changes
        playerHP.OnChange -= OnPlayerHPChanged;
    }
}