using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
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
        public int StartPoint { get; set; }
        public int FinishPoint { get; set; }

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
            get => edgesM;
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
                    Console.WriteLine("Ошибка считывания вершин!");
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
            }
            else
            {
                Console.WriteLine("File not exist");
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
            Debug.Write("\n");
            for (int i = 0; i < Vertices.Count; i++) { 
                for (int j = 0; j < Vertices.Count; j++) {
                    Debug.Write($"{PheromonsM[i,j]} ");
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

    class AntAl{
        
        Graph gra { get; set; }

        int _antCount;    //количество муравьев,
        int _iterCount;   //количество итераций алгоритма,
        double _tau0;        //начальное количество феромона на дугах графа,
        int _antTau;      //запас феромона каждого муравья,
        double _P;        //коэффициент испарения,
        int _alpha;       //α - эмпирические коэффициенты
        int _beta;        //β – эмпирические коэффициенты

        bool _isRunning;    // Флаг запущенного алгоритма

        public AntAl(Graph graph, int antCount = 1, int iterCount = 1, double tau0 = 10, int antTau = 10, double P = 1, int alpha = 1, int beta = 1) {
            gra = graph; 
            _antCount = antCount;
            _iterCount = iterCount;
            _tau0 = tau0;   
            _antTau = antTau;
            _P = P;
            _alpha = alpha;
            _beta = beta; 
        }

        // Выбор следующей вершины
        int ChooseNextVertex(int cur) {
            double[] prod = CalcProbable(cur);
            int next = SelectNextVertex(prod);

            return next;
        }

        // Подсчёт вероятностей
        double[] CalcProbable(int cur) {

            int count = gra.Vertices.Count;
            double[] probable = new double[count];
            double sum = 0;

            for (int i = 0; i < count; i++)
            {
                if (gra.EdgesM[cur, i] == 0) { continue; }
                double pher = gra.PheromonsM[cur, i];
                double vis = 1.0 / gra.EdgesM[cur,i];

                probable[i] = Math.Pow(pher, _alpha) * Math.Pow(vis, _beta);
                sum += probable[i];
            }

            for (int i = 0; i < count; i++) { 
                probable[i] /= sum;
                Debug.WriteIf(gra.EdgesM[cur, i] != 0, $"| {i} : {probable[i]:F2} ");
            }
            Debug.Write($"| = {sum}\n");

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

        void DepositPheromones(List<int> path )
        {
            double deposit = _antTau / path.Count();

            for (int i = 0; i < path.Count() - 1; i++)
            {
                gra.PheromonsM[path[i], path[i+1]] += deposit;
            }
        }

        public (List<int> path, double distance) AntTrip()
        {
            List<int> path = [gra.StartPoint];
            int cur = gra.StartPoint;
            double distance = 0;
            while (cur != gra.FinishPoint && path.Count() < gra.Vertices.Count() + 1 && cur != -1)
            {
                cur = ChooseNextVertex(cur);
                path.Add(cur);
            }

            Debug.Write($" path: {string.Join(" -> ",path)}");
            Debug.Write($" distance {distance}\n");
            

            if (cur == gra.FinishPoint)
            {
                DepositPheromones(path);
                return (path, distance);
            }
            return (path, -1);
        }
    }
}
