using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Fiddler;
using Microsoft.Win32;

[DllImport("User32.dll")]
static extern IntPtr GetClipboardData(uint uFormat);

[DllImport("User32.dll")]
static extern bool OpenClipboard(IntPtr hWndNewOwner);

[DllImport("User32.dll")]
static extern bool CloseClipboard();

[DllImport("User32.dll")]
static extern bool EmptyClipboard();

[DllImport("User32.dll")]
static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

const uint CF_UNICODETEXT = 13;

Boolean flag = true;
//创建证书并信任
CertMaker.createRootCert();
if (!CertMaker.rootCertExists())
{
    throw new Exception("创建失败");
}

X509Store x509Store = new X509Store(StoreName.Root);
x509Store.Open(OpenFlags.ReadWrite);
X509Certificate2 cert = CertMaker.GetRootCertificate();
x509Store.Add(cert);
x509Store.Close();
//设置代理
//获取原来的代理
string host = (Registry.GetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings",
    "ProxyServer", "") as string);
int enable =
    (int)Registry.GetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings",
        "ProxyEnable", 0);
//设置代理
Registry.SetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "ProxyServer",
    "127.0.0.1:8080");
Registry.SetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "ProxyEnable",
    "1");
//创建设置
FiddlerCoreStartupSettingsBuilder fiddlerCoreStartupSettingsBuilder = new FiddlerCoreStartupSettingsBuilder();
fiddlerCoreStartupSettingsBuilder.DecryptSSL();
fiddlerCoreStartupSettingsBuilder.ListenOnPort(8080);
//配置拦截器
FiddlerApplication.BeforeRequest += session =>
{
    if (session.url.Contains("xuegong"))
    {
        string token = session.oRequest.headers["Authorization"];
        if (token != "" && flag)
        {
            flag = false;
            Console.WriteLine("获取成功");
            Console.WriteLine(token);
            try
            {
                // 将文本写入剪贴板
                if (OpenClipboard(IntPtr.Zero))
                {
                    EmptyClipboard();
                    IntPtr handle = Marshal.StringToHGlobalUni(token);
                    SetClipboardData(CF_UNICODETEXT, handle);
                    CloseClipboard();
                    Console.WriteLine("已复制");
                }
                else
                {
                    Console.WriteLine("请手动复制");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine("按任意键结束");
        }
        
    }
};
//启动
FiddlerApplication.Startup(fiddlerCoreStartupSettingsBuilder.Build());
//关闭程序
Console.WriteLine("按任意键结束");
Console.ReadKey();
//恢复代理
Registry.SetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "ProxyServer",
    host);
Registry.SetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "ProxyEnable",
    enable);
Fiddler.FiddlerApplication.Shutdown();