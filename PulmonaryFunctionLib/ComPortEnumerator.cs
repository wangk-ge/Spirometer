using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PulmonaryFunctionLib
{
    public abstract class PortInfoBase
    {
        public string PortName { get; set; }

        public string Description { get; set; }

        public string DeviceId { get; set; }
    }

    public class ComPortInfo : PortInfoBase
    {
        public string Manufacturer { get; set; }

        public override string ToString()
        {
            return string.Format("PortName='{0}', Description='{1}', DeviceId='{2}', Manufacturer='{3}'",
                                 PortName, Description, DeviceId, Manufacturer);
        }
    }

    internal class ComPortInfoBuilder
    {
        private readonly ManagementBaseObject _mbo;
        private readonly ComPortInfo _comPortInfo = new ComPortInfo();

        public ComPortInfoBuilder(ManagementBaseObject mbo)
        {
            //mbo.AssertNotNull("mbo");
            _mbo = mbo;
        }

        public ComPortInfo Build()
        {
            BuildPortName();
            BuildDescription();
            BuildDeviceId();
            BuildManufacturer();

            return _comPortInfo;
        }

        private void BuildPortName()
        {
            const string pattern = @"COM\d+";
            _comPortInfo.PortName = Regex.Match((string)_mbo["Caption"], pattern, RegexOptions.RightToLeft).Value;
        }

        private void BuildDescription()
        {
            _comPortInfo.Description = (string)_mbo["Description"];
        }

        private void BuildDeviceId()
        {
            _comPortInfo.DeviceId = (string)_mbo["DeviceID"];
        }

        private void BuildManufacturer()
        {
            _comPortInfo.Manufacturer = (string)_mbo["Manufacturer"];
        }
    }
    public interface IComPortEnumerator
    {
        ComPortInfo[] Enumerate();
    }

    public class ComPortEnumerator : IComPortEnumerator
    {
        //private readonly ILog _logger = LogManager.GetLogger(typeof(ComPortEnumerator));

        public ComPortInfo[] Enumerate()
        {
            var comPortInfos = new List<ComPortInfo>();

            try
            {
                //Because Win32_SerialPort can't find the virtual COM ports, use Win32_PnPEntity here
                using (var searcher = new ManagementObjectSearcher(@"root\CIMV2", "SELECT * FROM Win32_PnPEntity WHERE Status='OK'"))
                {
                    foreach (var mbo in searcher.Get())
                    {
                        var result = Parse(mbo);

                        if (result.Item1)
                            comPortInfos.Add(result.Item2);
                    }
                }
            }
            catch (Exception exception)
            {
                //_logger.Error(exception);
                Console.WriteLine(exception);
            }

            return comPortInfos.ToArray();
        }

        private Tuple<bool, ComPortInfo> Parse(ManagementBaseObject mbo)
        {
            try
            {
                if (BelongsToComPort(mbo))
                {
                    var comPortInfo = BuildComPortInfo(mbo);
                    return Tuple.Create(true, comPortInfo);
                }
            }
            catch (Exception exception)
            {
                //_logger.Error(exception);
                Console.WriteLine(exception);
            }

            return Tuple.Create(false, (ComPortInfo)null);
        }

        private static bool BelongsToComPort(ManagementBaseObject mbo)
        {
            var value = mbo["Caption"] as string;

            if (value == null)
                return false;

            const string pattern = @"(\(COM\d+\))$";
            return Regex.IsMatch(value, pattern);
        }

        private static ComPortInfo BuildComPortInfo(ManagementBaseObject mbo)
        {
            var builder = new ComPortInfoBuilder(mbo);
            return builder.Build();
        }
    }
}
