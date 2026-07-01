using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
public class CharactorSkillPanel : UIPanel
{
    public CharactorPanel charactorPanel;
    
    public List<SkillGroup> passiveSkillGroupList = new List<SkillGroup>();
    public List<SkillGroup> activeSkillGroupList = new List<SkillGroup>();

    public override void UpdateDisplay()
    {
        base.UpdateDisplay();
        RefreshUI();
    }

    public void RefreshUI()
    {
        PieceData pieceData = charactorPanel.pieceData;
        Player player = charactorPanel.player;
        
        foreach (var group in activeSkillGroupList)
        {
            group.gameObject.SetActive(false);
        }

        for (var index = 0; index < pieceData.skillPacks.Count; index++)
        {
            var VARIABLE = pieceData.skillPacks[index];
            if (activeSkillGroupList.Count > index)
            {
                activeSkillGroupList[index].gameObject.SetActive(true);
                activeSkillGroupList[index].skillTitle.text = VARIABLE.skillName;
                activeSkillGroupList[index].skillDesc.text = VARIABLE.GetSkillDesc();
            }
        }
        
        foreach (var group in passiveSkillGroupList)
        {
            group.gameObject.SetActive(false);
        }

        for (var index = 0; index < pieceData.passiveSkillTypes.Count; index++)
        {
            //var skillPack = pieceData.passiveSkillPacks[index];
            var skillData =
                GM.Ins.DM.passiveSkillConfigSO.GetSkillData(pieceData.passiveSkillTypes[index]);
            if (passiveSkillGroupList.Count > index)
            {
                passiveSkillGroupList[index].gameObject.SetActive(true);
                passiveSkillGroupList[index].skillTitle.text = skillData.skillName;
                passiveSkillGroupList[index].skillDesc.text = skillData.description;
            }
        }
    }
}