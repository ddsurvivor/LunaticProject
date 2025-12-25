using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InfoBox : MonoBehaviour
{
    public Text infoText;

    public void ShowInfo(PieceController piece)
    {
        infoText.text = $"{piece.name}\nHP: {piece.unitAttrCenter.CurHealth}/{piece.unitAttrCenter.MaxHealth}" +
                        $"\n MP: {piece.unitAttrCenter.CurMovePoint}/{piece.unitAttrCenter.MaxMovePoint}";
        foreach (var damageType in Enum.GetValues(typeof(DamageType)))
        {
            infoText.text += $"\n {damageType} Armor: {piece.unitAttrCenter.attr.GetArmor((DamageType)damageType)}";
            infoText.text += $"\n {damageType} Atk: {piece.unitAttrCenter.attr.GetAtk((DamageType)damageType)}";
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
