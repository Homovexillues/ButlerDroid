using System.Globalization;

namespace ButlerDroid.Core.Scheduling;

public static class ScheduleOffsetParser
{
	public static TimeSpan Parse(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			throw new FormatException("Offset cannot be empty.");

		var normalized = value.Replace(" ", "", StringComparison.Ordinal);
		if (!normalized.StartsWith("T-", StringComparison.Ordinal))
			throw new FormatException("Offset must start with T-.");

		var body = normalized[2..];
		if (body.Length < 2)
			throw new FormatException("Offset is missing a duration.");

		var unit = body[^1];
		var numberText = body[..^1];
		if (!int.TryParse(numberText, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
			throw new FormatException("Offset duration is not a valid integer.");

		var duration = unit switch
		{
			'd' => TimeSpan.FromDays(number),
			'h' => TimeSpan.FromHours(number),
			'm' => TimeSpan.FromMinutes(number),
			_ => throw new FormatException("Offset unit must be d, h, or m."),
		};

		return duration.Negate();
	}
}
