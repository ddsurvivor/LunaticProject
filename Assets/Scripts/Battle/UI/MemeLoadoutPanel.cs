using System;
using UnityEngine;

/// <summary>角色双槽装配草稿；只在确认时调用正式装配接口，关闭或失去控制时丢弃草稿。</summary>
public sealed partial class MemeLoadoutPanel : MonoBehaviour
{
    private static MemeLoadoutPanel active;
    public static bool IsOpen => active != null && active.gameObject.activeInHierarchy;
    private Player player;
    private PLAYERPROFILE profile;
    private PieceData data;
    private PieceController actor;
    private BattleManager battle;
    private MemeCatalogSO catalog;
    private readonly int[] draft = new int[MemeEquipment.SlotCount]; // 草稿不引用存档数组，取消不写回。
    private readonly int[] original = new int[MemeEquipment.SlotCount]; // 打开时的存档快照，防止覆盖其他入口更新。
    private int selectedSlot, inspectedId;
    private bool editableAtOpen, submitting;
    private string message;

    /// <summary>打开战内模因装配；展示使用原角色，确认才支付一次2AP。</summary>
    public static MemeLoadoutPanel OpenBattle(PieceController actor, UnityEngine.UI.Text template)
    {
        if (actor == null || actor.playerData == null || template == null) return null;
        return Open(actor.playerData, actor.pieceData, actor, template);
    }

    /// <summary>角色页面入口；战斗中自动寻找对应棋子，缺少控制权时只读，不绕过战内费用。</summary>
    public static MemeLoadoutPanel OpenCharacter(CharactorPanel page)
    {
        if (page == null || page.player == null || page.nameText == null) return null;
        PieceController piece = null;
        var battle = BattleScene.Ins?.BM;
        if (battle != null && battle.IsBattleRunning)
            piece = battle.PlayerController.pieces.Find(p => p != null && ReferenceEquals(p.playerData, page.player));
        return Open(page.player, page.pieceData, piece, page.nameText);
    }

    private static MemeLoadoutPanel Open(Player player, PieceData data, PieceController actor, UnityEngine.UI.Text template)
    {
        if (IsOpen) active.Close();
        var canvas = template.GetComponentInParent<Canvas>()?.rootCanvas;
        if (canvas == null || GM.Ins?.PLAYERPROFILE == null) return null;
        var root = new GameObject("MemeLoadoutPanel", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        var view = root.AddComponent<MemeLoadoutPanel>();
        view.player = player;
        view.data = data;
        view.actor = actor;
        view.profile = GM.Ins.PLAYERPROFILE;
        view.battle = BattleScene.Ins?.BM;
        if (view.battle != null && !view.battle.IsBattleRunning) view.battle = null;
        view.catalog = view.battle != null ? view.battle.memeService.Catalog : GM.Ins.DM.MemeCatalog;
        for (int i = 0; i < view.draft.Length; i++) view.draft[i] = player.memeSlots != null && player.memeSlots.Length > i ? player.memeSlots[i] : 0;
        Array.Copy(view.draft, view.original, view.draft.Length);
        view.inspectedId = view.draft[0];
        view.editableAtOpen = view.battle == null || view.battle.CanControlPiece(actor);
        active = view;
        view.Build(template);
        view.Refresh();
        return view;
    }

    /// <summary>切换编辑槽位，不提交资源或装配。</summary>
    public void SelectSlot(int slot)
    {
        if (!Current || slot < 0 || slot >= draft.Length) return;
        selectedSlot = slot;
        message = null;
        inspectedId = draft[slot];
        Refresh();
    }

    /// <summary>查看并尝试放入当前草稿槽；锁定、重复或无效词条保留原草稿并展示失败原因。</summary>
    public void SelectMeme(int id)
    {
        if (!Current) return;
        inspectedId = id;
        var candidate = (int[])draft.Clone();
        candidate[selectedSlot] = id;
        if (id == 0) { draft[selectedSlot] = 0; message = null; Refresh(); return; }
        if (!MemeEquipment.Validate(catalog, profile.memeProgress, candidate, out message)) { Refresh(); return; }
        draft[selectedSlot] = id;
        message = null;
        Refresh();
    }

    /// <summary>确认当前草稿；重复点击、配置未变及资源/控制校验失败均不重复提交。</summary>
    public bool TryConfirm()
    {
        if (!Current || submitting) return false;
        string error = ConfirmError();
        if (error != null) { message = error; Refresh(); return false; }
        submitting = true;
        bool applied;
        if (battle != null) applied = battle.memeService.TryEquipInBattle(actor, draft, out error);
        else applied = MemeEquipment.TryEquipOutsideBattle(player, profile.memeProgress, catalog, draft, out error);
        submitting = false;
        if (!applied) { message = error; Refresh(); return false; }
        Close();
        return true;
    }

    private bool Current => active == this && gameObject.activeInHierarchy && ReferenceEquals(GM.Ins?.PLAYERPROFILE, profile);
    private string ConfirmError()
    {
        if (!ReferenceEquals(GM.Ins?.PLAYERPROFILE, profile)) return "存档已切换，请重新打开。";
        if (!MemeEquipment.SameSlots(player.memeSlots, original)) return "配置已由其他入口更新，请重新打开。";
        if (!MemeEquipment.Validate(catalog, profile.memeProgress, draft, out var error)) return error;
        if (MemeEquipment.SameSlots(player.memeSlots, draft)) return "配置没有变化。";
        if (battle != null && (!battle.CanControlPiece(actor) || battle.HasExternalPlayerControl || actor.IsUsingSkill)) return "仅当前可操作角色能确认战内换装。";
        if (battle != null && actor.unitAttrCenter.CurMovePoint < MemeEquipment.BattleEquipCost) return "行动点不足，确认需要2 AP。";
        return null;
    }

    private void Update()
    {
        if (!Current || battle == null && BattleScene.Ins?.BM?.IsBattleRunning == true || battle != null && (!battle.IsBattleRunning || editableAtOpen && !battle.CanControlPiece(actor))) { Close(); return; }
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) { Close(); return; }
        RefreshStatus();
    }

    /// <summary>取消或退出时直接销毁本次界面与草稿；由行动展示流程恢复战斗菜单。</summary>
    public void Close()
    {
        if (active == this) active = null;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
    private void OnDisable() { if (active == this) active = null; }
    private void OnDestroy() { if (active == this) active = null; }

    /// <summary>给既有角色页增加模因入口，重复切换角色不重复创建按钮。</summary>
    public static void EnsureCharacterEntry(CharactorPanel page)
    {
        if (page == null || page.nameText == null || page.transform.Find("MemeLoadoutEntry") != null) return;
        var button = MakeButton("MemeLoadoutEntry", page.transform, page.nameText, "模因装配", () => OpenCharacter(page));
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
        rect.sizeDelta = new Vector2(144, 40);
        rect.anchoredPosition = new Vector2(-96, -28);
    }
}
