using System.Collections;
using System.Linq;
using UnityEngine;

public partial class BattleManager
{
    private enum StartupPhase { Ready, Sweeping, Introduction, Active, Cancelled }
    private StartupPhase startupPhase;
    private bool startupInitialized, startupInitializing, startupRequested;
    private int startupVersion;
    // 一个句柄覆盖开始前等待与整场巡视，不再另建计时 Tween 推测巡视结束。
    private Coroutine sweepCoroutine;

    private void ResetBattleStartup()
    {
        CancelBattleStartup();
        startupPhase = StartupPhase.Ready;
        PlayerController.isInTurn = false;
        AIController.isInTurn = false;
    }

    public void StartBattle()
    {
        if (!isActiveAndEnabled || startupPhase == StartupPhase.Cancelled) return;
        // GM 可以比 BattleScene.Start 更早请求开战，等 Init 完成后再执行。
        if (!startupInitialized)
        {
            startupRequested = true;
            return;
        }
        if (!inBattle || startupPhase != StartupPhase.Ready) return;
        startupRequested = false;
        startupPhase = StartupPhase.Sweeping; // 在调用可能触发事件的初始化前占用。
        int version = ++startupVersion;
        Debug.Log("战斗开始");
        characterSkillManager.Init(PlayerController.pieces.Concat(AIController.pieces)
            .Concat(summonPieces).Distinct().ToList());
        if (!IsCurrentSweep(version)) return;
        SetSkipVisible(true);
        if (!IsCurrentSweep(version)) return;
        sweepCoroutine = StartCoroutine(BattleStartupRoutine(version));
    }

    // 保留场景/UnityEvent 的公开入口；重复调用不会额外启动协程。
    public void SweepActiveEnemies() => StartBattle();

    private bool IsCurrentSweep(int version) =>
        isActiveAndEnabled && inBattle && startupPhase == StartupPhase.Sweeping && startupVersion == version;

    private IEnumerator BattleStartupRoutine(int version)
    {
        // 首次 yield 也保证协程句柄赋值后才有可能同步完成（例如没有敌人）。
        yield return new WaitForSeconds(0.5f);
        if (!IsCurrentSweep(version)) yield break;
        Debug.Log("【战前扫视】开始...");
        // 快照避免增援/事件修改原列表，并对每个敌人重新检查有效性。
        var enemies = AIController.pieces?.OfType<EnemyController>().ToArray();
        if (cameraController != null && enemies != null)
        {
            foreach (var enemy in enemies)
            {
                if (!IsCurrentSweep(version)) yield break;
                if (enemy == null || !enemy.isActived || enemy.isDead || !enemy.gameObject.activeInHierarchy) continue;
                cameraController.SetFollow(enemy.transform);
                // SetFollow/启用 UI 可能触发外部事件，返回后仍须确认未被跳过。
                if (!IsCurrentSweep(version)) yield break;
                yield return new WaitForSeconds(Mathf.Max(0f, moveWaitTime));
                if (!IsCurrentSweep(version)) yield break;
                yield return new WaitForSeconds(Mathf.Max(0f, gazeWaitTime));
            }
        }
        if (!IsCurrentSweep(version)) yield break;
        Debug.Log("【战前扫视】结束。");
        sweepCoroutine = null; // 自然结束不对正在执行的协程调用 StopCoroutine。
        EnterBattleSequence(version);
    }

    private void EnterBattleSequence(int version)
    {
        if (!IsCurrentSweep(version)) return;
        // 先切状态和失效版本，再调用 UI/教程，防止重复点击或事件重入。
        startupPhase = StartupPhase.Introduction;
        StopBattleSweep();
        SetSkipVisible(false);
        RestorePlayerCamera();
        if (!isActiveAndEnabled || !inBattle || startupPhase != StartupPhase.Introduction) return;
        if (tutorialManager == null || !tutorialManager.CheckAndShowTutorial())
            CompleteBattleIntroduction();
    }

    /// <summary>教程关闭与无教程路径共用，只在本场战斗启动阶段执行一次。</summary>
    public void CompleteBattleIntroduction()
    {
        if (!isActiveAndEnabled || !inBattle || startupPhase != StartupPhase.Introduction) return;
        startupPhase = StartupPhase.Active;
        battleDialogueManager?.TriggerBattleStart();
        // 对话触发器也可能要求退出/重新初始化场景。
        if (isActiveAndEnabled && inBattle && startupPhase == StartupPhase.Active) PlayerStart();
    }

    public void OnClickSkip()
    {
        // 含开始前的等待；不依赖句柄是否已赋值，连点只推进一次。
        if (!IsCurrentSweep(startupVersion)) return;
        Debug.Log("【战前扫视】玩家选择跳过。");
        EnterBattleSequence(startupVersion);
    }

    private void StopBattleSweep()
    {
        startupVersion++;
        var routine = sweepCoroutine;
        sweepCoroutine = null;
        if (routine != null) StopCoroutine(routine);
    }

    private void CancelBattleStartup()
    {
        startupPhase = StartupPhase.Cancelled;
        StopBattleSweep();
        SetSkipVisible(false);
    }

    private void SetSkipVisible(bool visible)
    {
        // 仅操作属于自己的场景 UI，旧场景不能关闭新场景的按钮。
        var scene = BattleScene.Ins;
        if (scene != null && scene.BM == this) scene.UM?.skipButton?.gameObject.SetActive(visible);
    }

    private void RestorePlayerCamera()
    {
        if (cameraController == null) return;
        var firstPlayer = PlayerController.pieces?.FirstOrDefault(p =>
            p != null && !p.isDead && p.gameObject.activeInHierarchy);
        cameraController.SetFollow(firstPlayer != null ? firstPlayer.transform : null);
    }
}
