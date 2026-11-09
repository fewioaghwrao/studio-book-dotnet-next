using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Studiobook_backend.ErrorHandling;

/// <summary>
/// コントローラーで処理されなかった例外を、JSON のエラー応答（ProblemDetails）に変換する。
///
/// 例外ハンドラーがないと、Kestrel が本文なし・CORS ヘッダーなしの 500 を返す。
/// その応答はブラウザが CORS 違反として捨てるため、フロントでは fetch 自体が失敗し
/// 「通信エラー」と表示されていた。ここで応答を作ると CORS ミドルウェアのヘッダーが付き、
/// フロントは HTTP ステータスに応じたメッセージを表示できる。
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public const string ServerErrorMessage = "サーバーでエラーが発生しました。時間をおいて再度お試しください。";
    public const string UnauthorizedMessage = "ログイン情報を確認できませんでした。再度ログインしてください。";

    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // コントローラーがログインユーザーを特定できない場合（トークンの内容不正など）は 401
        var (statusCode, message) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, UnauthorizedMessage),
            _ => (StatusCodes.Status500InternalServerError, ServerErrorMessage)
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception. {Method} {Path} TraceId={TraceId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                httpContext.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning(
                "Request rejected. {Method} {Path} Status={StatusCode} Reason={Reason} TraceId={TraceId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                statusCode,
                exception.Message,
                httpContext.TraceIdentifier);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = message
        };

        // フロントは message を表示に使う。traceId はログ（Heroku logs）との照合用
        problem.Extensions["message"] = message;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        // 例外の内容は開発環境でのみ返す（本番では内部情報を出さない）
        if (_environment.IsDevelopment())
        {
            problem.Extensions["detail"] = exception.Message;
        }

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true;
    }
}
