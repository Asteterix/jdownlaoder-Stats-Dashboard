using System.Globalization;

namespace jdownloader_Stats.Services;

public static class ByteFormatter
{
	private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

	public static string Format(double bytes, string suffix = "")
	{
		var value = Math.Max(0, bytes);
		var unit = 0;
		while (value >= 1024 && unit < Units.Length - 1)
		{
			value /= 1024;
			unit++;
		}

		var format = unit == 0 ? "0" : "0.0";
		return $"{value.ToString(format, CultureInfo.CurrentCulture)} {Units[unit]}{suffix}";
	}
}
