import { render, screen } from "@testing-library/react";
import SalesTrendChart, { formatCompactYen, formatYen } from "./SalesTrendChart";

type SeriesProps = {
  name?: string;
  fill?: string;
  stroke?: string;
};

jest.mock("recharts", () => ({
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="responsive-container">{children}</div>
  ),
  ComposedChart: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="composed-chart">{children}</div>
  ),
  CartesianGrid: () => null,
  Legend: () => null,
  Tooltip: () => null,
  XAxis: () => null,
  YAxis: () => null,
  Bar: ({ name, fill }: SeriesProps) => (
    <div data-testid="bar" data-fill={fill}>
      {name}
    </div>
  ),
  Line: ({ name, stroke }: SeriesProps) => (
    <div data-testid="line" data-stroke={stroke}>
      {name}
    </div>
  ),
}));

describe("SalesTrendChart", () => {
  test("凡例名を表示し、棒と線に色を指定する", () => {
    render(
      <SalesTrendChart
        rows={[{ label: "2026-04", booked: 24000, paid: 25000 }]}
        bookedName="見込み売上"
        paidName="確定売上"
      />
    );

    const bar = screen.getByTestId("bar");
    const line = screen.getByTestId("line");

    expect(bar).toHaveTextContent("見込み売上");
    expect(line).toHaveTextContent("確定売上");

    // 未指定だと recharts の既定（黒）で描画されるため、色が渡っていることを確認する
    expect(bar.dataset.fill).toMatch(/^#[0-9a-f]{6}$/i);
    expect(bar.dataset.fill).not.toBe("#000000");
    expect(line.dataset.stroke).toMatch(/^#[0-9a-f]{6}$/i);
  });
});

describe("formatters", () => {
  test("金額を円表記にする", () => {
    expect(formatYen(319560)).toBe("￥319,560");
  });

  test("軸目盛を短い表記にする", () => {
    expect(formatCompactYen(0)).toBe("0");
    expect(formatCompactYen(24000)).toBe("2.4万");
    expect(formatCompactYen(1200000)).toBe("120万");
  });
});
