using Sirenix.Serialization;
using UnityEngine;

public class SelfExploreSummonPiece : MonoBehaviour
{
    [OdinSerialize] public SkillPack skillPack;
    [Header("爆炸设置")]
    [SerializeField] private float explosionRadius = 5f; // 爆炸半径，单位为世界坐标米。
    [SerializeField] private LayerMask effectLayer;
    [Header("自爆兵属性")]
    private float moveRange = 20.0f; // 单次回合最大移动距离。
    private float explodeRange = 5f; // 抵达目标后的自爆触发距离。
    private bool explosionQueued; // 已登记的自爆不再接受移动或重复触发。
    private bool isActionFinished = true; // 旧阵营回合的持续时间结算每次行动最多执行一次。
    private int actionVersion; // 停用或新行动使旧移动回调失效。
    private int actionLife = -1; // 当前状态所属的棋子 Init 版本。
    public PieceController pieceController;
    public Animator animator;

    private void OnEnable()
    {
        explosionQueued = false;
        isActionFinished = true;
        actionLife = -1;
    }

    private void OnDisable()
    {
        actionVersion++;
        isActionFinished = true;
        BattleScene.Ins?.BM?.CancelWorldEffects(this);
        BattleScene.Ins?.BM?.ReleaseExplosiveBody(transform);
    }

    /// <summary>由回合调度器驱动一次自爆兵行动；在途行动及已排队自爆不重复执行。</summary>
    public void ExecuteAI()
    {
        var manager = BattleScene.Ins?.BM;
        if (!isActiveAndEnabled || manager == null || !manager.IsBattleRunning ||
            pieceController == null || pieceController.isDead) return;
        if (actionLife != pieceController.BattleLifeVersion)
        {
            actionLife = pieceController.BattleLifeVersion;
            explosionQueued = false;
            isActionFinished = true;
        }
        if (!isActionFinished || explosionQueued) return;
        isActionFinished = false;
        int version = ++actionVersion; // 捕获本次移动，拒绝重复抵达与复用后的回调。
        int life = actionLife;
        var action = new BattleActionContext(pieceController, skillPack);
        var unit = pieceController.unitAttrCenter;
        var buffs = manager.buffManager;
        if (!manager.SpeedTurnsActive) buffs.ProcessBuffs(unit);
        if (unit.pc.isDead || unit.GetBuffStacks(BuffType.Stun) != 0 ||
            unit.GetBuffStacks(BuffType.Bind) != 0)
        {
            FinishAction();
            return;
        }

        GameObject targetEnemy = FindNearestEnemy();
        if (targetEnemy == null || !buffs.OnAction(unit) || unit.GetBuffStacks(BuffType.Bind) != 0)
        {
            FinishAction();
            return;
        }
        int targetLife = targetEnemy.GetComponent<PieceController>().BattleLifeVersion; // 不追击移动期间已回收复用的目标。

        Vector3 targetPosition = CalculateMovementTarget(targetEnemy.transform.position);
        if (!manager.moveManager.PreviewAIMove(gameObject, targetPosition, EffectiveMoveRange))
        {
            FinishAction();
            return;
        }
        if (animator != null) animator.SetBool("isMoving", true);
        float distance = manager.moveManager.ExecuteMove(gameObject, () =>
        {
            // 移动不由世界效果队列托管，须单独阻止切场景、死亡及对象复用后的回调。
            if (this == null || !isActiveAndEnabled || version != actionVersion || isActionFinished ||
                pieceController == null || pieceController.isDead || pieceController.BattleLifeVersion != life ||
                manager == null || !manager.IsBattleRunning || BattleScene.Ins?.BM != manager) return;
            if (animator != null) animator.SetBool("isMoving", false);
            var target = targetEnemy != null ? targetEnemy.GetComponent<PieceController>() : null;
            if (target == null || target.isDead || !target.gameObject.activeInHierarchy ||
                target.BattleLifeVersion != targetLife || target.isPlayerPiece == pieceController.isPlayerPiece ||
                !BuffManager.CanTarget(pieceController, target) ||
                Vector3.Distance(transform.position, target.transform.position) > explodeRange)
            {
                FinishAction();
                return;
            }
            using (action.Enter()) TriggerExplosion();
        });
        if (distance <= 0)
        {
            if (animator != null) animator.SetBool("isMoving", false);
            FinishAction();
        }
    }

    private GameObject FindNearestEnemy()
    {
        GameObject nearestEnemy = null;
        float minDistance = float.MaxValue;
        foreach (var piece in BattleScene.Ins.BM.AIController.pieces)
        {
            if (piece == null || piece.isDead || !piece.gameObject.activeInHierarchy ||
                piece.isPlayerPiece == pieceController.isPlayerPiece ||
                !BuffManager.CanTarget(pieceController, piece)) continue;
            float distance = Vector3.Distance(transform.position, piece.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestEnemy = piece.gameObject;
            }
        }
        return nearestEnemy;
    }

    private Vector3 CalculateMovementTarget(Vector3 enemyPos)
    {
        Vector3 currentPos = transform.position;
        Vector3 direction = (enemyPos - currentPos).normalized;
        float maxMovableDistance = Vector3.Distance(currentPos, enemyPos) - 1f; // 保留一米间距，避免重叠。
        if (maxMovableDistance <= 0) return currentPos;
        return currentPos + direction * Mathf.Min(EffectiveMoveRange, maxMovableDistance);
    }

    private void TriggerExplosion()
    {
        if (explosionQueued) return;
        explosionQueued = true;
        var action = new BattleActionContext(pieceController, skillPack);
        if (BattleScene.Ins.BM.TryDeferBurstEffect(this, CompleteExplosion, action))
        {
            FinishAction();
            return;
        }
        using (action.Enter()) CompleteExplosion();
    }

    private void CompleteExplosion()
    {
        if (animator != null) animator.SetBool("isDeath", true);
        var targetPieces = EnemyExplosiveTargets.CollectTargets(pieceController, transform.position, explosionRadius, effectLayer);
        // 自爆已触发后施法者可能死亡；仅本次副本允许死亡结算，不改变技能资产。
        BattleScene.Ins.BM.PieceSkill(pieceController, targetPieces, skillPack.WithDeadCaster(), transform.position);
        if (skillPack.skillSound != null && Camera.main != null)
            AudioSource.PlayClipAtPoint(skillPack.skillSound, Camera.main.transform.position);
        ObjectPool.Ins.GenerateObject(skillPack.skillVFXType,
            transform.position + Vector3.up * 3f, Quaternion.identity);
        BattleScene.Ins.BM.ScheduleWorldEffect(this, 1f, () => gameObject.SetActive(false));
        FinishAction();
    }

    private void FinishAction()
    {
        if (isActionFinished) return;
        isActionFinished = true;
        if (pieceController != null && !BattleScene.Ins.BM.SpeedTurnsActive)
            BattleScene.Ins.BM.buffManager.ProcessBuffDurations(pieceController.unitAttrCenter);
    }

    private float EffectiveMoveRange => moveRange * Mathf.Max(0f,
        pieceController.unitAttrCenter.buffAttrDic[BuffAttrType.MoveRangePercent]) / 100f;
}
