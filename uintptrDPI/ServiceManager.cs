using System;
using System.Management;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace uintptrDPI
{
    public class ServiceManager
    {
        private readonly string _serviceName;

        public ServiceManager(string serviceName)
        {
            _serviceName = serviceName;
        }

        public async Task<bool> InstallService(string cmdFilePath)
        {
            try
            {
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = $"/c \"{cmdFilePath}\" Y";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.Verb = "runas"; // Run as administrator

                    process.Start();
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"Service installation failed. Exit code: {process.ExitCode}");
                    }

                    return await SetServiceStartupType(ServiceStartMode.Automatic);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(ex, "An error occurred during service installation.");
                return false;
            }
        }

        public async Task<bool> SetServiceStartupType(ServiceStartMode startMode)
        {
            try
            {
                using (var service = new ManagementObject($"Win32_Service.Name='{_serviceName}'"))
                {
                    service.Get();
                    service["StartMode"] = startMode.ToString();
                    service.Put();
                    return true;
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(ex, "An error occurred while setting the service startup type.");
                return false;
            }
        }

        public async Task<ServiceStatus> GetServiceStatus()
        {
            try
            {
                using (var sc = new ServiceController(_serviceName))
                {
                    return new ServiceStatus
                    {
                        Name = sc.ServiceName,
                        Status = sc.Status,
                        StartType = await GetServiceStartType()
                    };
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(ex, "An error occurred while checking the service status.");
                return null;
            }
        }

        private async Task<ServiceStartMode> GetServiceStartType()
        {
            try
            {
                using (var service = new ManagementObject($"Win32_Service.Name='{_serviceName}'"))
                {
                    service.Get();
                    return (ServiceStartMode)Enum.Parse(typeof(ServiceStartMode), service["StartMode"].ToString());
                }
            }
            catch
            {
                return ServiceStartMode.Manual;
            }
        }
    }

    public class ServiceStatus
    {
        public string Name { get; set; }
        public ServiceControllerStatus Status { get; set; }
        public ServiceStartMode StartType { get; set; }
    }
}