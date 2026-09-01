using SQLite;

namespace ButlerDroid.Services;

public static class Database
{
	private static SQLiteAsyncConnection? _connection;

	public static SQLiteAsyncConnection Connection => _connection
		?? throw new InvalidOperationException("Database has not been initialized.");

	public static async Task InitializeAsync()
	{
		if (_connection is not null)
			return;

		SQLitePCL.Batteries_V2.Init();
		var path = Path.Combine(FileSystem.AppDataDirectory, "butlerdroid.db3");
		_connection = new SQLiteAsyncConnection(path);
		await _connection.CreateTableAsync<ButlerDroid.Core.Models.ScheduledTask>();
		await EnsureIntervalColumnsAsync();
		await EnsureTaskKeyColumnAsync();
	}

	private static async Task EnsureIntervalColumnsAsync()
	{
		const string intervalColumn = "IntervalSeconds";
		const string anchorColumn = "AnchorAtUnixMs";

		await TryAddColumnAsync(intervalColumn, "INTEGER NOT NULL DEFAULT 0");
		await TryAddColumnAsync(anchorColumn, "INTEGER NOT NULL DEFAULT 0");
	}

	private static async Task EnsureTaskKeyColumnAsync()
	{
		await TryAddColumnAsync("TaskKey", "TEXT NULL");
		await Connection.ExecuteAsync(
			"UPDATE ScheduledTask SET TaskKey = '' WHERE TaskKey IS NULL");

		var tasks = await Connection.QueryAsync<ButlerDroid.Core.Models.ScheduledTask>(
			"SELECT * FROM ScheduledTask WHERE TaskKey IS NULL OR TaskKey = ''");

		foreach (var task in tasks)
		{
			task.TaskKey = Guid.NewGuid().ToString("N");
			await Connection.UpdateAsync(task);
		}
	}

	private static async Task TryAddColumnAsync(string columnName, string definition)
	{
		try
		{
			await Connection.ExecuteAsync(
				$"ALTER TABLE ScheduledTask ADD COLUMN {columnName} {definition}");
		}
		catch (SQLiteException)
		{
			// The column already exists in databases created by a newer version.
		}
	}
}
