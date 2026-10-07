using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine
{
    public class GameObject
    {
        public bool activeInHierarchy=true;
        public Action<bool> ActiveChanged;
        public void SetActive(bool b) { activeInHierarchy=b;ActiveChanged?.Invoke(b); }
    }
    public class Transform { }
    public class Coroutine
    {
        public IEnumerator Routine;
        public bool Stopped,Done;
        public bool Step(bool ignoreStop=false)
        {
            if(Done || (Stopped && !ignoreStop)) return false;
            if(!Routine.MoveNext()) { Done=true;return false; }
            return true;
        }
    }
    public class MonoBehaviour
    {
        public bool isActiveAndEnabled=true;
        public readonly List<Coroutine> Routines=new();
        public Coroutine StartCoroutine(IEnumerator routine)
        {
            var c=new Coroutine {Routine=routine};Routines.Add(c);c.Step();return c;
        }
        public void StopCoroutine(Coroutine c) { c.Stopped=true; }
    }
    public class WaitForSeconds { public float Seconds;public WaitForSeconds(float f) {Seconds=f;} }
    public static class Mathf { public static float Max(float a,float b)=>Math.Max(a,b); }
    public static class Debug { public static void Log(object s) { } }
}
public class PieceController { public bool isDead;public GameObject gameObject=new();public Transform transform=new(); }
public class EnemyController:PieceController { public bool isActived=true; }
public class PlayerController { public bool isInTurn;public List<PieceController> pieces=new(); }
public class CameraController
{
    public readonly List<Transform> FollowHistory=new();
    public Action OnFollow;
    public void SetFollow(Transform t) { FollowHistory.Add(t);OnFollow?.Invoke(); }
}
public class CharacterSkillManager
{
    public int InitCalls;public Action OnInit;
    public void Init(List<PieceController> pieces) { InitCalls++;OnInit?.Invoke(); }
}
public class TutorialManager { public bool HasTutorial;public int Calls;public bool CheckAndShowTutorial() { Calls++;return HasTutorial; } }
public class BattleDialogueManager { public int Calls;public Action OnStart;public void TriggerBattleStart() { Calls++;OnStart?.Invoke(); } }
public class Button { public GameObject gameObject=new(); }
public class UIManager { public Button skipButton=new(); }
public class BattleScene { public static BattleScene Ins;public BattleManager BM;public UIManager UM=new(); }
public partial class BattleManager:MonoBehaviour
{
    public PlayerController PlayerController=new(),AIController=new();
    public List<PieceController> summonPieces=new();
    public CharacterSkillManager characterSkillManager=new();
    public CameraController cameraController=new();
    public TutorialManager tutorialManager=new();
    public BattleDialogueManager battleDialogueManager=new();
    private bool inBattle=true;
    private float moveWaitTime=1f,gazeWaitTime=0.5f;
    public int PlayerStarts;
    public void PlayerStart() { PlayerStarts++;PlayerController.isInTurn=true; }
    // Test fixture supplies the non-startup scene initialization; startup methods are production code.
    public void InitializeFixture()
    {
        ResetBattleStartup();startupInitialized=true;inBattle=true;
        if(startupRequested)StartBattle();
    }
    public void DisableFixture()
    {
        isActiveAndEnabled=false;CancelBattleStartup();startupInitialized=false;startupRequested=false;
    }
    public void EndFixture() { CancelBattleStartup();inBattle=false; }
}
