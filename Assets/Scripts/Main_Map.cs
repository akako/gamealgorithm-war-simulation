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
    [SerializeField]
    GameObject touchBlocker;

    List<Main_Cell> cells = new List<Main_Cell>();
    Main_Unit.Teams currentTeam;
    Dictionary<Main_Unit.Teams, Main_AI> ais = new Dictionary<Main_Unit.Teams, Main_AI>();

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

    public void SetAI(Main_Unit.Teams team, Main_AI ai)
    {
        ai.Initialize(this);
        ais[team] = ai; 
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
            unit.IsMoved = team != unit.team;
        }

        if (ais.ContainsKey(team))
        {
            touchBlocker.SetActive(true);
            var ai = ais[team];
            ai.Run();
        }
        else
        {
            touchBlocker.SetActive(false);
        }
    }

    /// <summary>
    /// 次のターンに移ります
    /// </summary>
    public void NextTurn()
    {
        var nextTeam = currentTeam == Main_Unit.Teams.Player1 ? Main_Unit.Teams.Player2 : Main_Unit.Teams.Player1;
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

    public Main_Cell[] GetAttackableCells()
    {
        return cells.Where(x => x.IsAttackable).ToArray();
    }

    public Main_Cell[] GetMovableCells()
    {
        return cells.Where(x => x.IsMovable).ToArray();
    }

    /// <summary>
    /// 指定座標から各マスまで、移動コストいくつで行けるかを計算します
    /// </summary>
    /// <returns>The move amount to cells.</returns>
    /// <param name="from">From.</param>
    public List<CoordinateAndValue> GetMoveCostToAllCells(Main_Cell from)
    {
        var infos = new List<CoordinateAndValue>();
        infos.Add(new CoordinateAndValue(from.X, from.Y, 0));
        var i = 0;
        while (true)
        {
            var appendInfos = new List<CoordinateAndValue>();
            foreach (var calcTargetInfo in infos.Where(info => info.value == i))
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
                    var remainingMoveAmount = i + targetCell.Cost;
                    appendInfos.Add(new CoordinateAndValue(aroundCellCoordinate.x, aroundCellCoordinate.y, remainingMoveAmount));
                }
            }
            infos.AddRange(appendInfos);

            i++;
            if (i > infos.Max(x => x.value < 999 ? x.value : 0))
            {
                break;
            }
        }
        return infos.Where(x => x.value < 999).ToList();
    }

    /// <summary>
    /// 指定位置までの移動ルートと移動コストを返します
    /// </summary>
    /// <returns>The route coordinates and move amount.</returns>
    /// <param name="from">From.</param>
    /// <param name="to">To.</param>
    public List<CoordinateAndValue> CalcurateRouteCoordinatesAndMoveAmount(Main_Cell from, Main_Cell to)
    {
        var costs = GetMoveCostToAllCells(from);
        if (!costs.Any(info => info.coordinate.x == to.X && info.coordinate.y == to.Y))
        {
            throw new ArgumentException(string.Format("x:{0}, y:{1} is not movable.", to.X, to.Y));
        }

        var toCost = costs.First(info => info.coordinate.x == to.X && info.coordinate.y == to.Y);
        var route = new List<CoordinateAndValue>();
        route.Add(toCost);
        while (true)
        {
            var currentCost = route.Last();
            var currentCell = cells.First(cell => cell.X == currentCost.coordinate.x && cell.Y == currentCost.coordinate.y);
            var prevMoveCost = currentCost.value - currentCell.Cost;
            var previousCost = costs.FirstOrDefault(info => (Mathf.Abs(info.coordinate.x - currentCell.X) + Mathf.Abs(info.coordinate.y - currentCell.Y)) == 1 && info.value == prevMoveCost);
            if (null == previousCost)
            {
                break;
            }
            route.Add(previousCost);
        }
        route.Reverse();
        return route.ToList();
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

    public Main_Cell[] GetCellsByDistance(Main_Cell baseCell, int distanceMin, int distanceMax)
    {
        return cells.Where(x =>
            {
                var distance = Math.Abs(baseCell.X - x.X) + Math.Abs(baseCell.Y - x.Y);
                return distanceMin <= distance && distance <= distanceMax;
            }).ToArray();
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
        var hasTarget = false;
        foreach (var cell in GetCellsByDistance(startCell, attackRangeMin, attackRangeMax))
        {
            if (null != cell.Unit && cell.Unit.team != currentTeam)
            {
                hasTarget = true;
                cell.IsAttackable = true;
                cell.Unit.GetComponent<Button>().interactable = true;
            }
        }
        return hasTarget;
    }

    /// <summary>
    /// マスのハイライトを消します
    /// </summary>
    public void ClearHighlight()
    {
        foreach (var cell in cells)
        {
            if (cell.IsAttackable)
            {
                cell.Unit.GetComponent<Button>().interactable = false;
            }
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
            var previousMoveAmount = currentCellInfo.value + currentCell.Cost;
            var previousCellInfo = infos.FirstOrDefault(info => (Mathf.Abs(info.coordinate.x - currentCell.X) + Mathf.Abs(info.coordinate.y - currentCell.Y)) == 1 && info.value == previousMoveAmount);
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
            case Main_Unit.Teams.Player2:
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
                    unit.IsMoved = true;
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
        FocusingUnit.IsMoved = true;
    }

    /// <summary>
    /// 自軍のユニットを取得します
    /// </summary>
    /// <returns>The own units.</returns>
    public Main_Unit[] GetOwnUnits()
    {
        return unitContainer.GetComponentsInChildren<Main_Unit>().Where(x => x.team == currentTeam).ToArray();
    }

    /// <summary>
    /// 敵軍のユニットを取得します
    /// </summary>
    /// <returns>The enemy units.</returns>
    public Main_Unit[] GetEnemyUnits()
    {
        return unitContainer.GetComponentsInChildren<Main_Unit>().Where(x => x.team != currentTeam).ToArray();
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
    CoordinateAndValue[] GetRemainingMoveAmountInfos(Main_Cell startCell, int moveAmount)
    {
        var infos = new List<CoordinateAndValue>();
        infos.Add(new CoordinateAndValue(startCell.X, startCell.Y, moveAmount));
        for (var i = moveAmount; i >= 0; i--)
        {
            var appendInfos = new List<CoordinateAndValue>();
            foreach (var calcTargetInfo in infos.Where(info => info.value == i))
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
                    appendInfos.Add(new CoordinateAndValue(aroundCellCoordinate.x, aroundCellCoordinate.y, remainingMoveAmount));
                }
            }
            infos.AddRange(appendInfos);
        }
        // 残移動力が0以上（移動可能）なマスの情報だけを返す
        return infos.Where(x => x.value >= 0).ToArray();
    }

    /// <summary>
    /// 攻撃可能範囲の計算を行います
    /// </summary>
    /// <returns>The remaining move amount infos.</returns>
    /// <param name="startCell">Start cell.</param>
    /// <param name="moveAmount">Move amount.</param>
    CoordinateAndValue[] GetRemainingAccountRangeInfos(Main_Cell startCell, int attackRangeMin, int attackRangeMax)
    {
        var infos = new List<CoordinateAndValue>();
        infos.Add(new CoordinateAndValue(startCell.X, startCell.Y, attackRangeMax));
        for (var i = attackRangeMax; i >= 0; i--)
        {
            var appendInfos = new List<CoordinateAndValue>();
            foreach (var calcTargetInfo in infos.Where(info => info.value == i))
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
                    appendInfos.Add(new CoordinateAndValue(aroundCellCoordinate.x, aroundCellCoordinate.y, remainingMoveAmount));
                }
            }
            infos.AddRange(appendInfos);
        }
        // 攻撃範囲内のマスの情報だけを返す
        return infos.Where(x => 0 <= x.value && x.value <= (attackRangeMax - attackRangeMin)).ToArray();
    }

    /// <summary>
    /// 座標と数値情報を紐付けるためのクラス
    /// </summary>
    public class CoordinateAndValue
    {
        public readonly Coordinate coordinate;
        public readonly int value;

        public CoordinateAndValue(int x, int y, int value)
        {
            this.coordinate = new Coordinate(x, y);
            this.value = value;
        }
    }

    /// <summary>
    /// 座標クラス
    /// </summary>
    public class Coordinate
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
