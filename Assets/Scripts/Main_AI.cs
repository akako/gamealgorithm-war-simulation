using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class Main_AI : MonoBehaviour
{
    // TODO AIパラメータ群の実装

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
        var units = map.GetOwnUnits().Where(x => !x.IsMoved).OrderByDescending(x => x.Life);
        foreach (var unit in units)
        {
            yield return MoveAndAttackCoroutine(unit);
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
        var movableCells = map.GetMovableCells();
        if (movableCells.Length == 0)
        {
            yield return AttackIfPossibleCoroutine();
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
            var movableRoute = route.Where(r => movableCells.Any(c => c.X == r.coordinate.x && c.Y == r.coordinate.y));
            if (movableRoute.Count() > 0)
            {
                var targetRoute = movableRoute.OrderByDescending(r => r.value).First();
                var moveTargetCell = map.GetCell(targetRoute.coordinate.x, targetRoute.coordinate.y);
                yield return new WaitForSeconds(0.5f);
                moveTargetCell.OnClick();
                // 移動完了を待つ
                yield return WaitMoveCoroutine(unit, moveTargetCell);
            }
            yield return AttackIfPossibleCoroutine();
        }
    }

    IEnumerator AttackIfPossibleCoroutine()
    {
        var attackableCells = map.GetAttackableCells();
        if (0 < attackableCells.Length)
        {
            // 攻撃可能なマスのうち、HPが一番低いユニットが居るマスに攻撃
            attackableCells.OrderBy(x => x.Unit.Life).First().Unit.OnClick();
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
            cells.AddRange(map.GetCellsByDistance(enemyUnit.Cell, unit.attackRangeMin, unit.attackRangeMax));
        }
        return cells.Distinct().ToArray();
    }
}
