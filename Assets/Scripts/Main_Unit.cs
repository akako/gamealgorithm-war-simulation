using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Button))]
public class Main_Unit : MonoBehaviour
{
    public enum Teams
    {
        Player,
        Enemy
    }

    public int x;
    public int y;
    public int moveAmount = 4;
    public int attackRangeMin = 2;
    public int attackRangeMax = 3;
    public Teams team;

    [SerializeField]
    Main_Map map;

    bool isFocusing = false;

    public bool IsFocusing { get { return isFocusing; } set { isFocusing = value; } }

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        isFocusing = !isFocusing;
        if (isFocusing)
        {
            map.HighlightMovableCells(x, y, moveAmount);
        }
        else
        {
            map.ClearHighlight();
        }
    }
}
