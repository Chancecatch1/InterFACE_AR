
// mj

using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class PressScale : MonoBehaviour
{
    [Tooltip("Which transform to scale on press. Defaults to this object.")]
    public Transform target;

    [Tooltip("Automatically pick a visible child target when none is set. Turn this off to scale the whole button/root.")]
    [SerializeField] private bool autoPickTarget = true;

    [Tooltip("Scale factor to apply while pressed.")]
    [Range(0.6f, 1.0f)]
    public float pressedScale = 0.94f;

    [Tooltip("Seconds for each leg of the press animation (down and up)." )]
    [Range(0.01f, 0.25f)]
    public float stepDuration = 0.06f;

    [Tooltip("Easing for the scale animation.")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    Vector3 _initialScale;
    Coroutine _anim;

    Button _uiButton;

    // MRTK reflection wiring
    bool _mrtkHooked;
    object _siComp;

    object _onClickedEvent;
    Delegate _onClickedDel;
    System.Reflection.MethodInfo _onClickedRemove;

    object _selectEnteredEvent;
    Delegate _selectEnteredDel;
    System.Reflection.MethodInfo _selectEnteredRemove;

    object _selectExitedEvent;
    Delegate _selectExitedDel;
    System.Reflection.MethodInfo _selectExitedRemove;

    void Awake()
    {
        if (target == null)
        {
            if (autoPickTarget)
            {
                var best = FindBestVisualTarget(transform);
                target = best != null ? best : transform;
            }
            else
            {
                target = transform;
            }
        }

        _initialScale = target != null ? target.localScale : Vector3.one;

        _uiButton = GetComponent<Button>();
    }

    void OnEnable()
    {
        // Subscribe to available click/press sources.
        if (_uiButton != null)
        {
            _uiButton.onClick.AddListener(PressPulse);
        }
        TryWireMRTK();
    }

    void OnDisable()
    {
        if (_uiButton != null)
        {
            _uiButton.onClick.RemoveListener(PressPulse);
        }
        UnwireMRTK();

        if (_anim != null)
        {
            StopCoroutine(_anim);
            _anim = null;
        }
        // Ensure we leave the object at its initial scale.
        if (target != null)
            target.localScale = _initialScale;
    }

    // Public methods in case you later want to wire SelectEntered/Exited directly.
    public void PressDown()
    {
        StartAnim(_initialScale * pressedScale, stepDuration);
    }

    public void ReleaseUp()
    {
        StartAnim(_initialScale, stepDuration);
    }

    public void PressPulse()
    {
        // Down then up.
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(CoPulse());
    }

    IEnumerator CoPulse()
    {
        yield return CoLerpScale(_initialScale, _initialScale * pressedScale, stepDuration);
        yield return CoLerpScale(target.localScale, _initialScale, stepDuration);
        _anim = null;
    }

    void StartAnim(Vector3 to, float duration)
    {
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(CoLerpScale(target.localScale, to, duration));
    }

    IEnumerator CoLerpScale(Vector3 from, Vector3 to, float duration)
    {
        if (target == null) yield break;
        if (duration <= 0f)
        {
            target.localScale = to;
            yield break;
        }
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float k = Mathf.Clamp01(t);
            float e = ease != null ? ease.Evaluate(k) : k;
            target.localScale = Vector3.LerpUnclamped(from, to, e);
            yield return null;
        }
        target.localScale = to;
    }

    // Reflection hook for MRTK StatefulInteractable OnClicked so this works without UI Button.
    void TryWireMRTK()
    {
        if (_mrtkHooked) return;
        try
        {
            var type = FindType("MixedReality.Toolkit.UX.StatefulInteractable");
            if (type == null) return;
            _siComp = GetComponent(type);
            if (_siComp == null) return;

            // Hook OnClicked (no-arg), used as a fallback pulse
            TryHookNoArgEvent(type, _siComp, "OnClicked", PressPulse,
                out _onClickedEvent, out _onClickedDel, out _onClickedRemove);

            // Hook SelectEntered -> PressDown (immediate visual when pressed)
            TryHookGenericEvent(
                type,
                _siComp,
                new[] {
                    "OnSelectEntered", "onSelectEntered", "m_OnSelectEntered",
                    "FirstSelectEntered", "m_FirstSelectEntered",
                    "SelectEntered", "selectEntered", "m_SelectEntered"
                },
                GetType().GetMethod(nameof(OnGenericEnter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance),
                out _selectEnteredEvent, out _selectEnteredDel, out _selectEnteredRemove);

            // Hook SelectExited -> ReleaseUp
            TryHookGenericEvent(
                type,
                _siComp,
                new[] {
                    "OnSelectExited", "onSelectExited", "m_OnSelectExited",
                    "LastSelectExited", "m_LastSelectExited",
                    "SelectExited", "selectExited", "m_SelectExited"
                },
                GetType().GetMethod(nameof(OnGenericExit), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance),
                out _selectExitedEvent, out _selectExitedDel, out _selectExitedRemove);

            _mrtkHooked = (_onClickedDel != null) || (_selectEnteredDel != null) || (_selectExitedDel != null);
        }
        catch { /* ignore */ }
    }

    void UnwireMRTK()
    {
        if (!_mrtkHooked) return;
        try
        {
            // Remove OnClicked
            TryRemoveListener(_onClickedEvent, _onClickedRemove, _onClickedDel);
            _onClickedEvent = null; _onClickedRemove = null; _onClickedDel = null;

            // Remove SelectEntered/Exited
            TryRemoveListener(_selectEnteredEvent, _selectEnteredRemove, _selectEnteredDel);
            _selectEnteredEvent = null; _selectEnteredRemove = null; _selectEnteredDel = null;

            TryRemoveListener(_selectExitedEvent, _selectExitedRemove, _selectExitedDel);
            _selectExitedEvent = null; _selectExitedRemove = null; _selectExitedDel = null;
        }
        catch { /* ignore */ }
        finally
        {
            _mrtkHooked = false;
            _siComp = null;
        }
    }

    static System.Type FindType(string fullName)
    {
        var asms = System.AppDomain.CurrentDomain.GetAssemblies();
        foreach (var asm in asms)
        {
            var t = asm.GetType(fullName);
            if (t != null) return t;
        }
        return null;
    }

    // Helper: hook no-arg UnityEvent via reflection
    void TryHookNoArgEvent(System.Type hostType, object host, string propertyName, UnityAction action,
        out object unityEvent, out Delegate del, out System.Reflection.MethodInfo removeMethod)
    {
        unityEvent = null; del = null; removeMethod = null;
        try
        {
            var prop = hostType.GetProperty(propertyName);
            if (prop == null) return;
            var evt = prop.GetValue(host, null);
            if (evt == null) return;
            var evtType = evt.GetType();
            var add = evtType.GetMethod("AddListener", new System.Type[] { typeof(UnityEngine.Events.UnityAction) });
            var remove = evtType.GetMethod("RemoveListener", new System.Type[] { typeof(UnityEngine.Events.UnityAction) });
            if (add == null || remove == null) return;
            add.Invoke(evt, new object[] { action });
            unityEvent = evt;
            del = action;
            removeMethod = remove;
        }
        catch { /* ignore */ }
    }

    // Helper: hook UnityEvent<T> via reflection to call generic wrapper
    void TryHookGenericEvent(System.Type hostType, object host, string[] memberNames, System.Reflection.MethodInfo genericHandler,
        out object unityEvent, out Delegate del, out System.Reflection.MethodInfo removeMethod)
    {
        unityEvent = null; del = null; removeMethod = null;
        try
        {
            object evt = null; System.Type evtType = null;
            // Try properties first
            foreach (var name in memberNames)
            {
                var prop = hostType.GetProperty(name);
                if (prop != null)
                {
                    evt = prop.GetValue(host, null);
                    evtType = evt?.GetType();
                    if (evt != null) break;
                }
                var field = hostType.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (field != null)
                {
                    evt = field.GetValue(host);
                    evtType = evt?.GetType();
                    if (evt != null) break;
                }
            }
            if (evt == null) return;

            // Find AddListener with one parameter (UnityAction<T>)
            var add = evtType.GetMethods().FirstOrDefault(m => m.Name == "AddListener" && m.GetParameters().Length == 1);
            var remove = evtType.GetMethods().FirstOrDefault(m => m.Name == "RemoveListener" && m.GetParameters().Length == 1);
            if (add == null || remove == null) return;

            var paramType = add.GetParameters()[0].ParameterType; // UnityAction<T>
            if (!paramType.IsGenericType) return;
            var argType = paramType.GenericTypeArguments[0];

            var closedHandler = genericHandler.MakeGenericMethod(argType);
            var handlerDelegate = Delegate.CreateDelegate(paramType, this, closedHandler);

            add.Invoke(evt, new object[] { handlerDelegate });

            unityEvent = evt;
            del = handlerDelegate;
            removeMethod = remove;
        }
        catch { /* ignore */ }
    }

    void TryRemoveListener(object evt, System.Reflection.MethodInfo remove, Delegate d)
    {
        try
        {
            if (evt != null && remove != null && d != null)
            {
                remove.Invoke(evt, new object[] { d });
            }
        }
        catch { /* ignore */ }
    }

    // Generic wrappers to satisfy UnityEvent<T>
    void OnGenericEnter<T>(T _)
    {
        PressDown();
    }

    void OnGenericExit<T>(T _)
    {
        ReleaseUp();
    }

    Transform FindBestVisualTarget(Transform root)
    {
        // 1) Try common names in your buttons
        var known = FindDeepChildByNames(root, new[] { "Frontplate", "FrontPlate", "Front", "Backplate", "BackPlate", "AnimatedContent", "Icon", "RawImage", "Backglow", "Label", "Text" });
        if (known != null) return known;
        // 2) Fallback to first descendant that has a UI Graphic
        var g = FindFirstGraphicChild(root);
        if (g != null) return g;
        // 3) Give up and use root
        return root;
    }

    Transform FindDeepChildByNames(Transform parent, string[] names)
    {
        foreach (var n in names)
        {
            var t = FindDeepChild(parent, n);
            if (t != null) return t;
        }
        return null;
    }

    Transform FindDeepChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            if (string.Equals(c.name, name, System.StringComparison.OrdinalIgnoreCase))
                return c;
            var r = FindDeepChild(c, name);
            if (r != null) return r;
        }
        return null;
    }

    Transform FindFirstGraphicChild(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            if (c.GetComponent<UnityEngine.UI.Graphic>() != null)
                return c;
            var r = FindFirstGraphicChild(c);
            if (r != null) return r;
        }
        return null;
    }
}
