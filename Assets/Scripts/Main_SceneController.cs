using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Main_SceneController : MonoBehaviour
{
    [SerializeField]
    Main_Map map;
    [SerializeField]
    Main_Unit unitNekoPrefab;
    [SerializeField]
    Main_Unit unitMatatabiPrefab;
    [SerializeField]
    Main_Unit unitKobanPrefab;
    [SerializeField]
    Main_Unit unitNeko2Prefab;
    [SerializeField]
    Button endTurnButton;
    [SerializeField]
    Main_AI enemyAI;

    IEnumerator Start()
    {
        endTurnButton.onClick.AddListener(() =>
            {
                map.NextTurn();
            });

        unitNekoPrefab.gameObject.SetActive(false);
        unitMatatabiPrefab.gameObject.SetActive(false);
        unitKobanPrefab.gameObject.SetActive(false);
        unitNeko2Prefab.gameObject.SetActive(false);

        // マップ生成
        map.Generate(9, 9);
        // GridLayoutによる自動レイアウトで、マスの座標が確定するのを待つ
        yield return null;
        // ユニット配置
        map.PutUnit(7, 7, unitNekoPrefab, Main_Unit.Teams.Player);
        map.PutUnit(6, 7, unitMatatabiPrefab, Main_Unit.Teams.Player);
        map.PutUnit(7, 6, unitKobanPrefab, Main_Unit.Teams.Player);
        map.PutUnit(6, 6, unitNeko2Prefab, Main_Unit.Teams.Player);
        // 敵ユニット配置
        map.PutUnit(1, 1, unitNekoPrefab, Main_Unit.Teams.Enemy);
        map.PutUnit(2, 1, unitMatatabiPrefab, Main_Unit.Teams.Enemy);
        map.PutUnit(1, 2, unitKobanPrefab, Main_Unit.Teams.Enemy);
        map.PutUnit(2, 2, unitNeko2Prefab, Main_Unit.Teams.Enemy);

        // AI設定
        map.SetAI(Main_Unit.Teams.Enemy, enemyAI);

        // ターン開始
        map.StartTurn(Main_Unit.Teams.Player);
    }


}
