using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine
{
    public class GameObject
    {
        public bool activeInHierarchy=true,activeSelf=true;
        public void SetActive(bool value) { activeSelf=activeInHierarchy=value; }
    }
    public class Transform { public Vector3 position; }
    public class MonoBehaviour { }
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
    public static class Mathf { public const float PI=(float)Math.PI; public static float Sin(float f)=>(float)Math.Sin(f);public static float Lerp(float a,float b,float t)=>a+(b-a)*t; }
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
public class UnitAttrCenter { public int TauntValue; }
public class PieceController
{
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
    private class AttackPlan { public PieceController Target; }
    private readonly Dictionary<EnemyController,AttackPlan> pendingAttacks=new();
    private static bool IsValidTarget(EnemyController a,PieceController b)=>b!=null&&!b.isDead&&b.gameObject.activeInHierarchy&&BuffManager.CanTarget(a,b);
    public void CommitForTest(EnemyController e,PieceController t) { pendingAttacks[e]=new AttackPlan {Target=t}; }
}
public partial class EnemyController:PieceController
{
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
public class BattleManager { public bool CanInspectEnemyTargets=true;public PlayerController PlayerController=new();public AIController AIController=new();public MoveManager moveManager=new(); }
public class BattleScene { public static BattleScene Ins;public BattleManager BM=new();public ClickManager CM=new(); }
public partial class ClickManager:MonoBehaviour
{
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
