using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Serialization;

public class CameraController : MonoBehaviour
{
    // 引用cinemachine 使用鼠标滚轮缩放，缩放尺寸限制范围
    [LabelText("拖动相机")] public Cinemachine.CinemachineVirtualCamera virtualCamera;
    [LabelText("跟随相机")] public Cinemachine.CinemachineVirtualCamera followVCam;
    public float zoomSpeed = 10f;
    public float minZoom = 10f;
    public float maxZoom = 50f;

    // 中键按屏幕射线与水平面的交点拖动，位移不再乘帧间隔。
    private Vector3 _lastMousePosition;
    private bool isDragging;
    [LabelText("键盘平移速度"), MinValue(0)] public float panSpeed = 20f;
    [LabelText("中键拖动灵敏度"), MinValue(0)] public float dragSensitivity = 1f;

    private Sequence shakeTweener;
    private bool hasPendingFollow;
    private Transform pendingFollow; // 受击镜头期间最后一次跟随请求，结束后统一应用。
    private float focusOriginSize;
    private bool hitTweenFinished;
    private Transform hitOrigin;
    /// <summary>受击聚焦及缩放恢复尚未结束；回合调度应等待此状态解除。</summary>
    public bool IsPresentingHit { get; private set; }

    public Camera mainCam;
    public Camera colorCam;
    public Camera playerCam;

    public void Start()
    {
        virtualCamera.Priority = 0;
        followVCam.Priority = 10;
        impulse = followVCam.GetComponent<Cinemachine.CinemachineImpulseSource>();
        // 初始将相机设置到中等缩放
        float initialZoom = 10f;
        virtualCamera.m_Lens.OrthographicSize = initialZoom;
        followVCam.m_Lens.OrthographicSize = initialZoom;
    }

    


    void LateUpdate()
    {
        if (virtualCamera == null || followVCam == null) return;
        TryCompleteHitFocus();
        if (IsPresentingHit) { isDragging = false; return; }
        var selectedUI = UnityEngine.EventSystems.EventSystem.current != null
            ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
        if (selectedUI != null && selectedUI.GetComponent<UnityEngine.UI.InputField>() != null)
        {
            isDragging = false;
            return;
        }
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            float newSize = virtualCamera.m_Lens.OrthographicSize - scroll * zoomSpeed;
            virtualCamera.m_Lens.OrthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
            // 同时设置跟随相机的缩放
            followVCam.m_Lens.OrthographicSize = virtualCamera.m_Lens.OrthographicSize;
        }
        var battle = BattleScene.Ins != null ? BattleScene.Ins.BM : null;
        if (battle == null || (battle.PlayerController == null || !battle.PlayerController.isInTurn) && !battle.IsDeploying)
        {
            isDragging = false;
            return;
        }

        Vector2 keyboard = new Vector2(
            (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
            (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0));
        keyboard = Vector2.ClampMagnitude(keyboard, 1f);
        if (keyboard.sqrMagnitude > 0)
        {
            SetFollow(null);
            Vector3 right = Vector3.ProjectOnPlane(virtualCamera.transform.right, Vector3.up).normalized;
            Vector3 forward = Vector3.Cross(right, Vector3.up);
            virtualCamera.transform.position += (right * keyboard.x + forward * keyboard.y)
                * Mathf.Max(0, panSpeed) * Time.unscaledDeltaTime;
        }

        if (Input.GetMouseButtonDown(2))
        {
            _lastMousePosition = Input.mousePosition;
            isDragging = UnityEngine.EventSystems.EventSystem.current == null ||
                !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            if (isDragging) SetFollow(null);
        }
        if (!Input.GetMouseButton(2)) isDragging = false;

        if (isDragging)
        {
            SetFollow(null);
            var camera = mainCam != null ? mainCam : Camera.main;
            if (camera != null)
            {
                var ground = new Plane(Vector3.up, Vector3.zero);
                Ray previous = camera.ScreenPointToRay(_lastMousePosition);
                Ray current = camera.ScreenPointToRay(Input.mousePosition);
                if (ground.Raycast(previous, out float previousDistance) &&
                    ground.Raycast(current, out float currentDistance))
                {
                    Vector3 move = previous.GetPoint(previousDistance) - current.GetPoint(currentDistance);
                    move.y = 0;
                    virtualCamera.transform.position += move * Mathf.Max(0, dragSensitivity);
                }
            }
            _lastMousePosition = Input.mousePosition;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) isDragging = false;
    }

    private void OnDisable()
    {
        isDragging = false;
        CancelHitFocus();
        hasPendingFollow = false;
        pendingFollow = null;
    }

