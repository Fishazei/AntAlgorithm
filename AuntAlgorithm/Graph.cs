using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

    class AntAl : INotifyPropertyChanged
    {
        
        Graph gra { get; set; }

        int _antCount;    //количество муравьев,
        int _iterCount;   //количество итераций алгоритма,
        double _tau0;        //начальное количество феромона на дугах графа,
        int _antTau;      //запас феромона каждого муравья,
        double _P;        //коэффициент испарения,
        int _alpha;       //α - эмпирические коэффициенты
        int _beta;        //β – эмпирические коэффициенты

        bool _isRunning;    // Флаг запущенного алгоритма
        int _iter;

        private int _startPoint;
        private int _finishPoint;

        double _optDist;

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
                //Debug.WriteIf(gra.EdgesM[cur, i] != 0, $"| {i} : {probable[i]:F2} ");
            }
            //Debug.Write($"| = {sum:F2}\n");

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

        // Путешествие отдельно взятого муравья
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
        public void AntTravelStep()
        {
            if ( Iter > _iterCount) {
                IsRunning = false;
                Iter = 0;
                return; 
            }
            double optTmp = double.MaxValue;
            for (int i = 0; i < _antCount; i++){
                Debug.Write($"Ant: {i} || iter: {_iter + 1}\n");
                (List<int> curPath, double curDist) = AntTripFromTo();
                if (curDist > 0 && curDist < optTmp) {
                    optTmp = curDist;
                    gra.optPath = curPath;
                }
            }
            OptDist = optTmp;
            if (optTmp == double.MaxValue)
            {
                gra.optPath = new List<int>();
            }
            EvaporatePheromones();

            Debug.Write($"| iter: {++Iter}\n" + $"| optimal path: {string.Join(" -> ", gra.optPath)}" + "\n");
            Debug.Write($"| optimal distance on iter: {_optDist}\n");
        }
        #endregion
    }
}
