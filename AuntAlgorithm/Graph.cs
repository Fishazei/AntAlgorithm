using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.Json;

namespace AuntAlgorithm
{
    class Graph
    {
        // Матрицы ребер и феремонов на них
        double[,]? edgesM;
        public double[,] PheromonsM;

        // Вершины
        Dictionary<int, Point>? vertices;

        public List<int> optPath;

        public Graph() { PheromonsM = new double[1, 1]; optPath = new List<int>(); }

        public Dictionary<int, Point> Vertices
        {
            get
            {
                if (vertices != null)
                    return new Dictionary<int, Point>(vertices);
                else
                    return new Dictionary<int, Point>();
            }
        }


        public double[,] EdgesM
        {
            get { return (edgesM == null)? new double[1, 1] : edgesM; }
        }

        public bool ParseFromFile(string Path)
        {
            if (File.Exists(Path))
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,             // Игнорировать регистр свойств
                    ReadCommentHandling = JsonCommentHandling.Skip, // Пропускать комментарии
                    AllowTrailingCommas = true                      // Разрешать запятые в конце
                };

                string gstring = File.ReadAllText(Path);
                GraphData graph = JsonSerializer.Deserialize<GraphData>(gstring, options);

                if (graph.Vertices == null){
                    Debug.WriteLine("Ошибка считывания вершин!");
                    return false;
                }

