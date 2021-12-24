namespace Tgs.ProfitRebate.ViewModel
{
    public class MessageResponse
    {
        public bool Valid { get; set; }

        public string Message { get; set; }

        public string[] Errors { get; set; }

        public MessageResponse()
        {
            Valid = false;
            Message = string.Empty;
            Errors = new string[] { };
        }
    }
}