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

    Sequence shakeTweener;

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

    private void OnDisable() => isDragging = false;

    public void SetFollow(Transform transform)
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

    public void FocusTarget(Transform transform, Transform origin = null)
    {
        virtualCamera.Priority = 0;
        followVCam.Priority = 10;
        followVCam.Follow = transform;
        DOVirtual.DelayedCall(1.6f, () =>
        {
            SetFollow(origin);
        });
    }
    public void FocusShake(Transform transform, Transform origin = null, float amplitude = 1f)
    {
        virtualCamera.Priority = 0;
        followVCam.Priority = 10;
        followVCam.Follow = transform;
        //virtualCamera.m_Lens.OrthographicSize = minZoom;
        float currentSize = followVCam.m_Lens.OrthographicSize;
        float zoom = minZoom + (maxZoom - minZoom) * 0.2f;
        shakeTweener?.Kill();
        shakeTweener = DOTween.Sequence();
        shakeTweener.SetUpdate(UpdateType.Normal, false);
        shakeTweener.Append(DOTween.To(
            () => followVCam.m_Lens.OrthographicSize,
            x => followVCam.m_Lens.OrthographicSize = x,
            zoom,
            shakeDelay/10f
        ));
        shakeTweener.AppendCallback(() =>
        {
            followVCam.Follow = null;
            // 调用impulse的方法让相机震动
            if (impulse != null)
            {
                impulse.m_ImpulseDefinition.m_AmplitudeGain = amplitude; // 设置震动强度
                impulse.GenerateImpulse();
                //Debug.Log("相机震动");
            }
        });
        shakeTweener.AppendInterval(0.8f);
        shakeTweener.Append(DOTween.To(
            () => followVCam.m_Lens.OrthographicSize,
            x => followVCam.m_Lens.OrthographicSize = x,
            currentSize,
            shakeDelay)
        );
        shakeTweener.AppendInterval(0.5f);
        shakeTweener.OnComplete(() =>
        {
            virtualCamera.Priority = 10;
            followVCam.Priority = 0;
            if(origin!= null)
                SetFollow(origin);
            else
                followVCam.Follow = null;
        });
    }
}
