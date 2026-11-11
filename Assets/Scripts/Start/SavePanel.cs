using System.Collections.Generic;

public class SavePanel : UIPanel
{
    public List<SaveCell> saveCells = new List<SaveCell>();

    /// <summary>只读入口（如主菜单读取游戏）下隐藏所有保存按钮。</summary>
    public bool LoadOnly { get; private set; }

    /// <summary>以只读载入模式打开面板，不显示保存按钮。</summary>
    public void OpenLoadOnly()
    {
        LoadOnly = true;
        Open();
    }

    /// <summary>以完整存档模式打开面板，允许保存与读取。</summary>
    public void OpenSaveLoad()
    {
        LoadOnly = false;
        Open();
    }

    public override void UpdateDisplay()
    {
        base.UpdateDisplay();
        for (int i = 0; i < saveCells.Count; i++)
        {
            if (GM.Ins.DM.playerprofiles.ContainsKey(i))
            {
                var playerprofile = GM.Ins.DM.playerprofiles[i];
                saveCells[i].SetData(playerprofile);
            }
            else
            {
                saveCells[i].SetData(null);
            }
            saveCells[i].SetLoadOnly(LoadOnly);
        }
    }
}
