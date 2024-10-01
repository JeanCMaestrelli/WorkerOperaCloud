
namespace WorkerOperaCloud.Models
{
    public class ResponseDto
    {
        public bool IsOk { get; set; }
        public string? Error { get; set; }
    }

    public class ResponseDtoWithData<T>
    {
        public bool IsOk { get; set; }
        public string? Error { get; set; }
        public List<T>? Data { get; set; }
    }

}
