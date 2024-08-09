namespace Microsoft.AspNetCore.MyHttpLogging;

//Create a class to hold all the above parameters
public class RequestLogEntry
{
    public int RequestNumber { get; set; }
    public string? Protocol { get; set; }
    public string? Method { get; set; }
    public DateTime EntryTime { get; set; }
    public string? Path { get; set; }
    public string? PathBase { get; set; }
    public List<string> RequestHeaders { get; set; } = new();
    public List<string> ResponseHeaders { get; set; } = new();
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public string? StatusCode { get; set; }
    public string? Duration { get; set; }
    public string? ResponseContentType { get; set; }
}


//namespace CloudDebugger.Features.RequestLogger
//{
//    public class RequestLogEntry
//    {
//        public int RequestNumber { get; set; }
//        public DateTime EntryTime { get; set; }
//        public string Protocol { get; set; }
//        public string Method { get; set; }
//        public string Path { get; set; }
//        public List<string> RequestHeaders { get; set; }
//        public List<string> ResponseHeaders { get; set; }
//        public string? RequestBody { get; set; }
//        //public string? ResponseBody { get; set; }
//    }
//}
