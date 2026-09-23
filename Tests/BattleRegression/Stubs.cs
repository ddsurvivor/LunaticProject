using System;
using System.Collections.Generic;
using UnityEngine;

// Presentation and scene dependencies only. Combat rules are linked from Assets by Run.ps1.
namespace UnityEngine
{
    public class SerializeField : Attribute { }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string s) {} }
    public class MinAttribute : Attribute { public MinAttribute(int n) {} }
    public class Component
    {
        public GameObject gameObject;
        private readonly Transform fallback = new Transform();
        public Transform transform => gameObject?.transform ?? fallback;
        public string name => gameObject?.name ?? "test"; public int GetInstanceID()=>GetHashCode(); public T GetComponentInParent<T>() where T:class=>GetComponent<T>();
        public T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
    }
    public class MonoBehaviour : Component { }
    public class GameObject
    {
        public string name = "test";
        public Transform transform = new Transform();
        private readonly Dictionary<Type, object> components = new();
        public T Add<T>(T value) where T : Component { components[typeof(T)] = value; value.gameObject = this; return value; }
        public T GetComponent<T>() where T : class { foreach(var value in components.Values) if(value is T match) return match; return null; }
        public bool activeInHierarchy = true; public void SetActive(bool active) { activeInHierarchy=active; }
    }
    public class Transform { public Vector3 position, localScale; public Quaternion rotation; }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x,float y,float z) { this.x=x; this.y=y; this.z=z; }
        public static Vector3 up => new(0,1,0);
        public float sqrMagnitude=>x*x+y*y+z*z;
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
        public static float Angle(Vector3 a,Vector3 b) { var length=Math.Sqrt(a.sqrMagnitude*b.sqrMagnitude); return length==0?0:(float)(Math.Acos(Math.Clamp((a.x*b.x+a.y*b.y+a.z*b.z)/length,-1,1))*180/Math.PI); }
        public static Vector3 operator +(Vector3 a,Vector3 b) => new(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator *(Vector3 a,float f) => new(a.x*f,a.y*f,a.z*f);
    }
    public struct Quaternion { public static Quaternion identity => default; }
    public static class Mathf
    {
        public static int Max(int a,int b)=>Math.Max(a,b); public static float Max(float a,float b)=>Math.Max(a,b);
        public static int Min(int a,int b)=>Math.Min(a,b); public static float Abs(float a)=>Math.Abs(a);
        public static int Clamp(int v,int a,int b)=>Math.Clamp(v,a,b); public static float Clamp(float v,float a,float b)=>Math.Clamp(v,a,b);
        public static int FloorToInt(float v)=>(int)Math.Floor(v); public static int CeilToInt(float v)=>(int)Math.Ceiling(v);
        public static int RoundToInt(float v)=>(int)Math.Round(v);
    }
    public static class Random { public static float value=0f; public static int NextValue=1; public static int Range(int a,int b)=>Math.Clamp(NextValue,a,b-1); }
    public static class Debug { public static void Log(object o){} public static void LogWarning(object o){} public static void LogError(object o){} }
    public class Collider : Component { }
    public static class Physics { public static Collider[] Results=Array.Empty<Collider>(); public static Collider[] OverlapSphere(Vector3 p,float r)=>Results; }
}
namespace UnityEngine.Events { public class UnityEvent { } }
namespace Sirenix.OdinInspector
{
    public class SerializedMonoBehaviour : MonoBehaviour { }
    public class ReadOnlyAttribute : Attribute { }
    public class LabelTextAttribute : Attribute { public LabelTextAttribute(string s) {} }
}
namespace Sirenix.Serialization { public class OdinSerializeAttribute : Attribute { } }
namespace DG.Tweening { public static class TweenExtensions { public static void DOScale(this Transform t,Vector3 v,float f){} } }
public enum ItemName { A, B }
public enum ItemTag { All, Consumables, Plugins, Materials }
public enum UseType { InBattle, OutOfBattle, WhenEnergyNotFull, WhenHpNotFull, WhenStaminaNotFull, ActiveInBattle }
public enum ItemType { SHIELD, DAMAGE_TEXT, KINETIC_ATTACK, ENERGY_ATTACK, HEAL_EFFECT, CHARGE_EFFECT, SPECIALTY_ACTIVATE }
public enum AudioCueType { Heal }
public class CheckDicePanel { public void ShowResult(int n,int[] dice,bool success) {} }
public enum PieceDisplayState { Dodge }
public enum AttrOp { Get }
public class Player { public int AccessAttribute(int i, AttrOp op)=>0; }
public class SkillPack
{
    public int mpCost; public bool layerSkill, isRecognitionCheck, isDelaySkill;
    public SkillTarget target=SkillTarget.EnemyAll; public RangeType rangeType=RangeType.Circle; public float explodeRadius,rangeValue=10,rangeAgle=90;
    public List<ItemPack> consumeItems=new(); public List<AttackPack> attackPacks=new();
}
public class PieceData
{
    public int maxAmmoCount=3, maxHealth=100, maxMovePoint=3, maxMana=10, critRate, critDamageRate=100;
    public float moveRange=5, evasionRate; public List<SkillSystem.PassiveSkillType> passiveSkillTypes=new();
    public PieceElementType elementType;
    public Dictionary<EnemyGrowthAttribute,float> levelGrowth;
    public Dictionary<DamageType,int> attackDic=new(), _armorDic=new();
    public Dictionary<UnitAttrType,float> attrDic=new();
}
public class PieceController : MonoBehaviour
{
    public UnitAttrCenter unitAttrCenter; public PieceData pieceData=new();
    public bool isDead=>unitAttrCenter.CurHealth<=0;
    public bool isPlayerPiece;
    public PlayerController player;
    public PieceDisplay pieceDisplay=new();
    public int Deaths, Hurts;
    public void Dead(){ Deaths++; } public void Hurt(){ Hurts++; }
}
public class EnemyController : PieceController
{
    public EnemyCanvas enemyCanvas=new(); public int RecordedDamage;
    public void AddDamageRecord(PieceController p,int d){ RecordedDamage+=d; }
    public void UpdateHpBar(float f){}
}
public class EnemyCanvas { public void UpdateBuffs(List<BuffState> b){} }
public class PieceDisplay { public void ChangeDisplayState(PieceDisplayState s,bool b,float f){} }
public class HpBarUI { }
public class DamageText : MonoBehaviour { public void JumpOutNum(int n){} }
public class ObjectPool
{
    public static ObjectPool Ins=new();
    public GameObject GenerateObject(ItemType t,Vector3 p,Quaternion q) { var go=new GameObject(); go.Add(new DamageText()); return go; }
}
public class PlayerController { public bool isBursting; public PieceController burstTarget; public int totalDamage; public List<PieceController> pieces=new(); }
public class CaverSlot { public int evadeChance, damageReduction; }
public class DamageInfo { public int damageValue; public DamageInfo(int d,string type,bool crit){ damageValue=d; } }
public static class GameConst { public const float burstDamageRate=1.2f, burstAddDamageRate=0.2f; }
public class GameConstSO { public float FlankDamageRate=0.5f; public int GetActionPointCost(ActionType action)=>1; }
public class DataManager { public GameConstSO gameConstSO=new(); }
public class AudioManager { public void PlayAudio(AudioCueType t){} }
public class Profile
{
    public Dictionary<ItemName,int> Inventory=new();
    public int GetItemNum(ItemName name)=>Inventory.GetValueOrDefault(name);
    public void CostItem(ItemName name,int n){ Inventory[name]-=n; }
}
public class GM { public static GM Ins=new(); public Profile PLAYERPROFILE=new(); public DataManager DM=new(); public AudioManager AM=new(); }
public class UIManager { public void OnPieceStateChance(PieceController p){} public void ShowUndoMoveButton(bool b){} }
public class TipTextManager
{
    public void ShowMiss(Transform t){} public void ShowTip(Transform t,string s){}
    public void ShowHeal(Transform t,int n){} public void ShowBuffAdded(Transform t,string s,int n){}
}
public class BattleManager
{
    public BuffManager buffManager=new(); public TipTextManager tipTextManager=new(); public DiceCheckManager diceCheckManager=new();
    public SkillSystem.CharacterSkillManager characterSkillManager=new(); public PlayerController PlayerController=new();
    public CaverSlot CheckCoverObstruction(PieceController a,PieceController b)=>null;
}
public class BattleScene { public static BattleScene Ins=new(); public BattleManager BM=new(); public UIManager UM=new(); }
namespace SkillSystem
{
    public class PassiveSkillData { public string skillName="passive"; }
    public enum PassiveSkillType { Successor, Inspiration, SurvivalWisdom, ReflectiveECM, OffensiveAnalysis, TransferInterception, EdgeSurvival, MicromechanicalDamageControl, MaintenanceSupport }
    public class PassiveSkillConfigSO { public PassiveSkillData GetSkillData(PassiveSkillType type)=>new(); }
}
// A spy passive observes the real manager dispatch; it does not replace the manager.
public static class TestEvents
{
    public static int Kills, Hits, Turns, Casts, Recognition;
    public static void Reset(){ Kills=Hits=Turns=Casts=Recognition=0; }
}
public class EventSpy : SkillSystem.BasePassiveSkill
{
    public override void OnKillEnemy(GameObject a,GameObject t){ TestEvents.Kills++; }
    public override void OnTakeDamage(GameObject t,GameObject a){ TestEvents.Hits++; }
    public override void OnTurnEnd(){ TestEvents.Turns++; }
    public override void OnCastActiveSkill(GameObject a){ TestEvents.Casts++; }
    public override void OnPatternRecognitionPassed(GameObject a){ TestEvents.Recognition++; }
}