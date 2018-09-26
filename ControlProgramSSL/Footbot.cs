//message Footbot
//{ 
//bool barrier_state                     = 1;
//    uint32 robot_actual_angle              = 2;
//    uint32 robot_actual_accel              = 3;
//    uint32 robot_actual_gyro               = 4;
//    uint32 robot_actual_magnet             = 5;
//    uint32 robot_kicker_charge_status      = 6;

//    int32 speed_x                          = 7;
//    int32 speed_y                          = 8;
//    int32 speed_r                          = 9;
//    int32 speed_dribbler                   = 10;
//bool dribbler_enable                   = 11;

//    int32 kicker_voltage_level             = 12;
//bool kicker_charge_enable              = 13;
//bool kick_up                           = 14;
//bool kick_forward                      = 15;
//}

//| 			0-3		|   		4 	| 5-8 | 9-12 | 13-16 | 17-20 |         21-21		      | 22-25   |	26-29 | 30 - 33|  34-37 | 38-41   | 42-45   |  46-49 |
//| 0xAA 0xAA 0xAA 0xAA | barrier_state | q0  | q1   | q2    | q3    | robot_kicker_charge_status | voltage |  ip     |  left_x| left_y | right_x | right_y | crc32  |

//| 			0-3		| 	4-7   |    8-11 |  12-15  |  16-19         |        20	     | 		21-24   	    |        25		       |	26   |  	 27 	| 28-31 |	
//| 0xAA 0xAA 0xAA 0xAA | speed_x | speed_y | speed_r | speed_dribbler | dribbler_enable | kicker_voltage_level | kicker_charge_enable | kick_up | kick_forward | crc32 |


using System;

public class Footbot
{
    // speed_r - 0...65535
    // speed_x, y, z -100...1000
    // speed_dribbler 0...100
    
    public const int MaxIncomePacketLenght = 50;
    private const int BarrierStateIndex = 4;
    private const int Q0Index = 5;
    private const int Q1Index = 9;
    private const int Q2Index = 13;
    private const int Q3Index = 17;
    private const int KickerChargeStatusIndex = 21;
    private const int VoltageIndex = 22;
    private const int IpIndex = 26;
    private const int LeftXIndex = 30;
    private const int LeftYIndex = 34;
    private const int RightXIndex = 38;
    private const int RightYIndex = 42;
    private const int Crc32Index = 46;

    //
    public byte BarrierState = 0;
    public uint Q0 = 0;
    public uint Q1 = 0;
    public uint Q2 = 0;
    public uint Q3 = 0;
    public byte KickerChargeStatus = 0;
    public uint Voltage = 0;
    public uint Ip = 0;
    public uint LeftX = 0;
    public uint LeftY = 0;
    public uint RightX = 0;
    public uint RightY = 0;


    // экземпл€р структуры с данными от робота и с джойстика
    public int SpeedX;
    public int SpeedY;
    public int SpeedR;
    public int SpeedDribbler;
    public byte DribblerEnable;

    public int KickerVoltageLevel;
    public byte KickerChargeEnable;
    public byte KickUp;
    public byte KickForward;

    public byte BarrirrState;
    public uint RobotActualAngle;
    public uint RobotActualAccel;
    public uint RobotActualGyro;
    public uint RobotActualMagnet;
    public uint RobotActualChargeStatus;
    // возвращает массив на отправку с crc
    public byte[] getBytes()
    {
        var result = new byte[32];
        result[0] = 0xAA;
        result[1] = 0xAA;
        result[2] = 0xAA;
        result[3] = 0xAA;
        result[20] = DribblerEnable;
        result[25] = KickerChargeEnable;
        result[26] = KickUp;
        result[27] = KickForward;
        var speedX = BitConverter.GetBytes(SpeedX);
        var speedY = BitConverter.GetBytes(SpeedY);
        var speedR = BitConverter.GetBytes(SpeedR);
        var speedDribbler = BitConverter.GetBytes(SpeedDribbler);
        var kickerVoltageLevel = BitConverter.GetBytes(KickerVoltageLevel);
        Array.Copy(speedX, 0, result, 8, 4);
        Array.Copy(speedY, 0, result, 4, 4);
        Array.Copy(speedR, 0, result, 12, 4);
        Array.Copy(speedDribbler, 0, result, 16, 4);
        Array.Copy(kickerVoltageLevel, 0, result, 21, 4);
        var crc = Crc32(result, 28);
        var crcArray = BitConverter.GetBytes(crc);
        Array.Copy(crcArray, 0, result, 28, 4);
        return result;
    }
    public static Footbot getStruct(byte[] data)
    {
        var calcCrc = Crc32(data, Crc32Index);
        var messageCrc = GetIUint32(data, Crc32Index);
        if(calcCrc != messageCrc)
        {
            return null;
        }
        var result = new Footbot
        {

            BarrierState = data[BarrierStateIndex],
            Q0 = GetIUint32(data, Q0Index),
            Q1 = GetIUint32(data, Q1Index),
            Q2 = GetIUint32(data, Q2Index),
            Q3 = GetIUint32(data, Q3Index),
            KickerChargeStatus = data[KickerChargeStatusIndex],
            Voltage = GetIUint32(data, VoltageIndex),
            Ip = GetIUint32(data, IpIndex),
            LeftX = GetIUint32(data, LeftXIndex),
            LeftY = GetIUint32(data, LeftYIndex),
            RightX = GetIUint32(data, RightXIndex),
            RightY = GetIUint32(data, RightYIndex),
        };
        return result;
    }
    private static uint GetIUint32(byte[] data, int startIndex)
    {
        byte[] array = new byte[4];
        Array.Copy(data, startIndex, array, 0, 4);
        var result = BitConverter.ToUInt32(array, 0);
        return result;
    }


    static UInt32 Crc32(byte[] buf, int len)
    {
        uint[] crc_table = new uint[256];
        uint crc;

        uint i;
        for (i = 0; i < 256; i++)
        {
            uint j;
            crc = i;
            for (j = 0; j < 8; j++)
            {
                if ((crc & 1) > 0)
                {
                    crc = (crc >> 1) ^ 0xEDB88320;
                }
                else
                {
                    crc = crc >> 1;
                }

            }
            crc_table[i] = crc;
        };

        crc = 0xFFFFFFFF;

        uint vsp = 0;
        while (len-- > 0)
        {
            crc = crc_table[(crc ^ buf[vsp]) & 0xFF] ^ (crc >> 8);
            vsp++;
        }

        return crc ^ 0xFFFFFFFF;
    }

}