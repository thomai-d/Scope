using Scope.Controls.Visualization;
using Scope.Data;
using Scope.Interface.Probe;
using Scope.MVVM;
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

namespace Scope.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public const double VRef = 5.0;

        public const int StreamBufferSize = 3000;

        private ProbeConnection probe;
        private ReadStreamCommand readStreamCommand;

        #region Stream/Config-fields

        private IBufferedStream<double> dac0Stream;
        private IBufferedStream<double> dac1Stream;
        private IBufferedStream<double> adc0Stream;
        private IBufferedStream<double> adc1Stream;
        private IBufferedStream<double> adc2Stream;
        private IBufferedStream<double> adc3Stream;
        private LineConfiguration dac0Config;
        private LineConfiguration dac1Config;
        private LineConfiguration adc0Config;
        private LineConfiguration adc1Config;
        private LineConfiguration adc2Config;

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
                    this.OnDAC0Value_Changed(value);
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
                    this.OnDAC1Value_Changed(value);
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
                }
            }
        }

        private bool _IsFunctionGenActive;
        public bool IsFunctionGenActive
        {
            get { return _IsFunctionGenActive; }
            set
            {
                if (value != _IsFunctionGenActive)
                {
                    _IsFunctionGenActive = value;
                    this.RaisePropertyChanged();
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
                }
            }
        }

        private int _SamplesPerSecond = 1000;
        public int SamplesPerSecond
        {
            get { return _SamplesPerSecond; }
            set
            {
                if (value != _SamplesPerSecond)
                {
                    _SamplesPerSecond = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        public ObservableCollection<IBufferedStream<double>> DataStreams { get; } = new ObservableCollection<IBufferedStream<double>>();

        public ObservableCollection<LineConfiguration> LineConfigurations { get; } = new ObservableCollection<LineConfiguration>();

        #endregion

        #region Other properties

        public int[] SelectableSamplesPerSecond { get; private set; } = { 1, 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 3000, 3500 };

        #endregion

        #region Events

        public event EventHandler RedrawRequested;

        #endregion

        #region Commands

        public ICommand ConnectCommand { get; }
        public ICommand StartStreamCommand { get; }
        public ICommand StopStreamCommand { get; }
        public ICommand RampUpDAC0Command { get; }
        public ICommand RampDownDAC0Command { get; }
        public ICommand RampUpDownDAC0Command { get; }
        public ICommand SineDAC0Command { get; }

        #endregion

        public MainViewModel()
        {
            this.ConnectCommand = new RelayCommand(this.ConnectCommandHandler, () => !this.IsConnecting && !this.IsConnected);
            this.StartStreamCommand = new RelayCommand(this.StartStreamCommandHandler, () => this.IsConnected && !this.IsStreamStarted && !this.IsFunctionGenActive);
            this.StopStreamCommand = new RelayCommand(this.StopStreamCommandHandler, () => this.IsConnected && this.IsStreamStarted);
            this.RampUpDAC0Command = new RelayCommand(() => this.UserFunctionCommandHandler(x => x), () => this.IsConnected && !this.IsStreamStarted && !this.IsFunctionGenActive);
            this.RampDownDAC0Command = new RelayCommand(() => this.UserFunctionCommandHandler(x => (byte)(255-x)), () => this.IsConnected && !this.IsStreamStarted && !this.IsFunctionGenActive);
            this.RampUpDownDAC0Command = new RelayCommand(() => this.UserFunctionCommandHandler(x => (byte)(x < 128 ? 2*x : 255-2*x)), () => this.IsConnected && !this.IsStreamStarted && !this.IsFunctionGenActive);
            this.SineDAC0Command = new RelayCommand(() => this.UserFunctionCommandHandler(ByteToSineWave), () => this.IsConnected && !this.IsStreamStarted && !this.IsFunctionGenActive);

            this.dac0Stream = new BufferedStream<double>(StreamBufferSize);
            this.dac1Stream = new BufferedStream<double>(StreamBufferSize);
            this.adc0Stream = new BufferedStream<double>(StreamBufferSize);
            this.adc1Stream = new BufferedStream<double>(StreamBufferSize);
            this.adc2Stream = new BufferedStream<double>(StreamBufferSize);
            this.adc3Stream = new BufferedStream<double>(StreamBufferSize);
            this.DataStreams.Add(this.dac0Stream);
            this.DataStreams.Add(this.dac1Stream);
            this.DataStreams.Add(this.adc0Stream);
            this.DataStreams.Add(this.adc1Stream);
            this.DataStreams.Add(this.adc2Stream);
            this.DataStreams.Add(this.adc3Stream);

            this.dac0Config = new LineConfiguration { Name = "DAC0", Color = Colors.Blue, Unit = "V" };
            this.dac1Config = new LineConfiguration { Name = "DAC1", Color = Colors.Green, Unit = "V" };
            this.adc0Config = new LineConfiguration { Name = "ADC0", Color = Colors.Red, Unit = "V" };
            this.adc1Config = new LineConfiguration { Name = "ADC1", Color = Colors.Orange, Unit = "V" };
            this.adc2Config = new LineConfiguration { Name = "ADC2", Color = Colors.Violet, Unit = "V" };
            this.LineConfigurations.Add(this.dac0Config);
            this.LineConfigurations.Add(this.dac1Config);
            this.LineConfigurations.Add(this.adc0Config);
            this.LineConfigurations.Add(this.adc1Config);
            this.LineConfigurations.Add(this.adc2Config);
        }

        public override void OnViewLoaded()
        {
            base.OnViewLoaded();

            this.ConnectCommand.Execute(null);
        }

        public override void OnViewUnloaded()
        {
            base.OnViewUnloaded();
        }

        /* Command handlers */

        private async void ConnectCommandHandler()
        {
            try
            {
                this.probe = new ProbeConnection(Settings.Default.ProbePort, Settings.Default.ProbeBaud);

                this.IsConnecting = true;
                CommandManager.InvalidateRequerySuggested();

                await this.probe.OpenAsync();
                this.IsConnected = true;
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
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async void StartStreamCommandHandler()
        {
            this.ResetViewData();

            try
            {
                this.readStreamCommand = new ReadStreamCommand(this.probe, this.SamplesPerSecond, this.adc0Stream, this.adc1Stream, this.adc2Stream);
                this.readStreamCommand.BurstReceived += this.OnRedrawRequested;
                var readStreamTask = this.readStreamCommand.Execute();

                this.IsStreamStarted = true;

                await readStreamTask;
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
                this.readStreamCommand.BurstReceived -= this.OnRedrawRequested;
                this.readStreamCommand = null;

                this.IsStreamStarted = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void StopStreamCommandHandler()
        {
            this.readStreamCommand?.Cancel();
        }

        private async void UserFunctionCommandHandler(Func<byte, byte> func)
        {
            this.IsFunctionGenActive = true;
            CommandManager.InvalidateRequerySuggested();

            try
            {
                this.ResetViewData();

                for (int n = 0; n < 256; n++)
                {
                    for (int repeat = 0; repeat < 3; repeat++)
                    {
                        var funcOut = func((byte)n);
                        this.probe.WriteBytes((byte)Command.SetDAC0, funcOut);

                        this.probe.WriteBytes((byte)Command.GetADC, 0);
                        var adc0 = this.probe.ReadByte("GET SAMPLE");

                        this.probe.WriteBytes((byte)Command.GetADC, 1);
                        var adc1 = this.probe.ReadByte("GET SAMPLE");

                        this.probe.WriteBytes((byte)Command.GetADC, 2);
                        var adc2 = this.probe.ReadByte("GET SAMPLE");

                        this.dac0Stream.Push(this.DACRawToVoltage(funcOut));
                        this.adc0Stream.Push(this.ADCRawToVoltage(adc0));
                        this.adc1Stream.Push(this.ADCRawToVoltage(adc1));
                        this.adc2Stream.Push(this.ADCRawToVoltage(adc2));

                        await Task.Delay(1);
                        this.OnRedrawRequested(this, EventArgs.Empty);
                    }
                }
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
                this.IsFunctionGenActive = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        /* Callbacks */

        private void OnDAC0Value_Changed(int newValue)
        {
            if (!this.IsStreamStarted)
                return;

            try
            {
                newValue = Math.Min(Math.Max(0, newValue), 255);
                this.probe.WriteBytes((byte)Command.SetDAC0, (byte)newValue);
                this.dac0Config.CurrentValue = this.DACRawToVoltage((byte)newValue);
            }
            catch (Exception ex)
            {
                this.probe.Dispose();
                this.IsConnected = false;
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void OnDAC1Value_Changed(int newValue)
        {
            if (!this.IsStreamStarted)
                return;

            try
            {
                newValue = Math.Min(Math.Max(0, newValue), 255);
                this.probe.WriteBytes((byte)Command.SetDAC1, (byte)newValue);
                this.dac1Config.CurrentValue = this.DACRawToVoltage((byte)newValue);
            }
            catch (Exception ex)
            {
                this.probe.Dispose();
                this.IsConnected = false;
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        /* View Helper */

        private void ResetViewData()
        {
            this.DAC0Value = 0;
            this.DAC1Value = 0;

            foreach (var stream in this.DataStreams)
                stream.Clear();

            foreach (var config in this.LineConfigurations)
                config.CurrentValue = 0;
        }

        private void OnRedrawRequested(object sender, EventArgs args)
        {
            if (this.IsFunctionGenActive)
            {
                this.LineConfigurations[0].CurrentValue = this.dac0Stream.Last(1)[0];
                this.LineConfigurations[1].CurrentValue = this.dac1Stream.Last(1)[0];
            }

            this.LineConfigurations[2].CurrentValue = this.adc0Stream.Last(1)[0];
            this.LineConfigurations[3].CurrentValue = this.adc1Stream.Last(1)[0];
            this.LineConfigurations[4].CurrentValue = this.adc2Stream.Last(1)[0];

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

        private static byte ByteToSineWave(byte index)
        {
            return (byte)(128 + (Math.Sin(index / 255.0 * 4 * Math.PI) * 128.0));
        }
    }
}