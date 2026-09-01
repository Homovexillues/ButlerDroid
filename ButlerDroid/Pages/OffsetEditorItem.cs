using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ButlerDroid.Pages;

public sealed class OffsetEditorItem : INotifyPropertyChanged
{
	private string _value = "1";
	private string _unit = "天";

	public IReadOnlyList<string> Units { get; } = ["天", "小时", "分钟"];

	public string Value
	{
		get => _value;
		set
		{
			if (_value == value)
				return;
			_value = value;
			OnPropertyChanged();
		}
	}

	public string Unit
	{
		get => _unit;
		set
		{
			if (_unit == value)
				return;
			_unit = value;
			OnPropertyChanged();
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
