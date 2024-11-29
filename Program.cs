using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

class jpsplus
{
    private const char WallChar = '#';

    private static readonly List<(int, int)> CardinalDirections = new()
    {
        (-1, 0), // Север
        (0, 1),  // Восток
        (1, 0),  // Юг
        (0, -1)  // Запад
    };
    private static readonly List<(int, int)> DiagonalDirections = new()
    {
        (-1, 1),  // Северо-восток
        (1, 1),   // Юго-восток
        (1, -1),  // Юго-запад
        (-1, -1)  // Северо-запад
    };

    private static char[,]? _grid;
    
    static void Main(string[] args)
    {
        _grid = ParseInputGrid();
        
        /*
         * Шаг 1 - проходимся по всей сетке и находим основные точки прыжка
         * Здесь мы получаем список координат и направления для основных точек прыжка,
         * где идентификатор = координата формата (строка, столбец), и список направлений формата List<(+-строка, +-столбец)>
         */
        var primaryJumpPoints = GetPrimaryJumpPoints();
        
        /*
         * Шаг 2 - проходимся по всей сетке и находим прямые точек прыжков, а также рассчитываем расстояние до них
         * Здесь мы для каждой ячейки проверяем доступность точек прыжков в 4-х основных направлениях, и если таковые имеются - фиксируем расстояние до них
         */
        var cellsDistances = GetCellsDistances(primaryJumpPoints);

        PrintGridCellsDistances(cellsDistances);
    }

    /// <summary>
    /// Парсинг входных данных для интерактивных тестов на сайте codingame.com
    /// </summary>
    /// <returns></returns>
    private static char[,] ParseInputGrid()
    {
        string[] inputs = (Console.ReadLine() ?? "").Split(' ');
        int width = int.Parse(inputs[0]);
        int height = int.Parse(inputs[1]);
        char[,] grid = new char[height, width];
        for (int i = 0; i < height; i++)
        {
            string row = Console.ReadLine() ?? "";
            for (int j = 0; j < width; j++) {
                grid[i, j] = row[j];
            }
        }

        return grid;
    }

    /// <summary>
    /// Вывод расстояний для клеток в формате столбец строка N NE E SE S SW W NW, пропускает клетки-стены
    /// </summary>
    /// <param name="cellsDistances">Рассчитанный список расстояний</param>
    private static void PrintGridCellsDistances(Dictionary<(int, int), Dictionary<(int, int), int>> cellsDistances)
    {
        foreach (var cellDistances in cellsDistances)
        {
            int row = cellDistances.Key.Item1;
            int column = cellDistances.Key.Item2;
            if (CheckCellIsWall(cellDistances.Key)) continue;

            int n = cellDistances.Value.ContainsKey((-1, 0)) ? cellDistances.Value[(-1, 0)] : 0;
            int ne = cellDistances.Value.ContainsKey((-1, 1)) ? cellDistances.Value[(-1, 1)] : 0;
            int e = cellDistances.Value.ContainsKey((0, 1)) ? cellDistances.Value[(0, 1)] : 0;
            int se = cellDistances.Value.ContainsKey((1, 1)) ? cellDistances.Value[(1, 1)] : 0;
            int s = cellDistances.Value.ContainsKey((1, 0)) ? cellDistances.Value[(1, 0)] : 0;
            int sw = cellDistances.Value.ContainsKey((1, -1)) ? cellDistances.Value[(1, -1)] : 0;
            int w = cellDistances.Value.ContainsKey((0, -1)) ? cellDistances.Value[(0, -1)] : 0;
            int nw = cellDistances.Value.ContainsKey((-1, -1)) ? cellDistances.Value[(-1, -1)] : 0;
            
            Console.WriteLine($"{column} {row} {n} {ne} {e} {se} {s} {sw} {w} {nw}");
        }
    }

