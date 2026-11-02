using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace Studiobook_backend.Infrastructure.Reports
{
    /// <summary>
    /// QuestPDF のライセンス設定と日本語フォント（NotoSansJP）の登録。
    /// フォントファイルは API プロジェクトの Fonts フォルダから出力先へコピーされます。
    /// </summary>
    public static class QuestPdfSetup
    {
        private static readonly object SyncRoot = new();
        private static bool _configured;

        public static void Configure()
        {
            lock (SyncRoot)
            {
                if (_configured)
                {
                    return;
                }

                QuestPDF.Settings.License = LicenseType.Community;

                RegisterFont("NotoSansJP-Regular.ttf");
                RegisterFont("NotoSansJP-Bold.ttf");

                _configured = true;
            }
        }

        private static void RegisterFont(string fileName)
        {
            var fontPath = Path.Combine(
                AppContext.BaseDirectory,
                "Fonts",
                fileName
            );

            if (File.Exists(fontPath))
            {
                using var fontStream = File.OpenRead(fontPath);
                FontManager.RegisterFontWithCustomName("NotoSansJP", fontStream);
            }
        }
    }
}
