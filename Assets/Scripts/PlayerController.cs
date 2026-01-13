using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;



public class PlayerController : NetworkBehaviour
{
    [Header("SyncVars")]
    private readonly SyncVar<string> playerName = new SyncVar<string>(); // name of the player
    private readonly SyncVar<Color> playerColor = new SyncVar<Color>(); // color of the player
    private readonly SyncVar<int> playerHP = new SyncVar<int>(); // health points of the player

    [SerializeField] private float playerSpeed;
    [SerializeField] private Transform playerTransform;

    public CharacterController cc;
    private InputSystem_Actions inputActions;
    private Camera mainCamera;

    private void Start() 
    { 
        inputActions = new InputSystem_Actions();
        inputActions.Player.Enable();
        TimeManager.OnTick += OnServerTick;
        mainCamera = Camera.main;
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

    /// TODO: Damage, Health
    /// IDamageable interface übernehmen
}
