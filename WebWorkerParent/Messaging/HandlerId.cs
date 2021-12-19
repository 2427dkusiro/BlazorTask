namespace BlazorTask.Messaging
{
    public enum HandlerId
    {
        Null = 0, // To detect error only.
        ThisContext = 1,
        WorkerContext = 2,
    }
}

/**
 * Worker Messaging Protocol
 * 
 * Message has the type such as "Init" , "SCall"...
 * Type provide the definition of the way to transfer data.
 * 
 * Message body is JS object.(or null)
 *  field "t" : message Type. 
 *  field "d" : transfer Data(option).
 *  field "i" : message Id(option).
 *  
 * For JS<=>C# interop, use 2 buffer.
 *  general buffer: fixed size buffer to put interop argumetnts.
 *  data buffer: flex size buffer to put data.
 * These buffer is instance-shared. And first 4 byte must be payload length not to read unexpected field.
 * 
 * Init : Worker => Parent
 *  Notify worker INITialization completed. This message has no body.
 * 
 * SCall : Parent => Worker 
 *  CALL method from Serialized arguments.
 *  Body:
 *   i:number method call ID.
 *   d:arrayBuffer[]
 *    [0]:arrayBuffer UTF-16 encoded method mame string. 
 *    [1]:arrayBuffer UTF-8 encoded json arguments.
 *       
 *  C#=>JS: Use general buffer 20 bytes.
 *   [0]:Int32 payload length(20).
 *   [1]:Int32 pointer to method name string.
 *   [2]:Int32 method name length in bytes.(2x larger than string length)
 *   [3]:Int32 pointer to json arguments.
 *   [4]:Int32 json arguments length in bytes.
 *   
 *  JS=>C#: Use general buffer 20 bytes and use data buffer.
 *   general buffer(same to C#=>JS):
 *    [0]:Int32 payload length(20).
 *    [1]:Int32 pointer to method name string in data buffer.
 *    [2]:Int32 method name length in bytes.(2x larger than string length)
 *    [3]:Int32 pointer to json arguments in data buffer.
 *    [4]:Int32 json arguments length in bytes. 
 *   data buffer:
 *    + method name.
 *    + json args.
 *    
 * Res : Worker => Parent
 *  Return method call RESult.
 *  Body:
 *   d:arraybuffer[]
 *    [0]:Int32 payload size.
 *    [1]:Int32 call ID.
 *    [2]:Int32 result type. 
 *     { 
 *       0 = execution succeeded but returned nothing. 
 *       1 = allocated.
 *       2 = execution succeeded and returned json value.
 *       3 = exception occured but no information.
 *       4 = exception occured and re-throw it as json.
 *     }
 *    [3]:Any returned value.(flex length)
 *    
 *   C#=>JS:
 *    when return void: use general buffer 12 bytes.
 *     [0]:Int32 payload length.
 *     [1]:Int32 call ID.
 *     [2]:Int32 result type.
 *
 *    when return something: use general buffer 20 bytes.
 *     [0]:Int32 payload length.
 *     [1]:Int32 call ID.
 *     [2]:Int32 result type.
 *     [3]:Int32 pointer to return value.
 *     [4]:Int32 return value length.
 */