# TextMesh Pro 移除前引用清单

检查日期：2026-11-11。记录当前工作区快照；本次只新增清单，没有改动或删除代码、资源、包配置。

## 结论与待处理项

当前没有发现正在启用的业务 C# TMP 类型引用，没有发现场景/Prefab 上的 TMP 组件，没有发现托管 DLL 对 Unity.TextMeshPro 的程序集硬依赖。当前 Packages/manifest.json 和 packages-lock.json 已无 com.unity.textmeshpro 项。

| 项目 | 位置 | 当前状态 | 建议处理 |
|---|---|---|---|
| TMP 字体资产 | Assets/Fonts/Font/SourceHanSansCN-Normal SDF.asset:12 | 使用 TMP_FontAsset；是实际 TMP 资源依赖 | 如不再需要，删除该 asset 及同名 meta；无需迁移到场景文本 |
| 字体内嵌材质 | 同一 SDF.asset:6505 | 使用 TMP_SDF.shader；字体 atlas 与材质均内嵌于该文件 | 随 SDF 字体资产一起处理，不必寻找单独材质文件 |
| 遗留 using | Assets/Scripts/Battle/UI/PieceHeadUI.cs:4 | //using TMPro; 已注释；healthText 已是 UnityEngine.UI.Text（第 12 行） | 可删除这条注释，文本字段无需替换 |
| DOTween TMP 模块 | Assets/Plugins/Demigiant/DOTweenPro/DOTweenTextMeshPro.cs:8 | 整个文件处于 #if false // MODULE_MARKER 中 | 不产生编译依赖；按需保留禁用或删除适配文件及 meta |
| DOTween 运行时适配 | Assets/Plugins/Demigiant/DOTweenPro/DOTweenAnimation.cs | TMP using、Color/Fade/Text 分支均受 #if false // TEXTMESHPRO_MARKER 控制 | 保持禁用即可；详细行号见后文 |
| DOTween 检视器适配 | Assets/Plugins/Demigiant/DOTweenPro/Editor/DOTweenAnimationInspector.cs | TMP 类型表、Text 入口及组件识别分支均处于 #if false 中 | 保持禁用即可；详细行号见后文 |
| DOTween 功能开关 | Assets/Resources/DOTweenSettings.asset:47 | textMeshProEnabled: 0 | 已关闭，后续运行 DOTween Setup 时保持关闭 |
| DOTween 编辑器 DLL | Assets/Plugins/Demigiant/DOTween/Editor/DOTweenEditor.dll | 包含 TextMeshPro 字符串，但程序集引用表无 Unity.TextMeshPro/TMPro | 属于可选模块配置字符串命中，无须删除整个 DLL |

不要为了清理名称而删除 DOTweenAnimation.TargetType 的 TextMeshPro / TextMeshProUGUI 枚举项（第 56、57 行）：它们是枚举标签，不是 TMP 类型引用。现有序列化值为 14、15；扫描中没有发现 DOTweenAnimation 将 targetType 设置为这两个值。第三方代码优先保持原样并禁用模块。

## 字体资产及引用核对

- 文件：Assets/Fonts/Font/SourceHanSansCN-Normal SDF.asset。
- 配套 meta：Assets/Fonts/Font/SourceHanSansCN-Normal SDF.asset.meta。
- 资产 GUID：51ad3aabb834ca84a8afd8021f75b556。
- 第 12 行脚本 GUID：71c1514a6bd24e1e882cebbe1904ce04，对应 TMP_FontAsset。
- 第 6505 行 Shader GUID：68e6db2ebdc24f95958faec2be5558d6，对应 TMP_SDF.shader。
- 第 6456 行为内嵌 atlas，第 6504 行为内嵌材质名称。
- 对 Assets、Packages、ProjectSettings 中已扫描的文本资源反查字体 GUID，只有它自己的 meta 声明，没有找到其他消费者。
- SourceHanSansCN-Normal.otf 是源字体，GUID 为 9d6c5987eea1a364aae4da4e32440b78；它不是 TMP 专用字体资产，不因移除 TMP 就一并删除。

## 扫描范围与结果

