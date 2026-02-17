 using OuijaDesk.Contracts.Models;
using OuijaDesk.Application.DTO;
using System.Collections.ObjectModel;
using System.Windows.Input;
using OuijaDesk.Application.Contracts;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.ApplicationModel;
using OuijaDesk.Protocol.Encoding;

namespace OuijaDesk.App.ViewModels;

public class MainPageViewModel : INotifyPropertyChanged
{
    private readonly IDeviceClient _deviceClient;
    private readonly ISerialPortService _serialPortService;
	private SerialPortInfo? _selectedPort;
	private string _text = string.Empty;
	// Status messages collection (newest messages will be inserted at index 0)
	// and displayed in the UI with the newest on top.
	public ObservableCollection<string> StatusMessages { get; } = new();
	private DeviceStatusDto? _lastDeviceStatus;
	private TransferResultDto? _lastTransferResult;

	public event PropertyChangedEventHandler? PropertyChanged;

	public ICommand ScanPortsCommand { get; }
	public ICommand CheckStatusCommand { get; }
	public ICommand SendCommandCommand { get; }

	public ObservableCollection<SerialPortInfo> Ports { get; } = new();

	public SerialPortInfo? SelectedPort
	{
		get => _selectedPort;
		set
		{
			if (SetProperty(ref _selectedPort, value))
			{
				_ = CheckStatusAsync();
			}
		}
	}

	public string Text
	{
		get => _text;
		set
		{
			if (value == null)
			{
				SetProperty(ref _text, string.Empty);
				return;
			}

			// Filter to allow only numbers and Cyrillic letters, convert to uppercase
			var filtered = new string(value
				.Where(c => char.IsDigit(c) || (c >= 'А' && c <= 'я') || c == 'Ё' || c == 'ё')
				.Select(c => char.ToUpper(c))
				.ToArray());

			SetProperty(ref _text, filtered);
		}
	}

	// Adds a new status message to the collection (newest first).
	private void AddStatusMessage(string message)
	{
		// Ensure collection changes happen on the main thread for UI binding
		MainThread.BeginInvokeOnMainThread(() => StatusMessages.Insert(0, message));
	}

	// Returns a Russian description for the given command type
	private string GetCommandTypeDescription(byte commandType)
	{
		return commandType switch
		{
			0x00 => "Проверка статуса",
			0x01 => "Воспроизведение один раз",
			0x02 => "Повторное воспроизведение",
			0x03 => "Остановка",
			0x04 => "Да",
			0x05 => "Нет",
			_ => "Неизвестная команда"
		};
	}

	public DeviceStatusDto? LastDeviceStatus
	{
		get => _lastDeviceStatus;
		set => SetProperty(ref _lastDeviceStatus, value);
	}

	public TransferResultDto? LastTransferResult
	{
		get => _lastTransferResult;
		set => SetProperty(ref _lastTransferResult, value);
	}

    public MainPageViewModel(IDeviceClient deviceClient, ISerialPortService serialPortService)
    {
        _deviceClient = deviceClient;
        _serialPortService = serialPortService;
		ScanPortsCommand = new Command(async () => await ScanPortsAsync());
		CheckStatusCommand = new Command(async () => await CheckStatusAsync());
		SendCommandCommand = new Command<string>(async (cmd) => await SendCommandAsync(cmd));
	}

	private async Task ScanPortsAsync()
	{
		Ports.Clear();
		var availablePorts = _serialPortService.GetAvailablePorts();
		foreach (var port in availablePorts)
		{
			Ports.Add(port);
		}

		// Add a status message in Russian about the scan result
		var count = Ports.Count;
		if (count == 0)
		{
			AddStatusMessage("Порты не найдены");
		}
		else if (count == 1)
		{
			AddStatusMessage($"Найден 1 порт");
		}
		else if (count >= 2 && count <= 4)
		{
			AddStatusMessage($"Найдено {count} порта");
		}
		else
		{
			AddStatusMessage($"Найдено {count} портов");
		}

		await Task.CompletedTask;
	}

    private async Task CheckStatusAsync()
    {
        if (SelectedPort == null)
        {
            AddStatusMessage("Порт не выбран. Проверьте подключение.");
            return;
        }

        var portName = SelectedPort.PortName;
        LastDeviceStatus = await _deviceClient.CheckStatusAsync(portName);
        var msg = LastDeviceStatus != null ? LastDeviceStatus!.Message : "Не удалось получить статус устройства";
        AddStatusMessage(msg);
    }

	private async Task SendCommandAsync(string command)
	{
		if (!byte.TryParse(command, out byte commandType))
		{
			AddStatusMessage("Неверный формат команды");
			return;
		}

		// Do not send commands if device is not online
		if (LastDeviceStatus == null || !LastDeviceStatus.Online)
		{
			if (LastDeviceStatus == null)
			{
				AddStatusMessage("Не удалось получить статус устройства. Команда не отправлена");
			}
			else
			{
				AddStatusMessage("Устройство не подключено. Команда не отправлена");
			}
			return;
		}

		if ((commandType == Protocol.Constants.CommandType.PlayOnce || 
			 commandType == Protocol.Constants.CommandType.PlayRepeat) && 
			string.IsNullOrWhiteSpace(Text))
		{
			AddStatusMessage("Для команд воспроизведения необходимо указать текст");
			return;
		}

		byte[]? messageBytes = null;
		if (!string.IsNullOrWhiteSpace(Text))
		{
			try
			{
				messageBytes = TextEncoder.Encode(Text);
			}
			catch (ArgumentException ex)
			{
				AddStatusMessage($"Ошибка кодирования текста: {ex.Message}");
				return;
			}
		}

		var deviceCommand = new DeviceCommand
		{
			CommandType = commandType,
			Message = messageBytes
		};

        if (SelectedPort == null)
        {
            AddStatusMessage("Порт не выбран. Команда не отправлена");
            return;
        }

        var portName = SelectedPort.PortName;
        LastTransferResult = await _deviceClient.SendAsync(portName, deviceCommand);
        AddStatusMessage(LastTransferResult.Success ? $"Команда \"{GetCommandTypeDescription(commandType)}\" успешно отправлена" : $"Ошибка отправки команды: {LastTransferResult.Message}");
	}

	protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
			return false;

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
