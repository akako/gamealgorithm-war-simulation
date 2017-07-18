using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class Main_Map : MonoBehaviour
{
    [SerializeField]
    Main_Cell cellFieldPrefab;
    [SerializeField]
    Main_Cell cellForestPrefab;
    [SerializeField]
    Main_Cell cellRockPrefab;
    [SerializeField]
    Transform unitContainer;

    List<Main_Cell> cells = new List<Main_Cell>();
    Main_Unit.Teams currentTeam;

    /// <summary>
    /// 選択中のユニットを返します
    /// </summary>
    /// <value>The active unit.</value>
    public Main_Unit FocusingUnit
    {
        get { return unitContainer.GetComponentsInChildren<Main_Unit>().FirstOrDefault(x => x.IsFocusing); }
    }

    void Start()
    {
        foreach (var prefab in new Main_Cell[] {cellFieldPrefab, cellForestPrefab, cellRockPrefab})
        {
            prefab.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// ターンを開始します
    /// </summary>
    /// <param name="team">Team.</param>
    public void StartTurn(Main_Unit.Teams team)
    {
        currentTeam = team;
        foreach (var unit in unitContainer.GetComponentsInChildren<Main_Unit>())
        {
            unit.GetComponent<Button>().interactable = team == unit.team;
        }
    }

    /// <summary>
    /// 次のターンに移ります
    /// </summary>
    public void NextTurn()
    {
        var nextTeam = currentTeam == Main_Unit.Teams.Player ? Main_Unit.Teams.Enemy : Main_Unit.Teams.Player;
        StartTurn(nextTeam);
    }

    /// <summary>
    /// マップを生成します
    /// </summary>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    public void Generate(int width, int height)
    {
        foreach (var cell in cells)
        {
            Destroy(cell.gameObject);
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                Main_Cell cell;
                var rand = UnityEngine.Random.Range(0, 10);
                if (rand == 0)
                {
                    cell = Instantiate(cellRockPrefab);
                }
                else if (rand <= 2)
                {
                    cell = Instantiate(cellForestPrefab);
                }
                else
                {
                    cell = Instantiate(cellFieldPrefab);
                }
                cell.gameObject.SetActive(true);
                cell.transform.SetParent(transform);
                cell.SetCoordinate(x, y);
                cells.Add(cell);
            }
        }
    }

    /// <summary>
    /// 任意のマスを取得します
    /// </summary>
    /// <returns>The cell.</returns>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    public Main_Cell GetCell(int x, int y)
    {
        return cells.First(c => c.X == x && c.Y == y);
    }

    /// <summary>
    /// 移動可能なマスをハイライトします
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <param name="moveAmount">Move amount.</param>
    public void HighlightMovableCells(int x, int y, int moveAmount)
    {
        var startCell = cells.First(c => c.X == x && c.Y == y);
        foreach (var info in GetRemainingMoveAmountInfos(startCell, moveAmount))
        {
            var cell = cells.First(c => c.X == info.coordinate.x && c.Y == info.coordinate.y);
            if (null == cell.Unit)
            {
                cells.First(c => c.X == info.coordinate.x && c.Y == info.coordinate.y).IsMovable = true;
            }
        }
    }

    /// <summary>
    /// 攻撃可能なマスをハイライトします
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <param name="moveAmount">Move amount.</param>
    public bool HighlightAttackableCells(int x, int y, int attackRangeMin, int attackRangeMax)
    {
        var startCell = cells.First(c => c.X == x && c.Y == y);
        var targetInfos = GetRemainingAccountRangeInfos(startCell, attackRangeMin, attackRangeMax).Where(i =>
            {
                var cell = cells.First(c => i.coordinate.x == c.X && i.coordinate.y == c.Y);
                return null != cell.Unit && cell.Unit.team != FocusingUnit.team;
            });
        foreach (var info in targetInfos)
        {
            var cell = cells.First(c => c.X == info.coordinate.x && c.Y == info.coordinate.y);
            cell.IsAttackable = true;
            cell.Unit.GetComponent<Button>().interactable = true;
        }
        return targetInfos.Count() > 0;
    }

    /// <summary>
    /// マスのハイライトを消します
    /// </summary>
    public void ClearHighlight()
    {
        foreach (var cell in cells)
        {
            cell.IsMovable = false;
        }
    }

    /// <summary>
    /// 移動経路となるマスを返します
    /// </summary>
    /// <returns>The route cells.</returns>
    /// <param name="startCell">Start cell.</param>
    /// <param name="moveAmount">Move amount.</param>
    /// <param name="endCell">End cell.</param>
    public Main_Cell[] CalculateRouteCells(int x, int y, int moveAmount, Main_Cell endCell)
    {
        var startCell = cells.First(c => c.X == x && c.Y == y);
        var infos = GetRemainingMoveAmountInfos(startCell, moveAmount);
        if (!infos.Any(info => info.coordinate.x == endCell.X && info.coordinate.y == endCell.Y))
        {
            throw new ArgumentException(string.Format("endCell(x:{0}, y:{1}) is not movable.", endCell.X, endCell.Y));
        }

        var routeCells = new List<Main_Cell>();
        routeCells.Add(endCell);
        while (true)
        {
            var currentCellInfo = infos.First(info => info.coordinate.x == routeCells[routeCells.Count - 1].X && info.coordinate.y == routeCells[routeCells.Count - 1].Y);
            var currentCell = cells.First(cell => cell.X == currentCellInfo.coordinate.x && cell.Y == currentCellInfo.coordinate.y);
            var previousMoveAmount = currentCellInfo.amount + currentCell.Cost;
            var previousCellInfo = infos.FirstOrDefault(info => (Mathf.Abs(info.coordinate.x - currentCell.X) + Mathf.Abs(info.coordinate.y - currentCell.Y)) == 1 && info.amount == previousMoveAmount);
            if (null == previousCellInfo)
            {
                break;
            }
            routeCells.Add(cells.First(c => c.X == previousCellInfo.coordinate.x && c.Y == previousCellInfo.coordinate.y));
        }
        routeCells.Reverse();
        return routeCells.ToArray();
    }

    /// <summary>
    /// 指定座標にユニットを配置します
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <param name="unitPrefab">Unit prefab.</param>
    public void PutUnit(int x, int y, Main_Unit unitPrefab, Main_Unit.Teams team)
    {
        var unit = Instantiate(unitPrefab);
        unit.team = team;
        switch (unit.team)
        {
            case Main_Unit.Teams.Enemy:
                // 敵ユニットはちょっと色を変えて反転
                var image = unit.GetComponent<Image>();
                image.color = new Color(1f, 0.7f, 0.7f);
                var scale = image.transform.localScale;
                scale.x *= -1f;
                image.transform.localScale = scale;
                break;
        }
        unit.gameObject.SetActive(true);
        unit.transform.SetParent(unitContainer);
        unit.transform.position = cells.First(c => c.X == x && c.Y == y).transform.position;
        unit.x = x;
        unit.y = y;
    }

    /// <summary>
    /// ユニットを対象のマスに移動させます
    /// </summary>
    /// <param name="cell">Cell.</param>
    public void MoveTo(Main_Unit unit, Main_Cell cell)
    {
        Debug.Log("MoveTo");
        Debug.Log(unit);
        unit.GetComponent<Button>().enabled = false;
        ClearHighlight();
        var routeCells = CalculateRouteCells(unit.x, unit.y, unit.moveAmount, cell);
        var sequence = DOTween.Sequence();
        for (var i = 1; i < routeCells.Length; i++)
        {
            var routeCell = routeCells[i];
            sequence.Append(unit.transform.DOMove(routeCell.transform.position, 0.1f).SetEase(Ease.Linear));
        }
        sequence.OnComplete(() =>
            {
                unit.x = routeCells[routeCells.Length - 1].X;
                unit.y = routeCells[routeCells.Length - 1].Y;
                // 攻撃可能範囲のチェック
                var isAttackable = HighlightAttackableCells(unit.x, unit.y, unit.attackRangeMin, unit.attackRangeMax);
                if (!isAttackable)
                {
                    unit.GetComponent<Button>().enabled = true;
                    unit.GetComponent<Button>().interactable = false;
                    unit.IsFocusing = false;
                }
            });
    }

    /// <summary>
    /// 対象ユニットに攻撃します
    /// </summary>
    /// <param name="fromUnit">From unit.</param>
    /// <param name="toUnit">To unit.</param>
    public void AttackTo(Main_Unit fromUnit, Main_Unit toUnit)
    {
        Battle_SceneController.attacker = fromUnit;
        Battle_SceneController.defender = toUnit;
        SceneManager.LoadScene("Battle", LoadSceneMode.Additive);
        ClearHighlight();
        FocusingUnit.GetComponent<Button>().enabled = true;
        FocusingUnit.GetComponent<Button>().interactable = false;
        FocusingUnit.IsFocusing = false;
    }

    /// <summary>
    /// 任意の座標にいるユニットを取得します
    /// </summary>
    /// <returns>The unit.</returns>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    public Main_Unit GetUnit(int x, int y)
    {
        return unitContainer.GetComponentsInChildren<Main_Unit>().FirstOrDefault(u => u.x == x && u.y == y);
    }

    /// <summary>
    /// 移動力を元に移動可能範囲の計算を行います
    /// </summary>
    /// <returns>The remaining move amount infos.</returns>
    /// <param name="startCell">Start cell.</param>
    /// <param name="moveAmount">Move amount.</param>
    CoordinateAmountInfo[] GetRemainingMoveAmountInfos(Main_Cell startCell, int moveAmount)
    {
        var infos = new List<CoordinateAmountInfo>();
        infos.Add(new CoordinateAmountInfo(startCell.X, startCell.Y, moveAmount));
        for (var i = moveAmount; i >= 0; i--)
        {
            var appendInfos = new List<CoordinateAmountInfo>();
            foreach (var calcTargetInfo in infos.Where(info => info.amount == i))
            {
                // 四方のマスの座標配列を作成
                var calcTargetCoordinate = calcTargetInfo.coordinate;
                var aroundCellCoordinates = new Coordinate[]
                {
                    new Coordinate(calcTargetCoordinate.x - 1, calcTargetCoordinate.y),
                    new Coordinate(calcTargetCoordinate.x + 1, calcTargetCoordinate.y),
                    new Coordinate(calcTargetCoordinate.x, calcTargetCoordinate.y - 1),
                    new Coordinate(calcTargetCoordinate.x, calcTargetCoordinate.y + 1),
                };
                // 四方のマスの残移動力を計算
                foreach (var aroundCellCoordinate in aroundCellCoordinates)
                {
                    var targetCell = cells.FirstOrDefault(c => c.X == aroundCellCoordinate.x && c.Y == aroundCellCoordinate.y);
                    if (null == targetCell ||
                        infos.Any(info => info.coordinate.x == aroundCellCoordinate.x && info.coordinate.y == aroundCellCoordinate.y) ||
                        appendInfos.Any(info => info.coordinate.x == aroundCellCoordinate.x && info.coordinate.y == aroundCellCoordinate.y))
                    {
                        // マップに存在しない、または既に計算済みの座標はスルー
                        continue;
                    }
                    var remainingMoveAmount = i - targetCell.Cost;
                    appendInfos.Add(new CoordinateAmountInfo(aroundCellCoordinate.x, aroundCellCoordinate.y, remainingMoveAmount));
                }
            }
            infos.AddRange(appendInfos);
        }
        // 残移動力が0以上（移動可能）なマスの情報だけを返す
        return infos.Where(x => x.amount >= 0).ToArray();
    }

    /// <summary>
    /// 攻撃可能範囲の計算を行います
    /// </summary>
    /// <returns>The remaining move amount infos.</returns>
    /// <param name="startCell">Start cell.</param>
    /// <param name="moveAmount">Move amount.</param>
    CoordinateAmountInfo[] GetRemainingAccountRangeInfos(Main_Cell startCell, int attackRangeMin, int attackRangeMax)
    {
        var infos = new List<CoordinateAmountInfo>();
        infos.Add(new CoordinateAmountInfo(startCell.X, startCell.Y, attackRangeMax));
        for (var i = attackRangeMax; i >= 0; i--)
        {
            var appendInfos = new List<CoordinateAmountInfo>();
            foreach (var calcTargetInfo in infos.Where(info => info.amount == i))
            {
                // 四方のマスの座標配列を作成
                var calcTargetCoordinate = calcTargetInfo.coordinate;
                var aroundCellCoordinates = new Coordinate[]
                {
                    new Coordinate(calcTargetCoordinate.x - 1, calcTargetCoordinate.y),
                    new Coordinate(calcTargetCoordinate.x + 1, calcTargetCoordinate.y),
                    new Coordinate(calcTargetCoordinate.x, calcTargetCoordinate.y - 1),
                    new Coordinate(calcTargetCoordinate.x, calcTargetCoordinate.y + 1),
                };
                // 四方のマスの残攻撃範囲を計算
                foreach (var aroundCellCoordinate in aroundCellCoordinates)
                {
                    var targetCell = cells.FirstOrDefault(c => c.X == aroundCellCoordinate.x && c.Y == aroundCellCoordinate.y);
                    if (null == targetCell ||
                        infos.Any(info => info.coordinate.x == aroundCellCoordinate.x && info.coordinate.y == aroundCellCoordinate.y) ||
                        appendInfos.Any(info => info.coordinate.x == aroundCellCoordinate.x && info.coordinate.y == aroundCellCoordinate.y))
                    {
                        // マップに存在しない、または既に計算済みの座標はスルー
                        continue;
                    }
                    var remainingMoveAmount = i - 1;
                    appendInfos.Add(new CoordinateAmountInfo(aroundCellCoordinate.x, aroundCellCoordinate.y, remainingMoveAmount));
                }
            }
            infos.AddRange(appendInfos);
        }
        // 攻撃範囲内のマスの情報だけを返す
        return infos.Where(x => 0 <= x.amount && x.amount <= (attackRangeMax - attackRangeMin)).ToArray();
    }

    /// <summary>
    /// 対象座標での残○○力情報クラス
    /// </summary>
    class CoordinateAmountInfo
    {
        public readonly Coordinate coordinate;
        public readonly int amount;

        public CoordinateAmountInfo(int x, int y, int amount)
        {
            this.coordinate = new Coordinate(x, y);
            this.amount = amount;
        }
    }

    /// <summary>
    /// 座標クラス
    /// </summary>
    class Coordinate
    {
        public readonly int x;
        public readonly int y;

        public Coordinate(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