| 类别 | 已检查数量 | TMP 命中结果 |
|---|---:|---|
| C# 文件 | 500 | 4 个文件，业务文件只有一条注释，另外 3 个为 DOTween 适配 |
| 场景 | 46 | 未发现 TMP 组件或资源 GUID 引用，包含未加入构建的场景 |
| Prefab | 276 | 未发现 TMP 组件或资源 GUID 引用 |
| 文本 .asset | 243 | 1 个 SDF 字体资产，内含材质与 atlas |
| 独立材质 .mat | 71 | 未发现 TMP GUID 引用 |
| Animation / Controller | 112 / 112 | 未发现 TMP GUID 引用 |
| asmdef | 8 | 未发现 Unity.TextMeshPro/TMPro 引用 |
| 托管 DLL | 28 | PE 元数据程序集引用表无 Unity.TextMeshPro/TMPro 硬依赖 |

另外检查了脚本、配置、Shader、Lua、UI Toolkit 文件和 meta，按本机缓存 TMP 3.0.6/3.0.9 的脚本及其 Essential Resources、Examples & Extras 归档建立 339 个 GUID 的对照表。对 Assets 下现有 .unitypackage 归档中的可读资源和脚本做了额外检查，未发现 TMP 相关命中；归档未导入项目。

边界：10 个 NavMesh.asset 是二进制导航数据，未使用 Unity AssetDatabase 解析其内部依赖；项目全部场景与 Prefab 均为可读文本并已扫描。DLL 检查覆盖程序集硬依赖和相关字符串，无法证明运行时构造字符串或外部动态加载绝不存在。Library/Temp/构建输出和全局包缓存不作为项目使用点；缓存仅用于解析 TMP GUID。未启动 Unity、执行 Play 或验证卸载后的完整编译。

## 逐行文本命中

下面保留全部 TMP 名称命中的行号和内容，包括注释、禁用分支、枚举名称与配置开关，便于逐处清理。字体的 GUID 依赖已单独记录在上方。

### Assets/Plugins/Demigiant/DOTweenPro/DOTweenAnimation.cs

| 行号 | 内容 |
|---:|---|
| 11 | `#if false // TEXTMESHPRO_MARKER` |
| 12 | `using TMPro;` |
| 56 | `TextMeshPro,` |
| 57 | `TextMeshProUGUI` |
| 358 | `#if false // TEXTMESHPRO_MARKER` |
| 359 | `case TargetType.TextMeshProUGUI:` |
| 360 | `tween = ((TextMeshProUGUI)target).DOColor(endValueColor, duration);` |
| 362 | `case TargetType.TextMeshPro:` |
| 363 | `tween = ((TextMeshPro)target).DOColor(endValueColor, duration);` |
| 401 | `#if false // TEXTMESHPRO_MARKER` |
| 402 | `case TargetType.TextMeshProUGUI:` |
| 403 | `tween = ((TextMeshProUGUI)target).DOFade(endValueFloat, duration);` |
| 405 | `case TargetType.TextMeshPro:` |
| 406 | `tween = ((TextMeshPro)target).DOFade(endValueFloat, duration);` |
| 426 | `#if false // TEXTMESHPRO_MARKER` |
| 428 | `case TargetType.TextMeshProUGUI:` |
| 429 | `tween = ((TextMeshProUGUI)target).DOText(endValueString, duration, optionalBool0, optionalScrambleMode, optionalString);` |
| 431 | `case TargetType.TextMeshPro:` |
| 432 | `tween = ((TextMeshPro)target).DOText(endValueString, duration, optionalBool0, optionalScrambleMode, optionalString);` |

### Assets/Plugins/Demigiant/DOTweenPro/DOTweenTextMeshPro.cs

