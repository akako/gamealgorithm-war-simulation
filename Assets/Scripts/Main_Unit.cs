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
        Player1,
        Player2
    }

    public enum UnitTypes
    {
        Neko,
        Matatabi,
        Koban,
    }

    public UnitTypes unitType;
    public int x;
    public int y;
    public int moveAmount;
    public int attackRangeMin;
    public int attackRangeMax;
    public Teams team;

    [SerializeField]
    int lifeMax;
    [SerializeField]
    int attackPowerBase;
    [SerializeField]
    Main_Map map;

    int life;
    bool isFocusing = false;
    bool isMoved = false;

    public int LifeMax { get { return lifeMax; } }

    public int Life { get { return life; } }

    public int AttackPower { get { return Mathf.RoundToInt(attackPowerBase * (Mathf.Ceil((float)life / (float)lifeMax * 10f) / 10f)); } }

    public bool IsFocusing { get { return isFocusing; } set { isFocusing = value; } }

    public bool IsMoved
    {
        get { return isMoved; } 
        set
        { 
            isMoved = value;
            GetComponent<Button>().interactable = !isMoved;
        }
    }

    public Main_Cell Cell { get { return map.GetCell(x, y); } }

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
        life = lifeMax;
    }

    public void OnClick()
    {
        // 攻撃対象の選択中であれば攻撃アクション実行
        if (map.GetCell(x, y).IsAttackable)
        {
            map.AttackTo(map.FocusingUnit, this);
            return;
        }

        // 自分以外のユニットが選択状態であれば、そのユニットの選択を解除
        if (null != map.FocusingUnit && this != map.FocusingUnit)
        {
            map.FocusingUnit.isFocusing = false;
            map.ClearHighlight();
        }

        isFocusing = !isFocusing;
        if (isFocusing)
        {
            map.HighlightMovableCells(x, y, moveAmount);
            map.HighlightAttackableCells(x, y, attackRangeMin, attackRangeMax);
        }
        else
        {
            map.ClearHighlight();
        }
    }

    /// <summary>
    /// ダメージを与えます
    /// </summary>
    /// <param name="attacker">Attacker.</param>
    public void Damage(Main_Unit attacker)
    {
        life = Mathf.Max(0, life - CalcurateDamageValue(attacker));
    }

    /// <summary>
    /// ダメージ値を計算します
    /// </summary>
    /// <param name="attacker">Attacker.</param>
    public int CalcurateDamageValue(Main_Unit attacker)
    {
        // 三つ巴的な相性ダメージ Kuro < Shiro < Tora < Kuro ...
        var unitTypeBonus = new float[]{ 1f, 2f, 0.5f }[(((int)attacker.unitType - (int)unitType) + 3) % 3];
        var damage = Mathf.RoundToInt(attacker.AttackPower * unitTypeBonus * (1f - attacker.Cell.ReduceRate));
        return damage;
    }

    public void DestroyWithAnimate()
    {
        GetComponent<Button>().enabled = false;
        transform.DOScale(Vector3.zero, 0.5f).OnComplete(() =>
            {
                Destroy(gameObject);
            });
    }
}
