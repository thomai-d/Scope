using Scope.UI.Controls.Visualization;
using Scope.Interface.Probe;
using Scope.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TMD.MVVM;
using Scope.Data;
using Scope.UI.Properties;
using TMD;
using TMD.Extensions;
using TMD.Media;
using Scope.UI.Services;
using Scope.UI.Views;
using System.Xml.Serialization;
using System.IO;

namespace Scope.UI.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public const double VRef = 5.0;
        public const byte PrescalerDac0 = 4;
        public const byte PrescalerDac1 = 2;

        public const int StreamBufferSize = 3000;

        private ProbeConnection probe;
        private CancellationTokenSource streamCancellationToken;

        #region Stream/Config-fields

        private IBufferedStream<double>[] dacStreams;
        private IBufferedStream<double>[] adcStreams;

        #endregion

        #region NotifyProperties

        private int _DAC0Value;
        public int DAC0Value
        {
            get { return _DAC0Value; }
            set
            {
                if (value != _DAC0Value)
                {
                    _DAC0Value = value;
                    this.RaisePropertyChanged();
                    this.OnDAC0ValueChanged(value);
                }
            }
        }

        private int _DAC1Value;
        public int DAC1Value
        {
            get { return _DAC1Value; }
            set
            {
                if (value != _DAC1Value)
                {
                    _DAC1Value = value;
                    this.RaisePropertyChanged();
                    this.OnDAC1ValueChanged(value);
                }
            }
        }

        private bool _IsConnected;
        public bool IsConnected
        {
            get { return _IsConnected; }
            set
            {
                if (value != _IsConnected)
                {
                    _IsConnected = value;
                    this.RaisePropertyChanged();
                    this.InvalidateCommands();
                }
            }
        }

        private bool _IsConnecting;
        public bool IsConnecting
        {
            get { return _IsConnecting; }
            set
            {
                if (value != _IsConnecting)
                {
                    _IsConnecting = value;
                    this.RaisePropertyChanged();
                    this.InvalidateCommands();
                }
            }
        }

        private bool _IsStreamStarted;
        public bool IsStreamStarted
        {
            get { return _IsStreamStarted; }
            set
            {
                if (value != _IsStreamStarted)
                {
                    _IsStreamStarted = value;
                    this.RaisePropertyChanged();
                    this.InvalidateCommands();
                }
            }
        }

        private int _SamplesPerSecond = Settings.Default.SamplesPerSecond;
        public int SamplesPerSecond
        {
            get { return _SamplesPerSecond; }
            set
            {
                if (value != _SamplesPerSecond)
                {
                    _SamplesPerSecond = value;
                    this.RaisePropertyChanged();
                    Settings.Default.SamplesPerSecond = value;
                    Settings.Default.Save();
                }
            }
        }

        private DacFunction _Dac0Function = DacFunction.User;
        public DacFunction Dac0Function
        {
            get { return _Dac0Function; }
            set
            {
                if (value != _Dac0Function)
                {
                    _Dac0Function = value;
                    this.RaisePropertyChanged();
                    this.OnDacFunctionChanged(0, PrescalerDac0, value);
                }
            }
        }

        private DacFunction _Dac1Function = DacFunction.User;
        public DacFunction Dac1Function
        {
            get { return _Dac1Function; }
            set
            {
                if (value != _Dac1Function)
                {
                    _Dac1Function = value;
                    this.RaisePropertyChanged();
                    this.OnDacFunctionChanged(1, PrescalerDac1, value);
                }
            }
        }

        private string _SelectedCOMPort;
        public string SelectedCOMPort
        {
            get { return _SelectedCOMPort; }
            set
            {
                if (value != _SelectedCOMPort)
                {
                    _SelectedCOMPort = value;
                    this.RaisePropertyChanged();
                    this.OnSelectedCOMPortChanged();
                }
            }
        }

        private string[] _AvailableCOMPorts;
        public string[] AvailableCOMPorts
        {
            get { return _AvailableCOMPorts; }
            set
            {
                if (value != _AvailableCOMPorts)
                {
                    _AvailableCOMPorts = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        public List<IBufferedStream<double>> DataStreams { get; } = new List<IBufferedStream<double>>();

        public ChannelConfigurationList ChannelConfigurations { get; private set; } = new ChannelConfigurationList();

        #endregion

        #region Other properties

        public int[] SelectableSamplesPerSecond { get; private set; } = { 1, 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 3000, 3500, 4000, 5000, 5500 };

        #endregion

        #region Events

        public event EventHandler RedrawRequested;

        #endregion

        #region Commands

        public ICommand ConnectCommand { get; }
        public ICommand StartStreamCommand { get; }
        public ICommand StopStreamCommand { get; }
        public ICommand RefreshCOMPorts { get; }
        public ICommand EditChannelConfigurationCommand { get; }

        #endregion

        public MainViewModel()
        {
            this.ConnectCommand = new RelayCommand(this.ConnectCommandHandler, () => !this.IsConnecting && !this.IsConnected && !string.IsNullOrEmpty(this.SelectedCOMPort));
            this.StartStreamCommand = new RelayCommand(this.StartStreamCommandHandler, () => this.IsConnected && !this.IsStreamStarted);
            this.StopStreamCommand = new RelayCommand(this.StopStreamCommandHandler, () => this.IsConnected && this.IsStreamStarted);
            this.RefreshCOMPorts = new RelayCommand(this.RefreshCOMPortsHandler, () => !this.IsConnected);
            this.EditChannelConfigurationCommand = new RelayCommand<ChannelConfiguration>(this.EditChannelConfigurationHandler);

            this.InitializeChannels();

            this.RefreshCOMPortsHandler();
        }

        public override void OnViewLoaded()
        {
            base.OnViewLoaded();

#if DEBUG
            if (!string.IsNullOrEmpty(this.SelectedCOMPort))
                this.ConnectCommand.Execute(null);
#endif
        }

        public override void OnViewUnloaded()
        {
            base.OnViewUnloaded();
        }

        /* Command handlers */

        private async void ConnectCommandHandler()
        {
            if (string.IsNullOrEmpty(this.SelectedCOMPort))
                return;

            try
            {
                if (this.probe != null)
                    this.probe.BurstReceived -= this.OnRedrawRequested;

                this.probe = new ProbeConnection(this.SelectedCOMPort, Settings.Default.ProbeBaud);
                this.probe.BurstReceived += this.OnRedrawRequested;

                this.IsConnecting = true;

                await this.probe.OpenAsync();
                this.IsConnected = true;

                this.Dac0Function = DacFunction.User;
                this.Dac1Function = DacFunction.User;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Can't open {Settings.Default.ProbePort} @ {Settings.Default.ProbeBaud}:\n{ex.Message}", ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error);
                this.IsConnected = false;
                this.probe.Dispose();
                return;
            }
            finally
            {
                this.IsConnecting = false;
            }
        }

        private async void StartStreamCommandHandler()
        {
            try
            {
                this.IsStreamStarted = true;
                this.streamCancellationToken = new CancellationTokenSource();
                await this.probe.StartStream(this.SamplesPerSecond, this.dacStreams, this.adcStreams, this.streamCancellationToken.Token);
            }
            catch (Exception ex)
            {
                this.probe.Dispose();
                this.IsConnected = false;
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                this.IsStreamStarted = false;
            }
        }

        private void StopStreamCommandHandler()
        {
            this.streamCancellationToken?.Cancel();
            this.streamCancellationToken = null;
        }

        private void RefreshCOMPortsHandler()
        {
            this.AvailableCOMPorts = SerialPort.GetPortNames();
            this.SelectedCOMPort = this.AvailableCOMPorts.FirstOrDefault(x => x == Settings.Default.ProbePort);
        }

        private void EditChannelConfigurationHandler(ChannelConfiguration config)
        {
            DialogService.ShowDialog<ChannelConfigurationView>(new ChannelConfigurationViewModel(config));
            Settings.Default.ChannelConfigurations = this.ChannelConfigurations.ToJson();
            Settings.Default.Save();
        }

        /* UI Callbacks */

        private void OnDAC0ValueChanged(int newValue)
        {
            if (!this.IsStreamStarted)
                return;

            try
            {
                newValue = Math.Min(Math.Max(0, newValue), 255);
                var voltage = this.DACRawToVoltage((byte)newValue);
                this.probe.SetDAC(0, voltage);
            }
            catch (Exception ex)
            {
                this.probe.Dispose();
                this.IsConnected = false;
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void OnDAC1ValueChanged(int newValue)
        {
            if (!this.IsStreamStarted)
                return;

            try
            {
                newValue = Math.Min(Math.Max(0, newValue), 255);
                var voltage = this.DACRawToVoltage((byte)newValue);
                this.probe.SetDAC(1, voltage);
            }
            catch (Exception ex)
            {
                this.probe.Dispose();
                this.IsConnected = false;
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void OnDacFunctionChanged(byte index, byte prescaler, DacFunction function)
        {
            if (!this.IsConnected)
                return;

            switch (function)
            {
                case DacFunction.Sine:
                    this.probe.EnableDACBuffer(index, prescaler, Enumerable.Range(0, 256).Select(SineWave256).ToArray());
                    break;

                case DacFunction.RampUp:
                    this.probe.EnableDACBuffer(index, prescaler, Enumerable.Range(0, 256).Select(x => (byte)x).ToArray());
                    break;

                case DacFunction.RampDown:
                    this.probe.EnableDACBuffer(index, prescaler, Enumerable.Range(0, 256).Select(x => (byte)(255 - x)).ToArray());
                    break;

                case DacFunction.Triangle:
                    this.probe.EnableDACBuffer(index, prescaler, Enumerable.Range(0, 256).Select(x => (byte)(x < 128 ? 2 * x : 255 - 2 * x)).ToArray());
                    break;

                case DacFunction.User:
                    this.probe.DisableDACBuffer(index);
                    break;
            }
        }

        private void OnSelectedCOMPortChanged()
        {
            Settings.Default.ProbePort = this.SelectedCOMPort;
        }

        /* View Helper */

        private void OnRedrawRequested(object sender, EventArgs args)
        {
            int idx = 0;
            foreach (var stream in this.dacStreams)
                this.ChannelConfigurations[idx++].CurrentValue = stream.Last(1)[0];
            foreach (var stream in this.adcStreams)
                this.ChannelConfigurations[idx++].CurrentValue = stream.Last(1)[0];

            this.RedrawRequested?.Invoke(this, EventArgs.Empty);
        }

        /* Calculations */

        private double DACRawToVoltage(byte dacValue)
        {
            return dacValue * VRef / 255.0;
        }

        private double ADCRawToVoltage(byte adcValue)
        {
            return adcValue * VRef / 255.0;
        }

        private static byte SineWave256(int index)
        {
            return (byte)(128 + (Math.Sin(index / 255.0 * 4 * Math.PI) * 128.0));
        }

        /* Channel initializations */

        private void InitializeChannels()
        {
            // Initialize channels.
            if (string.IsNullOrEmpty(Settings.Default.ChannelConfigurations))
                this.InitializeNewChannelConfigurations();
            else
                this.InitializeExistingChannelConfigurations();

            foreach (var cfg in this.ChannelConfigurations)
                cfg.IsVisibleChanged += this.OnRedrawRequested;
        }

        private void InitializeExistingChannelConfigurations()
        {
            try
            {
                var existingChannelConfig = ChannelConfigurationList.FromJson(Settings.Default.ChannelConfigurations);
                var totalChannels = Settings.Default.ADCChannels + Settings.Default.DACChannels;
                if (existingChannelConfig.Count != totalChannels)
                {
                    // Config does not match total channels. Reset.
                    this.InitializeNewChannelConfigurations();
                    return;
                }

                this.ChannelConfigurations = existingChannelConfig;

                // Setup DAC streams.
                this.dacStreams = new IBufferedStream<double>[Settings.Default.DACChannels];
                for (int n = 0; n < Settings.Default.DACChannels; n++)
                {
                    this.dacStreams[n] = new BufferedStream<double>(StreamBufferSize);
                }
                this.DataStreams.AddRange(this.dacStreams);

                // Setup ADC streams.
                this.adcStreams = new IBufferedStream<double>[Settings.Default.ADCChannels];
                for (int n = 0; n < Settings.Default.ADCChannels; n++)
                {
                    this.adcStreams[n] = new BufferedStream<double>(StreamBufferSize);
                }
                this.DataStreams.AddRange(this.adcStreams);
            }
            catch
            {
                this.InitializeNewChannelConfigurations();
            }
        }

        private void InitializeNewChannelConfigurations()
        {
            this.ChannelConfigurations = new ChannelConfigurationList();
            var totalChannels = Settings.Default.ADCChannels + Settings.Default.DACChannels;
            var palette = new ColorPalette(totalChannels);

            // Setup DAC streams.
            this.dacStreams = new IBufferedStream<double>[Settings.Default.DACChannels];
            for (int n = 0; n < Settings.Default.DACChannels; n++)
            {
                this.dacStreams[n] = new BufferedStream<double>(StreamBufferSize);
                this.ChannelConfigurations.Add(new ChannelConfiguration($"DAC{n}", palette.NextColor(), 0, 5, "V"));
            }
            this.DataStreams.AddRange(this.dacStreams);

            // Setup ADC streams.
            this.adcStreams = new IBufferedStream<double>[Settings.Default.ADCChannels];
            for (int n = 0; n < Settings.Default.ADCChannels; n++)
            {
                this.adcStreams[n] = new BufferedStream<double>(StreamBufferSize);
                this.ChannelConfigurations.Add(new ChannelConfiguration($"ADC{n}", palette.NextColor(), 0, 5, "V"));
            }
            this.DataStreams.AddRange(this.adcStreams);

            Settings.Default.ChannelConfigurations = this.ChannelConfigurations.ToJson();
            Settings.Default.Save();
        }
    }

    public enum DacFunction
    {
        Sine,
        RampUp,
        RampDown,
        Triangle,
        User
    }
}