"use client";

import {
  Bar,
  CartesianGrid,
  ComposedChart,
  Legend,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";

export type SalesTrendRow = {
  label: string;
  booked: number;
  paid: number;
};

type SalesTrendChartProps = {
  rows: SalesTrendRow[];
  /** 棒グラフ（予約済み = 見込み）の凡例名 */
  bookedName: string;
  /** 折れ線（利用済み = 確定）の凡例名 */
  paidName: string;
};

// サイト全体のアクセント（sky）に合わせた配色
const BOOKED_FILL = "#bae6fd"; // sky-200
const BOOKED_STROKE = "#7dd3fc"; // sky-300
const PAID_STROKE = "#0369a1"; // sky-700
const GRID_STROKE = "#e7e5e4"; // stone-200
const AXIS_TICK = { fontSize: 12, fill: "#64748b" }; // slate-500

const yenFormatter = new Intl.NumberFormat("ja-JP", {
  style: "currency",
  currency: "JPY",
  maximumFractionDigits: 0,
});

// 軸目盛は「2.4万」のように短く表示する
const compactFormatter = new Intl.NumberFormat("ja-JP", {
  notation: "compact",
  maximumFractionDigits: 1,
});

export function formatYen(value: number) {
  return yenFormatter.format(value);
}

export function formatCompactYen(value: number) {
  return compactFormatter.format(value);
}

/**
 * 3か月分の売上推移（見込み = 棒、確定 = 折れ線）。
 * 管理者・ホストの統計画面で共通利用します。
 */
export default function SalesTrendChart({
  rows,
  bookedName,
  paidName,
}: SalesTrendChartProps) {
  return (
    <ResponsiveContainer width="100%" height="100%">
      <ComposedChart
        data={rows}
        margin={{ top: 16, right: 16, left: 4, bottom: 4 }}
      >
        <CartesianGrid
          stroke={GRID_STROKE}
          strokeDasharray="4 4"
          vertical={false}
        />

        <XAxis
          dataKey="label"
          interval={0}
          tick={AXIS_TICK}
          tickMargin={8}
          axisLine={{ stroke: GRID_STROKE }}
          tickLine={false}
        />

        <YAxis
          width={56}
          tick={AXIS_TICK}
          tickFormatter={(value) => formatCompactYen(Number(value))}
          axisLine={false}
          tickLine={false}
        />

        <Tooltip
          formatter={(value) => formatYen(Number(value))}
          cursor={{ fill: "#f5f5f4" }}
          contentStyle={{
            borderRadius: 12,
            borderColor: GRID_STROKE,
            fontSize: 12,
          }}
        />

        <Legend
          iconType="circle"
          iconSize={10}
          wrapperStyle={{ fontSize: 12, paddingTop: 8 }}
          // 既定では凡例の文字が系列色になり、淡い棒の色だと読みにくいため本文色で表示する
          formatter={(value) => <span style={{ color: "#475569" }}>{value}</span>}
        />

        <Bar
          dataKey="booked"
          name={bookedName}
          fill={BOOKED_FILL}
          stroke={BOOKED_STROKE}
          radius={[8, 8, 0, 0]}
          maxBarSize={64}
        />

        <Line
          type="monotone"
          dataKey="paid"
          name={paidName}
          stroke={PAID_STROKE}
          strokeWidth={2.5}
          dot={{ r: 4, fill: "#ffffff", stroke: PAID_STROKE, strokeWidth: 2 }}
          activeDot={{ r: 6 }}
        />
      </ComposedChart>
    </ResponsiveContainer>
  );
}
