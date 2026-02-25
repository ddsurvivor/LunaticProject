using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraController : MonoBehaviour
{
    // 按住鼠标中间拖动时，相机跟随移动
    private Vector3 _lastMousePosition;
    public float panSpeed = 20f;

    Tweener shakeTweener;
    void Update()
    {
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
            move = transform.right * move.x + transform.up * move.y;
            transform.position += move;
            _lastMousePosition = Input.mousePosition;
        }
    }
    
    
    // 引用cinemachine 使用鼠标滚轮缩放，缩放尺寸限制范围
    public Cinemachine.CinemachineVirtualCamera virtualCamera;
    public float zoomSpeed = 10f;
    public float minZoom = 10f;
    public float maxZoom = 50f;
    void LateUpdate()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            float newSize = virtualCamera.m_Lens.OrthographicSize - scroll * zoomSpeed;
            virtualCamera.m_Lens.OrthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }
    }

    public void SetFollow(Transform transform)
    {
        virtualCamera.Follow = transform;
    }

    // 相机震动
    public float shakeDelay = 0.5f;
    // 震动力度 
    //public float shakeMagnitude;
    public Cinemachine.CinemachineImpulseSource impulse;
    public void FocusShake(Transform transform)
    {
        virtualCamera.Follow = transform;
        //virtualCamera.m_Lens.OrthographicSize = minZoom;
        float currentSize = virtualCamera.m_Lens.OrthographicSize;
        shakeTweener?.Kill();
        shakeTweener = DOTween.To(
            () => virtualCamera.m_Lens.OrthographicSize,
            x => virtualCamera.m_Lens.OrthographicSize = x,
            minZoom,
            shakeDelay
        ).OnComplete(() =>
        {
            virtualCamera.Follow = null;
            // 调用impulse的方法让相机震动
            if (impulse != null)
            {
                impulse.GenerateImpulse();
                Debug.Log("相机震动");
            }

            DOTween.To(
                () => virtualCamera.m_Lens.OrthographicSize,
                x => virtualCamera.m_Lens.OrthographicSize = x,
                currentSize,
                shakeDelay).SetDelay(0.8f);
        });
    }
}