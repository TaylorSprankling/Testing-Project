using Unity.Netcode;
using UnityEngine;

public class BodyViewModel : NetworkBehaviour
{
    [SerializeField] private GameObject _playerModel;
    [SerializeField] private Transform _playerRotation;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner) return;
        _playerModel.SetActive(false);
    }
    
    private void Update()
    {
        if (IsOwner) return;
        _playerModel.transform.rotation = _playerRotation.rotation;
    }
}
