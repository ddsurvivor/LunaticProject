using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ClickManager {public void ClearEnemyTargetLineInteraction(){}}
public partial class BattleManager
{
    private bool inBattle=true;
    public List<PieceController> summonPieces=new();
    public bool CanControlPiece(PieceController piece)=>inBattle && SpeedTurnsActive && !HasCombatPresentation && !moveManager.IsMoving &&
        piece==ActivePiece && piece!=null && !piece.isDead && !piece.cantControl && !piece.isIdle && piece.unitAttrCenter.CurMovePoint>0;
    public List<PieceController> SpeedParticipants()=>PlayerController.pieces.Concat(AIController.pieces).Concat(summonPieces)
        .Where(p=>p!=null&&!p.isDead&&p.gameObject.activeInHierarchy&&(!(p is EnemyController e)||e.isActived)).Distinct().ToList();
    public void RestoreDelaySkill(PieceController piece,SkillPack skill,Vector3 position){DelayedSkills++;}
    public int DelayedSkills;
    private void CancelBattleStartup(){SpeedTurnsActive=false;}
    public bool CompleteForTest(BattleOutcome outcome)=>ObserveBattleOutcome(outcome);
}
public partial class PieceController
{
    public bool isIdle,cantControl,isUsingOrder;
    private bool _isAttacking,_isUsingSkill;
    public List<ActionType> availableActions=new(){ActionType.移动,ActionType.近战攻击,ActionType.远程攻击,ActionType.技能,ActionType.待机};
    public List<PieceController> CommandTargets;
    public ActionType CommandAction;
    public bool SkillAvailable(SkillPack skill)=>unitAttrCenter.HasMana(unitAttrCenter.GetSkillManaCost(skill))&&unitAttrCenter.HasItem(skill.consumeItems);
    public void StartDrag(){}
    public void StartMove(){}
    public void StopMove(){}
    protected void NotifySkillUsed(SkillPack skill){}
    protected void BeginCombatAction(SkillPack skill,List<PieceController> targets,Vector3 position,ActionType action,PieceDisplayState state,
        bool isFlank=false,CheckResult check=CheckResult.None,bool waitForRecognition=false)
    {LastSkill=skill;CommandTargets=targets;CommandAction=action;IsPerformingAction=true;}
}
namespace Newtonsoft.Json
{
    public enum Formatting {Indented}
    public static class JsonConvert
    {
        public static string SerializeObject(object value,Formatting formatting=Formatting.Indented)=>System.Text.Json.JsonSerializer.Serialize(value,new System.Text.Json.JsonSerializerOptions{IncludeFields=true,WriteIndented=true});
        public static T DeserializeObject<T>(string value)=>System.Text.Json.JsonSerializer.Deserialize<T>(value,new System.Text.Json.JsonSerializerOptions{IncludeFields=true});
    }
}