    /// <summary>统一切换跟随目标；受击表现期间延后执行，避免覆盖受击镜头。</summary>
    public void SetFollow(Transform transform)
    {
        if (IsPresentingHit)
        {
            hasPendingFollow = true;
            pendingFollow = transform;
            return;
        }
        ApplyFollow(transform);
    }

    private void ApplyFollow(Transform transform)
    {
        if (transform == null)
        {
            if (followVCam.Priority > virtualCamera.Priority)
            {
                // 拖动相机首先把位置和跟随相机重合
                var renderedCamera = mainCam != null ? mainCam : Camera.main;
                virtualCamera.transform.position = renderedCamera != null
                    ? renderedCamera.transform.position : followVCam.transform.position;
                // 当不跟随物体时，将拖动相机权重增大
                virtualCamera.Priority = 10;
                followVCam.Priority = 0;
            }
        }
        else
        {
            // 当跟随物体时，将跟随相机权重增大
            followVCam.Follow = transform;
            virtualCamera.Priority = 0;
            followVCam.Priority = 10;
        }
    }

    // 相机震动
    public float shakeDelay = 0.5f;

    // 震动力度 
    //public float shakeMagnitude;
    public Cinemachine.CinemachineImpulseSource impulse;

    /// <summary>聚焦受击目标；镜头与战斗表现全部结束后，由回合流程决定是否恢复行动者。</summary>
    public void FocusTarget(Transform target, Transform origin = null) => BeginHitFocus(target, origin, false, 1f);

    /// <summary>聚焦受击目标并震动、缩放；完整恢复缩放后才解除回合等待。</summary>
    public void FocusShake(Transform target, Transform origin = null, float amplitude = 1f) =>
        BeginHitFocus(target, origin, true, amplitude);

    private void CancelHitFocus()
    {
        shakeTweener?.Kill();
        shakeTweener = null;
        if (IsPresentingHit && followVCam != null) followVCam.m_Lens.OrthographicSize = focusOriginSize;
        IsPresentingHit = false;
        hitTweenFinished = false;
    }

    private void BeginHitFocus(Transform target, Transform origin, bool shake, float amplitude)
    {
        if (!isActiveAndEnabled || target == null || virtualCamera == null || followVCam == null) return;
        // 连续受击只替换旧任务，保留当前缩放和整段表现开始前的原始尺寸。
        shakeTweener?.Kill();
        if (!IsPresentingHit) focusOriginSize = followVCam.m_Lens.OrthographicSize;
        hitTweenFinished = false;
        hitOrigin = origin;
        IsPresentingHit = true;
        isDragging = false;
        ApplyFollow(target);
        shakeTweener = DOTween.Sequence();
        shakeTweener.SetUpdate(UpdateType.Normal, false);
        if (shake)
        {
            float zoom = minZoom + (maxZoom - minZoom) * 0.2f;
            shakeTweener.Append(DOTween.To(() => followVCam.m_Lens.OrthographicSize,
                x => followVCam.m_Lens.OrthographicSize = x, zoom, shakeDelay / 10f));
            shakeTweener.AppendCallback(() =>
            {
                followVCam.Follow = null;
                if (impulse == null) return;
                impulse.m_ImpulseDefinition.m_AmplitudeGain = amplitude;
                impulse.GenerateImpulse();
            });
            shakeTweener.AppendInterval(0.8f);
            shakeTweener.Append(DOTween.To(() => followVCam.m_Lens.OrthographicSize,
                x => followVCam.m_Lens.OrthographicSize = x, focusOriginSize, shakeDelay));
            shakeTweener.AppendInterval(0.5f);
        }
        else
        {
            // 普通命中接在暴击后时也平滑回到整段表现的原始尺寸。
            shakeTweener.Append(DOTween.To(() => followVCam.m_Lens.OrthographicSize,
                x => followVCam.m_Lens.OrthographicSize = x, focusOriginSize, shakeDelay));
            shakeTweener.AppendInterval(1.6f);
        }
        shakeTweener.OnComplete(() =>
        {
            shakeTweener = null;
            hitTweenFinished = true;
            TryCompleteHitFocus();
        });
    }
    private void TryCompleteHitFocus()
    {
        if (!IsPresentingHit || !hitTweenFinished) return;
        var battle = BattleScene.Ins != null ? BattleScene.Ins.BM : null;
        // 必须读取不含镜头的表现状态，否则镜头与回合调度会互相等待。
        if (battle != null && battle.HasActionPresentation) return;
        IsPresentingHit = false;
        hitTweenFinished = false;
        if (battle != null && battle.SpeedTurnsActive) battle.RestoreCameraAfterHit();
        else if (battle == null) ApplyFollow(hasPendingFollow ? pendingFollow : hitOrigin);
        hasPendingFollow = false;
        pendingFollow = null;
        hitOrigin = null;
    }

}
