using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.AI
{
    public class NavMeshAgent:Component {public float stoppingDistance=.1f,radius=.3f;public int areaMask=-1;}
    public struct NavMeshHit {public float distance;}
    public struct NavMeshTriangulation {public Vector3[] vertices;}
    public static class NavMesh
    {
        public static NavMeshTriangulation CalculateTriangulation()=>new(){vertices=new[]{new Vector3(-20,0,-20),new Vector3(-20,0,20),new Vector3(20,0,-20),new Vector3(20,0,20)}};
        public static bool FindClosestEdge(Vector3 point,out NavMeshHit hit,int mask) {hit=new(){distance=20-Math.Max(Math.Abs(point.x),Math.Abs(point.z))};return true;}
    }
}

public class FogController {public Dictionary<GameObject,List<EnemyController>> enemyPiecesDict=new();}
public class WaveController {public void RefreshEnemies(int round){}}
public class MapController {public List<LadderArea> ladderAreas=new();public LadderArea GetLadder(Vector3 position)=>null;}
public class LadderArea:Component {public Transform upPos,downPos;public Vector3 GetNearPos(Vector3 p)=>p;public void TriggerAction(PieceController piece){}}
public class RealtimeActionManager { }
public class RetreatWin:Component {public Transform targetPoint;public void CheckTargetReached(){}}
public class CameraController {public void SetFollow(Transform target){}}
public class MoveManager
{
    public bool IsMoving,NavigationAllowed=true;public int Moves;
    public bool TryGetAIMoveDestination(GameObject pawn,Vector3 goal,float budget,out Vector3 position,out float length,bool partial)
    {
        var offset=goal-pawn.transform.position;length=offset.magnitude;
        position=partial?pawn.transform.position+Vector3.ClampMagnitude(offset,budget):goal;
        return NavigationAllowed && (partial || length<=budget) && length>.01f;
    }
    public float ExecuteMove(GameObject pawn,Action done,Vector3 destination)
    {
        float length=Vector3.Distance(pawn.transform.position,destination);pawn.transform.position=destination;Moves++;done();return length;
    }
}

public partial class PieceController
{
    public CaverSlot CurCaverSlot;
    public List<string> AIActions=new();public SkillPack LastSkill;public PieceController LastTarget;
    private bool rangedAttack;
    public float GetRange(bool melee)=>(melee?pieceData.meleeAtk:pieceData.rangedAtk)?.rangeValue??0;
    public void StartNormalAttack(bool ranged){rangedAttack=ranged;}
    public void StartSkillAttack(SkillPack skill){LastSkill=skill;}
    public void ReloadAmmo(){unitAttrCenter.FullAmmo();AIActions.Add("Reload");}
    public void CheckFace(Vector3 direction){}
    public void PlayAudio(ActionType action){}
    public void CastAttackOnTarget(PieceController target){LastTarget=target;LastSkill=rangedAttack?pieceData.rangedAtk:pieceData.meleeAtk;AIActions.Add(rangedAttack?"Ranged":"Melee");if(rangedAttack)unitAttrCenter.CostAmmo();IsPerformingAction=true;}
    public void CastSkillOnTarget(PieceController target,SkillPack skill){LastTarget=target;LastSkill=skill;AIActions.Add("Skill");IsPerformingAction=true;}
    public void CastSupportSkill(PieceController target,SkillPack skill){LastTarget=target;LastSkill=skill;AIActions.Add("Heal");IsPerformingAction=true;}
    public void CastOnExplosive(IEnemyExplosiveTarget explosive,SkillPack skill,ActionType action){LastTarget=explosive.Piece;LastSkill=skill;AIActions.Add("Explosive");IsPerformingAction=true;}
}

public partial class AIController
{
    public bool DecideForTest(EnemyController enemy){enemy.IsPerformingAction=false;return EnemyActionSelect(enemy,CheckEnemyTarget(enemy));}
}
