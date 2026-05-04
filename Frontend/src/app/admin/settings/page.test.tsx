import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import AdminSettingsPage from "./page";

const mockFetch = jest.fn();

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

function mockSettingsResponse() {
  return {
    taxRatePercent: 10,
    adminFeeRatePercent: 15,
  };
}

async function setupSuccessInitialRender() {
  mockFetch.mockResolvedValueOnce({
    ok: true,
    status: 200,
    json: async () => mockSettingsResponse(),
  });

  render(<AdminSettingsPage />);

  await waitFor(() => {
    expect(
      screen.getByRole("heading", { name: "管理設定" })
    ).toBeInTheDocument();
  });
}

describe("AdminSettingsPage", () => {
  test("読み込み中メッセージを表示する", () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockSettingsResponse(),
    });

    render(<AdminSettingsPage />);

    expect(screen.getByText("管理設定を読み込み中...")).toBeInTheDocument();
  });

  test("初期データ取得後、管理設定フォームを表示する", async () => {
    await setupSuccessInitialRender();

    expect(
      screen.getByText(
        "税率・手数料など、予約料金計算に使用する設定値を管理します。"
      )
    ).toBeInTheDocument();

    expect(screen.getByDisplayValue("10")).toBeInTheDocument();
    expect(screen.getByDisplayValue("15")).toBeInTheDocument();

    expect(
      screen.getByText("入力範囲：0〜100。DBには 10% → 0.10 のように小数として保存します。")
    ).toBeInTheDocument();

    expect(screen.getByRole("link", { name: "戻る" })).toHaveAttribute(
      "href",
      "/admin"
    );
  });

  test("403の場合、管理者のみアクセス可能メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 403,
    });

    render(<AdminSettingsPage />);

    await waitFor(() => {
      expect(screen.getByText("管理者のみアクセスできます。")).toBeInTheDocument();
    });
  });

  test("GET失敗時、取得失敗メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<AdminSettingsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("管理設定の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<AdminSettingsPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("税率未入力の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    await setupSuccessInitialRender();

    const taxInput = screen.getByDisplayValue("10");

    await user.clear(taxInput);
    await user.click(screen.getByRole("button", { name: "保存" }));

    expect(screen.getByText("税率を入力してください。")).toBeInTheDocument();
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

test("税率が範囲外の場合、バリデーションメッセージを表示する", async () => {
  const user = userEvent.setup();

  await setupSuccessInitialRender();

  const taxInput = screen.getByDisplayValue("10");

  await user.clear(taxInput);
  await user.type(taxInput, "-1");

  await user.click(screen.getByRole("button", { name: "保存" }));

  await waitFor(() => {
    expect(
      screen.getByText("税率は0〜100の範囲で入力してください。")
    ).toBeInTheDocument();
  });

  expect(mockFetch).toHaveBeenCalledTimes(1);
});

  test("手数料未入力の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    await setupSuccessInitialRender();

    const feeInput = screen.getByDisplayValue("15");

    await user.clear(feeInput);
    await user.click(screen.getByRole("button", { name: "保存" }));

    expect(screen.getByText("手数料を入力してください。")).toBeInTheDocument();
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

test("手数料が範囲外の場合、バリデーションメッセージを表示する", async () => {
  const user = userEvent.setup();

  await setupSuccessInitialRender();

  const feeInput = screen.getByDisplayValue("15");

  await user.clear(feeInput);
  await user.type(feeInput, "-1");

  await user.click(screen.getByRole("button", { name: "保存" }));

  await waitFor(() => {
    expect(
      screen.getByText("手数料は0〜100の範囲で入力してください。")
    ).toBeInTheDocument();
  });

  expect(mockFetch).toHaveBeenCalledTimes(1);
});

  test("正常入力で保存するとPUTリクエストを送信する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockSettingsResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          taxRatePercent: 8,
          adminFeeRatePercent: 12,
        }),
      });

    render(<AdminSettingsPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "管理設定" })
      ).toBeInTheDocument();
    });

    const taxInput = screen.getByDisplayValue("10");
    const feeInput = screen.getByDisplayValue("15");

    await user.clear(taxInput);
    await user.type(taxInput, "8");

    await user.clear(feeInput);
    await user.type(feeInput, "12");

    await user.click(screen.getByRole("button", { name: "保存" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    expect(mockFetch).toHaveBeenLastCalledWith(
      "https://localhost:7226/api/admin/settings",
      expect.objectContaining({
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
        credentials: "include",
        body: JSON.stringify({
          taxRatePercent: 8,
          adminFeeRatePercent: 12,
        }),
      })
    );

    expect(screen.getByText("管理設定を保存しました。")).toBeInTheDocument();
    expect(screen.getByDisplayValue("8")).toBeInTheDocument();
    expect(screen.getByDisplayValue("12")).toBeInTheDocument();
  });

  test("PUTで403の場合、管理者のみアクセス可能メッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockSettingsResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 403,
      });

    render(<AdminSettingsPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "管理設定" })
      ).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "保存" }));

    await waitFor(() => {
      expect(screen.getByText("管理者のみアクセスできます。")).toBeInTheDocument();
    });
  });

  test("PUT失敗時、APIから返されたエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockSettingsResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({
          message: "税率の設定値が不正です。",
        }),
      });

    render(<AdminSettingsPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "管理設定" })
      ).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "保存" }));

    await waitFor(() => {
      expect(screen.getByText("税率の設定値が不正です。")).toBeInTheDocument();
    });
  });

  test("PUT通信エラー時、通信エラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockSettingsResponse(),
      })
      .mockRejectedValueOnce(new Error("Network Error"));

    render(<AdminSettingsPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "管理設定" })
      ).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "保存" }));

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });
});