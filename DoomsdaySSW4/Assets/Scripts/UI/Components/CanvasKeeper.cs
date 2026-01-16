using UnityEngine;
using System.Collections;

/// <summary>
/// Canvas保持器：确保Canvas在运行时始终激活
/// </summary>
[RequireComponent(typeof(Canvas))]
public class CanvasKeeper : MonoBehaviour
{
    private Canvas _canvas;


    /// <summary>
    /// 在场景加载后立即确保所有Canvas都是激活的
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureAllCanvasesActive()
    {
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true); // true表示包括未激活的对象

        foreach (Canvas canvas in allCanvases)
        {
            if (!canvas.gameObject.activeInHierarchy)
            {
                canvas.gameObject.SetActive(true);
            }
            if (!canvas.enabled)
            {
                canvas.enabled = true;
            }
        }
    }

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
        if (_canvas == null)
        {
            _canvas = GetComponentInParent<Canvas>();
        }

        // 强制激活Canvas（无论当前状态如何）
        if (_canvas != null)
        {
            // 先激活GameObject
            if (!_canvas.gameObject.activeInHierarchy)
            {
                _canvas.gameObject.SetActive(true);
            }
            // 再启用Canvas组件
            if (!_canvas.enabled)
            {
                _canvas.enabled = true;
            }
            // 确保Render Mode正确
            if (_canvas.renderMode != RenderMode.ScreenSpaceOverlay && _canvas.renderMode != RenderMode.ScreenSpaceCamera)
            {
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }
    }

    private void OnEnable()
    {
        EnsureCanvasActive();
    }

    private void OnDisable()
    {
        // 只在运行时且GameObject仍然激活时才尝试重新激活Canvas
        // 退出Play模式时，GameObject会被禁用，此时无法启动协程，也不需要重新激活
        if (Application.isPlaying && gameObject.activeInHierarchy)
        {
            // 不能在OnDisable中直接调用SetActive，使用协程延迟到下一帧
            StartCoroutine(ReEnableCanvasNextFrame());
        }
    }

    /// <summary>
    /// 在下一帧重新启用Canvas
    /// </summary>
    private IEnumerator ReEnableCanvasNextFrame()
    {
        yield return null; // 等待一帧

        if (_canvas != null)
        {
            if (!_canvas.gameObject.activeInHierarchy)
            {
                _canvas.gameObject.SetActive(true);
            }
            if (!_canvas.enabled)
            {
                _canvas.enabled = true;
            }
        }
    }

    private void Start()
    {
        EnsureCanvasActive();
        
        // 启动协程持续监控Canvas状态
        StartCoroutine(MonitorCanvasState());
    }

    /// <summary>
    /// 协程：持续监控Canvas状态
    /// </summary>
    private IEnumerator MonitorCanvasState()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f); // 每0.1秒检查一次

            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
                if (_canvas == null)
                {
                    _canvas = GetComponentInParent<Canvas>();
                }
            }

            if (_canvas != null)
            {
                bool wasInactive = !_canvas.gameObject.activeInHierarchy;
                bool wasDisabled = !_canvas.enabled;
                
                if (wasInactive || wasDisabled)
                {
                    if (wasInactive)
                    {
                        _canvas.gameObject.SetActive(true);
                    }
                    if (wasDisabled)
                    {
                        _canvas.enabled = true;
                    }
                }
            }
        }
    }

    private void Update()
    {
        // 每帧都强制检查并激活Canvas（更激进的方法）
        if (_canvas == null)
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }
        }

        if (_canvas != null)
        {
            // 无论什么情况，都强制激活Canvas
            if (!_canvas.gameObject.activeInHierarchy)
            {
                _canvas.gameObject.SetActive(true);
            }
            if (!_canvas.enabled)
            {
                _canvas.enabled = true;
            }
        }
    }

    private void EnsureCanvasActive()
    {
        if (_canvas == null)
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }
        }

        if (_canvas != null)
        {
            bool wasInactive = !_canvas.gameObject.activeInHierarchy;
            bool wasDisabled = !_canvas.enabled;
            
            if (wasInactive || wasDisabled)
            {
                if (wasInactive)
                {
                    _canvas.gameObject.SetActive(true);
                }
                if (wasDisabled)
                {
                    _canvas.enabled = true;
                }
            }
        }
    }
}
