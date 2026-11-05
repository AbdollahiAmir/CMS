namespace BackEnd.DTOs.Users
{
    public class UserResult
    {
        public bool Succeeded { get; set; }
        public bool NotFound { get; set; }
        public bool Forbidden { get; set; }
        public IEnumerable<string> Errors { get; set; } = [];

        public static UserResult Ok()
        {
            return new UserResult { Succeeded = true };
        }

        public static UserResult Fail(params string[] errors)
        {
            return new UserResult { Errors = errors };
        }

        public static UserResult Fail(IEnumerable<string> errors)
        {
            return new UserResult { Errors = errors };
        }

        public static UserResult NotFoundResult()
        {
            return new UserResult { NotFound = true, Errors = ["کاربر یافت نشد"] };
        }

        public static UserResult ForbiddenResult(string message)
        {
            return new UserResult { Forbidden = true, Errors = [message] };
        }
    }
}