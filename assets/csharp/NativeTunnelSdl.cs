using System.Collections.Generic;

namespace Interpreter
{
    internal class NativeTunnelSdl
    {
        public static void Run()
        {
            new NativeTunnelSdl().Initialize();
        }

        public void Initialize()
        {
            Interpreter.Vm.CrayonWrapper.PST_RegisterExtensibleCallback("nativeTunnelSend", HandleSdlRequest);
            Interpreter.Vm.CrayonWrapper.PST_RegisterExtensibleCallback("nativeTunnelRecv", HandleSdlFlush);
        }

        private object HandleSdlRequest(object[] request)
        {
            NativeTunnelMessageWrapper message = new NativeTunnelMessageWrapper()
            {
                ID = this.nextMessageId++,
                Type = request[0].ToString(),
                InboundPayload = request[1].ToString()
            };
            this.messages.Add(message);
            AbstractSdlAction action = CrayonSdlBridge.CreateSdlAction(message, message.InboundPayload.Split(','));
            if (action == null)
            {
                message.NotRegistered = true;
            }
            else
            {
                action.MessageWrapper = message;
            }
            action.Run();
            return message.ID;
        }

        private int nextMessageId = 1;
        private List<NativeTunnelMessageWrapper> messages = new List<NativeTunnelMessageWrapper>();

        private object HandleSdlFlush(object[] dataOut)
        {
            for (int i = 0; i < this.messages.Count; i++)
            {
                NativeTunnelMessageWrapper message = this.messages[i];
                if (message.IsCompleted)
                {
                    this.messages.RemoveAt(i);
                    dataOut[0] = message.ID;
                    dataOut[1] = message.IsCompleted ? 1 : message.NotRegistered ? 2 : 0;
                    dataOut[2] = message.OutboundPayload;
                    dataOut[3] = false;
                    return true;
                }
            }
            return false;
        }
    }

    internal class NativeTunnelMessageWrapper
    {
        public int ID { get; set; }
        public bool IsCompleted { get; set; }
        public bool NotRegistered { get; set; }
        public string InboundPayload { get; set; }
        public string OutboundPayload { get; set; }
        public string Type { get; set; }
    }

    public abstract class AbstractSdlAction
    {
        internal NativeTunnelMessageWrapper MessageWrapper { get; set; }

        public void MarkAsCompleted()
        {
            this.MarkAsCompleted(null);
        }

        public void MarkAsCompleted(IList<string> args)
        {
            this.MessageWrapper.IsCompleted = true;
            this.MessageWrapper.OutboundPayload = args == null ? "" : string.Join(',', args);
        }

        public abstract void Run();
    }
}
