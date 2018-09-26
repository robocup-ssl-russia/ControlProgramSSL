using SlimDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Enjoy
{
    class GamepadButton
    {
        private bool _state = false;
        private bool _pressedOnce = false;

        public void SetFixationState(bool state)
        {
            if(state == false && _pressedOnce == true)
            {
                _state = !_state;
                _pressedOnce = false;
            }
            if(_pressedOnce == false && state == true)
            {
                _pressedOnce = true;
            }
        }
        public byte GetState()
        {
            if(_state == true)
            {
                return 1;
            }
            return 0;
        }
        public void SetState(bool state)
        {
            _state = state;
        }
        public void Toggle()
        {
            _state = !_state;
        }
    }
    class Gamepad : IDisposable
    {
        private Joystick _joystick;
        private JoystickState _state = new JoystickState();
        public event EventHandler<Footbot> ValueComposed;

        private const int _kickForwardIndex = 0;
        private const int _dribblerIndex = 1;
        private const int _kickerEnableIndex = 2;
        private const int _kickUpIndex = 3;
        private GamepadButton _kickerEnable = new GamepadButton();
        private GamepadButton _kickUp = new GamepadButton();
        private GamepadButton _dribbler = new GamepadButton();
        private GamepadButton _kickForward = new GamepadButton();
        private DeviceInstance _instance;

        private int _speedDribbler;
        private int _voltageLevel;
        private int _speedR;
        private int _speedX;
        private int _speedY;
        private volatile bool _isimeToExit = false;
        private bool _isXBoxGamePad = false;

        public Gamepad(DeviceInstance instance, bool isXBoxGamePad)
        {
            _isXBoxGamePad = isXBoxGamePad;
            _instance = instance;
            _joystick = Acquire(_instance);
            _joystick.Poll();
            new Thread(RefreshState) { IsBackground = true }.Start();
        }

        private void RefreshState()
        {
            while (true)
            {
                _state = _joystick.GetCurrentState();

                if(_isimeToExit == true)
                {
                    _joystick.Unacquire();
                    _joystick.Dispose();
                    return;
                }

                var btns = _state.GetButtons();
                if(_isXBoxGamePad == false)
                {
                    _speedX = Convert(_state.RotationZ, true);
                    _speedY = Convert(_state.Z, false);
                }
                else
                {
                    _speedX = Convert(_state.RotationX);
                    _speedY = Convert(_state.RotationY, true);
                }
                _kickUp.SetState(btns[_kickUpIndex]);
                _kickForward.SetState(btns[_kickForwardIndex]);
                _kickerEnable.SetFixationState(btns[_kickerEnableIndex]);
                _dribbler.SetFixationState(btns[_dribblerIndex]);
                setPovButtons();
                _speedR = Convert(_state.X);
                
                var footBot = new Footbot()
                {
                    SpeedX = _speedX,
                    SpeedY = _speedY,
                    SpeedR = -_speedR,
                    SpeedDribbler = _speedDribbler,
                    DribblerEnable = _dribbler.GetState(),
                    KickerVoltageLevel = _voltageLevel,
                    KickerChargeEnable = _kickerEnable.GetState(),
                    KickUp = _kickUp.GetState(),
                    KickForward = _kickForward.GetState()
                };
                ValueComposed?.Invoke(this, footBot);
                Thread.Sleep(100);//here we can change time of sleeping so it means we can change our refresh time!!!!!!
            }
        }
        private bool _isSpeedDribblerIncPressed = false;
        private bool _isSpeedDribblerDecPressed = false;
        private bool _isVoltageLevelIncPressed = false;
        private bool _isVoltageLevelDecPressed = false;

        private const int ButtonRelease = -1;
        private const int SpeedDribblerIncValue = 0;
        private const int SpeedDribblerDecValue = 18000;
        private const int VoltageLevelIncValue = 9000;
        private const int VoltageLevelDecValue = 27000;
        private void setPovButtons()
        {
            var povControllers = _state.GetPointOfViewControllers();
            var value = povControllers[0];
            switch (value)
            {
                case ButtonRelease:
                    {
                        if (_isSpeedDribblerIncPressed && _speedDribbler < 100)
                        {
                            _speedDribbler+=10;
                        }
                        if (_isSpeedDribblerDecPressed && _speedDribbler > 0)
                        {
                            _speedDribbler-=10;
                        }
                        if (_isVoltageLevelIncPressed && _voltageLevel < 30)
                        {
                            _voltageLevel+=6;
                        }
                        if (_isVoltageLevelDecPressed && _voltageLevel > 0)
                        {
                            _voltageLevel-=6;
                        }
                        _isSpeedDribblerIncPressed = false;
                        _isSpeedDribblerDecPressed = false;
                        _isVoltageLevelIncPressed = false;
                        _isVoltageLevelDecPressed = false;
                        break;
                    }

                case SpeedDribblerIncValue:
                    {
                        _isSpeedDribblerIncPressed = true;
                        break;
                    }
                case SpeedDribblerDecValue:
                    {
                        _isSpeedDribblerDecPressed = true;
                        break;
                    }
                case VoltageLevelIncValue:
                    {
                        _isVoltageLevelIncPressed = true;
                        break;
                    }
                case VoltageLevelDecValue:
                    {
                        _isVoltageLevelDecPressed = true;
                        break;
                    }
            }

        }
        private int Convert(int val, bool isReverse = false)
        {
            var result = 0;
            val -= 32762;
            result = val / 328;
            //result = (int)(result * 0.7);
            if(Math.Abs(result) < 17 )
            {
                result = 0;
            }
            if (isReverse)
            {
                return result * (-1);
            }
            return result;
        }

        private Joystick Acquire(DeviceInstance di)
        {
            DirectInput dinput = new DirectInput();

            var pad = new Joystick(dinput, di.InstanceGuid);
            foreach (DeviceObjectInstance doi in pad.GetObjects(ObjectDeviceType.Axis))
            {
                //pad.GetObjectPropertiesById((int)doi.ObjectType).SetRange(-5000, 5000);
            }

            pad.Properties.AxisMode = DeviceAxisMode.Absolute;
            //pad.SetCooperativeLevel(parent, (CooperativeLevel.Nonexclusive | CooperativeLevel.Background));
            pad.Acquire();
            return pad;
        }

        public void Dispose()
        {
            _isimeToExit = true;
        }
    }
}
