namespace ButlerDroid.Core.Scheduling;

public sealed class OffsetSchedule : ISchedule
{
	private readonly ISchedule _base;
	private readonly TimeSpan[] _offsets;

	public OffsetSchedule(ISchedule baseSchedule, IEnumerable<TimeSpan> offsets)
	{
		_base = baseSchedule;
		_offsets = offsets.ToArray();
		if (_offsets.Length == 0)
			throw new ArgumentException("At least one offset is required.", nameof(offsets));
	}

	public DateTimeOffset? NextAfter(DateTimeOffset since)
	{
		DateTimeOffset? earliest = null;
		foreach (var offset in _offsets)
		{
			var next = _base.NextAfter(since.Subtract(offset));
			if (next is null)
				continue;

			var candidate = next.Value.Add(offset);
			if (earliest is null || candidate < earliest.Value)
				earliest = candidate;
		}

		return earliest;
	}
}
