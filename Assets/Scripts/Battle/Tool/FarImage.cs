
    using UnityEngine;

    public class FarImage: MonoBehaviour
    {
        // 远景控制器，自身坐标跟随相机保持同步
        // 开始时计算一个默认偏移量
        private Vector3 offset;
        public Transform cameraTransform;
        
        
        [Range(0f, 1f)]
        public float parallaxRatio = 1f;
        private void Start()
        {
            if (cameraTransform != null)
            {
                offset = transform.position - cameraTransform.position;
            }
        }
        private void LateUpdate()
        {
            if (cameraTransform != null)
            {
                transform.position = (cameraTransform.position +  offset) * parallaxRatio;;
            }
        }
    }
