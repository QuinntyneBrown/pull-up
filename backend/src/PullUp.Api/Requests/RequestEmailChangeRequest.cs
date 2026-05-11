namespace PullUp.Api.Requests;

public sealed record RequestEmailChangeRequest(string NewEmail, string CurrentPassword);
