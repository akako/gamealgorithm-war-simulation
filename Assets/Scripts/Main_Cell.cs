using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class Main_Cell : MonoBehaviour
{
    [SerializeField]
    Main_Map map;
    [SerializeField]
    Image highlight;
    [SerializeField]
    int cost;
    [SerializeField]
    Color movableColor;
    [SerializeField]
    Color attackableColor;
    /// <summary>
    /// 地形によるダメージ軽減率
    /// </summary>
    [SerializeField]
    float reduceRate;

    int x;
    int y;

    public float ReduceRate
    {
        get { return reduceRate; }
    }

    /// <summary>
    /// 移動可能なマスかどうか
    /// </summary>
    /// <value><c>true</c> if this instance is movable; otherwise, <c>false</c>.</value>
    public bool IsMovable
    {
        set
        {
            highlight.color = movableColor;
            highlight.gameObject.SetActive(value);
        }
        get { return highlight.gameObject.activeSelf && highlight.color == movableColor; }
    }

    public bool IsAttackable
    {
        set
        { 
            highlight.color = attackableColor;
            highlight.gameObject.SetActive(value);
        }
        get { return highlight.gameObject.activeSelf && highlight.color == attackableColor; }
    }

    public int Cost
    {
        get { return cost; }
    }

    public int X
    {
        get { return x; }
    }

    public int Y
    {
        get { return y; }
    }

    public Main_Unit Unit
    {
        get { return map.GetUnit(x, y); }
    }

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    /// <summary>
    /// 座標をセットします
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    public void SetCoordinate(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public void OnClick()
    {
        if (IsMovable)
        {
            map.MoveTo(map.FocusingUnit, this);
        }
    }
}
