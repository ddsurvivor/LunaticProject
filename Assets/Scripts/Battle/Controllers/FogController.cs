using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class FogController : SerializedMonoBehaviour
{
    public Dictionary<GameObject, List<EnemyController>> enemyPiecesDict = new();
    
}
