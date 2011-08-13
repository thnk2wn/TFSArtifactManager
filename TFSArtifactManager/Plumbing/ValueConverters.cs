using System;
using System.Windows.Data;

namespace TFSArtifactManager.Plumbing
{
    internal class StringIntValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: another situation that makes me wonder about the F# or functional programming solution
            int convertedNumber = 0;

            if (string.IsNullOrWhiteSpace(value.ToString()))
                convertedNumber = 0;
            else
            {
                int parsedNumber = 0;

                if (int.TryParse(value.ToString(), out parsedNumber))
                    convertedNumber = parsedNumber;
            }

            return convertedNumber;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToString();
        }
    }
}
