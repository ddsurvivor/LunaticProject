# 通用目标选择与敌人攻击预览

范围几何和阵营规则统一由 `SkillTargeting` 查询。`RangeUI` 负责输入、绘制、高亮，`SkillManager.GetTargets` 保留原接口并增加世界坐标重载。

```csharp
// 无需 UI，按技能配置查询敌人、队友、自身或友军尸体。
var targets = SkillTargeting.Query(caster, skill, worldPosition);

// 从给定集合选出存活队友，去重且不包含自身。
var allies = SkillTargeting.SelectAllies(caster, candidates, includeSelf: false);

// 覆盖目标类型时生成浅拷贝，不改变原技能资产；效果列表共享。
caster.StartSkillAttack(skill.WithTarget(SkillTarget.Ally));

// AI 与鼠标输入复用范围更新接口。
rangeUI.ShowPreview(caster, skill, worldPosition);
rangeUI.SetSelectionPosition(nextWorldPosition);
```

`RangeUI.ShowSkillRange(skill, targetType)` 只覆盖预览规则；实际施放请传入 `WithTarget` 后的技能。圆形沿用原来半径 1 的单体拾取范围，手雷以选点为爆炸中心，Nova 以施法者为中心；扇形保留 1 米近身盲区。弧形按显示的圆心、圆环宽度和半圆心角判断，玩家与敌人使用同一算法。

敌人近战、远程、技能及反应攻击在攻击动画前显示 0.8 秒范围。提示期间 `IsPerformingAction` 保持开启，暂停冻结计时，死亡/禁用取消提示和后续攻击。目标和位置沿用动作开始时的快照。未配置 RangeUI 的旧敌人会复用场上玩家的范围组件；也可在 `SkillManager.rangeUIPrefab` 显式指定模板。

运行 `./Tests/BattleRegression/Run.ps1`。新增检查覆盖友军选择、选点限距、圆形/手雷半径、双阵营扇形、Nova 中心、弧形旋转和退化参数，以及敌人范围组件的绑定/复用。测试使用数学和场景替身，不替代 Unity Play Mode。

Unity 场景验收：

1. 玩家分别预览单体敌人、队友、友军尸体、扇形、手雷、Nova、弧形，确认高亮与结算一致；移动至空地时清除高亮。
2. 敌人近战、远程和范围技能出手前显示范围约 0.8 秒，提示期间没有第二个 AI 动作或提前结束回合。
3. 提示中暂停再恢复，确认剩余时间继续；提示中杀死或禁用敌人，确认范围消失且不产生后续伤害。
4. 检查敌人警戒/夹击，以及第一次和后续攻击的范围位置、旋转和大小。