    /// <summary>
    /// Рассчитать координаты точек прыжков для заданной сетки
    /// Точка прыжка рассчитывается по принципу 'forced neighbor'
    /// </summary>
    /// <returns>Список координат формата (строка, столбец, направление)</returns>
    private static Dictionary<(int, int), List<(int, int)>> GetPrimaryJumpPoints()
    {
        if (_grid == null || _grid.GetLength(0) == 0 || _grid.GetLength(1) == 0) throw new InvalidOperationException("Сетка должна быть заполнена");
        
        /*
         * Логика такова
         * Проходим по каждой ПУСТОЙ ячейке сетки, смотрим в основные направления С Ю З В
         * Если в одном из заданных направлений встречаем СТЕНУ, определяем какое это направление - горизонтальное (З В) или вертикальное (С Ю)
         * Если это было горизонтальное направление - берем вертикальные направления, и наоборот
         * С этими направлениями смотрим сетку от найденной стены
         * Если встречаем пустоту, то в этом направлении от ИЗНАЧАЛЬНОЙ точки будет точка прыжка с заданным направлением (если там не стена и не конец сетки)
         * Таким образом формируем список всех точек прыжков вместе со всеми доступными направлениями
         */
        
        Dictionary<(int, int), List<(int, int)>> primaryJumpPoints = new Dictionary<(int, int), List<(int, int)>>();
        int rowsCount = _grid.GetLength(0);
        int columnsCount = _grid.GetLength(1);

        /*
         * Направления обхода пустых ячеек (по часовой стрелке, но это не принципиально)
         * Здесь первое значение - вектор направления по вертикали (строки)
         * Второе - вектор направления горизонтали (столбцов)
         *
         * Например, 0 -1 значит, что мы берем направление в 0 по вертикали и -1 по горизонтали, то есть смотрим на предыдущий столбец, или влево или на запад
         * 1 0 значит что смотрим на 1 по вертикали, то есть на следующую строку, вниз, Ю
         */

        // Горизонтальные и вертикальные направления отдельно
        var verticalDirections = CardinalDirections.Where(d => d.Item2 == 0).ToList();
        var horizontalDirections = CardinalDirections.Where(d => d.Item1 == 0).ToList();

        for (int row = 0; row < rowsCount; row++)
        {
            for (int column = 0; column < columnsCount; column++)
            {
                if (CheckCellIsWall((row, column))) continue;
                foreach (var direction in CardinalDirections)
                {
                    var nextCellCoordinate = GetGridCoordinate(row, column, direction);
                    
                    // Вернет пустоту, если дальше конец границы сетки, такое мы пропускаем, также пропускаем все пустые ячейки
                    if (nextCellCoordinate is not var (nextRow, nextCol) || _grid[nextRow, nextCol] != WallChar) continue;

                    // Определяем возможные направления для проверки
                    var checkDirections = direction.Item2 != 0 ? verticalDirections : horizontalDirections;
                    foreach (var checkDirection in checkDirections)
                    {
                        var nextWallCellCoordinate = GetGridCoordinate(nextRow, nextCol, checkDirection);
                        if (nextWallCellCoordinate.HasValue && !CheckCellIsWall(nextWallCellCoordinate.Value))
                        {
                            var potentialJumpPoint = GetGridCoordinate(row, column, checkDirection);
                            if (potentialJumpPoint.HasValue && !CheckCellIsWall(potentialJumpPoint.Value))
                            {
                                if (!primaryJumpPoints.ContainsKey(potentialJumpPoint.Value))
                                {
                                    primaryJumpPoints[potentialJumpPoint.Value] = new List<(int, int)> { checkDirection };
                                }
                                else if (!primaryJumpPoints[potentialJumpPoint.Value].Contains(checkDirection))
                                {
                                    primaryJumpPoints[potentialJumpPoint.Value].Add(checkDirection);
                                }
                            }
                        }
                    }
                }
            }
        }

        return primaryJumpPoints;
    }

