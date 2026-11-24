using UnityEngine;

/// <summary>可选中的独立护盾目标；不登记到棋子行动队列、胜负名单或奖励名单。</summary>
public sealed class RangeBarrier : PieceController
{
    public BarrierEmitter Emitter { get; private set; }
    [SerializeField] private Vector3 targetOffset = new Vector3(2.2f, 0, 0);
    [SerializeField, Min(.01f)] private float authoredRadius = 8;
    private bool dismissed;
    private bool broken; // 重复死亡回调不重复施加停机或播放破盾表现。
    private PieceController previewAttacker;
    private SkillPack previewSkill;
    public Vector3 Center => Emitter != null ? Emitter.transform.position : transform.position;
    public float Radius => Emitter != null ? Emitter.radius : 0;
    public bool IsActive => !dismissed && Emitter != null && Emitter.isActiveAndEnabled && Emitter.IsCurrentLife &&
        BattleManager.IsSpeedParticipant(Emitter.Owner) && !isDead && gameObject.activeInHierarchy;
    public bool Contains(Vector3 point) => (point - Center).sqrMagnitude <= Radius * Radius;

    /// <summary>由发射器创建一次，生命独立且不具有行动资源；原始阵营沿用发射者。</summary>
    public void Initialize(BarrierEmitter emitter)
    {
        Emitter = emitter;
        isPlayerPiece = false;
        Init(null, new PieceData { pieceName = "群体护盾", maxHealth = emitter.durability, speed = 0 });
        var visual = pieceDisplay.pieceSpriteRenderer.transform;
        float ratio = Radius / authoredRadius;
        visual.localScale *= ratio;
        visual.localPosition = (visual.localPosition + targetOffset) * ratio - targetOffset;
        isPlayerPiece = emitter.Owner.isPlayerPiece;
        transform.position = emitter.transform.position + targetOffset;
        BattleScene.Ins.BM.RangeBarriers.Add(this);
        pieceDisplay.ChangeDisplayState(PieceDisplayState.Idle);
    }

    private void LateUpdate()
    {
        if (!IsActive) { if (!isDead) Dismiss(); return; }
        transform.position = Emitter.transform.position + targetOffset;
    }

    public override void Hurt() { }

    /// <summary>独立耐久预览复用统一伤害公式，不创建另一套命中面板。</summary>
    public override void OnBeTarget(PieceController attacker, SkillPack skill)
    { previewAttacker = attacker; previewSkill = skill; }

    public override void ShowHighlight(bool option)
    {
        base.ShowHighlight(option);
        if (!option) { previewAttacker = null; previewSkill = null; }
    }

    public DamageManager.Preview ReadPreview() => previewAttacker != null && previewSkill != null
        ? BattleScene.Ins.BM.damageManager.PreviewSkill(previewAttacker, this, previewSkill,
            orderedTargets: previewAttacker.rangeUI?.PreviewTargets) : null;

    /// <summary>破盾立即使来源停机并禁用选择碰撞；破坏序列按现有聚能表现队列延期。</summary>
    public override void Dead()
    {
        if (dismissed || broken) return;
        broken = true;
        Emitter?.OnBarrierBroken(this);
        var battle = BattleScene.Ins?.BM;
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        System.Action play = () => pieceDisplay.ChangeDisplayState(PieceDisplayState.Death, finish: () => Destroy(gameObject));
        if (battle == null || !battle.TryDeferBurstEffect(this, play)) play();
        else pieceDisplay.FreezeBurstFirstFrame(PieceDisplayState.Death);
    }

    /// <summary>来源死亡、离场或复用时直接清理，不触发棋子死亡或掉落回调。</summary>
    public void Dismiss() { dismissed = true; BattleScene.Ins?.BM?.RangeBarriers.Remove(this); Destroy(gameObject); }

    protected override void OnDisable() { base.OnDisable(); BattleScene.Ins?.BM?.RangeBarriers.Remove(this); }
}
