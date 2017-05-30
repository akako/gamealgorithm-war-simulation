using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Button))]
public class Main_Unit : MonoBehaviour
{
    public int x;
    public int y;
    public int moveAmount = 4;

    [SerializeField]
    Main_Map map;

    bool isFocused = false;

    public bool IsFocused { get { return isFocused; } }

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    /// <summary>
    /// ユニットを対象のマスに移動させます
    /// </summary>
    /// <param name="cell">Cell.</param>
    public void MoveTo(Main_Cell cell)
    {
        GetComponent<Button>().enabled = false;
        map.ResetMovableCells();
        var routeCells = map.CalculateRouteCells(x, y, moveAmount, cell);
        var sequence = DOTween.Sequence();
        for (var i = 1; i < routeCells.Length; i++)
        {
            var routeCell = routeCells[i];
            sequence.Append(transform.DOMove(routeCell.transform.position, 0.1f).SetEase(Ease.Linear));
        }
        sequence.OnComplete(() =>
            {
                x = routeCells[routeCells.Length - 1].X;
                y = routeCells[routeCells.Length - 1].Y;
                isFocused = false;
                GetComponent<Button>().enabled = true;
            });
    }

    void OnClick()
    {
        isFocused = !isFocused;
        if (isFocused)
        {
            map.HighlightMovableCells(x, y, moveAmount);
        }
        else
        {
            map.ResetMovableCells();
        }
    }
}
