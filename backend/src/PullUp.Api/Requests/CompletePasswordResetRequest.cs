namespace PullUp.Api.Requests;

public sealed record CompletePasswordResetRequest(string Token, string NewPassword);
