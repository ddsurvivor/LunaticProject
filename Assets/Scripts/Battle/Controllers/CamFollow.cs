using UnityEngine;

/// <summary>叠加相机在绘制前同步主相机缩放，避免棋子和遮挡层落后一帧。</summary>
[ExecuteAlways]
public class CamFollow : MonoBehaviour
{
    public Camera mainCam;

    public Camera thisCam;
    private void Update() => SyncZoom();

    private void OnPreCull()
    {
        // Cinemachine及受击镜头会在Update后改Lens，必须读取本次渲染的最终尺寸。
        SyncZoom();
    }

    private void SyncZoom()
    {
        if (mainCam == null || thisCam == null || mainCam == thisCam) return;
        thisCam.orthographicSize = mainCam.orthographicSize;
    }
}
