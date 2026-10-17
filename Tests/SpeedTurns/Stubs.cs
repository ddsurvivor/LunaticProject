using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine
{
    public class Coroutine { public IEnumerator Routine; public Stack<IEnumerator> Stack = new(); }
    public class MonoBehaviour
    {
        public Coroutine Running;
        public Coroutine StartCoroutine(IEnumerator routine) => Running = new Coroutine { Routine = routine };
        public void Destroy(object obj) { }
        public void StopCoroutine(Coroutine coroutine) { if (Running == coroutine) Running = null; }
    }
    public class WaitForSeconds { public WaitForSeconds(float value) { } }
    public class GameObject { public bool activeInHierarchy = true; }
    public class Transform { }
    public class Material { public Material(Material m) { } public void DisableKeyword(string s) { } }
    public class SpriteRenderer { public Material sharedMaterial; }
    public static class Mathf { public static int Max(int a, int b) => Math.Max(a, b); }
}
public class Event { public void Invoke() { } }
public enum BuffType { Stun }
public class PieceDisplay { public SpriteRenderer pieceSpriteRenderer; }
public class UnitAttrCenter { public PieceController Owner; public int CurMovePoint = 2; public int GetBuffStacks(BuffType b)=>0; public void FullMovePoint()=>CurMovePoint=2; public void SetMovePoint(int p)=>CurMovePoint=p; }
public class PieceController
{
    public GameObject gameObject = new(); public Transform transform = new();
    public UnitAttrCenter unitAttrCenter; public bool isDead, cantControl, isIdle;
    public PieceDisplay pieceDisplay; public void CancelSelect() { } public void OnSelect() { }
    public float TurnSpeed = 10; public int Starts, Ends;
    public PieceController() { unitAttrCenter = new() { Owner = this }; }
    public void TurnStart() { Starts++; isIdle = false; unitAttrCenter.CurMovePoint = 2; }
    public void TurnEnd() { Ends++; isIdle = true; }
    public T GetComponent<T>() where T : class => null;
}
public class EnemyController : PieceController { public bool isActived = true; }
public class SelfExploreSummonPiece { public void ExecuteAI() { } }
public class PlayerController
{
    public List<PieceController> pieces = new(); public bool isInTurn; public Event OnTurnEnd = new();
    public BurstGauge Gauge = new(); public bool isBursting;
    public bool ableBurst => Gauge.Ready && !isBursting;
    public int AffordableBurstActors => Gauge.AffordableActors;
    public void RefreshBurstUI() { }
    public bool SpendBurst(int n)=>Gauge.Spend(n);
    public void EnterBurstMode()=>isBursting=true;
    public void EndBurstMode() { isBursting=false; Gauge.BeginCooldown(); }
    public void CompleteNormalTurnForBurst()=>Gauge.CompleteNormalTurn();
    public void BeginPieceTurn(PieceController p) { isInTurn = true; }
    public void EndPieceTurn() { isInTurn = false; }
}
public class AIController : PlayerController
{
    public void RefreshSpeedTurnWorld() { }
    public void BeginEnemyPieceTurn(EnemyController p) { isInTurn = true; }
}
public class BuffManager
{
    public List<PieceController> Starts = new(); public Action<PieceController> OnStart;
    public void ProcessBuffs(UnitAttrCenter u) { Starts.Add(u.Owner); OnStart?.Invoke(u.Owner); }
}
public class MoveManager { public bool IsMoving; }
public class OrderManager { public void ClearForPiece(PieceController p) { } }
public class CameraController { public void SetFollow(Transform t) { } }
public class Dialogue { public void TriggerTurnNumStart(int round) { } }
public class Text { public string text; }
public class LogPanel { public void PlayerLog(string s) { } }
public class UIManager { public LogPanel logPanel=new(); public Text turnNumberText = new(); public void EnableSpeedTimeline(BattleManager b) { } }
public class ClickManager { public void SelectActivePiece(PieceController p) { } public void ClearTurnSelection() { } }
public class BattleScene
{
    public static BattleScene Ins; public UIManager UM = new(); public ClickManager CM = new();
}
public partial class BattleManager : MonoBehaviour
{
    enum StartupPhase { Active }
    StartupPhase startupPhase = StartupPhase.Active;
    bool inBattle = true; int _turnNumber;
    public int winTurnCondition = 999;
    public bool HasCombatPresentation;
    public PlayerController PlayerController = new(); public AIController AIController = new();
    public List<PieceController> summonPieces = new();
    public MoveManager moveManager = new(); public BuffManager buffManager = new();
    public OrderManager orderManager = new(); public CameraController cameraController = new();
    public Dialogue battleDialogueManager = new(); PieceController _delaySkillCaster;
    public void ShowBurstGray(bool option) { }
    void HandleDelaySkill() { }
    void PlayerCheckWin() { }
    void PlayerWin() { inBattle = false; StopSpeedTurns(); }
    public static BattleManager Fixture() { BattleScene.Ins = new(); return new(); }
    public void StartFixture() => StartSpeedTurns();
    public void StopFixture() => StopSpeedTurns();
    public void Step()
    {
        var current=Running; if(current==null)return;
        if(current.Stack.Count==0)current.Stack.Push(current.Routine);
        while(current.Stack.Count>0 && Running==current)
        {
            var top=current.Stack.Peek();
            if(!top.MoveNext()){current.Stack.Pop();continue;}
            if(top.Current is IEnumerator nested){current.Stack.Push(nested);continue;}
            return;
        }
    }
    public void Until(Func<bool> done)
    {
        for (int i = 0; i < 100 && !done(); i++) Step();
        if (!done()) throw new Exception("Scheduler stalled");
    }
}
