using System.Collections.Generic;

public class SavePanel : UIPanel
{
    public List<SaveCell> saveCells = new List<SaveCell>();

    public override void UpdateDisplay()
    {
        base.UpdateDisplay();
        for (int i = 0; i < saveCells.Count; i++)
        {
            if (i < GM.Ins.DM.playerprofiles.Count)
            {
                var playerprofile = GM.Ins.DM.playerprofiles[i];
                saveCells[i].SetData(playerprofile.lastSaveTime.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else
            {
                saveCells[i].SetData("空存档");
            }
        }
    }
}