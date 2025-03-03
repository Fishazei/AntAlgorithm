using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AuntAlgorithm
{
    class Graph
    {
        // Матрицы ребер и феремонов на них
        double[,] edgesM;
        public double[,] PheromonsM;

        // Вершины
        Dictionary<int, Point> vertices;
        public int StartPoint { get; set; }
        public int FinishPoint { get; set; }

        public Dictionary<int, Point> Vertices
        {
            get => new Dictionary<int, Point>(vertices);
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
                    PropertyNameCaseInsensitive = true, // Игнорировать регистр свойств
                    ReadCommentHandling = JsonCommentHandling.Skip, // Пропускать комментарии
                    AllowTrailingCommas = true // Разрешать запятые в конце
                };

                string gstring = File.ReadAllText(Path);
                GraphData graph = JsonSerializer.Deserialize<GraphData>(gstring, options);

                if (graph.Vertices == null)
                {
                    Console.WriteLine("Ошибка считывания вершин!");
                    return false;
                }

                vertices = new Dictionary<int, Point>();
                for (int i = 0; i < graph.Vertices.Count(); i++)
                {
                    vertices[i] = new Point(graph.Vertices[i].X, graph.Vertices[i].Y);
                    
                }
                edgesM = new double[graph.Vertices.Count(), graph.Vertices.Count()];
                foreach (var edge in graph.Edges)
                {
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
        
        Graph gra;

        int antCount;    //количество муравьев,
        int iterCount;   //количество итераций алгоритма,
        int tau0;        //начальное количество феромона на дугах графа,
        int antTau;      //запас феромона каждого муравья,
        double P;        //коэффициент испарения,
        int alpha;       //α - эмпирические коэффициенты
        int beta;        //β – эмпирические коэффициенты

    }
}
