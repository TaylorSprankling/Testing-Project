using System.Collections;
using NaughtyAttributes;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class CanvasGroupController : MonoBehaviour
{
    public enum FadeOptions { FadeIn, FadeOut }
    
    private CanvasGroup _canvasGroup;
    
    [BoxGroup("Default Fade Options")]
    [SerializeField] private bool _fadeOnStart;
    [BoxGroup("Default Fade Options")]
    [SerializeField] private FadeOptions _fadeType = FadeOptions.FadeOut;
    [BoxGroup("Default Fade Options")] [Range(0.01f, 10f)]
    [SerializeField] private float _fadeTime = 1f;
    [BoxGroup("Default Fade Options")]
    [SerializeField] private float _delayBeforeFade;
    
    private void Reset()
    {
        if (!_canvasGroup) _canvasGroup = GetComponent<CanvasGroup>();
    }
    
    private void Awake()
    {
        if (!_canvasGroup) _canvasGroup = GetComponent<CanvasGroup>();
    }
    
    private void Start()
    {
        if (!_fadeOnStart) return;
        _canvasGroup.alpha = _fadeType == FadeOptions.FadeIn ? 0f : 1f;
        FadeCanvasGroup();
    }
    
    [Button]
    public void EnableCanvasGroup()
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }
    
    [Button]
    public void DisableCanvasGroup()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }
    
    public void FadeCanvasGroup()
    {
        FadeCanvasGroup(_fadeType, _fadeTime, _delayBeforeFade);
    }

    public void FadeCanvasGroup(FadeOptions type, float time, float delay = 0f)
    {
        StartCoroutine(FadeCanvas(type, time, delay));
    }
    
    private IEnumerator FadeCanvas(FadeOptions type, float time, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        
        switch (type)
        {
            default:
            case FadeOptions.FadeOut:
            {
                while (_canvasGroup.alpha > 0f)
                {
                    _canvasGroup.alpha -= Time.unscaledDeltaTime / time;
                    yield return null;
                }
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
                break;
            }
            
            case FadeOptions.FadeIn:
            {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
                while (_canvasGroup.alpha < 1f)
                {
                    _canvasGroup.alpha += Time.unscaledDeltaTime / time;
                    yield return null;
                }
                break;
            }
        }
    }
}