    /// <summary>
    /// Метод расчета расстояний до стен/точек прыжков в 8-и направлениях
    /// </summary>
    /// <param name="primaryJumpPoints">Основные точки прыжков</param>
    /// <returns>Возвращает список точек и расстояния в 8-и направлениях в формате ключ = (строка, столбец), значение = список направлений, где ключ = направление, значение = расстояние</returns>
    private static Dictionary<(int, int), Dictionary<(int, int), int>> GetCellsDistances(Dictionary<(int, int), List<(int, int)>> primaryJumpPoints)
    {
        if (_grid == null || _grid.GetLength(0) == 0 || _grid.GetLength(1) == 0) throw new InvalidOperationException("Сетка должна быть заполнена");

        int rowsCount = _grid.GetLength(0);
        int columnsCount = _grid.GetLength(1);

        Dictionary<(int, int), Dictionary<(int, int), int>> cellsDistances = new Dictionary<(int, int), Dictionary<(int, int), int>>();
        
        // Проходимся по каждой ячейке сетки и заполняем расстояния в кардинальных направлениях (С Ю З В)
        for (int row = 0; row < rowsCount; row++)
        {
            for (int column = 0; column < columnsCount; column++)
            {
                Dictionary<(int, int), int> cellDirections = new Dictionary<(int, int), int>();
                foreach (var direction in CardinalDirections)
                {
                    int distance = 0;
                    while (true)
                    {
                        // Берем следующую клетку в указанном направлении
                        var nextCell = GetGridCoordinate(row, column, (direction.Item1 * (distance + 1), direction.Item2 * (distance + 1)));
                        
                        // Если вышли за пределы или это стена возвращаем отрицательное расстояние или 0
                        if (!nextCell.HasValue || CheckCellIsWall(nextCell.Value))
                        {
                            cellDirections.Add(direction, -distance);
                            break;
                        }
                        
                        // Если в указанном направлении в следующей клетке точка прыжка сохраняем положительное расстояние (+1, т.к. это следующая клетка)
                        if (primaryJumpPoints.ContainsKey(nextCell.Value) && primaryJumpPoints[nextCell.Value].Contains(direction))
                        {
                            cellDirections.Add(direction, distance + 1);
                            break;
                        }
                        distance++;
                    }
                }
                cellsDistances.Add((row, column), cellDirections);
            }
        }
        
        // Проходимся по каждой вычисленной ячейке и прогоняем оставшиеся 4 диагональные направления
        foreach (var gridPoint in cellsDistances)
        {
            foreach (var diagonalDirection in DiagonalDirections)
            {
                int distance = 0;

                while (true)
                {
                    // Берем текущую ячейку в указанном направлении с текущим расстоянием
                    var currentCell = GetGridCoordinate(gridPoint.Key.Item1, gridPoint.Key.Item2, (distance * diagonalDirection.Item1, distance * diagonalDirection.Item2));
                    
                    // Если вышли за пределы или в этой клетке стена, возвращаем отрицательное расстояние
                    if (!currentCell.HasValue || CheckCellIsWall(currentCell.Value))
                    {
                        gridPoint.Value.Add(diagonalDirection, -distance);
                        break;
                    }
                    
                    // Переводим диагональное направление в кардинальные для проверки соседей и расстояний
                    var horizontalDirection = (0, diagonalDirection.Item2);
                    var verticalDirection = (diagonalDirection.Item1, 0);

                    // Если мы рассматриваем не начальную точку и ее расстояния в кардинальных направлениях относительно текущего вертикального направления положительны - это валидная точка прыжка или прямая точки прыжка
                    if (distance > 0 && (cellsDistances[currentCell.Value][horizontalDirection] > 0 || cellsDistances[currentCell.Value][verticalDirection] > 0))
                    {
                        gridPoint.Value.Add(diagonalDirection, distance);
                        break;
                    }
                    
                    // Берем вертикального и горизонтального соседа от исходной точки в кардинальных направлениях
                    var horizontalNeighbor = GetGridCoordinate(currentCell.Value.Item1, currentCell.Value.Item2, horizontalDirection);
                    var verticalNeighbor = GetGridCoordinate(currentCell.Value.Item1, currentCell.Value.Item2, verticalDirection);
                    
                    // Если соседние клетки это выход за границу или стены - возвращаем отрицательное расстояние
                    if (!horizontalNeighbor.HasValue || !verticalNeighbor.HasValue || CheckCellIsWall(horizontalNeighbor.Value) || CheckCellIsWall(verticalNeighbor.Value))
                    {
                        gridPoint.Value.Add(diagonalDirection, -distance);
                        break;
                    }
                    
                    // Берем следующую клетку в указанном диагональном направлении
                    var nextCell = GetGridCoordinate(currentCell.Value.Item1, currentCell.Value.Item2, diagonalDirection);
                    
                    // Если следующая клетка стена или выход за границу - возвращаем отрицацтельное расстояние
                    if (!nextCell.HasValue || CheckCellIsWall(nextCell.Value))
                    {
                        gridPoint.Value.Add(diagonalDirection, -distance);
                        break;
                    }
                    distance++;
                }
            }
        }
        
        return cellsDistances;
    }

    /// <summary>
    /// Метод для получения координаты сетки в указанном направлении
    /// </summary>
    /// <param name="startRow">Индекс строки откуда начинаем движение</param>
    /// <param name="startColumn">Индекс столбца откуда начинаем движение</param>
    /// <param name="direction">Направление (строка, столбец)</param>
    /// <returns>Возвращает координату формата (строка, столбец) или null, если вышли за пределы сетки</returns>
    private static (int, int)? GetGridCoordinate(int startRow, int startColumn, (int, int) direction)
    {
        if (_grid == null || _grid.GetLength(0) == 0 || _grid.GetLength(1) == 0) throw new InvalidOperationException("Сетка должна быть заполнена");
        
        int rowsCount = _grid.GetLength(0);
        int columnsCount = _grid.GetLength(1);
        int newRow = startRow + direction.Item1;
        int newCol = startColumn + direction.Item2;

        return newRow < 0 || newRow >= rowsCount || newCol < 0 || newCol >= columnsCount ? null : (newRow, newCol);
    }

    /// <summary>
    /// Проверка, что ячейка сетки это стена
    /// </summary>
    /// <param name="coordinate"></param>
    /// <returns>Вернет true/false если это стена</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static bool CheckCellIsWall((int, int) coordinate)
    {
        if (_grid == null || _grid.GetLength(0) == 0 || _grid.GetLength(1) == 0) throw new InvalidOperationException("Сетка должна быть заполнена");

        return GetGridCoordinate(coordinate.Item1, coordinate.Item2, (0, 0)).HasValue && _grid[coordinate.Item1, coordinate.Item2] == WallChar;
    }
}