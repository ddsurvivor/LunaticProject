using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine
{
    public class HeaderAttribute : Attribute { public HeaderAttribute(string value) { } }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string value) { } }
    public class RangeAttribute : Attribute { public RangeAttribute(float min,float max) { } }
    public class MinAttribute : Attribute { public MinAttribute(int min) { } }
    public class GameObject
    {
        public bool activeInHierarchy=true,activeSelf=true;
        public void SetActive(bool value) { activeSelf=activeInHierarchy=value; }
    }
    public class Transform { public Vector3 position; }
    public class MonoBehaviour
    {
        public Transform transform=new();
        protected static RangeUI Instantiate(RangeUI template,Transform parent)=>new();
    }
    public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a) { this.r=r;this.g=g;this.b=b;this.a=a; } }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public static Vector3 up => new(0,1,0);
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator *(Vector3 a,float f)=>new(a.x*f,a.y*f,a.z*f);
        public static Vector3 operator *(float f,Vector3 a)=>a*f;
        public static float Distance(Vector3 a,Vector3 b)=>(float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y)+(a.z-b.z)*(a.z-b.z));
    }
    public static class Mathf { public const float PI=(float)Math.PI;public static float Clamp01(float value)=>Math.Clamp(value,0f,1f); public static float Sin(float f)=>(float)Math.Sin(f);public static float Lerp(float a,float b,float t)=>a+(b-a)*t; }
    public static class Time { public static float unscaledTime; }
    public class LineRenderer
    {
        public GameObject gameObject=new();
        public bool enabled,useWorldSpace;public int positionCount;
        public float widthMultiplier=2;
        public Color startColor=new(1,0,0,0.8f),endColor=new(1,0.5f,0,0.6f);
        public Vector3[] Positions;
        public void SetPositions(Vector3[] p) { Positions=(Vector3[])p.Clone(); }
    }
}
namespace Sirenix.OdinInspector { public class LabelTextAttribute : Attribute { public LabelTextAttribute(string value) { } } }
public enum EnemyAIType { Shoot=2,SkillUser=3,Combine=5,Special=6,NearestAdaptive=10 }
public enum EnemyHealthPriority {CurrentHealth,MaximumHealth}
public enum EnemyFragilePriority {MaximumHealth,Defense}
public enum DamageType {Ranged}
public class AttrCenter {public int GetArmor(DamageType type)=>0;}
public static class EnemyBehaviorRules
{
    public static EnemyTargetStrategy TargetStrategy(EnemyController enemy,EnemyStrategy strategy)=>strategy.Target;
    public static PieceController ChooseInRangeWeakTarget(EnemyController enemy,List<PieceController> candidates,PieceController nearest,PieceController previewPiece,Vector3? previewPosition)=>nearest;
}
public class UnitAttrCenter { public AttrCenter attr=new();public int TauntValue; public int CurHealth=100,MaxHealth=100; }
public class PieceController
{
    public PieceData pieceData=new();
    public bool isDead,isPlayerPiece,Blocked,Outline;
    public UnitAttrCenter unitAttrCenter=new();
    public GameObject gameObject=new();public Transform transform=new();public PlayerController player;
    public void ShowOutline(bool b) { Outline=b; }
    protected virtual void OnDisable() { }
}
public class PlayerController { public List<PieceController> pieces=new(); }
public static class BuffManager { public static bool CanTarget(PieceController a,PieceController b)=>!b.Blocked; }
public partial class AIController:PlayerController
{
    private bool TryCreateExplosivePlan(EnemyController enemy,out AttackPlan plan) { plan=null;return false; }
    private class AttackPlan { public PieceController Target;public EnemyStrategy Strategy; }
    private readonly Dictionary<EnemyController,AttackPlan> pendingAttacks=new();
    private static bool IsValidTarget(EnemyController a,PieceController b)=>b!=null&&!b.isDead&&b.gameObject.activeInHierarchy&&BuffManager.CanTarget(a,b);
    public void CommitForTest(EnemyController e,PieceController t) { pendingAttacks[e]=new AttackPlan {Target=t,Strategy=e.ResolveStrategy()}; }
}
public partial class EnemyController:PieceController
{
    public EnemyAIType enemyAIType=EnemyAIType.Combine;
    public bool isActived=true;public LineRenderer tagetLine=new();
    private PieceController _curTargetPc;
    public Dictionary<PieceController,int> damageDic=new();
    public void DisableForTest() { OnDisable(); }
}
public class MoveManager
{
    public PieceController MovingPiece;
    public bool ValidPreview;public GameObject PreviewPawn;public Vector3 Destination;
    public bool TryGetPreviewPosition(GameObject pawn,out Vector3 pos) { pos=Destination;return ValidPreview&&pawn==PreviewPawn; }
}
public class SkillPack { public float rangeValue; }
public class PieceData { public SkillPack rangedAtk=new() {rangeValue=5};public SkillPack meleeAtk=new() {rangeValue=2}; }
public class RangeUI
{
    public GameObject gameObject=new();public Vector3 Position;public float Radius;
    public void Bind(PieceController owner) { }
    public void CloseRange() { }
    public void ShowInspectionRange(Vector3 position,float radius) { Position=position;Radius=radius; }
}
public class SkillManager { public RangeUI EnsureRangeUI(PieceController owner)=>new(); }
public class BattleManager { public bool CanInspectEnemyTargets=true;public PlayerController PlayerController=new();public AIController AIController=new();public MoveManager moveManager=new();public SkillManager skillManager=new(); }
public class BattleScene { public static BattleScene Ins;public BattleManager BM=new();public ClickManager CM=new(); }
public partial class ClickManager:MonoBehaviour
{
    private RangeUI _rangeUI=new();
    public RangeUI InspectionRange=>enemyAttackRangeUI;
    private bool _isDragging;
    private PieceController _selectedPiece,lastHoveredPiece;
    public bool dragMove;
    private void CancelMovementPreview() { _isDragging=false; }
    public void Hover(PieceController p) { SetHoveredPiece(p);UpdateTargetLinePresentation(); }
    public void Tick() { UpdateTargetLinePresentation(); }
    public void Drag(PieceController p,bool direct=false) { _isDragging=true;_selectedPiece=p;dragMove=direct; }
    public void EndDrag() { _isDragging=false; }
    public void DisableForTest() { OnDisable(); }
}
