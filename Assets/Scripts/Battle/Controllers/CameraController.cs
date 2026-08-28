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

    // 按住鼠标中间拖动时，相机跟随移动
    private Vector3 _lastMousePosition;
    [LabelText("拖动速度")] public float panSpeed = 20f;

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
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            float newSize = virtualCamera.m_Lens.OrthographicSize - scroll * zoomSpeed;
            virtualCamera.m_Lens.OrthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
            // 同时设置跟随相机的缩放
            followVCam.m_Lens.OrthographicSize = virtualCamera.m_Lens.OrthographicSize;
        }
        if (!BattleScene.Ins.BM.PlayerController.isInTurn) return;
        if (Input.GetMouseButtonDown(2))
        {
            _lastMousePosition = Input.mousePosition;
            SetFollow(null);
        }

        if (Input.GetMouseButton(2))
        {
            Vector3 delta = Input.mousePosition - _lastMousePosition;
            Vector3 move = new Vector3(-delta.x, -delta.y, 0) * panSpeed * Time.deltaTime;
            //transform.Translate(move, Space.Self);
            // 将 move 投影到本地 x/y 平面（z=0）
            move = virtualCamera.transform.right * move.x + virtualCamera.transform.up * move.y;
            virtualCamera.transform.position += move;
            _lastMousePosition = Input.mousePosition;
        }
    }

    public void SetFollow(Transform transform)
    {
        if (transform == null)
        {
            if (followVCam.Priority > virtualCamera.Priority)
            {
                // 拖动相机首先把位置和跟随相机重合
                virtualCamera.transform.position = followVCam.transform.position;
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