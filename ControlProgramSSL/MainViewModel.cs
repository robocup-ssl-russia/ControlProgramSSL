using SlimDX.DirectInput;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Enjoy
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        public MainViewModel()
        {
            Device1.Name = "Device 1";
            Device2.Name = "Device 2";
            Device3.Name = "Device 3";
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
        public event PropertyChangedEventHandler PropertyChanged;
        Device _device1 = new Device();
        public Device Device1
        {
            get { return _device1; }
            set
            {
                _device1 = value;
                RaisePropertyChanged(nameof(Device1));
            }
        }
        Device _device2 = new Device();
        public Device Device2
        {
            get { return _device2; }
            set
            {
                _device2 = value;
                RaisePropertyChanged(nameof(Device2));
            }
        }

        Device _device3 = new Device();
        public Device Device3
        {
            get { return _device3; }
            set
            {
                _device3 = value;
                RaisePropertyChanged(nameof(Device3));
            }
        }


        public void RaisePropertyChanged(string propertyName)
        {
            // Если кто-то на него подписан, то вызывем его
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}



