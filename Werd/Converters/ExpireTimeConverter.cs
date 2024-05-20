using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.UI.Xaml.Data;

namespace Werd.Converters
{
	public class ExpireTimeConverter : IValueConverter
	{
		private const int Minute = 60;
		private const int Hour = Minute * 60;
		private const int Day = Hour * 24;
		private const int Year = Day * 365;

		private readonly Dictionary<long, Func<TimeSpan, string>> _thresholds = new Dictionary<long, Func<TimeSpan, string>>
	{
		{2, t => "one second"},
		{Minute,  t => String.Format(CultureInfo.InvariantCulture, "{0} seconds", (int)t.TotalSeconds)},
		{Minute * 2,  t => "a minute"},
		{Hour,  t => String.Format(CultureInfo.InvariantCulture, "{0} minutes", (int)t.TotalMinutes)},
		{Hour * 2,  t => "one hour"},
		{Day,  t => String.Format(CultureInfo.InvariantCulture, "{0} hours", (int)t.TotalHours)},
		{Day * 2,  t => "one day"},
		{Day * 30,  t => String.Format(CultureInfo.InvariantCulture, "{0} days", (int)t.TotalDays)},
		{Day * 60,  t => "one month"},
		{Year,  t => String.Format(CultureInfo.InvariantCulture, "{0} months", (int)t.TotalDays / 30)},
		{Year * 2,  t => "one year"},
		{Int64.MaxValue,  t => String.Format(CultureInfo.InvariantCulture, "{0} years", (int)t.TotalDays / 365)}
	};

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var dateTime = (DateTime)value;

			TimeSpan difference;
			string trail = " ago";
			var expireTime = dateTime.AddHours(24).ToUniversalTime();
			if (expireTime > DateTime.UtcNow)
			{
				difference = expireTime - DateTime.UtcNow;
				trail = " left";
			}
			else
			{
				difference = DateTime.UtcNow - dateTime.ToUniversalTime();
			}
			return _thresholds.First(t => difference.TotalSeconds < t.Key).Value(difference) + trail;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
