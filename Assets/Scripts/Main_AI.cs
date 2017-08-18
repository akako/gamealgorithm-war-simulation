using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class Main_AI : MonoBehaviour
{
    // 攻撃対象選択時のランダム性
    [SerializeField, Range(0, 100)]
    int randomizeAttackTarget = 50;

    // 検知距離（これ以上近づいたら襲ってくる）
    [SerializeField, Range(0, 100)]
    int detectionDistance = 4;

    // 地形効果の重視度合い
    [SerializeField, Range(0, 100)]
    int cellReduceRateImportance = 0;

    Main_Map map;

    public void Initialize(Main_Map map)
    {
        this.map = map;
    }

    public void Run()
    {
        StartCoroutine(RunCoroutine());
    }

    IEnumerator RunCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        // 行動可能なユニットを取得
        var ownUnits = map.GetOwnUnits().OrderByDescending(x => x.Life);
        var enemyUnits = map.GetEnemyUnits();
        if (ownUnits.Min(ou => enemyUnits.Min(eu => Mathf.Abs(ou.x - eu.x) + Mathf.Abs(ou.y - eu.y))) <= detectionDistance)
        {
            // 敵ユニットが指定距離内に入ったら行動開始
            foreach (var unit in ownUnits)
            {
                yield return MoveAndAttackCoroutine(unit);
            }
        }
        yield return new WaitForSeconds(0.5f);
        // 全ての操作が完了したらターン終了
        map.NextTurn();
    }

    IEnumerator MoveAndAttackCoroutine(Main_Unit unit)
    {
        // 移動可能な全てのマスまでの移動コストを取得
        var moveCosts = map.GetMoveCostToAllCells(unit.Cell);

        var attackBaseCells = GetAttackBaseCells(unit).ToList();
        if (attackBaseCells.Count() == 0)
        {
            // 攻撃拠点となるマスが無いなら行動終了
            yield return new WaitForSeconds(0.5f);
            unit.IsMoved = true;
            yield return new WaitForSeconds(0.5f);
            yield break;
        }

        // 攻撃拠点となるマスのうち、一番近い場所を目標地点とする
        var targetCell = attackBaseCells.OrderBy(cell => moveCosts.First(cost =>
                {
                    return cost.coordinate.x == unit.Cell.X && cost.coordinate.y == unit.Cell.Y;
                }).value).First();

        // ユニットを選択
        unit.OnClick();

        var route = map.CalcurateRouteCoordinatesAndMoveAmount(unit.Cell, targetCell);
        var movableCells = map.GetMovableCells().ToList();
        if (movableCells.Count == 0)
        {
            yield return AttackIfPossibleCoroutine(unit);
            if (!unit.IsMoved)
            {
                // 行動不能な場合は行動終了
                unit.OnClick();
                yield return new WaitForSeconds(0.5f);
                unit.IsMoved = true;
                yield return new WaitForSeconds(0.5f);
            }
        }
        else
        {
            // 自分の居るマスも移動先の選択肢に含める
            movableCells.Add(unit.Cell);
            var moveTargetCell = movableCells.OrderByDescending(c =>
                {
                    var matchedRoute = route.FirstOrDefault(r => r.coordinate.x == c.X && r.coordinate.y == c.Y);
                    return (null != matchedRoute ? matchedRoute.value : 0) +
                    c.ReduceRate * cellReduceRateImportance;
                }).First();

            if (moveTargetCell != unit.Cell)
            {
                yield return new WaitForSeconds(0.5f);
                moveTargetCell.OnClick();
                // 移動完了を待つ
                yield return WaitMoveCoroutine(unit, moveTargetCell);
            }

            yield return AttackIfPossibleCoroutine(unit);
        }
    }

    IEnumerator AttackIfPossibleCoroutine(Main_Unit unit)
    {
        var attackableCells = map.GetAttackableCells();
        if (0 < attackableCells.Length)
        {
            if (Random.Range(0, 100) < randomizeAttackTarget)
            {
                // ランダムで対象を選ぶ
                attackableCells[Random.Range(0, attackableCells.Length)].Unit.OnClick();
            }
            else
            {
                // 攻撃可能なマスのうち、できるだけ倒せる/大ダメージを与えられる/反撃が痛くないマスに攻撃
                attackableCells.OrderByDescending(x =>
                    {
                        var damageValue = x.Unit.CalcurateDamageValue(unit);
                        return damageValue * (x.Unit.Life <= damageValue ? 10 : 1) - unit.CalcurateDamageValue(x.Unit);
                    }).First().Unit.OnClick();
            }
            yield return WaitBattleCoroutine();
        }
    }

    /// <summary>
    /// 移動の終了を待つコルーチン
    /// </summary>
    /// <returns>The move coroutine.</returns>
    /// <param name="unit">Unit.</param>
    /// <param name="cell">Cell.</param>
    IEnumerator WaitMoveCoroutine(Main_Unit unit, Main_Cell cell)
    {
        while (true)
        {
            if (cell.Unit == unit)
            {
                break;
            }
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>
    /// Battleシーンの終了を待つコルーチン
    /// </summary>
    /// <returns>The battle coroutine.</returns>
    IEnumerator WaitBattleCoroutine()
    {
        while (true)
        {
            // Battleシーンが終わるまで待つ
            var scene = SceneManager.GetSceneByName("Battle");
            if (!scene.IsValid())
            {
                break;
            }
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>
    /// 敵ユニットに攻撃可能となるマスを取得します
    /// </summary>
    /// <returns>The attack base cells.</returns>
    /// <param name="unit">Unit.</param>
    Main_Cell[] GetAttackBaseCells(Main_Unit unit)
    {
        var cells = new List<Main_Cell>();
        foreach (var enemyUnit in map.GetEnemyUnits())
        {
            cells.AddRange(map.GetCellsByDistance(enemyUnit.Cell, unit.attackRangeMin, unit.attackRangeMax).Where(c => c.Cost < 999));
        }
        return cells.Distinct().ToArray();
    }
}
