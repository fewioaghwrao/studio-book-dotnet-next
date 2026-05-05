import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import HostRoomClosuresPage from "./page";

const mockFetch = jest.fn();

jest.mock("next/navigation", () => ({
  useParams: () => ({
    id: "1",
  }),
}));

jest.mock("@fullcalendar/react", () => {
  return function MockFullCalendar(props: {
    initialView?: string;
    events?: Array<{
      id: string;
      title: string;
      start: string;
      end: string;
      allDay: boolean;
    }>;
    eventClick?: (arg: { event: { id: string } }) => void;
  }) {
    return (
      <div data-testid="full-calendar">
        <div>initialView: {props.initialView}</div>
        <div>events: {props.events?.length ?? 0}</div>
        {props.events?.map((event) => (
          <button
            key={event.id}
            type="button"
            onClick={() => props.eventClick?.({ event: { id: event.id } })}
          >
            {event.title}
          </button>
        ))}
      </div>
    );
  };
});

jest.mock("@fullcalendar/daygrid", () => ({}));
jest.mock("@fullcalendar/timegrid", () => ({}));
jest.mock("@fullcalendar/interaction", () => ({}));
jest.mock("@fullcalendar/core/locales/ja", () => ({}));

beforeEach(() => {
  jest.clearAllMocks();
  mockFetch.mockReset();

  global.fetch = mockFetch as jest.Mock;

  Object.defineProperty(window, "innerWidth", {
    writable: true,
    configurable: true,
    value: 1024,
  });
});

function mockClosuresResponse() {
  return [
    {
      id: 1,
      roomId: 1,
      startAt: "2026-05-10T00:00:00",
      endAt: "2026-05-11T00:00:00",
      reason: "メンテナンス",
    },
    {
      id: 2,
      roomId: 1,
      startAt: "2026-05-12T10:00:00",
      endAt: "2026-05-12T12:00:00",
      reason: null,
    },
  ];
}

function mockEventsResponse() {
  return [
    {
      id: 1,
      title: "メンテナンス",
      start: "2026-05-10",
      end: "2026-05-11",
      allDay: true,
    },
  ];
}

function setupSuccessResponses() {
  mockFetch
    .mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockClosuresResponse(),
    })
    .mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => mockEventsResponse(),
    });
}

async function waitForMaintenanceText() {
  await waitFor(() => {
    expect(screen.getAllByText("メンテナンス").length).toBeGreaterThan(0);
  });
}

describe("HostRoomClosuresPage", () => {
  test("初期表示と読み込み中メッセージを表示する", async () => {
    setupSuccessResponses();

    render(<HostRoomClosuresPage />);

    expect(
      screen.getByRole("heading", { name: "休館日設定" })
    ).toBeInTheDocument();

    expect(screen.getByText("CLOSURE SETTINGS")).toBeInTheDocument();

    expect(
      screen.getByText("スタジオの休館日時を登録・確認・削除できます。")
    ).toBeInTheDocument();

    expect(screen.getByText("休館日を読み込み中...")).toBeInTheDocument();
    expect(screen.getByText("カレンダー読込中...")).toBeInTheDocument();

    await waitForMaintenanceText();
  });

  test("API成功時、休館日一覧とカレンダーを表示する", async () => {
    setupSuccessResponses();

    render(<HostRoomClosuresPage />);

    await waitForMaintenanceText();

    expect(screen.getByText("休館日カレンダー")).toBeInTheDocument();
    expect(
      screen.getByText(
        "登録済みの休館日を月・週・日単位で確認できます。イベントをクリックすると削除できます。"
      )
    ).toBeInTheDocument();

    expect(screen.getByTestId("full-calendar")).toBeInTheDocument();
    expect(screen.getByText("initialView: dayGridMonth")).toBeInTheDocument();
    expect(screen.getByText("events: 1")).toBeInTheDocument();

    expect(screen.getByText("登録済みの休館日")).toBeInTheDocument();
    expect(screen.getByText("全 2 件")).toBeInTheDocument();

    expect(screen.getByText("CLOSURE #1")).toBeInTheDocument();
    expect(screen.getByText("CLOSURE #2")).toBeInTheDocument();

    expect(screen.getByText("休館")).toBeInTheDocument();
    expect(screen.getByText("終日")).toBeInTheDocument();

    expect(screen.getAllByRole("button", { name: "削除" }).length).toBe(2);
  });

  test("休館日が0件の場合、空メッセージを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => [],
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => [],
      });

    render(<HostRoomClosuresPage />);

    await waitFor(() => {
      expect(
        screen.getByText("登録済みの休館日はありません。")
      ).toBeInTheDocument();
    });

    expect(screen.getByText("全 0 件")).toBeInTheDocument();
  });

  test("closures取得で403の場合、権限エラーを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: false,
        status: 403,
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockEventsResponse(),
      });

    render(<HostRoomClosuresPage />);

    await waitFor(() => {
      expect(
        screen.getByText("このスタジオの休館日を操作する権限がありません。")
      ).toBeInTheDocument();
    });
  });

  test("closures取得失敗時、取得失敗メッセージを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: false,
        status: 500,
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockEventsResponse(),
      });

    render(<HostRoomClosuresPage />);

    await waitFor(() => {
      expect(
        screen.getByText("休館日の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("events取得で403の場合、カレンダー権限エラーを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockClosuresResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 403,
      });

    render(<HostRoomClosuresPage />);

    await waitFor(() => {
      expect(
        screen.getByText("このスタジオの休館日を表示する権限がありません。")
      ).toBeInTheDocument();
    });
  });

  test("events取得失敗時、カレンダー取得失敗メッセージを表示する", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockClosuresResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 500,
      });

    render(<HostRoomClosuresPage />);

    await waitFor(() => {
      expect(
        screen.getByText("カレンダー情報の取得に失敗しました。")
      ).toBeInTheDocument();
    });
  });

