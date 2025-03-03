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

namespace AuntAlgorithm;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        Graph graph = new Graph();
        
        graph.ParseFromFile("C:\\Users\\fisha\\source\\repos\\Test\\Test\\graph (2).graph");
        InitializeComponent();

        GraphViewModel gVM = new GraphViewModel(graph, GraphCanvas);

        Debug.WriteLine($"Wat: {GraphCanvas.Width}, {GraphCanvas.Height}");
        gVM.NormalizeCoordinates(GraphCanvas.Width, GraphCanvas.Height);

        GraphCanvas.SizeChanged += (s, e) =>
        {
            gVM.NormalizeCoordinates(GraphCanvas.ActualWidth, GraphCanvas.ActualHeight);
            Debug.WriteLine($"Canvas size: {GraphCanvas.ActualWidth}, {GraphCanvas.ActualHeight})");

            gVM.Render();
        };

        gVM.Render();
        
        //gRender.Render();

    }
}