using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public PieceController casterPc;
    private SkillPack _curSkillPack;
    private List<PieceController> resultTargets = new();

    public List<PieceController> GetTargets(PieceController caster, Transform target
        , SkillPack skill)
    {
        CheckRange(caster, target, skill);
        return resultTargets;
    }

    /// <summary>
    /// 根据范围类型检测目标
    /// </summary>
    /// <param name="caster"></param>
    /// <param name="target"></param>
    /// <param name="skill"></param>
    private void CheckRange(PieceController caster, Transform target, SkillPack skill)
    {
        casterPc = caster;
        _curSkillPack = skill;
        Collider[] hitColliders = null;
        List<PieceController> newTargets = new();
        if (skill.rangeType == RangeType.Circle) // 单体敌人锁定
        {
            // 检测球体范围内的所有敌人
            hitColliders = Physics.OverlapSphere(target.transform.position, 1f);
        }
        else if (skill.rangeType == RangeType.Grenade) // 爆炸范围锁定
        {
            float explodeRadius = skill.explodeRadius;
            // 检测球体范围内的所有敌人
            hitColliders =
                Physics.OverlapSphere(target.transform.position, explodeRadius);
        }
        else if (skill.rangeType == RangeType.Fan)
        {
            // 扇形范围
            // 进行扇形有限距离的穿透射线检测
            // 根据扇形角度，等间距的发射多根射线进行检测，结果需要去掉重复
            float halfAngle = _curSkillPack.rangeAgle / 2f;
            int rayCount = Mathf.CeilToInt(_curSkillPack.rangeAgle / 5f); // 每5度发射一根射线
            //HashSet<PieceController> hitPieces = new HashSet<PieceController>();
            for (int i = 0; i <= rayCount; i++)
            {
                float angle = -halfAngle + i * (_curSkillPack.rangeAgle / rayCount);
                Vector3 targetDir = target.position - caster.transform.position;
                Vector3 direction = Quaternion.Euler(0, angle, 0) * targetDir;
                Ray ray = new Ray(caster.transform.position, direction);
                if (Physics.Raycast(ray, out RaycastHit hitInfo, _curSkillPack.rangeValue))
                {
                    PieceController piece = hitInfo.collider.GetComponent<PieceController>();
                    if (piece != null && !newTargets.Contains(piece))
                    {
                        newTargets.Add(piece);
                    }
                }
            }
        }


        if (hitColliders != null)
        {
            foreach (var hitCollider in hitColliders)
            {
                PieceController pc = hitCollider.GetComponent<PieceController>();
                if (pc != null)
                {
                    newTargets.Add(pc);
                }
            }
        }

        CheckTarget(newTargets);
    }

    /// <summary>
    /// 根据目标类别筛选
    /// </summary>
    /// <param name="newTargets"></param>
    private void CheckTarget(List<PieceController> targets)
    {
        resultTargets = new();
        foreach (var piece in targets)
        {
            if (piece == null) continue;
            if (_curSkillPack.target == SkillTarget.All)
            {
                piece.rangeUI?.ShowHighlight(true);
                resultTargets.Add(piece);
            }
            else if (_curSkillPack.target == SkillTarget.EnemyAll)
            {
                if (piece.isPlayerPiece != casterPc.isPlayerPiece)
                {
                    piece.rangeUI?.ShowHighlight(true);
                    resultTargets.Add(piece);
                }
            }
            else if (_curSkillPack.target == SkillTarget.Enemy)
            {
                if (piece.isPlayerPiece != casterPc.isPlayerPiece)
                {
                    piece.rangeUI?.ShowHighlight(true);
                    resultTargets.Add(piece);
                    return;
                }
            }
            else if (_curSkillPack.target == SkillTarget.Ally)
            {
                if (piece.isPlayerPiece != casterPc.isPlayerPiece)
                {
                    piece.rangeUI?.ShowHighlight(true);
                    resultTargets.Add(piece);
                    return;
                }
            }
        }
    }
}