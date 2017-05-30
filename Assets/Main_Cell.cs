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

    int x;
    int y;

    /// <summary>
    /// 移動可能なマスかどうか
    /// </summary>
    /// <value><c>true</c> if this instance is movable; otherwise, <c>false</c>.</value>
    public bool IsMovable
    {
        set { highlight.gameObject.SetActive(value); }
        get { return highlight.gameObject.activeSelf; }
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

    void OnClick()
    {
        if (IsMovable)
        {
            map.ActiveUnit.MoveTo(this);
        }
    }
}
