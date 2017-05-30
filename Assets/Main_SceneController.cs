using System.Collections;
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

        map.Generate(9, 9);
        yield return null;
        map.PutUnit(4, 4, unitPrefab);
    }
}
