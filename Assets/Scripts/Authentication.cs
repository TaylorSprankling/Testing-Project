using System.Threading.Tasks;
using ParrelSync;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Authentication : MonoBehaviour
{
    private void Awake()
    {
        _ = Login();
    }
    
    private static async Task Login() 
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized) 
        {
            InitializationOptions options = new InitializationOptions();
            
            #if UNITY_EDITOR
            // Used to differentiate the clients using ParrelSync, otherwise lobby will count them as the same
            options.SetProfile(ClonesManager.IsClone() ? ClonesManager.GetArgument() : "Primary");
            #endif
            
            await UnityServices.InitializeAsync(options);
        }
        
        if (!AuthenticationService.Instance.IsSignedIn) 
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Signed in anonymously with ID: {AuthenticationService.Instance.PlayerId}");
            await SceneManager.LoadSceneAsync("Game");
        }
    }
}
