namespace jpsplus;

class Program
{
    const char WALL_CHAR = '#';
    const char CLEAR_CHAR = '.';

    static void Main(string[] args)
    {
        // char[,] grid = {
        //     {'.', '.', '.', '.', '.'},
        //     {'.', '.', '#', '.', '.'},
        //     {'.', '.', '.', '.', '.'},
        //     {'.', '.', '.', '.', '.'},
        //     {'.', '.', '.', '.', '.'},
        // };
        
        // char[,] grid =
        // {
        //     { '.', '.', '#', '.', '.', '.', '#', '.', '.' },
        //     { '.', '.', '.', '.', '.', '.', '#', '.', '.' },
        //     { '.', '#', '#', '.', '.', '.', '#', '#', '.' },
        //     { '.', '.', '#', '.', '.', '.', '.', '.', '.' },
        //     { '.', '.', '#', '.', '.', '.', '#', '.', '.' },
        // };

        char[,] grid =
        {
            {'#', '.', '#', '#', '#', '#', '#', '.', '#', '#', '#', '#', '#', '.', '#', '#', '#', },
            {'#', '.', '.', '.', '.', '.', '#', '.', '.', '.', '.', '.', '.', '.', '.', '.', '#', },
            {'.', '.', '.', '.', '.', '.', '#', '.', '.', '.', '.', '#', '.', '.', '.', '.', '#', },
            {'#', '.', '.', '.', '.', '.', '#', '.', '.', '.', '.', '#', '.', '.', '.', '.', '#', },
            {'#', '#', '.', '#', '#', '#', '#', '#', '#', '#', '#', '#', '#', '.', '#', '#', '#', },
            {'#', '.', '.', '.', '.', '.', '#', '.', '.', '.', '.', '#', '.', '.', '.', '.', '#', },
            {'#', '.', '.', '.', '.', '.', '#', '.', '.', '.', '.', '#', '.', '.', '.', '.', '.', },
            {'#', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '#', '.', '.', '.', '.', '#', },
            {'#', '#', '#', '.', '#', '#', '#', '#', '#', '.', '#', '#', '#', '#', '.', '#', '#', },
            {'#', '.', '.', '.', '.', '.', '#', '.', '.', '.', '.', '#', '.', '.', '.', '.', '#', },
            {'#', '.', '.', '.', '.', '.', '#', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', },
            {'.', '.', '.', '.', '.', '.', '#', '.', '.', '.', '.', '#', '.', '.', '.', '.', '#', },
            {'#', '#', '#', '#', '.', '#', '#', '#', '.', '#', '#', '#', '#', '#', '#', '#', '#', },
        };

        /*
         * Шаг 1 - проходимся по всей сетке и находим основные точки прыжка
         * Здесь мы получаем список координат и направления для основных точек прыжка,
         * где идентификатор = координата формата (строка, столбец), и список направлений формата List<(+-строка, +-столбец)>
         */
        var primaryJumpPoints = GetPrimaryJumpPoints(grid);
        
        /*
         * Шаг 2 - проходимся по всей сетке и находим прямые точек прыжков, а также рассчитываем расстояние до них
         * Здесь мы для каждой ячейки проверяем доступность точек прыжков в 4-х основных направлениях, и если таковые имеются - фиксируем расстояние до них
         */
        var straightJumpPoints = GetStraightJumpPoints(grid, primaryJumpPoints);
        foreach (var jumpPoint in straightJumpPoints)
        {
            int row = jumpPoint.Key.Item1;
            int column = jumpPoint.Key.Item2;
            if (grid[row, column] == '#') continue;

            int n = jumpPoint.Value.ContainsKey((-1, 0)) ? jumpPoint.Value[(-1, 0)] : 0;
            int ne = jumpPoint.Value.ContainsKey((-1, 1)) ? jumpPoint.Value[(-1, 1)] : 0;
            int e = jumpPoint.Value.ContainsKey((0, 1)) ? jumpPoint.Value[(0, 1)] : 0;
            int se = jumpPoint.Value.ContainsKey((1, 1)) ? jumpPoint.Value[(1, 1)] : 0;
            int s = jumpPoint.Value.ContainsKey((1, 0)) ? jumpPoint.Value[(1, 0)] : 0;
            int sw = jumpPoint.Value.ContainsKey((1, -1)) ? jumpPoint.Value[(1, -1)] : 0;
            int w = jumpPoint.Value.ContainsKey((0, -1)) ? jumpPoint.Value[(0, -1)] : 0;
            int nw = jumpPoint.Value.ContainsKey((-1, -1)) ? jumpPoint.Value[(-1, -1)] : 0;
            
            Console.WriteLine($"{column} {row} {n} {ne} {e} {se} {s} {sw} {w} {nw}");
        }
    }

    /// <summary>
    /// Рассчитать координаты точек прыжков для заданной сетки
    /// Точка прыжка рассчитывается по принципу 'forced neighbor'
    /// </summary>
    /// <param name="grid">Сетка</param>
    /// <returns>Список координат формата (строка, столбец, направление)</returns>
    static Dictionary<(int, int), List<(int, int)>> GetPrimaryJumpPoints(char[,] grid)
    {
        /*
         * Логика такова
         * Проходим по каждой ПУСТОЙ ячейке сетки, смотрим в основные направления С Ю З В
         * Если в одном из заданных направлений встречаем СТЕНУ, определяем какое это направление - горизонтальное (З В) или вертикальное (С Ю)
         * Если это было горизонтальное направление - берем вертикальные направления, и наоборот
         * С этими направлениями смотрим сетку от найденной стены
         * Если встречаем пустоту, то в этом направлении от ИЗНАЧАЛЬНОЙ точки будет точка прыжка с заданным направлением (если там не стена и не конец сетки)
         * Таким образом формируем список всех точек прыжков вместе со всеми доступными направлениями
         */
        
        var primaryJumpPoints = new Dictionary<(int, int), List<(int, int)>>();
        int rowsCount = grid.GetLength(0);
        int columnsCount = grid.GetLength(1);

        /*
         * Направления обхода пустых ячеек (по часовой стрелке, но это не принципиально)
         * Здесь первое значение - вектор направления по вертикали (строки)
         * Второе - вектор направления горизонтали (столбцов)
         *
         * Например, 0 -1 значит, что мы берем направление в 0 по вертикали и -1 по горизонтали, то есть смотрим на предыдущий столбец, или влево или на запад
         * 1 0 значит что смотрим на 1 по вертикали, то есть на следующую строку, вниз, Ю
         */
        
        var directions = new List<(int, int)>
        {
            (-1, 0), // Вверх С
            (0, 1), // Вправо В
            (1, 0), // Вниз Ю
            (0, -1), // Влево З
        };

        // Горизонтальные и вертикальные направления отдельно
        var verticalDirections = directions.Where(d => d.Item2 == 0).ToList();
        var horizontalDirections = directions.Where(d => d.Item1 == 0).ToList();

        for (int row = 0; row < rowsCount; row++)
        {
            for (int column = 0; column < columnsCount; column++)
            {
                if (grid[row, column] == WALL_CHAR) continue;
                foreach (var direction in directions)
                {
                    var nextCeilCoord = GetGridCoordinate(grid, row, column, direction);
                    // Вернет пустоту, если дальше конец границы сетки, такое мы пропускаем, также пропускаем все пустые ячейки
                    if (nextCeilCoord is not var (nextRow, nextCol) || grid[nextRow, nextCol] != WALL_CHAR) continue;

                    // Определяем возможные направления для проверки
                    var checkDirections = direction.Item2 != 0 ? verticalDirections : horizontalDirections;
                    foreach (var checkDirection in checkDirections)
                    {
                        var nextWallCeilCoord = GetGridCoordinate(grid, nextRow, nextCol, checkDirection);
                        if (nextWallCeilCoord is var (wallRow, wallCol) && grid[wallRow, wallCol] == CLEAR_CHAR)
                        {
                            var potentialJumpPoint = GetGridCoordinate(grid, row, column, checkDirection);
                            if (potentialJumpPoint is var (jumpRow, jumpCol) && grid[jumpRow, jumpCol] == CLEAR_CHAR)
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
    /// Метод расчета прямых точек прыжков
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="primaryJumpPoints"></param>
    static Dictionary<(int, int), Dictionary<(int, int), int>> GetStraightJumpPoints(char[,] grid, Dictionary<(int, int), List<(int, int)>> primaryJumpPoints)
    {
        int rowsCount = grid.GetLength(0);
        int columnsCount = grid.GetLength(1);
        var directions = new List<(int, int)>
        {
            (-1, 0), // Вверх С
            (0, 1), // Вправо В
            (1, 0), // Вниз Ю
            (0, -1), // Влево З
        };
        
        Dictionary<(int, int), Dictionary<(int, int), int>> straightJumpPoints = new Dictionary<(int, int), Dictionary<(int, int), int>>();
        
        for (int row = 0; row < rowsCount; row++)
        {
            for (int column = 0; column < columnsCount; column++)
            {
                Dictionary<(int, int), int> ceilDirections = new Dictionary<(int, int), int>();
                foreach (var direction in directions)
                {
                    int distance = 1;
                    while (true)
                    {
                        var nextCeil = GetGridCoordinate(grid, row, column, (direction.Item1 * distance, direction.Item2 * distance));
                        if (!nextCeil.HasValue || grid[nextCeil.Value.Item1, nextCeil.Value.Item2] == WALL_CHAR)
                        {
                            ceilDirections.Add(direction, (distance - 1) * -1);
                            break;
                        }
                        if (primaryJumpPoints.ContainsKey(nextCeil.Value) && primaryJumpPoints[nextCeil.Value].Contains(direction))
                        {
                            ceilDirections.Add(direction, distance);
                            break;
                        }
                        distance++;
                    }
                }
                straightJumpPoints.Add((row, column), ceilDirections);
            }
        }
        
        return straightJumpPoints;
    }

    /// <summary>
    /// Метод для получения координаты сетки в указанном направлении
    /// </summary>
    /// <param name="grid">Сетка</param>
    /// <param name="startRow">Индекс строки откуда начинаем движение</param>
    /// <param name="startColumn">Индекс столбца откуда начинаем движение</param>
    /// <param name="direction">Направление (строка, столбец)</param>
    /// <returns>Возвращает координату формата (строка, столбец) или null, если вышли за пределы сетки</returns>
    static (int, int)? GetGridCoordinate(char[,] grid, int startRow, int startColumn, (int, int) direction)
    {
        int rowsCount = grid.GetLength(0);
        int columnsCount = grid.GetLength(1);
        int newRow = startRow + direction.Item1;
        int newCol = startColumn + direction.Item2;

        return newRow < 0 || newRow >= rowsCount || newCol < 0 || newCol >= columnsCount ? null : (newRow, newCol);
    }
}