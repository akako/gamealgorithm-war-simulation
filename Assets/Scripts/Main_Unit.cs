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

    public enum UnitTypes
    {
        Kuro,
        Shiro,
        Tora,
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

    public int LifeMax { get { return lifeMax; } }

    public int Life { get { return life; } }

    public int AttackPower { get { return Mathf.RoundToInt(attackPowerBase * (Mathf.Ceil((float)life / (float)lifeMax * 10f) / 10f)); } }

    public bool IsFocusing { get { return isFocusing; } set { isFocusing = value; } }

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
        life = lifeMax;
    }

    void OnClick()
    {
        if (map.GetCell(x, y).IsAttackable)
        {
            map.AttackTo(map.FocusingUnit, this);
            return;
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

    public void Damage(Main_Unit attacker)
    {
        // 三つ巴的な相性ダメージ Kuro < Shiro < Tora < Kuro ...
        var unitTypeBonus = new float[]{ 1f, 2f, 0.5f }[(((int)attacker.unitType - (int)unitType) + 3) % 3];
        var damage = Mathf.RoundToInt(attacker.AttackPower * unitTypeBonus);
        life = Mathf.Max(0, life - damage);
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