test("通信エラー時、通信エラーメッセージを表示する", async () => {
  mockFetch
    .mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => [],
    })
    .mockRejectedValueOnce(new Error("Network Error"));

  render(<HostRoomClosuresPage />);

  await waitFor(() => {
    expect(
      screen.getByText("カレンダー情報の取得中に通信エラーが発生しました。")
    ).toBeInTheDocument();
  });
});

  test("開始日時または終了日時が未入力の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    setupSuccessResponses();

    render(<HostRoomClosuresPage />);

    await waitForMaintenanceText();

    await user.click(screen.getByRole("button", { name: "追加" }));

    expect(
      screen.getByText("開始日時と終了日時を入力してください。")
    ).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledTimes(2);
  });

  test("終了日時が開始日時以前の場合、バリデーションメッセージを表示する", async () => {
    const user = userEvent.setup();

    setupSuccessResponses();

    render(<HostRoomClosuresPage />);

    await waitForMaintenanceText();

    const datetimeInputs = document.querySelectorAll(
      'input[type="datetime-local"]'
    );

    await user.type(datetimeInputs[0], "2026-05-10T12:00");
    await user.type(datetimeInputs[1], "2026-05-10T10:00");

    await user.click(screen.getByRole("button", { name: "追加" }));

    expect(
      screen.getByText("終了日時は開始日時より後にしてください。")
    ).toBeInTheDocument();

    expect(mockFetch).toHaveBeenCalledTimes(2);
  });

  test("終日設定で開始日時が未入力の場合、エラーメッセージを表示する", async () => {
    const user = userEvent.setup();

    setupSuccessResponses();

    render(<HostRoomClosuresPage />);

    await waitForMaintenanceText();

    await user.click(screen.getByRole("button", { name: "終日設定" }));

    expect(
      screen.getByText("終日設定を使う場合は、先に開始日時を入力してください。")
    ).toBeInTheDocument();
  });

  test("終日設定を押すと開始・終了日時を1日範囲に補正する", async () => {
    const user = userEvent.setup();

    setupSuccessResponses();

    render(<HostRoomClosuresPage />);

    await waitForMaintenanceText();

    const datetimeInputs = document.querySelectorAll(
      'input[type="datetime-local"]'
    ) as NodeListOf<HTMLInputElement>;

    await user.type(datetimeInputs[0], "2026-05-10T15:30");

    await user.click(screen.getByRole("button", { name: "終日設定" }));

    expect(datetimeInputs[0]).toHaveValue("2026-05-10T00:00");
    expect(datetimeInputs[1]).toHaveValue("2026-05-11T00:00");
  });

  test("正常入力で休館日を追加するとPOSTし、再取得する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => [],
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => [],
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockClosuresResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockEventsResponse(),
      });

    render(<HostRoomClosuresPage />);

    await waitFor(() => {
      expect(
        screen.getByText("登録済みの休館日はありません。")
      ).toBeInTheDocument();
    });

    const datetimeInputs = document.querySelectorAll(
      'input[type="datetime-local"]'
    );
    const reasonInput = screen.getByPlaceholderText("メンテナンスなど");

    await user.type(datetimeInputs[0], "2026-05-10T10:00");
    await user.type(datetimeInputs[1], "2026-05-10T12:00");
    await user.type(reasonInput, "臨時メンテナンス");

    await user.click(screen.getByRole("button", { name: "追加" }));

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(5);
    });

    expect(mockFetch.mock.calls[2][0]).toBe(
      "https://localhost:7226/api/host/rooms/1/closures"
    );

    expect(mockFetch.mock.calls[2][1]).toEqual(
      expect.objectContaining({
        method: "POST",        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          startAt: "2026-05-10T10:00",
          endAt: "2026-05-10T12:00",
          reason: "臨時メンテナンス",
        }),
      })
    );

    expect(screen.getByText("休館日を追加しました。")).toBeInTheDocument();

    expect(screen.getByPlaceholderText("メンテナンスなど")).toHaveValue("");
  });

  test("POSTで403の場合、追加権限エラーを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => [],
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => [],
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 403,
      });

    render(<HostRoomClosuresPage />);

    await waitFor(() => {
      expect(
        screen.getByText("登録済みの休館日はありません。")
      ).toBeInTheDocument();
    });

    const datetimeInputs = document.querySelectorAll(
      'input[type="datetime-local"]'
    );

    await user.type(datetimeInputs[0], "2026-05-10T10:00");
    await user.type(datetimeInputs[1], "2026-05-10T12:00");

    await user.click(screen.getByRole("button", { name: "追加" }));

    await waitFor(() => {
      expect(
        screen.getByText("このスタジオの休館日を追加する権限がありません。")
      ).toBeInTheDocument();
    });
  });

  test("POST失敗時、追加失敗メッセージを表示する", async () => {
    const user = userEvent.setup();

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => [],
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => [],
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 500,
      });

    render(<HostRoomClosuresPage />);

    await waitFor(() => {
      expect(
        screen.getByText("登録済みの休館日はありません。")
      ).toBeInTheDocument();
    });

    const datetimeInputs = document.querySelectorAll(
      'input[type="datetime-local"]'
    );

    await user.type(datetimeInputs[0], "2026-05-10T10:00");
    await user.type(datetimeInputs[1], "2026-05-10T12:00");

    await user.click(screen.getByRole("button", { name: "追加" }));

    await waitFor(() => {
      expect(
        screen.getByText("休館日の追加に失敗しました。")
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
        json: async () => mockClosuresResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockEventsResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => [],
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => [],
      });

    render(<HostRoomClosuresPage />);

    await waitForMaintenanceText();

    await user.click(screen.getAllByRole("button", { name: "削除" })[0]);

    expect(window.confirm).toHaveBeenCalledWith("この休館日を削除しますか？");

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(5);
    });

    expect(mockFetch.mock.calls[2][0]).toBe(
      "https://localhost:7226/api/host/rooms/1/closures/1"
    );

    expect(mockFetch.mock.calls[2][1]).toEqual(
      expect.objectContaining({
        method: "DELETE",      })
    );

    expect(screen.getByText("休館日を削除しました。")).toBeInTheDocument();
  });

  test("削除確認でキャンセルした場合、DELETEしない", async () => {
    const user = userEvent.setup();

    jest.spyOn(window, "confirm").mockReturnValueOnce(false);

    setupSuccessResponses();

    render(<HostRoomClosuresPage />);

    await waitForMaintenanceText();

    await user.click(screen.getAllByRole("button", { name: "削除" })[0]);

    expect(window.confirm).toHaveBeenCalledWith("この休館日を削除しますか？");
    expect(mockFetch).toHaveBeenCalledTimes(2);
  });

  test("DELETE失敗時、削除失敗メッセージを表示する", async () => {
    const user = userEvent.setup();

    jest.spyOn(window, "confirm").mockReturnValueOnce(true);

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockClosuresResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockEventsResponse(),
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 500,
      });

    render(<HostRoomClosuresPage />);

    await waitForMaintenanceText();

    await user.click(screen.getAllByRole("button", { name: "削除" })[0]);

    await waitFor(() => {
      expect(
        screen.getByText("休館日の削除に失敗しました。")
      ).toBeInTheDocument();
    });
  });

  test("カレンダーイベントクリックで削除処理を呼ぶ", async () => {
    const user = userEvent.setup();

    jest.spyOn(window, "confirm").mockReturnValueOnce(true);

    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => [],
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockEventsResponse(),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => [],
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => [],
      });

    render(<HostRoomClosuresPage />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "メンテナンス" }))
        .toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: "メンテナンス" }));

    expect(window.confirm).toHaveBeenCalledWith("この休館日を削除しますか？");

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(5);
    });

    expect(mockFetch.mock.calls[2][0]).toBe(
      "https://localhost:7226/api/host/rooms/1/closures/1"
    );
  });

  test("API呼び出しURLが正しい", async () => {
    setupSuccessResponses();

    render(<HostRoomClosuresPage />);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    expect(mockFetch.mock.calls[0][0]).toBe(
      "https://localhost:7226/api/host/rooms/1/closures"
    );

    expect(mockFetch.mock.calls[1][0]).toBe(
      "https://localhost:7226/api/host/rooms/1/closures/events"
    );
  });
});