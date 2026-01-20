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
	public DeviceStatusDto? LastTransferStatus { get; set; }
	public TransferResultDto? LastDeviceStatus { get; set; }

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
		// TODO: implement port scanning logic
		await Task.CompletedTask;
	}

	private async Task CheckStatusAsync()
	{
		// TODO: implement status check logic
		await Task.CompletedTask;
	}

	private async Task SendCommandAsync(string command)
	{
		// TODO: implement send command logic
		await Task.CompletedTask;
	}
}
