using OuijaDesk.Contracts.Models;
using OuijaDesk.Application.DTO;
using System.Collections.ObjectModel;
using System.Windows.Input;
using OuijaDesk.Application.Contracts;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OuijaDesk.App.ViewModels;

public class MainPageViewModel : INotifyPropertyChanged
{
    private readonly IDeviceClient _deviceClient;
    private readonly ISerialPortService _serialPortService;
	private SerialPortInfo? _selectedPort;
	private string _text = string.Empty;
	private string _statusMessage = string.Empty;
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
		set => SetProperty(ref _text, value);
	}

	public string StatusMessage
	{
		get => _statusMessage;
		set => SetProperty(ref _statusMessage, value);
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
		await Task.CompletedTask;
	}

	private async Task CheckStatusAsync()
	{
		LastDeviceStatus = await _deviceClient.CheckStatusAsync();
		StatusMessage = LastDeviceStatus != null ? $"Устройство {(LastDeviceStatus.Online ? "подключено" : "не подключено")}" : "Не удалось получить статус устройства";
    }

	private async Task SendCommandAsync(string command)
	{
		if (!byte.TryParse(command, out byte commandType))
		{
			StatusMessage = "Неверный формат команды";
			return;
		}

		if ((commandType == Protocol.Constants.CommandType.PlayOnce || 
		     commandType == Protocol.Constants.CommandType.PlayRepeat) && 
		    string.IsNullOrWhiteSpace(Text))
		{
			StatusMessage = "Для команд воспроизведения необходимо указать текст";
			return;
		}

		var deviceCommand = new DeviceCommand
		{
			CommandType = commandType,
			Message = Text
		};

		LastTransferResult = await _deviceClient.SendAsync(deviceCommand);
		StatusMessage = LastTransferResult.Success ? "Команда успешно отправлена" : $"Ошибка отправки команды: {LastTransferResult.Message}";
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