| 行号 | 内容 |
|---:|---|
| 15 | `using TMPro;` |
| 29 | `/// Methods that extend TMP_Text objects and allow to directly create and control tweens from their instances.` |
| 35 | `/// <summary>Tweens a TextMeshPro's color to the given value.` |
| 36 | `/// Also stores the TextMeshPro as the tween's target so it can be used for filtered operations</summary>` |
| 38 | `public static TweenerCore<Color, Color, ColorOptions> DOColor(this TMP_Text target, Color endValue, float duration)` |
| 45 | `/// <summary>Tweens a TextMeshPro's faceColor to the given value.` |
| 46 | `/// Also stores the TextMeshPro as the tween's target so it can be used for filtered operations</summary>` |
| 48 | `public static TweenerCore<Color, Color, ColorOptions> DOFaceColor(this TMP_Text target, Color32 endValue, float duration)` |
| 55 | `/// <summary>Tweens a TextMeshPro's outlineColor to the given value.` |
| 56 | `/// Also stores the TextMeshPro as the tween's target so it can be used for filtered operations</summary>` |
| 58 | `public static TweenerCore<Color, Color, ColorOptions> DOOutlineColor(this TMP_Text target, Color32 endValue, float duration)` |
| 65 | `/// <summary>Tweens a TextMeshPro's glow color to the given value.` |
| 66 | `/// Also stores the TextMeshPro as the tween's target so it can be used for filtered operations</summary>` |
| 69 | `public static TweenerCore<Color, Color, ColorOptions> DOGlowColor(this TMP_Text target, Color endValue, float duration, bool useSharedMaterial = false)` |
| 78 | `/// <summary>Tweens a TextMeshPro's alpha color to the given value.` |
| 79 | `/// Also stores the TextMeshPro as the tween's target so it can be used for filtered operations</summary>` |
| 81 | `public static TweenerCore<Color, Color, ColorOptions> DOFade(this TMP_Text target, float endValue, float duration)` |
| 88 | `/// <summary>Tweens a TextMeshPro faceColor's alpha to the given value.` |
| 89 | `/// Also stores the TextMeshPro as the tween's target so it can be used for filtered operations</summary>` |
| 91 | `public static TweenerCore<Color, Color, ColorOptions> DOFaceFade(this TMP_Text target, float endValue, float duration)` |
| 102 | `/// <summary>Tweens a TextMeshPro's scale to the given value (using correct uniform scale as TMP requires).` |
| 103 | `/// Also stores the TextMeshPro as the tween's target so it can be used for filtered operations</summary>` |
| 105 | `public static TweenerCore<Vector3, Vector3, VectorOptions> DOScale(this TMP_Text target, float endValue, float duration)` |
| 115 | `/// Tweens a TextMeshPro's text from one integer to another, with options for thousands separators` |
| 123 | `this TMP_Text target, int fromValue, int endValue, float duration, bool addThousandsSeparator = true, CultureInfo culture = null` |
| 137 | `/// <summary>Tweens a TextMeshPro's fontSize to the given value.` |
| 138 | `/// Also stores the TextMeshPro as the tween's target so it can be used for filtered operations</summary>` |
| 140 | `public static TweenerCore<float, float, FloatOptions> DOFontSize(this TMP_Text target, float endValue, float duration)` |
| 147 | `/// <summary>Tweens a TextMeshPro's maxVisibleCharacters to the given value.` |
| 148 | `/// Also stores the TextMeshPro as the tween's target so it can be used for filtered operations</summary>` |
| 150 | `public static TweenerCore<int, int, NoOptions> DOMaxVisibleCharacters(this TMP_Text target, int endValue, float duration)` |
| 157 | `/// <summary>Tweens a TextMeshPro's text to the given value.` |
| 158 | `/// Also stores the TextMeshPro as the tween's target so it can be used for filtered operations</summary>` |
| 166 | `public static TweenerCore<string, string, StringOptions> DOText(this TMP_Text target, string endValue, float duration, bool richTextEnabled = true, ScrambleMode scrambleMode = ScrambleMode.None, string scrambleChars = null)` |
| 184 | `/// Wrapper for <see cref="TMP_Text"/> objects that enables per-character tweening` |
| 188 | `/// DOTweenTMPAnimator animator = new DOTweenTMPAnimator(myTextMeshProTextField);<para/>` |
| 194 | `static readonly Dictionary<TMP_Text,DOTweenTMPAnimator> _targetToAnimator = new Dictionary<TMP_Text,DOTweenTMPAnimator>();` |
| 196 | `/// <summary><see cref="TMP_Text"/> that this animator is linked to</summary>` |
| 197 | `public TMP_Text target { get; private set; }` |
| 198 | `public TMP_TextInfo textInfo { get; private set; }` |
| 200 | `TMP_MeshInfo[] _cachedMeshInfos;` |
| 204 | `/// Creates a new instance of the <see cref="DOTweenTMPAnimator"/>, which is necessary to animate <see cref="TMP_Text"/> by single characters.<para/>` |
| 205 | `/// If a <see cref="DOTweenTMPAnimator"/> already exists for the same <see cref="TMP_Text"/> object it will be disposed` |
| 207 | `/// If you want to animate the whole text object you don't need this, and you can use direct <see cref="TMP_Text"/> DO shortcuts instead.<para/>` |
| 208 | `/// IMPORTANT: the <see cref="TMP_Text"/> target must have been enabled/activated at least once before you can use it with this` |
| 210 | `/// <param name="target">The <see cref="TMP_Text"/> that will be linked to this animator</param>` |
| 211 | `public DOTweenTMPAnimator(TMP_Text target)` |
| 238 | `TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);` |
| 244 | `public static void DisposeInstanceFor(TMP_Text target)` |
| 260 | `TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);` |
| 264 | `/// Refreshes the animator text data and resets all transformation data. Call this after you change the target <see cref="TMP_Text"/>` |
| 882 | `TMP_MeshInfo _meshInfo;` |
| 884 | `public CharTransform(int charIndex, TMP_TextInfo textInfo, TMP_MeshInfo[] cachedMeshInfos) : this()` |
| 893 | `public void Refresh(TMP_TextInfo textInfo, TMP_MeshInfo[] cachedMeshInfos)` |
| 895 | `TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];` |
| 907 | `public void ResetAll(TMP_Text target, TMP_MeshInfo[] meshInfos, TMP_MeshInfo[] cachedMeshInfos)` |
| 921 | `public void ResetGeometry(TMP_Text target, TMP_MeshInfo[] cachedMeshInfos)` |
| 934 | `public void ResetColors(TMP_Text target, TMP_MeshInfo[] meshInfos)` |
| 942 | `target.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);` |
| 945 | `public Color32 GetColor(TMP_MeshInfo[] meshInfos)` |
| 958 | `public void UpdateAlpha(TMP_Text target, Color alphaColor, TMP_MeshInfo[] meshInfos, bool apply = true)` |
| 966 | `if (apply) target.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);` |
| 969 | `public void UpdateColor(TMP_Text target, Color32 color, TMP_MeshInfo[] meshInfos, bool apply = true)` |
| 976 | `if (apply) target.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);` |
| 979 | `public void UpdateGeometry(TMP_Text target, Vector3 offset, Quaternion rotation, Vector3 scale, TMP_MeshInfo[] cachedMeshInfos, bool apply = true)` |
| 1006 | `public void ShiftVertices(TMP_Text target, Vector3 topLeftShift, Vector3 topRightShift, Vector3 bottomLeftShift, Vector3 bottomRightShift)` |
| 1021 | `public void ResetVerticesShift(TMP_Text target)` |

