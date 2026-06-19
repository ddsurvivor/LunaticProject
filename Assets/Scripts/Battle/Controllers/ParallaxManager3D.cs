using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector; // 完美适配你的 Odin 插件

[DefaultExecutionOrder(2000)] // 确保在 Cinemachine 计算完边界和抖动后执行
public class ParallaxManager3D : MonoBehaviour
{
    [System.Serializable]
    public struct ParallaxItem
    {
        [LabelText("视差物体/组")] 
        public Transform target;

        [LabelText("视差系数 (0=不跟随, 1=完全相对相机静止)")] 
        [Range(0f, 1f)] 
        public float parallaxFactor;

        [LabelText("开启缩放补偿")] 
        [Tooltip("开启后，远景在相机滚轮缩放或 DOTween 震动缩放时，会在屏幕上保持固定大小，不会跟着放大缩小。")]
        public bool compensateZoom;

        // 内部记录的初始状态
        [HideInInspector] public Vector3 startPosition;
        [HideInInspector] public Vector3 startScale;
    }

    [LabelText("主相机引用（留空自动获取）")] 
    public Camera mainCamera;

    [LabelText("视差层级列表")] 
    public List<ParallaxItem> parallaxItems = new List<ParallaxItem>();

    private Vector3 _startCameraPosition;
    private float _startOrthoSize;
    private bool _isInitialized = false;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    // 使用延迟初始化，防止游戏刚启动时 Cinemachine 还没来得及同步Lens尺寸
    void Initialize()
    {
        if (mainCamera == null) return;

        _startCameraPosition = mainCamera.transform.position;
        _startOrthoSize = mainCamera.orthographicSize;

        for (int i = 0; i < parallaxItems.Count; i++)
        {
            ParallaxItem item = parallaxItems[i];
            if (item.target != null)
            {
                item.startPosition = item.target.position;
                item.startScale = item.target.localScale;
                parallaxItems[i] = item; // 结构体值类型写回
            }
        }
        _isInitialized = true;
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;
        
        // 第一帧动态初始化
        if (!_isInitialized) Initialize();

        // 1. 计算相机的 3D 位移差（完美应对你 45° 角的 transform.up/right 移动）
        Vector3 cameraDelta = mainCamera.transform.position - _startCameraPosition;
        
        // 2. 计算相机的缩放比例（应对你的滚轮缩放与 DOTween FocusShake）
        float currentOrthoSize = mainCamera.orthographicSize;
        float zoomRatio = currentOrthoSize / _startOrthoSize;

        // 3. 遍历更新所有视差物体
        for (int i = 0; i < parallaxItems.Count; i++)
        {
            ParallaxItem item = parallaxItems[i];
            if (item.target == null) continue;

            // --- 位置视差 ---
            Vector3 targetPosition = item.startPosition + (cameraDelta * item.parallaxFactor);
            item.target.position = targetPosition;

            // --- 缩放补偿 ---
            if (item.compensateZoom)
            {
                // 当 factor 为 1 时，缩放比例完全随相机 1:1 抵消，在屏幕上看起来大小绝对固定
                // 当 factor 为 0 时，保持原始大小，随相机正常放大缩小
                float scaleMultiplier = 1f + (zoomRatio - 1f) * item.parallaxFactor;
                item.target.localScale = item.startScale * scaleMultiplier;
            }
        }
    }
}