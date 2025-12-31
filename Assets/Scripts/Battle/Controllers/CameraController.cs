using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraController : MonoBehaviour
{
    // 按住鼠标中间拖动时，相机跟随移动
    private Vector3 _lastMousePosition;
    public float panSpeed = 20f;

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
            transform.Translate(move, Space.Self);
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
    [FormerlySerializedAs("shakeDuration")] public float shakeDelay = 0.5f;
    // 震动力度 
    //public float shakeMagnitude;
    public Cinemachine.CinemachineImpulseSource impulse;
    public void FocusShake(Transform transform)
    {
        virtualCamera.Follow = transform;
        virtualCamera.m_Lens.OrthographicSize = minZoom;
        DOVirtual.DelayedCall(shakeDelay, () =>
        {
            virtualCamera.Follow = null;
            // 调用impulse的方法让相机震动
            if (impulse != null)
            {
                impulse.GenerateImpulse();
                Debug.Log("相机震动");
            }
        });
    }
}