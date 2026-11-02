import DashboardShell from "@/components/dashboard/DashboardShell";

export default function AdminLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return <DashboardShell area="admin">{children}</DashboardShell>;
}
