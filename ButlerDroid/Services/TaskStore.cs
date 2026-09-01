using ButlerDroid.Core.Models;
using SQLite;

namespace ButlerDroid.Services;

public static class TaskStore
{
	public static async Task<List<ScheduledTask>> GetAllAsync()
	{
		await Database.InitializeAsync();
		return await Database.Connection.Table<ScheduledTask>()
			.OrderBy(t => t.Id)
			.ToListAsync();
	}

	public static async Task<ScheduledTask?> GetAsync(int id)
	{
		await Database.InitializeAsync();
		return await Database.Connection.FindAsync<ScheduledTask>(id);
	}

	public static async Task<ScheduledTask?> GetByKeyAsync(string taskKey)
	{
		await Database.InitializeAsync();
		if (string.IsNullOrWhiteSpace(taskKey))
			return null;

		return await Database.Connection.Table<ScheduledTask>()
			.Where(t => t.TaskKey == taskKey)
			.FirstOrDefaultAsync();
	}

	public static async Task<int> SaveAsync(ScheduledTask task)
	{
		await Database.InitializeAsync();
		var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		task.UpdatedAtUnixMs = now;
		if (string.IsNullOrWhiteSpace(task.TaskKey))
			task.TaskKey = Guid.NewGuid().ToString("N");

		if (task.Id == 0)
		{
			task.CreatedAtUnixMs = now;
			await Database.Connection.InsertAsync(task);
		}
		else
		{
			await Database.Connection.UpdateAsync(task);
		}

		return task.Id;
	}

	public static async Task DeleteAsync(int id)
	{
		await Database.InitializeAsync();
		await Database.Connection.DeleteAsync<ScheduledTask>(id);
	}
}
