namespace ButlerDroid.Core.Scheduling;

public interface ISchedule
{
	DateTimeOffset? NextAfter(DateTimeOffset since);
}
