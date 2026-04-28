using System.ComponentModel.DataAnnotations;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.CorpHome.Models.ErrorViewModels;

public class ErrorViewModel: UiStackLayoutViewModel
{
    public ErrorViewModel()
    {
        PageTitle = "Error";
    }

    public ErrorViewModel(int code)
    {
        ErrorCode = code;
        PageTitle = code switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Access Denied",
            404 => "Not Found",
            500 => "Internal Server Error",
            _ => $"Error {code}"
        };
    }

    [Display(Name = "Error code")]
    public int ErrorCode { get; set; } = 500;

    [Display(Name = "Request ID")]
    public required string RequestId { get; set; }

    [Display(Name = "Show request ID")]
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    [Display(Name = "Return URL")]
    public string? ReturnUrl { get; set; }
}
