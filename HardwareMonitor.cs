

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualBasic;
using OpenHardwareMonitor.Hardware;

namespace TempAndFanServer
{
    public class HardwareMonitor
    {
        public record SensorDescrpitor(string HardwareType, string SensorType, string SensorName)
        {
            public static SensorDescrpitor FromSensor(ISensor sensor)
            {
                return new SensorDescrpitor(sensor.Hardware.HardwareType.ToString(),
                sensor.SensorType.ToString(),
                sensor.Name);
            }
        }
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

            [JsonIgnore]
            public string? FileName { get; set; }

            public static bool LoadFromFile(string fileName, out HardwareDescriptor hardwareDescriptor)
            {
                hardwareDescriptor = Default;
                if (!File.Exists(fileName))
                    return false;


                using var stream = new FileStream(fileName, FileMode.Open);
                HardwareDescriptor? result = JsonSerializer.Deserialize<HardwareDescriptor>(stream);
                if (result != null)
                {
                    hardwareDescriptor = result;
                    hardwareDescriptor.FileName = fileName;
                    return true;
                }
                return false;
            }

            internal bool SaveToFile(string? fileName = null)
            {
                try
                {
                    if (fileName != null)
                        FileName = fileName;
                    if (FileName == null)
                        return false;
                    string serialized = JsonSerializer.Serialize(this);
                    using var stream = File.Open(FileName, FileMode.OpenOrCreate, FileAccess.Write);
                    stream.SetLength(0);
                    stream.Write(Encoding.UTF8.GetBytes(serialized));
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }


        private readonly RTSS? rtss;
        private readonly Computer computer = new() { IsCpuEnabled = true, IsGpuEnabled = true, IsMotherboardEnabled = true };
        public HardwareMonitor(HardwareDescriptor hardwareDescriptor)
        {
            hwDescriptor = hardwareDescriptor;
            RTSS.Initialize(out rtss);
            computer.Open(false);
            _ = Task.Run(GetStatsAsync);
        }

        public event Action<string, SensorDescrpitor>? DescriptorChanged;

        private Server.Data cachedData = new(0, 0, 0, 0, 0);
        private HardwareDescriptor hwDescriptor;
        public SensorDescrpitor CpuTempDescriptor { get => hwDescriptor.CpuTempDescriptor; set { hwDescriptor = hwDescriptor with { CpuTempDescriptor = value }; DescriptorChanged?.Invoke("CPU temp", value); hwDescriptor.SaveToFile(); } }
        public SensorDescrpitor GpuTempDescriptor { get => hwDescriptor.GpuTempDescriptor; set { hwDescriptor = hwDescriptor with { GpuTempDescriptor = value }; DescriptorChanged?.Invoke("GPU temp", value); hwDescriptor.SaveToFile(); } }
        public SensorDescrpitor CpuFanDescriptor { get => hwDescriptor.CpuFanDescriptor; set { hwDescriptor = hwDescriptor with { CpuFanDescriptor = value }; DescriptorChanged?.Invoke("CPU fan", value); hwDescriptor.SaveToFile(); } }
        public SensorDescrpitor GpuFanDescriptor { get => hwDescriptor.GpuFanDescriptor; set { hwDescriptor = hwDescriptor with { GpuFanDescriptor = value }; DescriptorChanged?.Invoke("GPU fan", value); hwDescriptor.SaveToFile(); } }

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
            return rtss?.GetFPS() ?? 0.0f;
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

        public IList<IHardware> GetAllHardware()
        {
            foreach (var hardware in computer.Hardware)
            {
                hardware.Update();
                foreach (var subhardware in hardware.SubHardware)
                {
                    subhardware.Update();
                }

            }
            return computer.Hardware;
        }

        private readonly Dictionary<SensorDescrpitor, ISensor> sensorsDictionary = [];
        private float ReadHardwareSensor(SensorDescrpitor sensorDescrpitor, bool cached = true)
        {
            if (cached && sensorsDictionary.TryGetValue(sensorDescrpitor, out ISensor? sensor) && sensor != null)
            {
                sensor.Hardware.Update();
                return (float)(sensor.Value ?? 0.0f);
            }

            sensor = SelectSensor(sensorDescrpitor);
            if (sensor != null)
                sensorsDictionary[sensorDescrpitor] = sensor;
            return (float)((sensor?.Value) ?? 0.0);
        }

        public Server.Data GetStats()
        {
            return cachedData;
        }

    }
}