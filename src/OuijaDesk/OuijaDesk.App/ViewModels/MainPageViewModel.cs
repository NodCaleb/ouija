using OuijaDesk.Contracts.Models;
using OuijaDesk.Application.DTO;
using System.Collections.ObjectModel;
using System.Windows.Input;
using OuijaDesk.Application.Contracts;

namespace OuijaDesk.App.ViewModels;

public class MainPageViewModel
{
    private readonly IDeviceClient _deviceClient;
    private readonly ISerialPortService _serialPortService;
	public ICommand ScanPortsCommand { get; }
	public ICommand CheckStatusCommand { get; }
	public ICommand SendCommandCommand { get; }

	public ObservableCollection<SerialPortInfo> Ports { get; } = new();

	public SerialPortInfo? SelectedPort { get; set; }
	public string Text { get; set; } = string.Empty;
	public string StatusMessage { get; set; } = string.Empty;
	public DeviceStatusDto? LastDeviceStatus { get; set; }
	public TransferResultDto? LastTransferResult { get; set; }

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
		StatusMessage = LastDeviceStatus != null ? $"Устройство {(LastDeviceStatus.Online ? "подключено" : "отключено")}" : "Не удалось получить статус устройства";
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
}
