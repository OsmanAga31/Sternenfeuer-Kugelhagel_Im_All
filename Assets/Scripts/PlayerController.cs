using UnityEngine;
using FishNet.Object;


public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float playerSpeed;
    [SerializeField] private Transform playerTransform;

    public CharacterController cc;
    private InputSystem_Actions inputActions;

    private void Start() 
    { 
        inputActions = new InputSystem_Actions();
        inputActions.Player.Enable();
        TimeManager.OnTick += OnServerTick;
    }

    private void OnServerTick() 
    {
        // read our players movement 
        Vector2 movementInput = inputActions.Player.Move.ReadValue<Vector2>();

        // if we are the client and owner, request move
        if (IsClientInitialized && IsOwner){

            MoveServerRPC(movementInput);
        }
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
}
