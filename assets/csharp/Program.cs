using Interpreter.Vm;
using Interpreter.Structs;

namespace Interpreter
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            string byteCode = ResourceReader.ReadTextResource("ByteCode.txt");
            string resourceManifest = ResourceReader.ReadTextResource("ResourceManifest.txt");
            string imageManifest = ResourceReader.ReadTextResource("ImageManifest.txt") ?? "";
            VmContext vm = CrayonWrapper.createVm(byteCode, resourceManifest, imageManifest);
            TranslationHelper.ProgramData = vm;
            CrayonWrapper.vmEnableLibStackTrace(vm);
            CrayonWrapper.vmEnvSetCommandLineArgs(vm, args);
            NativeTunnelSdl.Run();
            EventLoop.StartInterpreter();
        }
    }
}
