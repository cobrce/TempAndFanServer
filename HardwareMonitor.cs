

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using OpenHardwareMonitor.Hardware;

namespace TempAndFanServer
{
    public class HardwareMonitor
    {
        public record SensorDescrpitor(string HardwareType, string SensorType, string SensorName);
        public record HardwareDescriptor(
            SensorDescrpitor CpuTempDescriptor,
            SensorDescrpitor GpuTempDescriptor,
            SensorDescrpitor CpuFanDescriptor,
            SensorDescrpitor GpuFanDescriptor
        )
        {
            public static readonly HardwareDescriptor Default = new(
               new SensorDescrpitor("SuperIO", "Temperature", ""),
                new SensorDescrpitor("GpuNvidia", "Temperature", ""),
                new SensorDescrpitor("SuperIO", "Control", "Fan #2"),
                new SensorDescrpitor("GpuNvidia", "Control", "GPU Fan 1")
                );
            public static bool LoadFromFile(string fileName, out HardwareDescriptor hardwareDescriptor)
            {
                hardwareDescriptor =  Default;
                if (!File.Exists(fileName))
                    return false;


                using var stream = new FileStream(fileName, FileMode.Open);
                HardwareDescriptor? result = JsonSerializer.Deserialize<HardwareDescriptor>(stream);
                if (result!=null)
                {
                    hardwareDescriptor = result;
                    return true;
                }
                return false;
            }
        }


        private readonly Computer computer = new() { IsCpuEnabled = true, IsGpuEnabled = true, IsMotherboardEnabled = true };
        public HardwareMonitor(HardwareDescriptor hardwareDescriptor)
        {
            hwDescriptor = hardwareDescriptor;
            computer.Open(false);
            _ = Task.Run(GetStatsAsync);
        }

        private Server.Data cachedData = new(0, 0, 0, 0, 0);
        private readonly HardwareDescriptor hwDescriptor;

        private async void GetStatsAsync()
        {
            while (true)
            {
                cachedData = new
                (
                    GetCpuTemp(),
                    GetGpuTemp(),
                    GetCpuFan(),
                    GetGpuFan(),
                    GetFps()
                );
                await Task.Delay(10);
            }
        }

        private float GetFps()
        {
            return 0.0f;
        }

        private float GetGpuFan() => ReadHardwareSensor(hwDescriptor.GpuFanDescriptor);
        private float GetCpuFan() => ReadHardwareSensor(hwDescriptor.CpuFanDescriptor);
        private float GetGpuTemp() => ReadHardwareSensor(hwDescriptor.GpuTempDescriptor);
        private float GetCpuTemp() => ReadHardwareSensor(hwDescriptor.CpuTempDescriptor);
        

        private ISensor? SelectSensor(IEnumerable<ISensor> sensors, SensorDescrpitor sensorDescrpitor)
        {

            return sensors.FirstOrDefault(
                (sensor) =>
                sensor.Hardware.HardwareType.ToString() == sensorDescrpitor.HardwareType &&
                sensor.SensorType.ToString() == sensorDescrpitor.SensorType &&
                (
                sensorDescrpitor.SensorName == "" ||
                sensor.Name == sensorDescrpitor.SensorName
                )
            );
        }
        private ISensor? SelectSensor(SensorDescrpitor sensorDescrpitor)
        {
            ISensor? sensor;
            foreach (var hardware in computer.Hardware)
            {
                hardware.Update();

                sensor = SelectSensor(hardware.Sensors, sensorDescrpitor);
                if (sensor != null)
                    return sensor;

                foreach (var subhardware in hardware.SubHardware)
                {
                    subhardware.Update();

                    sensor = SelectSensor(subhardware.Sensors, sensorDescrpitor);
                    if (sensor != null)
                        return sensor;
                }
            }

            return null;
        }

        private readonly Dictionary<SensorDescrpitor,ISensor> sensorsDictionary = [];
        private float ReadHardwareSensor(SensorDescrpitor sensorDescrpitor,bool cached = true)
        {
            if (cached && sensorsDictionary.TryGetValue(sensorDescrpitor, out ISensor? sensor) && sensor != null)
            {
                sensor.Hardware.Update();
                return (float)(sensor.Value ?? 0.0f);
            }

            sensor = SelectSensor(sensorDescrpitor);
            if(sensor!=null)
                sensorsDictionary[sensorDescrpitor] = sensor;
            return (float)((sensor?.Value) ?? 0.0);
        }

        public Server.Data GetStats()
        {
            return cachedData;
        }

    }
}