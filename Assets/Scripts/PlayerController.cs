using FishNet.Object;
using FishNet.Object.Synchronizing;
//using NUnit.Framework.Constraints;
using UnityEngine;



public class PlayerController : NetworkBehaviour
{
    [Header("SyncVars")]
    private readonly SyncVar<string> playerName = new SyncVar<string>(); // name of the player
    private readonly SyncVar<Color> playerColor = new SyncVar<Color>(); // color of the player
    private readonly SyncVar<int> playerHP = new SyncVar<int>(); // health points of the player

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

    public CharacterController cc;
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
        cc.Move(move * playerSpeed * (float)TimeManager.TickDelta);
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
            transform.rotation = targetRotation;
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

    private void OnDestroy() 
    {
        if (TimeManager != null)
        {
            TimeManager.OnTick -= OnServerTick;
        }

        // unsubscribe from SyncVar changes
        playerColor.OnChange -= OnPlayerColorChanged;
    }

    /// TODO: Damage, Health
    /// IDamageable interface übernehmen
}
