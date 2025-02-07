using System;
using System.Collections.Generic;
using System.Text;

namespace Enterprise.EmployeeManagement.core.Common.Responses
{
    public class ResponseMessage<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

        public static ResponseMessage<T> SuccessResult(T data, string message = "Operation successful") =>
            new ResponseMessage<T> { Success = true, Message = message, Data = data };

        public static ResponseMessage<T> FailureResult(string message) =>
            new ResponseMessage<T> { Success = false, Message = message };
    }
}
