using SlimDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Enjoy
{
    
    public class InetFlag
    {
        private bool flag;
        Socket socket;
        Socket handler;
        String IP;

        public InetFlag(String IP)
        {
            this.IP = IP;
            flag = false;
        }


        public void catchFlag()
        {
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(IP), 8005);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Bind(ipPoint);
                socket.Listen(10);

                handler = socket.Accept();


                StringBuilder builder = new StringBuilder();
                int bytes = 0;
                byte[] data = new byte[256];
                while (true)
                {
                    do
                    {
                        bytes = handler.Receive(data);
                        builder = new StringBuilder(Encoding.ASCII.GetString(data, 0, bytes));
                    }
                    while (handler.Available > 0);

                    Console.WriteLine(DateTime.Now.ToShortTimeString() + ": " + builder.ToString());
                    if (builder.ToString().ElementAt(0) == '1')
                    {
                        setFlag(true);
                        lock (MainViewModel.obj)
                            Console.WriteLine("!");
                            MainViewModel.flag = true;
                    }
                    else if (builder.ToString().ElementAt(0) == '0')
                    {
                        setFlag(false);
                        lock (MainViewModel.obj)
                            MainViewModel.flag = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void setFlag(bool flag)
        {
            this.flag = flag;
        }

        public bool getFlag()
        {
            return flag;
        }
    }



    // включает в себя 1 робота и 1 джойстик
    public class Device : INotifyPropertyChanged, IDisposable
    {
        bool flag;
        volatile Stopwatch _receiveTimeout = new Stopwatch();
        volatile bool _isTimeToExit = false;
        Gamepad _gamepad;
        //public InetFlag flag;
        Thread _connectionThread;
        UdpClient _receivingUdpClient;
        void CheckConnection()
        {
            _receiveTimeout.Start();
            while (true)
            {
                if (_receiveTimeout.ElapsedMilliseconds > 1500)
                {
                    IsConnected = false;
                }
                else
                {
                    IsConnected = true;
                }
                Thread.Sleep(10);
                if (_isTimeToExit)
                {
                    return;
                }
            }
        }
        
        public Device()
        {
            Refresh();
        }

        private RelayCommand _connect;
        public RelayCommand Connect
        {
            get
            {
                return _connect ??
                  (_connect = new RelayCommand(obj =>
                  {
                      var selectedInstance = GetInstance(SelectedGuid);
                      if(SelectedGuid.Contains("XBOX"))
                      {
                          _gamepad = new Gamepad(selectedInstance, true, this);
                      }
                      else
                      {
                          _gamepad = new Gamepad(selectedInstance, false, this);
                      }
                      
                      _gamepad.ValueComposed += _gamepad_ValueComposed;
                      _connectionThread = new Thread(CheckConnection)
                      {
                          IsBackground = true
                      };
                      _connectionThread.Start();
                  }));
            }
        }
        private byte[] _incomeArray = new byte[29];
        
        bool ContainHead(byte[] data, out int index)
        {
            index = -1;
            var result = false;
            int matchCounter = 1;
            for(int i = 0; i< data.Length; i++)
            {
                if(data[i] == 0xAA)
                {
                    while(matchCounter < 4 && data[i + matchCounter] == 0xAA)
                    {
                        matchCounter++;
                    }
                }
                if (matchCounter == 4)
                {
                    index = i;
                    return true;
                }
                else
                {
                    matchCounter = 1;
                }
            }

            return result;
        }

        private RelayCommand _refreshGuids;
        public RelayCommand RefreshGuids
        {
            get
            {
                return _refreshGuids ??
                  (_refreshGuids = new RelayCommand(obj =>
                  {
                      Refresh();
                  }));
            }
        }

        void Refresh()
        {
            Items.Clear();
            var devices = Available();
            foreach (var device in devices)
            {
                Items.Add(device.InstanceName + " " + device.InstanceGuid);
            }
            if (Items.Count != 0)
            {
                SelectedGuid = Items[0];
            }
        }
        DeviceInstance GetInstance(string guid)
        {
            var instances = Available();
            foreach (var instance in instances)
            {

                if ((instance.InstanceName + " " + instance.InstanceGuid) == guid)
                {
                    return instance;
                }
            }
            return null;
        }

        public IList<DeviceInstance> Available()
        {
            IList<DeviceInstance> result = new List<DeviceInstance>();
            DirectInput dinput = new DirectInput();
            foreach (DeviceInstance di in dinput.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly))
            {
                result.Add(di);
            }
            return result;
        }
        string _selectedGuid;
        public string SelectedGuid
        {
            get
            {
                return _selectedGuid;
            }
            set
            {
                _selectedGuid = value;
                RaisePropertyChanged(nameof(SelectedGuid));
            }
        }

        ObservableCollection<string> _items = new ObservableCollection<string>();
        public ObservableCollection<string> Items
        {
            get
            {
                return _items;
            }
            set
            {
                _items = value;
                RaisePropertyChanged(nameof(Items));
            }
        }
        private void _gamepad_ValueComposed(object sender, Footbot e)
        {
            //Range_1
            //0...20
            //Range_2
            //0...50
            //Range_3
            //0...100

            if (Range1)
            {
                e.SpeedX = (e.SpeedX * 20)/100;
                e.SpeedY = (e.SpeedY * 20)/100;
                e.SpeedR = (e.SpeedR * 20)/100;
            }

            if (Range2)
            {
                e.SpeedX = (e.SpeedX * 50) / 100;
                e.SpeedY = (e.SpeedY * 50) / 100;
                e.SpeedR = (e.SpeedR * 25) / 100;
            }
            SpeedXVal = e.SpeedX;
            SpeedYVal = e.SpeedY;
            SpeedRVal = e.SpeedR;
            SpeedDribblerVal = e.SpeedDribbler;
            DribblerEnableVal = e.DribblerEnable;
            KickerVoltageLevelVal = e.KickerVoltageLevel;
            KickerChargeEnableVal = e.KickerChargeEnable;
            KickUpVal = e.KickUp;
            KickForwardVal = e.KickForward;
            var message = e.getBytes();
            Send(message);
        }
        string _remoteIp = "192.168.43.0";
        public string RemoteIp
        {
            get { return _remoteIp; }
            set
            {
                _remoteIp = value;
                RaisePropertyChanged(nameof(RemoteIp));
            }
        }

        string _gamePadName = "";
        public string GamePadName
        {
            get { return _gamePadName; }
            set
            {
                _gamePadName = value;
                RaisePropertyChanged(nameof(GamePadName));
            }
        }

        string _remotePort = "10000";
        public string RemotePort
        {
            get { return _remotePort; }
            set
            {
                _remotePort = value;
                RaisePropertyChanged(nameof(RemotePort));
            }
        }

        string _localPort = "57000";
        public string LocalPort
        {
            get { return _localPort; }
            set
            {
                _localPort = value;
                RaisePropertyChanged(nameof(LocalPort));
            }
        }


        private void Send(byte[] bytes)
        {
            UdpClient sender = new UdpClient();
            try
            {
                
                var remoteIp = IPAddress.Parse(RemoteIp);
                var remotePort = int.Parse(RemotePort);
                IPEndPoint endPoint = new IPEndPoint(remoteIp, remotePort);
                sender.Send(bytes, bytes.Length, endPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            }
            finally
            {
                sender.Close();
            }
        }
        byte _barrierState;
        public byte BarrierState
        {
            get { return _barrierState; }
            set
            {
                _barrierState = value;
                RaisePropertyChanged(nameof(BarrierState));
            }
        }

        uint _actualAngle;
        public uint ActualAngle
        {
            get { return _actualAngle; }
            set
            {
                _actualAngle = value;
                RaisePropertyChanged(nameof(ActualAngle));
            }
        }

        uint _actualAccel;
        public uint ActualAccel
        {
            get { return _actualAccel; }
            set
            {
                _actualAccel = value;
                RaisePropertyChanged(nameof(ActualAccel));
            }
        }

        uint _actualGyro;
        public uint ActualGyro
        {
            get { return _actualGyro; }
            set
            {
                _actualGyro = value;
                RaisePropertyChanged(nameof(ActualGyro));
            }
        }

        uint _actualMagnet;
        public uint ActualMagnet
        {
            get { return _actualMagnet; }
            set
            {
                _actualMagnet = value;
                RaisePropertyChanged(nameof(ActualMagnet));
            }
        }

        byte _kickerChargeStatus;
        public byte KickerChargeStatus
        {
            get { return _kickerChargeStatus; }
            set
            {
                _kickerChargeStatus = value;
                RaisePropertyChanged(nameof(KickerChargeStatus));
            }
        }

        uint _voltage;
        public uint Voltage
        {
            get { return _voltage; }
            set
            {
                _voltage = value;
                RaisePropertyChanged(nameof(Voltage));
            }
        }

        int _speedXVal;
        public int SpeedXVal
        {
            get { return _speedXVal; }
            set
            {
                _speedXVal = value;
                RaisePropertyChanged(nameof(SpeedXVal));
            }
        }

        int _speedYVal;
        public int SpeedYVal
        {
            get { return _speedYVal; }
            set
            {
                _speedYVal = value;
                RaisePropertyChanged(nameof(SpeedYVal));
            }
        }

        int _speedRVal;
        public int SpeedRVal
        {
            get { return _speedRVal; }
            set
            {
                _speedRVal = value;
                RaisePropertyChanged(nameof(SpeedRVal));
            }
        }

        int _speedDribblerVal;
        public int SpeedDribblerVal
        {
            get { return _speedDribblerVal; }
            set
            {
                _speedDribblerVal = value;
                RaisePropertyChanged(nameof(SpeedDribblerVal));
            }
        }

        byte _dribblerEnableVal;
        public byte DribblerEnableVal
        {
            get { return _dribblerEnableVal; }
            set
            {
                _dribblerEnableVal = value;
                RaisePropertyChanged(nameof(DribblerEnableVal));
            }
        }


        int _kickerVoltageLevelVal;
        public int KickerVoltageLevelVal
        {
            get { return _kickerVoltageLevelVal; }
            set
            {
                _kickerVoltageLevelVal = value;
                RaisePropertyChanged(nameof(KickerVoltageLevelVal));
            }
        }

        byte _kickerChargeEnableVal;
        public byte KickerChargeEnableVal
        {
            get { return _kickerChargeEnableVal; }
            set
            {
                _kickerChargeEnableVal = value;
                RaisePropertyChanged(nameof(KickerChargeEnableVal));
            }
        }
        byte _kickUpVal;
        public byte KickUpVal
        {
            get { return _kickUpVal; }
            set
            {
                _kickUpVal = value;
                RaisePropertyChanged(nameof(KickUpVal));
            }
        }

        byte _kickForwardVal;
        public byte KickForwardVal
        {
            get { return _kickForwardVal; }
            set
            {
                _kickForwardVal = value;
                RaisePropertyChanged(nameof(KickForwardVal));
            }
        }



        bool _isConnected;
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                _isConnected = value;
                RaisePropertyChanged(nameof(IsConnected));
            }
        }

        bool _range1 = true;
        public bool Range1
        {
            get { return _range1; }
            set
            {
                _range1 = value;
                RaisePropertyChanged(nameof(Range1));
            }
        }

        bool _range2 = false;
        public bool Range2
        {
            get { return _range2; }
            set
            {
                _range2 = value;
                RaisePropertyChanged(nameof(Range2));
            }
        }

        bool _range3 = false;
        public bool Range3
        {
            get { return _range3; }
            set
            {
                _range3 = value;
                RaisePropertyChanged(nameof(Range3));
            }
        }

        string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                RaisePropertyChanged(nameof(Name));
            }
        }

        uint _q1;
        public uint Q1
        {
            get { return _q1; }
            set
            {
                _q1 = value;
                RaisePropertyChanged(nameof(Q1));
            }
        }

        uint _q2;
        public uint Q2
        {
            get { return _q2; }
            set
            {
                _q2 = value;
                RaisePropertyChanged(nameof(Q2));
            }
        }

        uint _q3;
        public uint Q3
        {
            get { return _q3; }
            set
            {
                _q3 = value;
                RaisePropertyChanged(nameof(Q3));
            }
        }

        uint _q0;
        public uint Q0
        {
            get { return _q0; }
            set
            {
                _q0 = value;
                RaisePropertyChanged(nameof(Q0));
            }
        }

        uint _ip;
        public uint Ip
        {
            get { return _ip; }
            set
            {
                _ip = value;
                RaisePropertyChanged(nameof(Ip));
            }
        }


        uint _leftX;
        public uint LeftX
        {
            get { return _leftX; }
            set
            {
                _leftX = value;
                RaisePropertyChanged(nameof(LeftX));
            }
        }

        uint _leftY;
        public uint LeftY
        {
            get { return _leftY; }
            set
            {
                _leftY = value;
                RaisePropertyChanged(nameof(LeftY));
            }
        }

        uint _rightX;
        public uint RightX
        {
            get { return _rightX; }
            set
            {
                _rightX = value;
                RaisePropertyChanged(nameof(RightX));
            }
        }

        uint _rightY;
        public uint RightY
        {
            get { return _rightY; }
            set
            {
                _rightY = value;
                RaisePropertyChanged(nameof(RightY));
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void Dispose()
        {
            _isTimeToExit = true;
            _gamepad?.Dispose();
            _receivingUdpClient?.Close();
            _receivingUdpClient = null;
        }
    }
}
