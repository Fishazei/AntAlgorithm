using System.Diagnostics;
using System.Windows;

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
        PathsGrid.ItemsSource = _antAl.Paths;
        DataContext = _antAl;
    }

    private void LoadGraph_Click(object sender, RoutedEventArgs e)
    {
        _gVM.M = Mode.Graph;
        _graph.ParseFromFile("graph.graph");
        _graph.InitPheromones(_antAl.Tau0);
        _antAl.IsRunning = false;
        _antAl.ZeroIter();
        _gVM.UpdateGraph(_graph);
    }

    private void Step_Click(object sender, RoutedEventArgs e)
    {
        if (_gVM.M == Mode.Comivoiaj) return;
        _antAl.IsRunning = true;
        _antAl.AntTravelStep(false);
        _gVM.Render();
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (_gVM.M == Mode.Comivoiaj) return;
        _antAl.IsRunning = true;

        
        await Task.Run(async () =>
        {
            while (_antAl.IsRunning)
            {
                _antAl.AntTravelStep(false);

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
        _graph.GenerateComivoiaj(_antAl.PC, (int)GraphCanvas.ActualWidth, (int)GraphCanvas.ActualHeight);
        _gVM.M = Mode.Comivoiaj;
        _graph.InitPheromones(_antAl.Tau0);
        _gVM.UpdateGraph(_graph);
        _gVM.NormalizeCoordinates(GraphCanvas.ActualWidth, GraphCanvas.ActualHeight);
        _gVM.LogNormVert();
        _gVM.Render();
    }

    private async void Solve_Click(object sender, RoutedEventArgs e)
    {
        if (_gVM.M == Mode.Graph) return;
        _antAl.IsRunning = true;

        await Task.Run(async () =>
        {
            while (_antAl.IsRunning)
            {
                _antAl.AntTravelStep(true);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _gVM.Render();
                });
                await Task.Delay(1000);
            }
        });

        _antAl.LogOptimalDistanceArchive();
    }
}
