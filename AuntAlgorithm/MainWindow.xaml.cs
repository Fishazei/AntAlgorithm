using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

// Допольнить:
// Для первой задачи:
// 1. Вывод кратчайшего пути снизу                                          X
// 2. Сбоку таблица путей всех муравьёв на итерации                         X
// 3. Табу лист для муравья (он не может посещать уже посещённую вершину)   V
// 4. Немного усложнить граф

namespace AuntAlgorithm;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    AntAl _antAl;
    Graph _graph;
    GraphViewModel _gVM;

    public MainWindow()
    {
        InitializeComponent();

        _graph = new Graph();
        _gVM = new GraphViewModel(_graph, GraphCanvas);

        //gVM.NormalizeCoordinates(GraphCanvas.Width, GraphCanvas.Height);

        GraphCanvas.SizeChanged += (s, e) =>
        {
            _gVM.NormalizeCoordinates(GraphCanvas.ActualWidth, GraphCanvas.ActualHeight);
            Debug.WriteLine($"Canvas size: {GraphCanvas.ActualWidth}, {GraphCanvas.ActualHeight})");

            _gVM.Render();
        };
        _graph.LogPheromones();

        _antAl = new AntAl(_graph, P: 0.1, antCount: 1, iterCount: 3);

        _antAl.FinishPoint = 13;
        _antAl.StartPoint = 0;

        // Установка контекста данных
        DataContext = _antAl;
    }

    private void LoadGraph_Click(object sender, RoutedEventArgs e)
    {
        _graph.ParseFromFile("C:\\Users\\fisha\\source\\repos\\AuntAlgorithm\\AuntAlgorithm\\graph.graph");
        _graph.InitPheromones(_antAl.Tau0);
        _antAl.IsRunning = false;
        _antAl.ZeroIter();
        _gVM.UpdateGraph(_graph);
    }

    private void Step_Click(object sender, RoutedEventArgs e)
    {
        _antAl.IsRunning = true;
        _antAl.AntTravelStep();
        _gVM.Render();
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        _antAl.IsRunning = true;

        
        await Task.Run(async () =>
        {
            while (_antAl.IsRunning)
            {
                _antAl.AntTravelStep();

                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _gVM.Render();
                });

                await Task.Delay(1000);
            }
        });
    }


    private void Generate_Click(object sender, RoutedEventArgs e)
    {

    }

    private void Solve_Click(object sender, RoutedEventArgs e)
    {

    }

}
