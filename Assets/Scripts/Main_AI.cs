using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class Main_AI : MonoBehaviour
{
    // TODO AIパラメータ群の実装

    Main_Unit.Teams team;
    Main_Map map;

    public void Initialize(Main_Unit.Teams team, Main_Map map)
    {
        this.team = team;
        this.map = map;
    }

    void Run()
    {
        StartCoroutine(RunCoroutine());
    }

    IEnumerator RunCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        // 行動可能なユニットを取得
        var units = map.GetMovableUnits().OrderByDescending(x => x.Life).ToList();
        Main_Unit previousUnit = null;
        while (units.Count > 0)
        {
            if (previousUnit == units[0])
            {
                // TODO 行動不可状態のユニットが複数居るとここでループしてしまう
                // 行動不可状態のユニットが居るとループしてしまうため、break
                break;
            }
            previousUnit = units[0];
            yield return MoveAndAttackCoroutine(units, units[0]);
        }
        yield return new WaitForSeconds(0.5f);
        // 全ての操作が完了したらターン終了
        map.NextTurn();
    }

    IEnumerator MoveAndAttackCoroutine(List<Main_Unit> movableUnits, Main_Unit unit)
    {
        movableUnits.Remove(unit);

        // 目標地点のマスを取得
        var targetCell = GetTargetCell(unit);
        unit.OnClick();

        var attackableCells = map.GetAttackableCells();
        if (0 < attackableCells.Length)
        {
            // 攻撃可能なマスがあった場合
            if (null != attackableCells.FirstOrDefault(x => x == targetCell))
            {
                // 攻撃可能なマスが目標地点だった場合
                targetCell.Unit.OnClick();
                yield return WaitBattleCoroutine();
                yield break;
            }
            else
            {
                // TODO 1発で敵が倒せる場合とかの処理 or 攻撃できるけど攻撃しない場合の処理
                yield return WaitBattleCoroutine();
                yield break;
            }
        }

        var movableCells = map.GetMovableCells();
        if (0 < movableCells.Length)
        {
            // 移動可能なマスがあった場合
            // TODO 移動先の決定処理
            var moveCell = GetMoveCell(unit, movableCells, targetCell);
            // 移動完了を待つ
            yield return WaitMoveCoroutine(unit, moveCell);
        }
        else
        {
            // 行動不能な場合は後回し
            movableUnits.Add(unit);
        }
    }

    /// <summary>
    /// 移動可能範囲の中で、移動先とするマスを返します
    /// </summary>
    /// <returns>The move cell.</returns>
    /// <param name="unit">Unit.</param>
    /// <param name="movableCells">Movable cells.</param>
    /// <param name="targetCell">Target cell.</param>
    Main_Cell GetMoveCell(Main_Unit unit, Main_Cell[] movableCells, Main_Cell targetCell)
    {
        // TODO 実装
        return null;
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
    /// ユニットの目標地点とするマスを取得します
    /// </summary>
    /// <returns>The target cell.</returns>
    /// <param name="unit">Unit.</param>
    Main_Cell GetTargetCell(Main_Unit unit)
    {


        // TODO 実装
        return null;
    }
}
