using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;

namespace AuntAlgorithm
{
    public class Arrow : Shape
    {
        // Зависимые свойства для начальной и конечной точек
        public static readonly DependencyProperty StartPointProperty =
            DependencyProperty.Register("StartPoint", typeof(Point), typeof(Arrow),
                new FrameworkPropertyMetadata(new Point(0, 0), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty EndPointProperty =
            DependencyProperty.Register("EndPoint", typeof(Point), typeof(Arrow),
                new FrameworkPropertyMetadata(new Point(100, 100), FrameworkPropertyMetadataOptions.AffectsRender));
        // Зависимое свойство для расположения наконечника
        public static readonly DependencyProperty ArrowHeadPositionProperty =
            DependencyProperty.Register("ArrowHeadPosition", typeof(double), typeof(Arrow),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        // Свойства для удобства
        public Point StartPoint
        {
            get => (Point)GetValue(StartPointProperty);
            set => SetValue(StartPointProperty, value);
        }
        public Point EndPoint
        {
            get => (Point)GetValue(EndPointProperty);
            set => SetValue(EndPointProperty, value);
        }
        public double ArrowHeadPosition
        {
            get => (double)GetValue(ArrowHeadPositionProperty);
            set => SetValue(ArrowHeadPositionProperty, value);
        }
        // Переопределение метода для создания геометрии стрелки
        protected override Geometry DefiningGeometry
        {
            get
            {
                Point aPoint = new Point(
                    StartPoint.X + (EndPoint.X - StartPoint.X) * ArrowHeadPosition,
                    StartPoint.Y + (EndPoint.Y - StartPoint.Y) * ArrowHeadPosition);

                LineGeometry line = new LineGeometry(StartPoint, EndPoint);

                double arrowLength = 10; // Длина наконечника
                double arrowAngle = 30; // Угол наконечника в градусах

                // Векторы для направления наконечника
                Vector direction = EndPoint - StartPoint;
                direction.Normalize();
                Vector normal = new Vector(-direction.Y, direction.X);

                Point aPoint1 = aPoint - direction * arrowLength + normal * arrowLength * Math.Tan(Math.PI * arrowAngle / 180);
                Point aPoint2 = aPoint - direction * arrowLength - normal * arrowLength * Math.Tan(Math.PI * arrowAngle / 180);

                PathFigure arrowFigure = new PathFigure
                {
                    StartPoint = aPoint,
                    IsClosed = true,
                    IsFilled = true
                };
                arrowFigure.Segments.Add(new LineSegment(aPoint1, true));
                arrowFigure.Segments.Add(new LineSegment(aPoint2, true));

                PathGeometry arrowGeometry = new PathGeometry();
                arrowGeometry.Figures.Add(arrowFigure);

                // Объединяем линию и наконечник
                GeometryGroup group = new GeometryGroup();
                group.Children.Add(line);
                group.Children.Add(arrowGeometry);

                return group;
            }
        }
    }
}
