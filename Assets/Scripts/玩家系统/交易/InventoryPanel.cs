using System.Collections.Generic;
using UnityEngine;


    public class InventoryPanel: UIPanel
    {
        public List<MarketItemCell> cells = new();
        public override void UpdateDisplay()
        {
            base.UpdateDisplay();
            for (int i = 0; i < GM.Ins.PLAYERPROFILE.itemPacks.Count; i++)
            {
                if (cells.Count > i)
                {
                    cells[i].SetItem(GM.Ins.PLAYERPROFILE.itemPacks[i]);
                }
            }
        }
    }
