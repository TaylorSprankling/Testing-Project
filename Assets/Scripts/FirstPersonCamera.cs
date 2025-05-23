using NaughtyAttributes;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class FirstPersonCamera : MonoBehaviour
{
    [SerializeField] private NetworkManager _networkManager;
    [SerializeField] private CinemachineCamera _cinemachineCamera;
    [SerializeField] private CinemachineInputAxisController _inputAxisController;
    [SerializeField] private Transform _playerOrientationReference;
    [SerializeField] [ReadOnly] private bool _controllingCamera;
    
    private void Reset()
    {
        if (!_inputAxisController) _inputAxisController = GetComponent<CinemachineInputAxisController>();
    }
    
    private void OnEnable()
    {
        _networkManager.OnClientConnectedCallback += AssignPlayerReferences;
    }
    
    private void OnDisable()
    {
        _networkManager.OnClientConnectedCallback -= AssignPlayerReferences;
    }
    
    private void AssignPlayerReferences(ulong obj)
    {
        _playerOrientationReference = _networkManager.SpawnManager.GetLocalPlayerObject().GetComponentInChildren<PlayerOrientation>().transform;
        _cinemachineCamera.Target.TrackingTarget = _playerOrientationReference.parent;
        _cinemachineCamera.enabled = true;
        EnableCameraControl(true);
    }
    
    public void EnableCameraControl(bool value)
    {
        switch (value)
        {
            case true:
                _controllingCamera = true;
                _inputAxisController.enabled = true;
                break;
            case false:
                _controllingCamera = false;
                _inputAxisController.enabled = false;
                break;
        }
    }
    
    private void Update()
    {
        if (!_controllingCamera || !_playerOrientationReference) return;
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        _playerOrientationReference.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    }
}