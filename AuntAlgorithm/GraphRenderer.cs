using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Drawing;
using System.Diagnostics;

namespace AuntAlgorithm
{
    class GraphViewModel : INotifyPropertyChanged
    {
        Graph graph;
        Canvas _canvas;
        Dictionary<int, Point> normVert;

        public GraphViewModel(Graph graph, Canvas canvas)
        {
            this.graph = graph;
            normVert = new Dictionary<int, Point>(graph.Vertices);
            _canvas = canvas;
        }

        // Свойство для нормализованных вершин
        public Dictionary<int, Point> NormalizedVertices
        {
            get => normVert;
            private set
            {
                normVert = value;
                OnPropertyChanged(nameof(NormalizedVertices));
            }
        }

        // Метод для нормализации координат
        public void NormalizeCoordinates(double canvasWidth, double canvasHeight)
        {
            if (graph.Vertices.Count == 0)
                return;

            // Находим минимальные и максимальные координаты
            double minX = graph.Vertices.Values.Min(v => v.X);
            double maxX = graph.Vertices.Values.Max(v => v.X);
            double minY = graph.Vertices.Values.Min(v => v.Y);
            double maxY = graph.Vertices.Values.Max(v => v.Y);

            // Вычисляем масштабные коэффициенты
            double scaleX = canvasWidth  / (1.5 * (maxX - minX));
            double scaleY = canvasHeight / (1.5 * (maxY - minY));

            double minScale = Math.Min(scaleY, scaleX);

            // Нормализуем координаты
            var normalized = new Dictionary<int, Point>();
            foreach (var vertex in graph.Vertices)
            {
                double x = (vertex.Value.X + 100 - minX) * minScale;
                double y = (vertex.Value.Y + 75 - minY) * minScale;
                normalized[vertex.Key] = new Point((int)x, (int)y);
            }

            normVert = normalized;
        }

        public void LogNormVert()
        {
            foreach (var vertex in normVert)
                Debug.WriteLine($"Н Вершина {vertex.Key}: ({vertex.Value.X}, {vertex.Value.Y})");
        }

        // Реализация INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void UpdateGraph(Graph newGraph)
        {
            graph = newGraph;
            NormalizeCoordinates(_canvas.ActualWidth, _canvas.ActualHeight);
            Render();
        }

        public void Render()
        {
            _canvas.Children.Clear();
            DrawEdges();
            DrawNodes();
        }

        void DrawNodes()
        {
            for (int i = 0; i < graph.Vertices.Count; i++)
            {
                var ell = new Ellipse
                {
                    Width = 25,
                    Height = 25,
                    Fill = Brushes.White,
                    Stroke = graph.optPath.Contains(i) ? Brushes.Green : Brushes.Black,
                    StrokeThickness = 2
                };
                Canvas.SetLeft(ell, normVert[i].X);
                Canvas.SetTop(ell, normVert[i].Y);
                _canvas.Children.Add(ell);

                // Сопутствующий текст
                var text = new TextBlock
                {
                    Text = (i+1).ToString(),
                    Foreground = Brushes.Black,
                    FontSize = 14,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                };
                Canvas.SetLeft(text, normVert[i].X + ((i > 9) ? 4 : 9));
                Canvas.SetTop(text, normVert[i].Y + 2);
                _canvas.Children.Add(text);
            }
        }

        void DrawEdges()
        {
            for (int i = 0; i < graph.Vertices.Count; i++)
            {
                for (int j = 0; j < graph.Vertices.Count; j++)
                {
                    if (graph.EdgesM[i, j] != 0)
                    {
                        System.Windows.Point start = new System.Windows.Point(normVert[i].X + 12, normVert[i].Y + 12);
                        System.Windows.Point end = new System.Windows.Point(normVert[j].X + 12, normVert[j].Y + 12);

                        bool isOptimalEdge = false;
                        for (int k = 0; k < graph.optPath.Count - 1; k++)
                        {
                            if (i == graph.optPath[k] && j == graph.optPath[k + 1])
                            {
                                isOptimalEdge = true;
                                break;
                            }
                        }

                        var arr = new Arrow
                        {
                            StartPoint = start,
                            EndPoint = end,
                            StrokeThickness = 2,
                            Stroke = isOptimalEdge ? Brushes.Green : Brushes.Black,
                            Fill =   isOptimalEdge ? Brushes.Green : Brushes.Black,
                            ArrowHeadPosition = 0.8
                        };
                        _canvas.Children.Add(arr);

                        // Сопутствующий текст (длина ребра : кол-во феромонов)
                        System.Windows.Point mid = new System.Windows.Point((start.X+end.X)/2, (start.Y+end.Y)/2);
                        var text = new TextBlock
                        {
                            Text = $"{graph.EdgesM[i,j]:F1}\n{graph.PheromonsM[i,j]:F1}",
                            Foreground = Brushes.Green,
                            FontSize = 14,
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                        };
                        Canvas.SetLeft(text, mid.X);
                        Canvas.SetTop(text, mid.Y);
                        _canvas.Children.Add(text);
                    }
                }
            }
        }
    }
}