### Assets/Plugins/Demigiant/DOTweenPro/Editor/DOTweenAnimationInspector.cs

| 行号 | 内容 |
|---:|---|
| 18 | `#if false // TEXTMESHPRO_MARKER` |
| 19 | `using TMPro;` |
| 123 | `#if false // TEXTMESHPRO_MARKER` |
| 125 | `{ DOTweenAnimation.AnimationType.Color, new[] { typeof(TextMeshPro), typeof(TextMeshProUGUI) } },` |
| 126 | `{ DOTweenAnimation.AnimationType.Fade, new[] { typeof(TextMeshPro), typeof(TextMeshProUGUI) } },` |
| 127 | `{ DOTweenAnimation.AnimationType.Text, new[] { typeof(TextMeshPro), typeof(TextMeshProUGUI) } }` |
| 144 | `#if false // TEXTMESHPRO_MARKER` |
| 616 | `#if false // TEXTMESHPRO_MARKER` |

### Assets/Resources/DOTweenSettings.asset

| 行号 | 内容 |
|---:|---|
| 47 | `textMeshProEnabled: 0` |

### Assets/Scripts/Battle/UI/PieceHeadUI.cs

| 行号 | 内容 |
|---:|---|
| 4 | `//using TMPro;` |

## 建议修改顺序

1. 处理上述 SDF.asset 及配套 meta，保留仍需要使用的 OTF 源字体。
2. 清理 PieceHeadUI 的遗留 using 注释。
3. 保持 DOTween TMP 模块禁用；如果要彻底清理适配代码，按上面的逐行清单操作，保留枚举数值兼容性。
4. 当前包清单已无 TMP，无需再次移除；等待 Unity 完成编译，打开实际使用场景检查 UI，并验证一次 Player 构建。

原始扫描明细位于 tmp/tmp-usage-audit.json，完整文件清单位于 tmp/tmp-usage-scanned-files.txt，托管 DLL 的程序集引用表位于 tmp/tmp-usage-dll-references.json。它们是被 Git 忽略的临时审计产物；长期保留本清单即可。
