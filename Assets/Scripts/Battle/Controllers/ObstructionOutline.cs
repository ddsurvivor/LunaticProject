using UnityEngine;

/// <summary>
/// 角色被遮挡时自动开启 AllIn1SpriteShader 的描边效果
/// </summary>
public class ObstructionOutline : MonoBehaviour
{
    [Header("遮挡检测设置")] [Tooltip("障碍物所在的 Layer")]
    public LayerMask obstacleLayer;

    [Tooltip("射线扩散半径，多点检测时使用")] public float checkRadius = 0.2f;
    [Tooltip("发射的射线数量，越多越精确")] public int rayCount = 5;
    [Tooltip("在 Scene 视图中绘制调试射线")] public bool debugMode = true;

    public GameObject hightLight;
    //[Header("AllIn1SpriteShader 描边控制")] public string outlinePropertyName = "OUTBASE_ON";
    //[Tooltip("描边颜色（可选，如果留空则不修改颜色）")] public Color outlineColor = Color.white;

    //public SpriteRenderer spriteRenderer; // 所有需要控制的 SpriteRenderer
    //private MaterialPropertyBlock propertyBlock;
    private bool wasObstructed = false; // 上一帧的遮挡状态
    private Transform camTransform;

    void Start()
    {
        camTransform = Camera.main.transform;
        // 获取自身及所有子物体上的 SpriteRenderer（角色可能有多个部位）
        //spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        //propertyBlock = new MaterialPropertyBlock();

        // 初始化：确保描边处于关闭状态（如果材质中原本是开启的，这里会强制关闭）
        SetOutlineEnabled(false);

        // 可选：检查材质属性是否存在，发出警告
        /*if (spriteRenderers.Length > 0)
        {
            Material mat = spriteRenderers[0].sharedMaterial;
            if (mat != null && !mat.HasProperty(outlinePropertyName))
            {
                Debug.LogWarning($"材质 {mat.name} 中不存在属性 {outlinePropertyName}，请检查属性名是否正确。");
            }
        }*/
    }

    void Update()
    {
        CheckUpdate();
    }

    /// <summary>
    /// 外部调用此方法来检查遮挡状态并更新描边效果
    /// </summary>
    public void CheckUpdate()
    {
        bool isObstructed = CheckObstruction();
        if (isObstructed != wasObstructed)
        {
            Debug.Log($"更新碰撞{isObstructed}");
            SetOutlineEnabled(isObstructed);
            wasObstructed = isObstructed;
        }
    }

    /// <summary>
    /// 使用 3D 射线检测角色与摄像机之间是否有障碍物
    /// </summary>
    bool CheckObstruction()
    {
        Vector3 camPos = camTransform.position;
        Vector3 targetPos = transform.position;

        Vector3 direction = targetPos - camPos;
        float distance = direction.magnitude;

        // 单点检测
        if (rayCount == 1)
        {
            RaycastHit hit;
            bool hasHit = Physics.Raycast(camPos, direction, out hit, distance, obstacleLayer);
            if (debugMode)
                Debug.DrawRay(camPos, direction, hasHit ? Color.red : Color.green);

            if (hasHit)
            {
                Debug.Log($"射线击中: {hit.collider.gameObject.name}");
            }
            // 如果击中的物体不是角色自身
            return hasHit && hit.collider.gameObject != gameObject;
        }
        // 多点检测（围绕角色中心随机偏移）
        else
        {
            bool allHit = true; // 记录是否所有射线都被遮挡
            for (int i = 0; i < rayCount; i++)
            {
                Vector2 randomOffset = Random.insideUnitCircle * checkRadius;
                Vector3 randomTarget = targetPos + new Vector3(randomOffset.x, randomOffset.y, 0);
                Vector3 randomDir = randomTarget - camPos;
                float randomDist = randomDir.magnitude;

                RaycastHit hit;
                bool hasHit = Physics.Raycast(camPos, randomDir, out hit, randomDist, obstacleLayer);
                if (debugMode)
                    Debug.DrawRay(camPos, randomDir.normalized * randomDist, hasHit ? Color.red : Color.green);

                if (hasHit && hit.collider.gameObject != gameObject)
                    allHit = true;
                else if(!hasHit)
                    allHit = false;
            }
            return allHit;
        }
    }

    /// <summary>
    /// 设置所有 SpriteRenderer 的描边开关状态
    /// </summary>
    void SetOutlineEnabled(bool enabled)
    {
        hightLight.SetActive(enabled);
        /*float value = enabled ? 1f : 0f;


        spriteRenderer.GetPropertyBlock(propertyBlock);

        // 设置描边开关
        propertyBlock.SetFloat(outlinePropertyName, value);

        // 如果指定了描边颜色且启用描边，则设置颜色（可选）
        if (enabled && outlineColor != Color.clear)
        {
            // 注意：不同版本的 AllIn1SpriteShader 颜色属性名可能不同，常见为 _OutlineColor
            if (spriteRenderer.sharedMaterial.HasProperty("_OutlineColor"))
                propertyBlock.SetColor("_OutlineColor", outlineColor);
        }

        spriteRenderer.SetPropertyBlock(propertyBlock);*/
    }
}