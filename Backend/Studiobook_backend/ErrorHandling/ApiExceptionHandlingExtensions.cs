namespace Studiobook_backend.ErrorHandling;

public static class ApiExceptionHandlingExtensions
{
    /// <summary>共通の例外ハンドラーを登録する。</summary>
    public static IServiceCollection AddApiExceptionHandling(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    /// <summary>
    /// 例外ハンドラーをパイプラインの先頭に入れる。UseCors より前（外側）に置くこと。
    /// CORS ミドルウェアは応答の送信直前（OnStarting）にヘッダーを付けるため、
    /// 内側で例外が起きても、ハンドラーが作ったエラー応答に CORS ヘッダーが付く。
    /// </summary>
    public static IApplicationBuilder UseApiExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseExceptionHandler();
    }
}
