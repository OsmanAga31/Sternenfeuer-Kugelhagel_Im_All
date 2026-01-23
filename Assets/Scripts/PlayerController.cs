using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using TMPro;
using UnityEngine;

public enum ShootPattern
{
    Straight,
    Spread,
    Spiral,
    Wave
}

public class PlayerController : NetworkBehaviour, IDamagable
{
    [Header("UI")]
    [SerializeField] private NameDisplay nameDisplay; // reference to the NameDisplay script

    
    private readonly SyncVar<string> playerName = new SyncVar<string>(); // name of the player
    public string PlayerName => playerName.Value; // public getter for SyncVar
    public int PlayerOwnerId => OwnerId;


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
    [SerializeField] private float fireRate = 0.1f; // seconds between shots
    [SerializeField] private Transform firePoint; // spawn point for bullet

    [Header("Shoot Patterns")]
    [SerializeField] private ShootPattern currentPattern = ShootPattern.Straight;    
    // Spread settings
    [SerializeField] private float spreadAngle = 30f;
    [SerializeField] private float spreadCooldown = 0.8f;

    // Spiral settings
    [SerializeField, Range(6, 20)] private int spiralBulletCount = 12;
    [SerializeField] private float spiralTightenSpeed = 5f;
    [SerializeField] private float spiralCooldown = 1.5f;

    // Wave settings
    [SerializeField, Range(3, 12)] private int waveBulletCount = 12;
    [SerializeField] private float waveAmplitude = 20f;
    [SerializeField] private float waveFrequenzy = 0.7f;
    [SerializeField] private float waveCooldown = 1.2f;

    private float lastFireTime;

    // seperate cooldown timersper pattern (server-only)
    private float lastSpreadTime;
    private float lastSpiralTime;
    private float lastWaveTime;

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

