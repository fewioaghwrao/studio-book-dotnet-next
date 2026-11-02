import DashboardShell from "@/components/dashboard/DashboardShell";

export default function HostLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return <DashboardShell area="host">{children}</DashboardShell>;
}
