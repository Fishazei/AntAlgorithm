using System;
using System.Globalization;
using System.Windows.Data;

namespace AuntAlgorithm
{
    public class AddOneConverter : IValueConverter
    {
        // Преобразование из источника в цель (например, из StartPoint в StartPoint + 1)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int startPoint)
            {
                return startPoint + 1;
            }
            return value; // Если значение не int, возвращаем как есть
        }

        // Преобразование из цели в источник (например, из StartPoint + 1 обратно в StartPoint)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue && int.TryParse(strValue, out int result))
            {
                return result - 1; // Обратное преобразование
            }
            return value; // Если значение не int, возвращаем как есть
        }
    }
}
