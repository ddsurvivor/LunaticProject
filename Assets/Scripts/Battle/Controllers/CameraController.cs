
    using UnityEngine;

    public class CameraController: MonoBehaviour
    {
        // 按住鼠标中间拖动时，相机跟随移动
        private Vector3 _lastMousePosition;
        public float panSpeed = 20f;
        void Update()
        {
            if (Input.GetMouseButtonDown(2))
            {
                _lastMousePosition = Input.mousePosition;
            }
            if (Input.GetMouseButton(2))
            {
                Vector3 delta = Input.mousePosition - _lastMousePosition;
                Vector3 move = new Vector3(-delta.x, 0, -delta.y) * panSpeed * Time.deltaTime;
                transform.Translate(move, Space.Self);
                _lastMousePosition = Input.mousePosition;
            }
        }
    }
