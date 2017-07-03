﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main_SceneController : MonoBehaviour
{
    [SerializeField]
    Main_Map map;
    [SerializeField]
    Main_Unit unitPrefab;

    IEnumerator Start()
    {
        unitPrefab.gameObject.SetActive(false);

        // マップ生成
        map.Generate(9, 9);
        // GridLayoutによる自動レイアウトで、マスの座標が確定するのを待つ
        yield return null;
        // ユニット配置
        map.PutUnit(4, 4, unitPrefab, Main_Unit.Teams.Player);
        // 敵ユニット配置
        map.PutUnit(1, 2, unitPrefab, Main_Unit.Teams.Enemy);
    }
}