                vertices = new Dictionary<int, Point>();
                for (int i = 0; i < graph.Vertices.Count(); i++){
                    vertices[i] = new Point(graph.Vertices[i].X, graph.Vertices[i].Y);
                }
                edgesM = new double[graph.Vertices.Count(), graph.Vertices.Count()];
                foreach (var edge in graph.Edges) {
                    double weight = edge.Weight == "" ? 1 : Convert.ToDouble(edge.Weight);
                    edgesM[edge.Vertex1, edge.Vertex2] = weight;
                    if (!edge.IsDirected)   // Для неориентированных графов
                    {
                        edgesM[edge.Vertex2, edge.Vertex1] = weight;
                    }
                }
                optPath = new List<int>();
            }
            else
            {
                Debug.WriteLine("File not exist");
            }
            return false;
        }

        public void GenerateComivoiaj(int N, int W, int H)
        {
            if (W == 0 || H == 0 || N <= 0) return;
            Dictionary<int, Point> tmpVert = new Dictionary<int, Point>();
            double[,] tmpEdgesM = new double[N, N];
            double[,] tmpPheromonsM = new double[N, N];

            // Создание словаря точек
            var lp = PointsGenerator.QDRangeGenerate(N, W, H, 50);
            if (lp.Count < N) return;

            for (int i = 0; i < N; i++) 
                tmpVert.Add(i, lp[i]);

            // Расчёт растояний между точками
            // (Заполнение матрицы растояний в зависимости от растояний между точками) 
            for (int i = 0; i < N; i++)
                for (int j = 0; j < N; j++)
                    tmpEdgesM[i, j] = CalcDist(tmpVert[i], tmpVert[j]);
           
            vertices = tmpVert;
            edgesM = tmpEdgesM;
            PheromonsM = tmpPheromonsM;
        }

        public void InitPheromones(double tau0) {
            if (edgesM == null) { return; }
            PheromonsM = new double[Vertices.Count, Vertices.Count];
            for (int i = 0; i < Vertices.Count; i++)
                for (int j = 0; j < Vertices.Count; j++)
                    if (edgesM[i, j] != 0)
                        PheromonsM[i, j] = tau0;
        }
        public void LogPheromones()
        {
            if (PheromonsM == null) { return; }
            Debug.Write("Pheromones:\n");
            for (int i = 0; i < Vertices.Count; i++) { 
                for (int j = 0; j < Vertices.Count; j++) {
                    Debug.Write($"{PheromonsM[i,j]:F2} ");
                }
                Debug.Write("\n");
            }
            Debug.Write("\n");
        }

        double CalcDist(Point one, Point two)
        {
            double dy = two.Y - one.Y;
            double dx = two.X - one.X;
            return Math.Sqrt(dy * dy + dx * dx);
        }
    }
    class AntAl : INotifyPropertyChanged
    {
        Graph gra { get; set; }

        int _antCount;    // количество муравьев,
        int _iterCount;   // количество итераций алгоритма,
        double _tau0;     // начальное количество феромона на дугах графа,
        int _antTau;      // запас феромона каждого муравья,
        double _P;        // коэффициент испарения,
        int _alpha;       // α - эмпирические коэффициенты
        int _beta;        // β – эмпирические коэффициенты

        bool _isRunning;    // Флаг запущенного алгоритма
        int _iter;          // Номер идущей итерации

        private int _startPoint;    // Стартовая вершина
        private int _finishPoint;
        private int _pc;            // Кол-во вершин

        // Статистика и т.п.
        double _optDist;
        ObservableCollection<PathRow> _paths;      // Здесь сохраняем пути
        List<double> _optD; 

        #region Свойства для связывания
        public double OptDist
        {
            get => _optDist;
            set
            {
                if (_optDist != value)
                {
                    _optDist = value;
                    OnPropertyChanged(nameof(OptDist));
                }
            }
        }

        public int AntCount
        {
            get => _antCount;
            set
            {
                if (_antCount != value && !_isRunning)
                {
                    _antCount = value;
                    OnPropertyChanged(nameof(AntCount));
                    Debug.Write($"ant count {_antCount}\n");
                }
            }
        }

        public int IterCount
        {
            get => _iterCount;
            set
            {
                if (_iterCount != value && !_isRunning)
                {
                    _iterCount = value;
                    OnPropertyChanged(nameof(IterCount));
                    Debug.Write($"iter count {_iterCount}\n");
                }
            }
        }

        public double Tau0
        {
            get => _tau0;
            set
            {
                if (_tau0 != value && !_isRunning)
                {
                    _tau0 = value;
                    OnPropertyChanged(nameof(Tau0));
                }
            }
        }

        public int AntTau
        {
            get => _antTau;
            set
            {
                if (_antTau != value && !_isRunning)
                {
                    _antTau = value;
                    OnPropertyChanged(nameof(AntTau));
                }
            }
        }

        public double P
        {
            get => _P;
            set
            {
                if (_P != value && !_isRunning)
                {
                    _P = value;
                    OnPropertyChanged(nameof(P));
                }
            }
        }

        public int Alpha
        {
            get => _alpha;
            set
            {
                if (_alpha != value && !_isRunning)
                {
                    _alpha = value;
                    OnPropertyChanged(nameof(Alpha));
                }
            }
        }

        public int Beta
        {
            get => _beta;
            set
            {
                if (_beta != value && !_isRunning)
                {
                    _beta = value;
                    OnPropertyChanged(nameof(Beta));
                }
            }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    OnPropertyChanged(nameof(IsRunning));
                    OnPropertyChanged(nameof(IsNotRunning)); // Для блокировки TextBox'ов
                }
            }
        }
        
        public int StartPoint
        {
            get => _startPoint;
            set
            {
                if (_startPoint != value && !_isRunning)
                {
                    _startPoint = value;
                    OnPropertyChanged(nameof(StartPoint));
                }
            }
        }

        public int FinishPoint
        {
            get => _finishPoint;
            set
            {
                if (_finishPoint != value && !_isRunning)
                {
                    _finishPoint = value;
                    OnPropertyChanged(nameof(FinishPoint));
                }
            }
        }

        public int Iter
        {
            get => _iter;
            set
            {
                if (_iter != value)
                {
                    _iter = value;
                    if (_iter > _iterCount)
                    {
                        _iter = 0;
                        _isRunning = !_isRunning;
                    }
                    OnPropertyChanged(nameof(Iter));
                }
            }
        }

        public int PC
        {
            get { return _pc; }
            set { _pc = value; OnPropertyChanged(nameof(PC));}
        }

        public ObservableCollection<PathRow> Paths
        {
            get { return _paths; }
            set { _paths = value; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Свойство для удобства привязки (обратное IsRunning)
        public bool IsNotRunning => !_isRunning;
        #endregion

        public AntAl(Graph graph, int antCount = 1, int iterCount = 1, double tau0 = 10, int antTau = 10, double P = 1, int alpha = 1, int beta = 1) {
            gra = graph; 
            _antCount = antCount;
            _iterCount = iterCount;
            _tau0 = tau0;   
            _antTau = antTau;
            _P = P;
            _alpha = alpha;
            _beta = beta; 
            _iter = 0;
            _isRunning = false;
            _pc = 10;
            _paths = new ObservableCollection<PathRow>();
            _optD = new List<double>();
        }

        public void ZeroIter() => _iter = 0;

        #region Сам алгоритм
        // Выбор следующей вершины
        int ChooseNextVertex(int cur, List<int> path) {
            double[] prod = CalcProbable(cur, path);
            int next = SelectNextVertex(prod);

            return next;
        }

        // Подсчёт вероятностей
        double[] CalcProbable(int cur, List<int> path) {
            int count = gra.Vertices.Count;
            double[] probable = new double[count];
            double sum = 0;

            for (int i = 0; i < count; i++)
            {
                if (gra.EdgesM[cur, i] == 0) { continue; }
                if (path.Contains(i)) { continue; }
                double pher = gra.PheromonsM[cur, i];
                double vis = 1.0 / gra.EdgesM[cur,i];

                probable[i] = Math.Pow(pher, _alpha) * Math.Pow(vis, _beta);
                sum += probable[i];
            }

            for (int i = 0; i < count; i++) { 
                probable[i] /= sum;
                Debug.WriteIf(gra.EdgesM[cur, i] != 0, $"| {i} : {probable[i]:F2} ");
            }
            Debug.Write($"| = {sum:F2}\n");

            return probable;
        }

        // Реальный выбор
        int SelectNextVertex(double[] prob) {
            double rVal = new Random().NextDouble();
            double cumulProb = 0;

            for (int i = 0; i < prob.Length; i++)
            {
                cumulProb += prob[i];
                if (rVal <= cumulProb)
                    return i;
            }

            return -1;
        }

        // Распределение феромонов
        void DepositPheromones(List<int> path, double dist)
        {
            double deposit = _antTau / dist;

            for (int i = 0; i < path.Count() - 1; i++)
            {
                gra.PheromonsM[path[i], path[i+1]] += deposit;
            }
        }

        //Выветривание феромонов
        void EvaporatePheromones()
        {
            for (int i = 0; i < gra.Vertices.Count; i++)
                for (int j = 0; j < gra.Vertices.Count; j++)
                    gra.PheromonsM[i, j] *= (1 - _P);
        }

        // Путешествие отдельно взятого муравья от и до
        (List<int> path, double distance) AntTripFromTo()
        {
            List<int> path = [_startPoint];
            int cur = _startPoint;
            while (cur != _finishPoint && path.Count() < gra.Vertices.Count() + 1 && cur != -1)
            {
                cur = ChooseNextVertex(cur, path);
                path.Add(cur);
            }

            Debug.Write($" path: {string.Join(" -> ",path)}" + "\n");

            if (cur == _finishPoint)
            {
                double dist = 0;

                for (int i = 1; i < path.Count; i++)
                    dist += gra.EdgesM[path[i - 1], path[i]];

                DepositPheromones(path, dist);
                Debug.Write($" distance {dist}\n");
                return (path, dist);
            }
            return (path, -1);
        }

        // Одна итерация алгоритма по всем муравьям
        public void AntTravelStep(bool salesman)
        {
            if ( Iter > _iterCount) {
                IsRunning = false;
                Iter = 0;
                return; 
            }

            if (Paths.Count > 0)
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => Paths.Clear()));
            
            double optTmp = double.MaxValue;
            var newPaths = new List<PathRow>();

            for (int i = 0; i < _antCount; i++){
                Debug.Write($"Ant: {i} || iter: {_iter + 1}\n");
                List<int> curPath; double curDist;
                if (!salesman)  (curPath, curDist) = AntTripFromTo();
                else            (curPath, curDist) = AntTripSalesman(1);
               
                if (curDist > 0 && curDist < optTmp) {
                    optTmp = curDist;
                    gra.optPath = new List<int>(curPath);
                }

                for (int j = 0; j < curPath.Count; j++) curPath[j]++;
                newPaths.Add(new PathRow(i, string.Join(" -> ", curPath), curDist));
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var path in newPaths) Paths.Add(path);
            });

            OptDist = optTmp;
            if (optTmp == double.MaxValue)
                gra.optPath = new List<int>();
            else 
                _optD.Add(optTmp);
            
            EvaporatePheromones();

            Debug.Write($"| iter: {++Iter}\n" + $"| optimal path: {string.Join(" -> ", gra.optPath)}" + "\n");
            Debug.Write($"| optimal distance on iter: {_optDist}\n");
        }

        // Путешествие отдельно взятого муравья по всем точкам
        (List<int> path, double distance) AntTripSalesman(int start)
        {
            List<int> path = [start];
            int cur = start;

            // Посещаем все вершины
            while (path.Count < gra.Vertices.Count)
            {
                cur = ChooseNextVertex(cur, path);
                if (cur == -1) // Если следующая вершина не найдена
                {
                    return (path, -1); // Некорректный путь
                }
                path.Add(cur);
            }

            // Возвращаемся в начальную вершину
            path.Add(start);

            double dist = 0;
            for (int i = 1; i < path.Count; i++)
            {
                dist += gra.EdgesM[path[i - 1], path[i]];
            }

            DepositPheromones(path, dist);

            Debug.Write($" path: {string.Join(" -> ", path)}" + "\n");
            Debug.Write($" distance {dist}\n");

            return (path, dist);
        }

        #endregion

        public void LogOptimalDistanceArchive()
        {
            Debug.WriteLine($"\ndistances: {string.Join("\n", _optD)}");
        }
    }

    #region Классы для десериализации
    public class Vertex
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Name { get; set; }
        public int Radius { get; set; }
        public string Background { get; set; }
        public int FontSize { get; set; }
        public string Color { get; set; }
        public string Border { get; set; }
    }

    public class Edge
    {
        public int Vertex1 { get; set; }
        public int Vertex2 { get; set; }
        public string Weight { get; set; }
        public bool IsDirected { get; set; }
        public int ControlStep { get; set; }
        public int FontSize { get; set; }
        public int LineWidth { get; set; }
        public string Background { get; set; }
        public string Color { get; set; }
    }

    public class GraphData
    {
        public double X0 { get; set; }
        public double Y0 { get; set; }
        public List<Vertex> Vertices { get; set; }
        public List<Edge> Edges { get; set; }
        public List<object> Texts { get; set; }
    }
    #endregion
    #region Генерация точек
    static class PointsGenerator
    {
        private static Random random = new Random();

        /// <summary>
        /// Генерация точек с помощью простого ГПСЧ
        /// </summary>
        /// <param name="N">кол-во точек</param>
        /// <param name="W">ширина поверхности</param>
        /// <param name="H">высота поверхности</param>
        /// <returns></returns>
        static public List<Point> SimpleRandGenerate(int N, int W, int H)
        {
            List<Point> points = new List<Point>();

            for (int i = 0; i < N; i++)
            {
                int x = random.Next() % W;
                int y = random.Next() % H;

                points.Add(new Point(x, y));
            }

            return points;
        }

        /// <summary>
        /// Генерация точек с помощью сетки
        /// </summary>
        /// <param name="N">кол-во точек</param>
        /// <param name="W">ширина поверхности</param>
        /// <param name="H">высота поверхности</param>
        /// <returns></returns>
        static public List<Point> GridRandGenerate(int N, int W, int H)
        {
            List<Point> points = new List<Point>();
            // Соотношение сторон плоскости
            double ratio = W / H;

            // Начальное приближение для k
            int k = (int)Math.Floor(Math.Sqrt(N * ratio));
            int m = (int)Math.Ceiling((double)N / k);

            // проверка главного условия: k * m >= N
            while (k * m < N)
            {
                m = (int)Math.Ceiling((double)N / ++k);
            }

            k = k < 2 ? 2 : k;
            // Шаги для сетки
            double deltaX = W / (k - 1);
            double deltaY = H / (m - 1);

            // Генерация узлов
            int z = 0;
            for (int i = 0; i < k; i++)
                for (int j = 0; j < m; j++)
                {
                    if (z >= N) break; // Остановимся, если достигли N узлов
                    double x = i * deltaX;
                    double y = j * deltaY;

                    points.Add(new Point((int)x + 25, (int)y + 25));
                    z++;
                }

            //Случайное смещение вершины, в радиусе r
            double r = Math.Min(deltaY, deltaX) / 2.2;
            for (int i = 0; i < N; i++)
            {
                double angle = random.NextDouble() * 2 * Math.PI;
                double rad = random.NextDouble() * r;
                double dx = rad * Math.Sin(angle);
                double dy = rad * Math.Cos(angle);

                Point p = points[i];
                Point np = new Point((int)dx + p.X, (int)dy + p.Y);
                points[i] = np;
            }

            return points;
        }
        
        /// <summary>
        /// Генерация точек с минимальным расстоянием между ними, проверка с помощью Quad дерева
        /// </summary>
        /// <param name="N">кол-во точек</param>
        /// <param name="W">ширина плоскости</param>
        /// <param name="H">высота плоскости</param>
        /// <param name="r">минимальное раст.</param>
        /// <param name="maxAtt">кол-во попыток на создание точки</param>
        /// <returns></returns>
        static public List<Point> QDRangeGenerate(int N, int W, int H, double r, int maxAtt = 1000)
        {
            var points = new List<Point>();
            var QDTree = new QuadTree(new QuadTree.Rectangle(0,0,W,H));
            int att = 0;

            while (points.Count < N && att < maxAtt)
            {
                double x = random.NextDouble() * W;
                double y = random.NextDouble() * H;

                var range = new QuadTree.Rectangle(x - r, y - r, 2 * r, 2 * r);
                var nearbyPoints = QDTree.Query(range);

                bool isValid = true;
                foreach (var point in nearbyPoints)
                {
                    double dx = point.X - x;
                    double dy = point.Y - y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);

                    if (dist < r)
                    {
                        isValid = false; break;
                    }
                }

                if (isValid)
                {
                    var p = new Point((int)x, (int)y);
                    points.Add(p);
                    QDTree.Insert(p);
                    att = 0;
                }
                else att++;
            }

            if (points.Count < N)
            {
                Debug.WriteLine($"WARRNING. удалось разместить только {points.Count} из {N}");
            }
            return points;
        }
    }

    // Чисто прикол
    // квадродерево, позволяет отслеживать всякое разное на плоскости, говорят крутое
    public class QuadTree
    {
        const int _capacity = 4;
        
        //public class Point
        //{
        //    public int X { get; }
        //    public int Y { get; }

        //    public Point(int x, int y)
        //    {
        //        X = x;
        //        Y = y;
        //    }
        //}

        public class Rectangle
        {
            public double X { get; }
            public double Y { get; }
            public double Width { get; }
            public double Height { get; }

            public Rectangle(double x, double y, double width, double height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public bool Contains(Point p)
            {
                return p.X >= X && p.X <= X + Width && p.Y >= Y && p.Y <= Y + Height;
            }

            public bool Intersects(Rectangle r) 
            {
                return r.X <= X + Width && r.X + r.Width >= X && 
                       r.Y <= Y + Height && r.Y + r.Height >= Y;
            }
        }

        private Rectangle boundary;
        private List<Point> points;
        private QuadTree[] children;

        public QuadTree(Rectangle b)
        {
            boundary = b;
            points = new List<Point>();
            children = new QuadTree[_capacity];
        }

        public bool Insert(Point p)
        {
            if (!boundary.Contains(p)) return false;

            if (points.Count < _capacity)
            {
                points.Add(p);
                return true;
            }

            if (children[0] == null) Subdivide();

            for (int i = 0; i < _capacity; i++)
                if (children[i].Insert(p)) return true;
            

            return false;
        }

        private void Subdivide()
        {
            double x = boundary.X;
            double y = boundary.Y;
            double w = boundary.Width / 2;
            double h = boundary.Height / 2;

            children[0] = new QuadTree(new Rectangle(x,y, w, h));
            children[1] = new QuadTree(new Rectangle(x + w, y, w, h));
            children[2] = new QuadTree(new Rectangle(x, y + h, w, h));
            children[3] = new QuadTree(new Rectangle(x + w, y + h, w, h));
        }

        public List<Point> Query(Rectangle range)
        {
            var found = new List<Point>();

            if (!boundary.Intersects(range))
                return found;

            foreach (var point in points)
                if (range.Contains(point)) found.Add(point);

            if (children[0] != null)
                for (int i = 0; i < _capacity; i++)
                    found.AddRange(children[i].Query(range));

            return found;
        }
    }
    #endregion
}
