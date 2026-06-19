using System.Collections.Generic;
using UnityEngine;

public class ParallaxManager3D : MonoBehaviour
{
    [System.Serializable]
    public struct ParallaxItem
    {
        [Header("视差物体/组")]
        public Transform target;

        [Header("视差系数 (0=完全不跟随, 0.5=慢速, 1=完全跟随相对相机静止)")]
        [Range(0f, 1f)]
        public float parallaxFactor;

        // 内部记录的初始世界坐标
        [HideInInspector] 
        public Vector3 startPosition;
    }

    [Header("相机引用（留空自动获取主相机）")]
    public Transform cameraTransform;

    [Header("需要在视差列表中管理的物体")]
    public List<ParallaxItem> parallaxItems = new List<ParallaxItem>();

    private Vector3 startCameraPosition;

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform == null)
        {
            Debug.LogError("ParallaxManager3D: 未找到主相机，请手动拖拽引用！");
            return;
        }

        // 记录相机的初始 3D 位置
        startCameraPosition = cameraTransform.position;

        // 记录所有物体的初始 3D 位置
        for (int i = 0; i < parallaxItems.Count; i++)
        {
            ParallaxItem item = parallaxItems[i];
            if (item.target != null)
            {
                item.startPosition = item.target.position;
                parallaxItems[i] = item;
            }
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // 核心改动：计算相机在 3D 空间中的【总位移向量】（包含 X, Y, Z）
        Vector3 cameraDelta = cameraTransform.position - startCameraPosition;

        // 遍历并更新列表中所有物体的 3D 坐标
        for (int i = 0; i < parallaxItems.Count; i++)
        {
            ParallaxItem item = parallaxItems[i];
            if (item.target == null) continue;

            // 核心改动：直接用 3D 向量乘以视差系数
            // 当系数为 1 时，targetPosition 的增量与相机完全一致，实现完美的相对静止
            Vector3 targetPosition = item.startPosition + (cameraDelta * item.parallaxFactor);

            // 应用新的 3D 位置
            item.target.position = targetPosition;
        }
    }
}