import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import HostPriceRulesPage from "./page";

const mockFetch = jest.fn();

jest.mock("next/navigation", () => ({
  useParams: () => ({
    id: "1",
  }),
}));

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;
});

function mockPriceRulesResponse() {
  return {
    roomId: 1,
    roomName: "池袋ダンスルーム",
    rules: [
      {
        id: 1,
        ruleType: "multiplier",
        weekday: 5,
        startHour: "18:00:00",
        endHour: "22:00:00",
        multiplier: 1.5,
        flatFee: null,
        note: "金曜夜間料金",
      },
      {
        id: 2,
        ruleType: "flat_fee",
        weekday: null,
        startHour: null,
        endHour: null,
        multiplier: null,
        flatFee: 2000,
        note: "清掃費",
      },
    ],
  };
}

function setupSuccessInitialRender() {
  mockFetch.mockResolvedValueOnce({
    ok: true,
    status: 200,
    json: async () => mockPriceRulesResponse(),
  });

  render(<HostPriceRulesPage />);
}

async function waitForLoaded() {
  await waitFor(() => {
    expect(
      screen.getByRole("heading", {
        name: "池袋ダンスルーム の料金ルール設定",
      })
    ).toBeInTheDocument();
  });
}

describe("HostPriceRulesPage", () => {
  test("読み込み中メッセージを表示する", () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockPriceRulesResponse(),
    });

    render(<HostPriceRulesPage />);

    expect(screen.getByText("料金ルールを読み込み中...")).toBeInTheDocument();
  });

  test("API成功時、料金ルール一覧を表示する", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(screen.getByText("PRICE RULES")).toBeInTheDocument();

    expect(
      screen.getByText(
        "曜日や時間帯に応じた倍率、または曜日単位の固定費を設定できます。"
      )
    ).toBeInTheDocument();

    expect(screen.getByText("料金ルールを追加")).toBeInTheDocument();
    expect(screen.getByText("現在の設定")).toBeInTheDocument();
    expect(screen.getByText("全 2 件")).toBeInTheDocument();

    expect(screen.getAllByText("倍率").length).toBeGreaterThan(0);
    expect(screen.getAllByText("固定費").length).toBeGreaterThan(0);

    expect(screen.getAllByText("金曜").length).toBeGreaterThan(0);
    expect(screen.getAllByText("全て").length).toBeGreaterThan(0);

    expect(screen.getAllByText("18:00").length).toBeGreaterThan(0);
    expect(screen.getAllByText("22:00").length).toBeGreaterThan(0);
    expect(screen.getAllByText("1.5").length).toBeGreaterThan(0);
    expect(screen.getAllByText("2,000円").length).toBeGreaterThan(0);

    expect(screen.getAllByText("金曜夜間料金").length).toBeGreaterThan(0);
    expect(screen.getAllByText("清掃費").length).toBeGreaterThan(0);
  });

  test("ルールが0件の場合、空メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        roomId: 1,
        roomName: "池袋ダンスルーム",
        rules: [],
      }),
    });

    render(<HostPriceRulesPage />);

    await waitForLoaded();

    expect(screen.getByText("保存済みのルールはありません。")).toBeInTheDocument();
    expect(screen.getByText("全 0 件")).toBeInTheDocument();
  });

  test("403の場合、ホストユーザーのみアクセス可能メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 403,
    });

    render(<HostPriceRulesPage />);

    await waitFor(() => {
      expect(
        screen.getByText("ホストユーザーのみアクセスできます。")
      ).toBeInTheDocument();
    });
  });

  test("404の場合、スタジオが見つからないメッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 404,
    });

    render(<HostPriceRulesPage />);

    await waitFor(() => {
      expect(screen.getByText("スタジオが見つかりません。")).toBeInTheDocument();
    });
  });

  test("API失敗時、取得失敗メッセージを表示する", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
    });

    render(<HostPriceRulesPage />);

    await waitFor(() => {
      expect(
        screen.getByText("料金ルールの取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("通信エラー時、通信エラーメッセージを表示する", async () => {
    mockFetch.mockRejectedValueOnce(new Error("Network Error"));

    render(<HostPriceRulesPage />);

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("タイプ未選択の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    setupSuccessInitialRender();
    await waitForLoaded();

    await user.click(screen.getByRole("button", { name: "追加する" }));

    expect(screen.getByText("タイプを選択してください。")).toBeInTheDocument();
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("倍率ルールで開始時刻・終了時刻未選択の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    setupSuccessInitialRender();
    await waitForLoaded();

    await user.selectOptions(screen.getByDisplayValue("選択"), "multiplier");

    await user.click(screen.getByRole("button", { name: "追加する" }));

    expect(
      screen.getByText("倍率ルールでは開始時刻・終了時刻を選択してください。")
    ).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("倍率ルールで倍率未入力の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    setupSuccessInitialRender();
    await waitForLoaded();

    await user.selectOptions(screen.getByDisplayValue("選択"), "multiplier");

    const selects = screen.getAllByRole("combobox");

    await user.selectOptions(selects[2], "18:00");
    await user.selectOptions(selects[3], "22:00");

    await user.click(screen.getByRole("button", { name: "追加する" }));

    expect(screen.getByText("倍率を入力してください。")).toBeInTheDocument();
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("倍率が0以下の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    setupSuccessInitialRender();
    await waitForLoaded();

    await user.selectOptions(screen.getByDisplayValue("選択"), "multiplier");

    const selects = screen.getAllByRole("combobox");
    const multiplierInput = screen.getByPlaceholderText("例 1.50");

    await user.selectOptions(selects[2], "18:00");
    await user.selectOptions(selects[3], "22:00");
    await user.type(multiplierInput, "0");

    await user.click(screen.getByRole("button", { name: "追加する" }));

    expect(
      screen.getByText("倍率は0より大きい値を入力してください。")
    ).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("固定費ルールで固定費未入力の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    setupSuccessInitialRender();
    await waitForLoaded();

    await user.selectOptions(screen.getByDisplayValue("選択"), "flat_fee");

    await user.click(screen.getByRole("button", { name: "追加する" }));

    expect(screen.getByText("固定費を入力してください。")).toBeInTheDocument();
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("固定費がマイナスの場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    setupSuccessInitialRender();
    await waitForLoaded();

    await user.selectOptions(screen.getByDisplayValue("選択"), "flat_fee");

    const flatFeeInput = screen.getByPlaceholderText("例 2000");

    await user.type(flatFeeInput, "-1");

    await user.click(screen.getByRole("button", { name: "追加する" }));

    expect(
      screen.getByText("固定費は0以上で入力してください。")
    ).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("倍率ルールを正常入力で追加するとPOSTし、再取得する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          roomId: 1,
          roomName: "池袋ダンスルーム",
          rules: [],
        }),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockPriceRulesResponse(),
      });

    render(<HostPriceRulesPage />);

    await waitForLoaded();

    await user.selectOptions(screen.getByDisplayValue("選択"), "multiplier");

    const selects = screen.getAllByRole("combobox");
    const multiplierInput = screen.getByPlaceholderText("例 1.50");
    const noteInput = screen.getByPlaceholderText("例 土日料金、夜間料金など");

    await user.selectOptions(selects[1], "5");
    await user.selectOptions(selects[2], "18:00");
    await user.selectOptions(selects[3], "22:00");
    await user.type(multiplierInput, "1.5");
    await user.type(noteInput, "金曜夜間料金");

    await user.click(screen.getByRole("button", { name: "追加する" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(3);
    });

    expect(mockFetch.mock.calls[1][0]).toBe(
      "https://localhost:7226/api/host/rooms/1/price-rules"
    );

    expect(mockFetch.mock.calls[1][1]).toEqual(
      expect.objectContaining({
        method: "POST",        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          ruleType: "multiplier",
          weekday: 5,
          startHour: "18:00",
          endHour: "22:00",
          multiplier: 1.5,
          flatFee: null,
          note: "金曜夜間料金",
        }),
      })
    );

    expect(screen.getByText("料金ルールを追加しました。")).toBeInTheDocument();
  });

  test("固定費ルールを正常入力で追加するとPOSTし、再取得する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          roomId: 1,
          roomName: "池袋ダンスルーム",
          rules: [],
        }),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockPriceRulesResponse(),
      });

    render(<HostPriceRulesPage />);

    await waitForLoaded();

    await user.selectOptions(screen.getByDisplayValue("選択"), "flat_fee");

    const selects = screen.getAllByRole("combobox");
    const flatFeeInput = screen.getByPlaceholderText("例 2000");
    const noteInput = screen.getByPlaceholderText("例 土日料金、夜間料金など");

    await user.selectOptions(selects[1], "");
    await user.type(flatFeeInput, "2000");
    await user.type(noteInput, "清掃費");

    await user.click(screen.getByRole("button", { name: "追加する" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(3);
    });

    expect(mockFetch.mock.calls[1][1]).toEqual(
      expect.objectContaining({
        method: "POST",        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          ruleType: "flat_fee",
          weekday: null,
          startHour: null,
          endHour: null,
          multiplier: null,
          flatFee: 2000,
          note: "清掃費",
        }),
      })
    );

    expect(screen.getByText("料金ルールを追加しました。")).toBeInTheDocument();
  });

  test("POST失敗時、APIから返されたエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          roomId: 1,
          roomName: "池袋ダンスルーム",
          rules: [],
        }),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({
          message: "料金ルールの設定値が不正です。",
        }),
      });

    render(<HostPriceRulesPage />);

    await waitForLoaded();

    await user.selectOptions(screen.getByDisplayValue("選択"), "flat_fee");

    const flatFeeInput = screen.getByPlaceholderText("例 2000");

    await user.type(flatFeeInput, "2000");

    await user.click(screen.getByRole("button", { name: "追加する" }));

    await waitFor(() => {
      expect(
        screen.getByText("料金ルールの設定値が不正です。")
      ).toBeInTheDocument();
    });
  });

  test("POST通信エラー時、通信エラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          roomId: 1,
          roomName: "池袋ダンスルーム",
          rules: [],
        }),
      })
      .mockRejectedValueOnce(new Error("Network Error"));

    render(<HostPriceRulesPage />);

    await waitForLoaded();

    await user.selectOptions(screen.getByDisplayValue("選択"), "flat_fee");

    const flatFeeInput = screen.getByPlaceholderText("例 2000");

    await user.type(flatFeeInput, "2000");

    await user.click(screen.getByRole("button", { name: "追加する" }));

    await waitFor(() => {
      expect(
        screen.getByText("通信エラーが発生しました。時間をおいて再度お試しください。")
      ).toBeInTheDocument();
    });
  });

  test("削除確認でOKの場合、DELETEし、再取得する", async () => {
    const user = userEvent.setup();

    jest.spyOn(window, "confirm").mockReturnValueOnce(true);

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockPriceRulesResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          roomId: 1,
          roomName: "池袋ダンスルーム",
          rules: [],
        }),
      });

    render(<HostPriceRulesPage />);

    await waitForLoaded();

    await user.click(screen.getAllByRole("button", { name: "削除" })[0]);

    expect(window.confirm).toHaveBeenCalledWith(
      "この料金ルールを削除します。よろしいですか？"
    );

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(3);
    });

    expect(mockFetch.mock.calls[1][0]).toBe(
      "https://localhost:7226/api/host/rooms/1/price-rules/1"
    );

    expect(mockFetch.mock.calls[1][1]).toEqual(
      expect.objectContaining({
        method: "DELETE",      })
    );

    expect(screen.getByText("料金ルールを削除しました。")).toBeInTheDocument();
  });

  test("削除確認でキャンセルした場合、DELETEしない", async () => {
    const user = userEvent.setup();

    jest.spyOn(window, "confirm").mockReturnValueOnce(false);

    setupSuccessInitialRender();

    await waitForLoaded();

    await user.click(screen.getAllByRole("button", { name: "削除" })[0]);

    expect(window.confirm).toHaveBeenCalledWith(
      "この料金ルールを削除します。よろしいですか？"
    );

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  test("DELETE失敗時、APIから返されたエラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    jest.spyOn(window, "confirm").mockReturnValueOnce(true);

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockPriceRulesResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({
          message: "料金ルールは削除できません。",
        }),
      });

    render(<HostPriceRulesPage />);

    await waitForLoaded();

    await user.click(screen.getAllByRole("button", { name: "削除" })[0]);

    await waitFor(() => {
      expect(screen.getByText("料金ルールは削除できません。")).toBeInTheDocument();
    });
  });

  test("戻るリンクの遷移先が正しい", async () => {
    setupSuccessInitialRender();

    await waitForLoaded();

    expect(screen.getByRole("link", { name: "詳細へ戻る" })).toHaveAttribute(
      "href",
      "/host/rooms/1"
    );
  });

  test("API呼び出しURLが正しい", async () => {
    setupSuccessInitialRender();

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });

    expect(mockFetch).toHaveBeenCalledWith(
      "https://localhost:7226/api/host/rooms/1/price-rules",
      expect.objectContaining({
        method: "GET",        cache: "no-store",
      })
    );
  });
});