import type { ReactNode } from "react";
import "../styling/PageLayout.css";

interface PageLayoutProps {
  children: ReactNode;
  className?: string;
  showBackdrop?: boolean;
}

export default function PageLayout({
  children,
  className = "",
  showBackdrop = true,
}: PageLayoutProps) {
  return (
    <div className={`page-layout ${className}`}>
      {showBackdrop && (
        <>
          <div className="page-layout__glow page-layout__glow--left" />
          <div className="page-layout__glow page-layout__glow--right" />
        </>
      )}
      <div className="page-layout__content">{children}</div>
    </div>
  );
}