    private InputSystem_Actions inputActions;
    private Camera mainCamera;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
    }

    private void Start() 
    { 
        inputActions = new InputSystem_Actions();
        inputActions.Player.Enable();
        //TimeManager.OnTick += OnServerTick;
        mainCamera = Camera.main;

        //// Assign a color to the player (server-side)
        //if(IsServerInitialized)
        //{
        //    AssignPlayerColor();
        //}

        //// react to color changes (for clients)
        //playerColor.OnChange += OnPlayerColorChanged;

        //// set initial color, if already existing
        //if(playerColor.Value != Color.clear)
        //{
        //    ApplyColor(playerColor.Value);
        //}

        //// react to HP changes
        //playerHP.OnChange += OnPlayerHPChanged;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // assign color on server
        AssignPlayerColor();

        // register player in ScoreManager
        if(ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RegisterPlayer(OwnerId);
        }
        else
        {
            //retry if ScoreManager not ready yet
            Invoke(nameof(RegisterInScoreManager), 0.2f);
        }
    }

    [Server]
    private void RegisterInScoreManager()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RegisterPlayer(OwnerId);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        TimeManager.OnTick += OnServerTick;

        // react to color changes (for clients)
        playerColor.OnChange += OnPlayerColorChanged;

        // react to HP changes
        playerHP.OnChange += OnPlayerHPChanged;


        // react to name changes
        playerName.OnChange += OnPlayerNameChanged;

        // apply initial color if already set (important for clients joining later)
        if(playerColor.Value != Color.clear)
        {
            Debug.Log($"[Client] Applaying initial color: {playerColor.Value}");
            ApplyColor(playerColor.Value);
        }

        //// assign a color to the player (server-side)
        //if (IsServerInitialized)
        //{
        //    AssignPlayerColor();
        //}

        //// react to color changes (for clients)
        //playerColor.OnChange += OnPlayerColorChanged;
        //if (playerColor.Value != Color.clear)
        //{
        //    ApplyColor(playerColor.Value);
        //}

        //// react to HP changes
        //playerHP.OnChange += OnPlayerHPChanged;

        // when a new client starts, update the name display
        if (nameDisplay != null)
        {
            nameDisplay.SetName(playerName.Value);
        }

        // set name only for Owner (after Network-Init)
        if (IsOwner)
        {
            //string playerName = $"Player{Owner.ClientId}";
            //SetPlayerNameServerRPC(playerName);
            //if (nameDisplay != null)
            //    nameDisplay.SetName(playerName);

            HubManager.Instance.nameInputField = TMP_InputField.FindAnyObjectByType<TMP_InputField>();
            if (HubManager.Instance.nameInputField != null)
            {
                HubManager.Instance.nameInputField.onValueChanged.AddListener(SetPlayerNameServerRPC);
            }

        }
    }

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



    [ServerRpc(RequireOwnership = true)]
    private void SetPlayerNameServerRPC(string name)
    {
        playerName.Value = name;
        Debug.Log($"[Server] Set player name: {name}");
    }

    private void OnPlayerNameChanged(string oldName, string newName, bool asServer)
    {
        Debug.Log($"Name changed: {oldName} -> {newName}");
        if(nameDisplay != null && !asServer)
            nameDisplay.SetName(newName);
    }

    private void OnServerTick() 
    {
        // if we are the client and owner, request move
        if (IsClientInitialized && IsOwner)
        {
            if (inputActions == null)
                return;
            
            // read movement input 
            Vector2 movementInput = inputActions.Player.Move.ReadValue<Vector2>();
            MoveServerRPC(movementInput);

            // send mouse position to server for rotation
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            RotateServerRPC(mouseWorldPos);

            // shooting input
            if (inputActions.Player.Shoot.IsPressed() && Time.time >= lastFireTime + fireRate)
            {
                lastFireTime = Time.time;
                ShootServerRPC();
            }
        }
    }

    private void Update()
    {
        // only the player controlling this character can change patterns (not other clients)
        if (!IsOwner) return;

        if (inputActions.Player.PatternStraight.WasPressedThisFrame())
        {
            ChangePatternServerRPC(ShootPattern.Straight);
        }
        else if (inputActions.Player.PatternSpread.WasPressedThisFrame())
        {
            ChangePatternServerRPC(ShootPattern.Spread);
        }
        else if (inputActions.Player.PatternSpiral.WasPressedThisFrame())
        {
            ChangePatternServerRPC(ShootPattern.Spiral);
        }
        else if (inputActions.Player.PatternWave.WasPressedThisFrame())
        {
            ChangePatternServerRPC(ShootPattern.Wave);
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

    [Server]
    private void AssignPlayerColor() 
    {
        // assign next available color and put it in the SyncVar
        // pick next color from color list
        Color assignedColor = availableColors[colorIndex % availableColors.Length];
        colorIndex++; // move to next color for next player

        Debug.Log($"[Server] Assigning color: {assignedColor} to player");

        // save color in SyncVar
        playerColor.Value = assignedColor;

        // apply color immediately on server
        ApplyColor(assignedColor);
    }

    // called automatically when playerColor SyncVar changes (on ALL clients)
    private void OnPlayerColorChanged(Color oldColor, Color newColor, bool asServer) 
    {
        if (!asServer)
        {
            Debug.Log($"[{(asServer ? "Server" : "Client")}] OnPlayerColorChanged: {oldColor} -> {newColor}");

            // apply the new color (only on clients, server already applied it)
            ApplyColor(newColor);
        }
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
    public void Damage(int damageAmount, NetworkObject shooter = null)
    {
        // only process damage on server
        if (!IsServerInitialized) return;

        // reduce player HP
        playerHP.Value -= damageAmount;
        if(playerHP.Value < 0 ) playerHP.Value = 0;
        Debug.Log($"[Server] Player damaged by {damageAmount}, current HP: {playerHP.Value}");

        if(shooter != null && shooter.OwnerId != int.MaxValue)
        {
            int points = 10;
            ScoreManager.Instance?.AddPointsToPlayerServer(shooter.OwnerId, points);
        }

        // check for death
        if (playerHP.Value <= 0 && !ScoreManager.Instance.IsPlayerDead(OwnerId))
        {
            Debug.Log($"[Server] Player {OwnerId} has died.");

            /// Handle player death (respawn, game over, etc.)

            // mark THIS player as dead in ScoreManager
            ScoreManager.Instance?.SetPlayerDead(OwnerId);

            ScoreManager.Instance?.CalculateFinalScore(OwnerId);

        }
    }

    [Server]
    public void Heal(int healAmount)
    {
        // only process healing on server
        if (!IsServerInitialized) return;
        playerHP.Value += healAmount;
        if (playerHP.Value > maxHP) playerHP.Value = maxHP;
        Debug.Log($"[Server] Player healed by {healAmount}, current HP: {playerHP.Value}");
    }

    public void SaveScore()
    {
        // save highscore in database
        ScoreNetworkManager.Instance?.RequestSaveScore(PlayerName, ScoreManager.Instance.GetPlayerScore(OwnerId));
    }

    // Callback for when player HP changes
    private void OnPlayerHPChanged(int previous, int current, bool asServer)
    {
        OnHealthChanged?.Invoke(previous, current); // notify subscribers about HP change
        Debug.Log($"[Client] Player HP changed from {previous} to: {current}");
    }

    #endregion

    #region Shooting & ShootingPatterns

    [ServerRpc]
    private void ChangePatternServerRPC(ShootPattern newPattern)
    {
        // called by client to tell server: change shooting pattern
        // only runs on server, syncs automatically to all clients
        currentPattern = newPattern;
        Debug.Log($"[Server] Changed shooting pattern to: {newPattern}");
    }

    [ServerRpc(RequireOwnership = true)] // only the player controlling this char may shoot
    private void ShootServerRPC()
    {
        Debug.Log("[Server] ShootServerRPC called!");
        // safety check: only run on server
        if (!IsServerInitialized) return;

        // Check if player is alive
        if (ScoreManager.Instance.IsPlayerDead(OwnerId)) return;

            Debug.Log($"[Server] Shooting with pattern: {currentPattern}");

        // execute the right shooting pattern based on currentPattern
        switch (currentPattern)
        {
            case ShootPattern.Straight:
                ShootStraight();
                break;
            case ShootPattern.Spread:
                if (Time.time >= lastSpreadTime + spreadCooldown)
                {
                    ShootSpread();
                    lastSpreadTime = Time.time;
                }
                break;
            case ShootPattern.Spiral:
                if (Time.time >= lastSpiralTime + spiralCooldown)
                {
                    StartCoroutine(ShootSpiral());
                    lastSpiralTime = Time.time;
                }
                break;
            case ShootPattern.Wave:
                if (Time.time >= lastWaveTime + waveCooldown)
                {
                    ShootWave();
                    lastWaveTime = Time.time;
                }
                break;
        }
    }

    // shoot one bullet straight ahead from firePoint
    private void ShootStraight()
    {
        SpawnBullet(firePoint.position, firePoint.rotation);
    }

    private void ShootSpread()
    {
        // middle
        SpawnBullet(firePoint.position, firePoint.rotation);

        // left (rotated left by spreadAngle)
        Quaternion leftRotation = firePoint.rotation * Quaternion.Euler(0, -spreadAngle, 0);
        SpawnBullet(firePoint.position, leftRotation);

        // right (rotated right by spreadAngle)
        Quaternion rightRotation = firePoint.rotation * Quaternion.Euler(0, spreadAngle, 0);
        SpawnBullet(firePoint.position, rightRotation);
    }

    private IEnumerator ShootSpiral()
    {
        for(int i = 0; i < spiralBulletCount; i++)
        {
            float angle = i * (360f / spiralBulletCount);
            float spiralOffset = i * spiralTightenSpeed; // gets tighter
            Quaternion rot = firePoint.rotation * Quaternion.Euler(0, angle + spiralOffset, 0);
            SpawnBullet(firePoint.position, rot);

            yield return new WaitForSeconds(0.05f);
        }
    }

    private void ShootWave()
    {
        // 5 bullets in sine wave pattern
        for(int i = 0; i < waveBulletCount; i++)
        {
            float waveOffset = Mathf.Sin(i * waveFrequenzy) * waveAmplitude;
            Quaternion rot = firePoint.rotation * Quaternion.Euler(0, waveOffset, 0);
            SpawnBullet(firePoint.position, rot);
        }
    }

    private void SpawnBullet(Vector3 position, Quaternion rotation)
    {
        Debug.Log($"[PlayerController] SpawnBullet called - OwnerId: {OwnerId}");

        // get bullet from object pool
        NetworkObject bulletPrefab = NewObjectPoolManager.Instance.getObject(PoolObjectType.Bullet);

        // instantiate
        NetworkObject bulletNetObj = Instantiate(bulletPrefab, position, rotation);

        // spawn as network object 
        Spawn(bulletNetObj.gameObject);

        // konfig bullet
        Bullet bullet = bulletNetObj.GetComponent<Bullet>();
        if(bullet != null)
        {
            // WICHTIG: "this.NetworkObject" ist die Referenz auf diesen Player!
            NetworkObject shooterRef = this.NetworkObject;

            Debug.Log($"[PlayerController] Passing shooter to bullet:");
            Debug.Log($"  - Shooter NetworkObject: {(shooterRef != null ? "EXISTS" : "NULL")}");
            Debug.Log($"  - Shooter OwnerId: {(shooterRef != null ? shooterRef.OwnerId.ToString() : "N/A")}");
            Debug.Log($"  - this.OwnerId: {this.OwnerId}");

            bullet.ShootBullet(bulletDamage, bulletSpeed, bulletLifeTime, ShooterType.Player, shooterRef);

            Debug.Log($"[PlayerController] Bullet spawned and configured");
        }
        else
        {
            Debug.LogError("[PlayerController] Bullet component not found on spawned object!");
        }
    }
    

    #endregion

    private void OnDestroy() 
    {
        if (TimeManager != null)        
            TimeManager.OnTick -= OnServerTick;
        

        // unsubscribe from SyncVar changes
        playerColor.OnChange -= OnPlayerColorChanged;

        // unsubscribe from HP changes
        playerHP.OnChange -= OnPlayerHPChanged;

        playerName.OnChange -= OnPlayerNameChanged;

        //inputActions?.Player.Disable();
        //inputActions?.Disable();
    }
}