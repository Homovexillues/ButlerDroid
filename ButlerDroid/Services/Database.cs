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
	}

	private static async Task EnsureIntervalColumnsAsync()
	{
		const string intervalColumn = "IntervalSeconds";
		const string anchorColumn = "AnchorAtUnixMs";

		await TryAddColumnAsync(intervalColumn);
		await TryAddColumnAsync(anchorColumn);
	}

	private static async Task TryAddColumnAsync(string columnName)
	{
		try
		{
			await Connection.ExecuteAsync(
				$"ALTER TABLE ScheduledTask ADD COLUMN {columnName} INTEGER NOT NULL DEFAULT 0");
		}
		catch (SQLiteException)
		{
			// The column already exists in databases created by a newer version.
		}
	}
}
